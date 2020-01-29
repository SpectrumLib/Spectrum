/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Prism.Pipeline
{
	// Contains information about a content project item to be built
	internal class BuildOrder
	{
		private static readonly byte[] C_HEADER = Encoding.ASCII.GetBytes("PBC");
		private const byte C_VERSION = 1;

		#region Fields
		// The item to build for the order
		public readonly ContentItem Item;
		// The item index
		public readonly uint Index;
		#endregion // Fields

		public BuildOrder(ContentItem item, uint idx)
		{
			Item = item;
			Index = idx;
		}

		// Checks against the build output and cache to check if the order needs a rebuild
		public bool NeedsRebuild(out bool compress)
		{
			compress = false;

			if (!Item.CacheFile.Exists || !Item.OutputFile.Exists)
				return true;

			try
			{
				using BinaryReader reader = new BinaryReader(
					Item.CacheFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));

				// Validate header
				var header = reader.ReadBytes(4);
				if (header[0] != C_HEADER[0] || header[1] != C_HEADER[1] || header[2] != C_HEADER[2] || header[3] != C_VERSION)
					return true;

				// Check build time and type
				var buildTime = DateTime.FromBinary(reader.ReadInt64());
				if (Item.InputFile.LastWriteTimeUtc > buildTime)
					return true;
				var typeName = reader.ReadString();
				if (Item.Type != typeName)
					return true;

				// Get last build settings
				compress = reader.ReadBoolean();

				// Check parameters
				var pcount = reader.ReadUInt32();
				if (Item.Params.Count != pcount)
					return true;
				Dictionary<string, string> pars = new Dictionary<string, string>((int)pcount);
				for (uint i = 0; i < pcount; ++i)
					pars.Add(reader.ReadString(), reader.ReadString());
				if (!ParametersEqual(Item.Params, pars))
					return true;

				return false;
			}
			catch
			{
				return true;
			}
		}

		// Writes the results of a item build task to a cache file
		public bool WriteCacheFile(ItemResult res)
		{
			try
			{
				Item.OutputFile.Refresh();

				using BinaryWriter writer =
					new BinaryWriter(Item.CacheFile.Open(FileMode.Create, FileAccess.Write, FileShare.None));

				writer.Write(C_HEADER);
				writer.Write(C_VERSION);
				writer.Write(Item.OutputFile.LastWriteTimeUtc.ToBinary());
				writer.Write(Item.Type);
				writer.Write(res.Compress);
				writer.Write((uint)Item.Params.Count);
				foreach (var pair in Item.Params)
				{
					writer.Write(pair.Key);
					writer.Write(pair.Value);
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool ParametersEqual(Dictionary<string, string> o, Dictionary<string, string> n)
		{
			foreach (var pair in o)
			{
				if (!n.TryGetValue(pair.Key, out var nv))
					return false;
				if (pair.Value != nv)
					return false;
			}
			return true;
		}
	}
}
