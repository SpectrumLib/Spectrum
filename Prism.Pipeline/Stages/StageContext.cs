using System;
using System.Collections.Generic;
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

		private protected readonly BuildTask Task;

		private readonly List<string> _tempFiles;
		/// <summary>
		/// Gets a list of the temp build files that are reserved for use by this specific content item.
		/// </summary>
		public IReadOnlyList<string> TempFiles => _tempFiles;
		#endregion // Fields

		private protected StageContext(BuildTask task, PipelineLogger logger)
		{
			Task = task;
			Logger = logger;
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
