using System;
using System.IO;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the content importing logic of a <see cref="ContentImporter{Tout}"/>
	/// instance.
	/// </summary>
	public sealed class ImporterContext
	{
		#region Fields
		private readonly FileInfo _fileInfo;
		/// <summary>
		/// The name of the current content file (without any directory info).
		/// </summary>
		public string FileName => _fileInfo.Name;
		/// <summary>
		/// The absolute path to the input file.
		/// </summary>
		public string FilePath => _fileInfo.FullName;
		/// <summary>
		/// The extension of the current content file, with the period.
		/// </summary>
		public string FileExtension => _fileInfo.Extension;
		/// <summary>
		/// The length of the current content file, in bytes.
		/// </summary>
		public uint FileLength => (uint)_fileInfo.Length;
		/// <summary>
		/// The date and time that the current content file was last changed.
		/// </summary>
		public DateTime LastWriteTime => _fileInfo.LastWriteTime;

		/// <summary>
		/// The logger to use to report messages inside of ContentImporter instances.
		/// </summary>
		public readonly PipelineLogger Logger;

		/// <summary>
		/// If the build has requested statistics.
		/// </summary>
		public readonly bool UseStats;
		#endregion // Fields

		internal ImporterContext(FileInfo finfo, PipelineLogger logger, bool stats)
		{
			_fileInfo = finfo;
			Logger = logger;
			UseStats = stats;
		}
	}
}
