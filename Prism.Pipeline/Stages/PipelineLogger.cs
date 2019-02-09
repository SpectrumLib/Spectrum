using System;
using Prism.Build;
using Prism.Content;

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

		private ContentItem _currItem;
		private uint _currId;
		private string _currStageName;
		#endregion // Fields

		internal PipelineLogger(BuildEngine engine)
		{
			Engine = engine;
		}

		internal void UpdateItem(ContentItem item, uint id)
		{
			_currItem = item;
			_currId = id;
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
			Logger.ItemInfo(_currItem, _currId, $"({_currStageName}) {str}");

		/// <summary>
		/// Logs a non-standard error message to the pipeline logging system that represents an unexpected state
		/// or recoverable error.
		/// </summary>
		/// <param name="str">The message to log.</param>
		public void Warn(string str) =>
			Logger.ItemWarn(_currItem, _currId, $"({_currStageName}) {str}");

		/// <summary>
		/// Logs a severe error message to the pipeline logging system that represents an unrecoverable error. The
		/// pipeline stage should be returned from after an error message is logged.
		/// </summary>
		/// <param name="str">The message to log.</param>
		public void Error(string str) =>
			Logger.ItemError(_currItem, _currId, $"({_currStageName}) {str}");
	}
}
