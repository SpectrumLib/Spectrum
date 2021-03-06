﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Prism.Content;

namespace Prism.Build
{
	// Holds the information for a single content item being processed by the build pipeline. Comparing the values
	//   from the current event, and the old cached event is how the pipeline decides if a content item needs rebuilding
	internal class BuildEvent
	{
		public static readonly DateTime ERROR_TIME = new DateTime(0);
		private static readonly byte[] BUILD_CACHE_HEADER = Encoding.ASCII.GetBytes("PBC");

		#region Fields
		// If not cached, the item in the content project
		public readonly ContentItem Item = null;
		public ItemPaths Paths => Item?.Paths ?? default;
		public readonly uint Index = UInt32.MaxValue; // The index of the current item

		// Path to the build cache file that this event represents/would represent
		private readonly string _cachePath = null;
		public string CachePath => Item?.Paths.CachePath ?? _cachePath;

		// Path to source file
		private readonly string _cachedSource = null;
		public string SourcePath => Item?.Paths.SourcePath ?? _cachedSource;

		// Path to output file
		private readonly string _cachedOutput = null;
		public string OutputPath => Item?.Paths.OutputPath ?? _cachedOutput;

		// If this item was built with compression (only valid for cached builds)
		public readonly bool Compress;

		// Importer name
		private readonly string _cachedImporter = null;
		public string ImporterName => Item?.ImporterName ?? _cachedImporter;

		// Processor name
		private readonly string _cachedProcessor = null;
		public string ProcessorName => Item?.ProcessorName ?? _cachedProcessor;

		// Processor parameters
		private readonly List<(string, string)> _cachedArgs;
		public List<(string Key, string Value)> ProcessorArgs => Item?.ProcessorArgs ?? _cachedArgs;

		// Modify times for the input and output files (ERROR_TIME = associated file does not exist)
		public readonly DateTime InputTime = ERROR_TIME;
		public readonly DateTime OutputTime = ERROR_TIME;

		// The size of the output file (real size, potentially compressed)
		public readonly uint RealSize = 0;
		// The uncompressed size of the output file (how much data to expect after decompression)
		public readonly uint UCSize = 0;

		// External dependencies, and when their edit times at the last build (valid for content items AFTER importer only)
		public List<(string Name, DateTime EditTime)> ExternalDependencies = null;
		// If the external dependencies have changed (valid for cached builds only)
		public readonly bool DependenciesDirty = false;
		#endregion // Fields

		// A build event for an item taken from a content project
		private BuildEvent(ContentItem item, uint idx, DateTime iTime, DateTime oTime, uint outSize)
		{
			Item = item;
			Index = idx;
			InputTime = iTime;
			OutputTime = oTime;
			RealSize = outSize;
			ExternalDependencies = new List<(string Name, DateTime EditTime)>();
		}

		// A build event for an item loaded from a cache file
		private BuildEvent(string c, string s, string o, bool compress, uint ncsize, string i, string p, string args, bool depUpdate)
		{
			_cachePath = c;
			_cachedSource = s;
			_cachedOutput = o;
			Compress = compress;
			UCSize = ncsize;
			_cachedImporter = i;
			_cachedProcessor = p;
			_cachedArgs = ContentItem.ParseArgs(args);
			DependenciesDirty = depUpdate;
		}

		// Compares this event with the potential cached event to see if a rebuild is needed
		// Can only be called on events from items, never on cached events
		public bool NeedsBuild(BuildEvent cached, ProcessorInstance processor, bool compress)
		{
			// There is no exising build for this file
			if (cached == null || OutputTime == ERROR_TIME)
				return true;

			// The source file has been modified since the last build
			if (InputTime >= OutputTime)
				return true;

			// The importer and/or processor have changed since the last build
			if (ImporterName != cached.ImporterName || ProcessorName != cached.ProcessorName)
				return true;

			// If the last build was with a different compression
			if (compress != cached.Compress)
				return true;

			// The dependencies have changed
			if (cached.DependenciesDirty)
				return true;

			// The parameters have changed since the last build
			if (!ParametersEqual(ProcessorArgs, cached.ProcessorArgs))
				return true;

			// We dont have to rebuild
			return false;
		}

