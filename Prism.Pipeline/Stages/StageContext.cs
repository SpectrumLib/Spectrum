using System;
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

		private protected readonly BuildTask Task;
		#endregion // Fields

		private protected StageContext(BuildTask task, PipelineLogger logger)
		{
			Task = task;
			Logger = logger;
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
	}
}
