using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Prism.Content;

namespace Prism.Build
{
	// Base class for the Prism tool to implement build process loggers
	internal abstract class BuildLogger
	{
		#region Fields
		public BuildEngine Engine { get; internal set; }
		public ContentProject Project => Engine.Project;

		private Stopwatch _timer;
		private DateTime _startTime;

		// The log time of the currently-handled message
		public DateTime MessageTime { get; private set; }

		private ConcurrentQueue<Message> _messages;
		#endregion // Fields

		protected BuildLogger()
		{
			_timer = new Stopwatch();
			_messages = new ConcurrentQueue<Message>();
		}

		#region Internal Logging
		internal void EngineInfo(string msg) => _messages.Enqueue(new Message(MessageType.EngineInfo, DateTime.Now, msg));

		internal void EngineWarn(string msg) => _messages.Enqueue(new Message(MessageType.EngineWarn, DateTime.Now, msg));

		internal void EngineError(string msg) => _messages.Enqueue(new Message(MessageType.EngineError, DateTime.Now, msg));

		internal void BuildStart(bool rebuild)
		{
			_startTime = DateTime.Now;
			_timer.Restart();

			_messages.Enqueue(new Message(MessageType.BuildStart, _startTime, rebuild));
		}

		internal void BuildEnd(bool success, TimeSpan elapsed, bool cancelled) =>
			_messages.Enqueue(new Message(MessageType.BuildEnd, _startTime + _timer.Elapsed, success, elapsed, cancelled));

		internal void CleanStart()
		{
			_startTime = DateTime.Now;
			_timer.Restart();

			_messages.Enqueue(new Message(MessageType.CleanStart, _startTime));
		}

		internal void CleanEnd(bool success, TimeSpan elapsed, bool cancelled) =>
			_messages.Enqueue(new Message(MessageType.CleanEnd, _startTime + _timer.Elapsed, success, elapsed, cancelled));

		internal void ItemStart(ContentItem item, uint id) =>
			_messages.Enqueue(new Message(MessageType.ItemStarted, _startTime + _timer.Elapsed, item, id));

		internal void ItemContinue(ContentItem item, uint id, ContinueStage stage) =>
			_messages.Enqueue(new Message(MessageType.ItemContinued, _startTime + _timer.Elapsed, item, id, stage));

		internal void ItemFinished(ContentItem item, uint id, TimeSpan elapsed) =>
			_messages.Enqueue(new Message(MessageType.ItemFinished, _startTime + _timer.Elapsed, item, id, elapsed));

		internal void ItemFailed(ContentItem item, uint id, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemFailed, _startTime + _timer.Elapsed, item, id, message));

		internal void ItemSkipped(ContentItem item, uint id) =>
			_messages.Enqueue(new Message(MessageType.ItemSkipped, _startTime + _timer.Elapsed, item, id));

		internal void ItemInfo(ContentItem item, uint id, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemInfo, _startTime + _timer.Elapsed, item, id, message));

		internal void ItemWarn(ContentItem item, uint id, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemWarning, _startTime + _timer.Elapsed, item, id, message));

		internal void ItemError(ContentItem item, uint id, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemError, _startTime + _timer.Elapsed, item, id, message));
		#endregion // Internal Logging

		// Called on the main thread in the Prism tool to process all queued messages
		public void Poll()
		{
			while (_messages.TryDequeue(out Message msg))
			{
				MessageTime = msg.Time;
				switch (msg.Type)
				{
					case MessageType.EngineInfo: onEngineInfo((string)msg.Args[0]); break;
					case MessageType.EngineWarn: onEngineWarning((string)msg.Args[0]); break;
					case MessageType.EngineError: onEngineError((string)msg.Args[0]); break;
					case MessageType.BuildStart: onBuildStart(MessageTime, (bool)msg.Args[0]); break;
					case MessageType.BuildEnd: onBuildEnd((bool)msg.Args[0], (TimeSpan)msg.Args[1], (bool)msg.Args[2]); break;
					case MessageType.CleanStart: onCleanStart(MessageTime); break;
					case MessageType.CleanEnd: onCleanEnd((bool)msg.Args[0], (TimeSpan)msg.Args[1], (bool)msg.Args[2]); break;
					case MessageType.ItemStarted: onItemStarted((ContentItem)msg.Args[0], (uint)msg.Args[1]); break;
					case MessageType.ItemContinued: onItemContinued((ContentItem)msg.Args[0], (uint)msg.Args[1], (ContinueStage)msg.Args[2]); break;
					case MessageType.ItemFinished: onItemFinished((ContentItem)msg.Args[0], (uint)msg.Args[1], (TimeSpan)msg.Args[2]); break;
					case MessageType.ItemFailed: onItemFailed((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
					case MessageType.ItemSkipped: onItemSkipped((ContentItem)msg.Args[0], (uint)msg.Args[1]); break;
					case MessageType.ItemInfo: onItemInfo((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
					case MessageType.ItemWarning: onItemWarn((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
					case MessageType.ItemError: onItemError((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
				}
			}
		}

		#region Message Handlers
		// Called when the build engine produces a general info message
		protected abstract void onEngineInfo(string msg);
		// Called when the build engine produces a general warning message
		protected abstract void onEngineWarning(string msg);
		// Called when the build engine produces a general error message
		protected abstract void onEngineError(string msg);

		// Called when a new build or rebuild action starts
		protected abstract void onBuildStart(DateTime start, bool rebuild);
		// Called when a build or rebuild action ends, either through success, early failure, or if it was cancelled
		protected abstract void onBuildEnd(bool success, TimeSpan elapsed, bool cancelled);

		// Called when a new clean action starts
		protected abstract void onCleanStart(DateTime start);
		// Called when a clean action ends
		protected abstract void onCleanEnd(bool success, TimeSpan elapsed, bool cancelled);

		// Called when processing on a content item starts
		protected abstract void onItemStarted(ContentItem item, uint id);
		// Called when a content item is advanced through the pipeline stages
		protected abstract void onItemContinued(ContentItem item, uint id, ContinueStage stage);
		// Called when a content item is finished processing
		protected abstract void onItemFinished(ContentItem item, uint id, TimeSpan elapsed);
		// Called when the build process for a content item fails
		protected abstract void onItemFailed(ContentItem item, uint idx, string message);
		// Called when a content item is skipped by the build engine
		protected abstract void onItemSkipped(ContentItem item, uint idx);

		// Called from a pipeline stage to relay normal-level information about a content item build process
		protected abstract void onItemInfo(ContentItem item, uint id, string message);
		// Called from a pipeline stage to relay warning-level information about a content item build process
		protected abstract void onItemWarn(ContentItem item, uint id, string message);
		// Called from a pipeline stage to relay error-level information about a content item build process
		protected abstract void onItemError(ContentItem item, uint id, string message);
		#endregion // Message Handlers

		#region Message Impl
		private struct Message
		{
			public MessageType Type;
			public DateTime Time;
			public object[] Args;

			public Message(MessageType t, DateTime tm, params object[] args)
			{
				Type = t;
				Time = tm;
				Args = args;
			}
		}

		private enum MessageType
		{
			EngineInfo,
			EngineWarn,
			EngineError,
			BuildStart,
			BuildEnd,
			CleanStart,
			CleanEnd,
			ItemStarted,
			ItemContinued,
			ItemFinished,
			ItemFailed,
			ItemSkipped,
			ItemInfo,
			ItemWarning,
			ItemError
		}
		#endregion // Message Impl

		// Represents the pipeline stage for ItemContinue messages
		public enum ContinueStage
		{
			Processing,
			Writing
		}
	}
}
