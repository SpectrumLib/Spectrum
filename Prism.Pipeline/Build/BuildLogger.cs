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
		internal void EngineInfo(string msg, bool important = false) => 
			_messages.Enqueue(new Message(MessageType.EngineInfo, DateTime.Now, msg, important));

		internal void EngineWarn(string msg) => _messages.Enqueue(new Message(MessageType.EngineWarn, DateTime.Now, msg));

		internal void EngineError(string msg) => _messages.Enqueue(new Message(MessageType.EngineError, DateTime.Now, msg));

		internal void BuildStart(bool rebuild, bool release)
		{
			_startTime = DateTime.Now;
			_timer.Restart();

			_messages.Enqueue(new Message(MessageType.BuildStart, _startTime, rebuild, release));
		}

		internal void BuildContinue(TimeSpan itemBuildTime) =>
			_messages.Enqueue(new Message(MessageType.BuildContinue, _startTime + _timer.Elapsed, _timer.Elapsed));

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

		internal void ItemStart(BuildEvent evt) =>
			_messages.Enqueue(new Message(MessageType.ItemStarted, _startTime + _timer.Elapsed, evt.Item, evt.Index));

		internal void ItemContinue(BuildEvent evt, ContinueStage stage) =>
			_messages.Enqueue(new Message(MessageType.ItemContinued, _startTime + _timer.Elapsed, evt.Item, evt.Index, stage));

		internal void ItemFinished(BuildEvent evt, TimeSpan elapsed) =>
			_messages.Enqueue(new Message(MessageType.ItemFinished, _startTime + _timer.Elapsed, evt.Item, evt.Index, elapsed));

		internal void ItemFailed(BuildEvent evt, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemFailed, _startTime + _timer.Elapsed, evt.Item, evt.Index, message));

		internal void ItemSkipped(BuildEvent evt) =>
			_messages.Enqueue(new Message(MessageType.ItemSkipped, _startTime + _timer.Elapsed, evt.Item, evt.Index));

		internal void ItemInfo(BuildEvent evt, string message, bool important = false) =>
			_messages.Enqueue(new Message(MessageType.ItemInfo, _startTime + _timer.Elapsed, evt.Item, evt.Index, message, important));

		internal void ItemWarn(BuildEvent evt, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemWarning, _startTime + _timer.Elapsed, evt.Item, evt.Index, message));

		internal void ItemError(BuildEvent evt, string message) =>
			_messages.Enqueue(new Message(MessageType.ItemError, _startTime + _timer.Elapsed, evt.Item, evt.Index, message));

		internal void ItemPack(ContentItem item, uint packNum) =>
			_messages.Enqueue(new Message(MessageType.ItemPack, _startTime + _timer.Elapsed, item, packNum));
		#endregion // Internal Logging

		// Called on the main thread in the Prism tool to process all queued messages
		public void Poll()
		{
			while (_messages.TryDequeue(out Message msg))
			{
				MessageTime = msg.Time;
				switch (msg.Type)
				{
					case MessageType.EngineInfo: onEngineInfo((string)msg.Args[0], (bool)msg.Args[1]); break;
					case MessageType.EngineWarn: onEngineWarning((string)msg.Args[0]); break;
					case MessageType.EngineError: onEngineError((string)msg.Args[0]); break;
					case MessageType.BuildStart: onBuildStart(MessageTime, (bool)msg.Args[0], (bool)msg.Args[1]); break;
					case MessageType.BuildContinue: onBuildContinue((TimeSpan)msg.Args[0]); break;
					case MessageType.BuildEnd: onBuildEnd((bool)msg.Args[0], (TimeSpan)msg.Args[1], (bool)msg.Args[2]); break;
					case MessageType.CleanStart: onCleanStart(MessageTime); break;
					case MessageType.CleanEnd: onCleanEnd((bool)msg.Args[0], (TimeSpan)msg.Args[1], (bool)msg.Args[2]); break;
					case MessageType.ItemStarted: onItemStarted((ContentItem)msg.Args[0], (uint)msg.Args[1]); break;
					case MessageType.ItemContinued: onItemContinued((ContentItem)msg.Args[0], (uint)msg.Args[1], (ContinueStage)msg.Args[2]); break;
					case MessageType.ItemFinished: onItemFinished((ContentItem)msg.Args[0], (uint)msg.Args[1], (TimeSpan)msg.Args[2]); break;
					case MessageType.ItemFailed: onItemFailed((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
					case MessageType.ItemSkipped: onItemSkipped((ContentItem)msg.Args[0], (uint)msg.Args[1]); break;
					case MessageType.ItemInfo: onItemInfo((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2], (bool)msg.Args[3]); break;
					case MessageType.ItemWarning: onItemWarn((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
					case MessageType.ItemError: onItemError((ContentItem)msg.Args[0], (uint)msg.Args[1], (string)msg.Args[2]); break;
					case MessageType.ItemPack: onItemPack((ContentItem)msg.Args[0], (uint)msg.Args[1]); break;
				}
			}
		}

		#region Message Handlers
		// Called when the build engine produces a general info message
		protected abstract void onEngineInfo(string msg, bool important);
		// Called when the build engine produces a general warning message
		protected abstract void onEngineWarning(string msg);
		// Called when the build engine produces a general error message
		protected abstract void onEngineError(string msg);

		// Called when a new build or rebuild action starts
		protected abstract void onBuildStart(DateTime start, bool rebuild, bool release);
		// Called when the build process moves from building content items to preparing them for output
		protected abstract void onBuildContinue(TimeSpan itemBuildTime);
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
		protected abstract void onItemInfo(ContentItem item, uint id, string message, bool important);
		// Called from a pipeline stage to relay warning-level information about a content item build process
		protected abstract void onItemWarn(ContentItem item, uint id, string message);
		// Called from a pipeline stage to relay error-level information about a content item build process
		protected abstract void onItemError(ContentItem item, uint id, string message);

		// Called from the packing process when an item is moved to the output
		protected abstract void onItemPack(ContentItem item, uint packNum);
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
			BuildContinue,
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
			ItemError,
			ItemPack
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
