using System;
using Prism.Build;

namespace Prism
{
	/// <summary>
	/// Contains logging functionality for content pipeline stages.
	/// </summary>
	public sealed class PipelineLogger
	{
		#region Fields
		internal readonly BuildEngine Engine;
		internal BuildLogger Logger => Engine.Logger;

		private BuildEvent _currEvent;
		private string _currStageName;
		#endregion // Fields

		internal PipelineLogger(BuildEngine engine)
		{
			Engine = engine;
		}

		internal void UseEvent(BuildEvent evt)
		{
			_currEvent = evt;
		}

		internal void UpdateStageName(string name)
		{
			_currStageName = name;
		}

		/// <summary>
		/// Logs an information (standard-level no error) message to the pipeline logging system.
		/// </summary>
		/// <param name="str">The message to log.</param>
		public void Info(string str) =>
			Logger.ItemInfo(_currEvent, $"({_currStageName}) {str}");

		/// <summary>
		/// Logs a non-standard error message to the pipeline logging system that represents an unexpected state
		/// or recoverable error.
		/// </summary>
		/// <param name="str">The message to log.</param>
		public void Warn(string str) =>
			Logger.ItemWarn(_currEvent, $"({_currStageName}) {str}");

		/// <summary>
		/// Logs a severe error message to the pipeline logging system that represents an unrecoverable error. The
		/// pipeline stage should be returned from after an error message is logged.
		/// </summary>
		/// <param name="str">The message to log.</param>
		public void Error(string str) =>
			Logger.ItemError(_currEvent, $"({_currStageName}) {str}");
	}
}