		// Checks if the parameter set keys are equal, and their values are equal
		//   TODO: In the future, we will want to also compare against the default values, as if a parameter was added
		//   or removed, but its value is/was the default value, then they are the same and we dont have to rebuild
		private static bool ParametersEqual(List<(string, string)> p1, List<(string, string)> p2)
		{
			// Both are empty
			if (p1.Count == 0 && p2.Count == 0)
				return true;

			// Different number of parameters (wont work when we start to compare against default)
			if (p1.Count != p2.Count)
				return false;

			// We know they are the same length, so if we get through this loop without issues then they are the same
			foreach (var pair in p1)
			{
				int idx = p2.FindIndex(arg => arg.Item1 == pair.Item1);
				if (idx == -1)
					return false;
				if (p2[idx].Item2 != pair.Item2)
					return false;
			}

			// All are the same
			return true;
		}

		// Saves the event info into a cache file
		// This function will only ever be called on events that are loaded from items and have been rebuilt
		public void SaveCache(BuildEngine engine, uint ucsize, bool compress)
		{
			try
			{
				using (var writer = new BinaryWriter(File.Open(CachePath, FileMode.Create, FileAccess.Write, FileShare.None)))
				{
					writer.Write(BUILD_CACHE_HEADER);
					writer.Write(compress);
					writer.Write(ucsize);
					writer.Write(ImporterName);
					writer.Write(ProcessorName);
					var argStr = String.Join(";", ProcessorArgs.Select(arg => $"{arg.Key}={arg.Value}"));
					writer.Write(argStr);
					writer.Write((uint)ExternalDependencies.Count);
					foreach (var ed in ExternalDependencies)
					{
						writer.Write(ed.Name);
						writer.Write(ed.EditTime.Ticks);
					}
				}
			}
			catch (Exception e)
			{
				engine.Logger.ItemWarn(this, $"Could not create build cache file, reason: {e.Message}");
			}
		}

		#region Creation
		public static BuildEvent FromItem(BuildEngine engine, ContentItem item, uint idx)
		{
			var iInfo = new FileInfo(item.Paths.SourcePath);
			var oInfo = new FileInfo(item.Paths.OutputPath);
			return new BuildEvent(
				item, 
				idx, 
				iInfo.Exists ? iInfo.LastWriteTimeUtc : ERROR_TIME, 
				oInfo.Exists ? oInfo.LastWriteTimeUtc : ERROR_TIME,
				oInfo.Exists ? (uint)oInfo.Length : 0u
			);
		}

		public static BuildEvent FromCacheFile(BuildEngine engine, ContentItem item)
		{
			if (!File.Exists(item.Paths.CachePath))
				return null;

			try
			{
				using (var reader = new BinaryReader(File.Open(item.Paths.CachePath, FileMode.Open, FileAccess.Read, FileShare.None)))
				{
					var header = reader.ReadBytes(3);
					if (header[0] != BUILD_CACHE_HEADER[0] || header[1] != BUILD_CACHE_HEADER[1] || header[2] != BUILD_CACHE_HEADER[2])
						return null;

					return new BuildEvent(
						item.Paths.CachePath,
						item.Paths.SourcePath,
						item.Paths.OutputPath,
						reader.ReadBoolean(),
						reader.ReadUInt32(),
						reader.ReadString(),
						reader.ReadString(),
						reader.ReadString(),
						DepsChanged(reader)
					);
				}
			}
			catch { return null; }
		}

		private static bool DepsChanged(BinaryReader reader)
		{
			uint count = reader.ReadUInt32();
			if (count == 0) return false;

			for (uint i = 0; i < count; ++i)
			{
				var name = reader.ReadString();
				var last = new DateTime(reader.ReadInt64());
				if (!File.Exists(name))
					return true; // An old dependency no longer exists, rebuild
				var curr = File.GetLastWriteTimeUtc(name);
				if (curr > last)
					return true; // The dependency has been modified
			}

			return false; // No changes to the dependencies
		}
		#endregion // Creation
	}
}
