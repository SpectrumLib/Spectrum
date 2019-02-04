using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Prism
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
		internal void BuildStart()
		{
			_startTime = DateTime.Now;
			_timer.Restart();

			_messages.Enqueue(new Message(MessageType.BuildStart, _startTime));
		}

		internal void BuildEnd(bool success, TimeSpan elapsed, bool cancelled) =>
			_messages.Enqueue(new Message(MessageType.BuildEnd, _startTime + _timer.Elapsed, success, elapsed, cancelled));

		internal void CleanStart()
		{
			_startTime = DateTime.Now;
			_timer.Restart();

			_messages.Enqueue(new Message(MessageType.CleanStart, _startTime));
		}

		internal void CleanEnd(bool success, TimeSpan elapsed) =>
			_messages.Enqueue(new Message(MessageType.CleanEnd, _startTime + _timer.Elapsed, success, elapsed));
		#endregion // Internal Logging

		// Called on the main thread in the Prism tool to process all queued messages
		public void Poll()
		{
			Message msg;
			while (_messages.TryDequeue(out msg))
			{
				MessageTime = msg.Time;
				switch (msg.Type)
				{
					case MessageType.BuildStart: onBuildStart(MessageTime); break;
					case MessageType.BuildEnd: onBuildEnd((bool)msg.Args[0], (TimeSpan)msg.Args[1], (bool)msg.Args[2]); break;
					case MessageType.CleanStart: onCleanStart(MessageTime); break;
					case MessageType.CleanEnd: onCleanEnd((bool)msg.Args[0], (TimeSpan)msg.Args[1]); break;
				}
			}
		}

		#region Message Handlers
		// Called when a new build or rebuild action starts
		protected abstract void onBuildStart(DateTime start);
		// Called when a build or rebuild action ends, either through success, early failure, or if it was cancelled
		protected abstract void onBuildEnd(bool success, TimeSpan elapsed, bool cancelled);

		// Called when a new clean action starts
		protected abstract void onCleanStart(DateTime start);
		// Called when a clean action ends
		protected abstract void onCleanEnd(bool success, TimeSpan elapsed);
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
			BuildStart,
			BuildEnd,
			CleanStart,
			CleanEnd
		}
		#endregion // Message Impl
	}
}
