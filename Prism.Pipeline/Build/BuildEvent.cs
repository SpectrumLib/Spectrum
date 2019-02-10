using System;
using System.IO;
using Prism.Content;

namespace Prism.Build
{
	// Holds the information for a single content item being processed by the build pipeline. Comparing the values
	//   from the current event, and the old cached event is how the pipeline decides if a content item needs rebuilding
	internal class BuildEvent
	{
		public static readonly DateTime ERROR_TIME = new DateTime(0);

		#region Fields
		// If not cached, the item in the content project
		public readonly ContentItem Item;
		public ItemPaths Paths => Item?.Paths ?? default;

		// Path to the build cache file that this event represents/would represent
		private readonly string _cachePath = null;
		public string CachePath => Item?.Paths.CachePath ?? _cachePath;

		// Path to source file
		private readonly string _cachedSource = null;
		public string SourcePath => Item?.Paths.SourcePath ?? _cachedSource;

		// Path to output file
		private readonly string _cachedOutput = null;
		public string OutputPath => Item?.Paths.OutputPath ?? _cachedOutput;

		// Importer name
		private readonly string _cachedImporter = null;
		public string ImporterName => Item?.ImporterName ?? _cachedImporter;

		// Processor name
		private readonly string _cachedProcessor = null;
		public string ProcessorName => Item?.ProcessorName ?? _cachedProcessor;

		// Modify times for the input and output files (ERROR_TIME = associated file does not exist)
		public readonly DateTime InputTime;
		public readonly DateTime OutputTime;
		#endregion // Fields

		private BuildEvent(ContentItem item, DateTime iTime, DateTime oTime)
		{
			Item = item;
			InputTime = iTime;
			OutputTime = oTime;
		}

		// Compares this event with the potential cached event to see if a rebuild is needed
		public bool NeedsBuild(BuildEvent cached)
		{
			return true;
		}

		#region Creation
		public static BuildEvent FromItem(ContentItem item)
		{
			var iInfo = new FileInfo(item.Paths.SourcePath);
			var oInfo = new FileInfo(item.Paths.OutputPath);
			return new BuildEvent(item, iInfo.Exists ? iInfo.LastWriteTimeUtc : ERROR_TIME, oInfo.Exists ? oInfo.LastWriteTimeUtc : ERROR_TIME);
		}

		public static BuildEvent FromCacheFile(BuildEngine engine, ContentItem item)
		{
			return null;
		}
		#endregion // Creation

		// Prevents writing 'evt.Item' a million times in BuildTask
		public static implicit operator ContentItem (BuildEvent evt) => evt.Item;
	}
}
