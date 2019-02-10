using System;
using System.IO;
using Prism.Content;

namespace Prism.Build
{
	// Holds the information for a single content item being processed by the build pipeline. Comparing the values
	//   from the current event, and the old cached event is how the pipeline decides if a content item needs rebuilding
	internal class BuildEvent
	{
		#region Fields
		public readonly ContentItem Item; // The item this build event is attached to, null if it was cached

		// Path to source file
		private readonly string _cachedSource = null;
		public string SourcePath => Item?.Paths.SourcePath ?? _cachedSource;

		// Path to output file
		private readonly string _cachedOutput = null;
		public string OutputPath => Item?.Paths.OutputPath ?? _cachedOutput;

		// Importer name
		private readonly string _cachedImporter = null;
		public string Importer => Item?.ImporterName ?? _cachedImporter;

		// Processor name
		private readonly string _cachedProcessor = null;
		public string Processor => Item?.ProcessorName ?? _cachedProcessor;

		// Modify times for the input and output files
		public readonly DateTime InputTime;
		public readonly DateTime OutputTime;
		#endregion // Fields

		private BuildEvent(ContentItem item, DateTime iTime, DateTime oTime)
		{
			Item = item;
			InputTime = iTime;
			OutputTime = oTime;
		}

		#region Creation
		public static BuildEvent FromItem(ContentItem item)
		{
			var iInfo = new FileInfo(item.Paths.SourcePath);
			var oInfo = new FileInfo(item.Paths.OutputPath);
			return iInfo.Exists
				? new BuildEvent(item, iInfo.LastWriteTimeUtc, oInfo.Exists ? oInfo.LastWriteTimeUtc : new DateTime(0))
				: null;
		}

		public static BuildEvent FromCacheFile(ContentItem item)
		{
			return null;
		}
		#endregion // Creation
	}
}
