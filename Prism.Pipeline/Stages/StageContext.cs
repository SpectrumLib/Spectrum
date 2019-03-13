using System;
using System.Collections.Generic;
using System.IO;
using Prism.Build;

namespace Prism
{
	/// <summary>
	/// Base type for content classes passed to pipeline stages.
	/// </summary>
	public abstract class StageContext
	{
		#region Fields
		/// <summary>
		/// The logger to use to report messages inside of the content pipeline.
		/// </summary>
		public readonly PipelineLogger Logger;

		/// <summary>
		/// If the build has requested statistics.
		/// </summary>
		public bool UseStats => Task.Engine.UseStats;

		protected readonly FileInfo FileInfo;
		/// <summary>
		/// The name of the current content file (without any directory info).
		/// </summary>
		public string FileName => FileInfo.Name;
		/// <summary>
		/// The absolute path to the directory that the content file is in.
		/// </summary>
		public string FileDirectory => FileInfo.DirectoryName;
		/// <summary>
		/// The absolute path to the input file.
		/// </summary>
		public string FilePath => FileInfo.FullName;
		/// <summary>
		/// The extension of the current content file, with the period.
		/// </summary>
		public string FileExtension => FileInfo.Extension;
		/// <summary>
		/// The length of the current content file, in bytes.
		/// </summary>
		public uint FileLength => (uint)FileInfo.Length;
		/// <summary>
		/// The date and time that the current content file was last changed.
		/// </summary>
		public DateTime LastWriteTime => FileInfo.LastWriteTime;

		private protected readonly BuildTask Task;

		private readonly List<string> _tempFiles;
		/// <summary>
		/// Gets a list of the temp build files that are reserved for use by this specific content item.
		/// </summary>
		public IReadOnlyList<string> TempFiles => _tempFiles;
		#endregion // Fields

		private protected StageContext(BuildTask task, PipelineLogger logger, FileInfo finfo)
		{
			Task = task;
			Logger = logger;
			FileInfo = finfo;
			_tempFiles = new List<string>();
		}

		/// <summary>
		/// Shortcut to the info log function for the <see cref="Logger"/>.
		/// </summary>
		public void LInfo(string msg, bool important = false) => Logger.Info(msg, important);

		/// <summary>
		/// Shortcut to the warn log function for the <see cref="Logger"/>.
		/// </summary>
		public void LWarn(string msg) => Logger.Warn(msg);

		/// <summary>
		/// Shortcut to the error log function for the <see cref="Logger"/>.
		/// </summary>
		public void LError(string msg) => Logger.Error(msg);

		/// <summary>
		/// Shortcut to the stats log function for the <see cref="Logger"/>.
		/// </summary>
		public void LStats(string msg) => Logger.Stats(msg);

		/// <summary>
		/// Gets the name of a temporary file that will be unique to this content item in the build pipeline. These
		/// files are not saved, and do not persist outside of the pipline for the content item in which they are created.
		/// </summary>
		/// <returns>The full path to a new temporary build file for the pipeline to use for the current content item.</returns>
		public string GetTempFile()
		{
			string path = Task.Manager.ReserveTempFile();
			_tempFiles.Add(path);
			return path;
		}
	}
}
