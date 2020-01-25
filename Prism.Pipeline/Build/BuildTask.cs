/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using System.Threading;

namespace Prism.Pipeline
{
	// Encapsultes the pipeline process on a single thread, managing the building of a single item at a time
	internal class BuildTask
	{
		#region Fields
		public readonly BuildEngine Engine;

		public bool IsRelease => Engine.IsRelease;
		public bool Compress => Engine.Compress;

		public bool ShouldStop => Engine.ShouldStop;

		private Thread _thread;
		public bool Running => (_thread != null);
		#endregion // Fields

		public BuildTask(BuildEngine engine)
		{
			Engine = engine;
		}

		public void Start(bool rebuild)
		{
			if (Running)
				throw new InvalidOperationException("Cannot start a build task that is already running.");

			_thread = new Thread(() => {
				_thread_func(rebuild);
				_thread = null;
			});
			_thread.Start();
		}

		public void Join() => _thread?.Join();

		private void _thread_func(bool rebuild)
		{
			Stopwatch timer = Stopwatch.StartNew();

			// Iterate over build orders
			while (!ShouldStop && Engine.GetNextOrder(out var order))
			{
				// Report start
				Engine.Logger.ItemStart(order.Item, order.Index);
				timer.Restart();

				// Check the source file exists
				order.Item.InputFile.Refresh();
				if (!order.Item.InputFile.Exists)
				{
					Engine.Logger.ItemFailed(order.Item, order.Index, "Source file does not exist.");
					continue;
				}

				// Report the end of the item build
				Engine.Logger.ItemFinished(order.Item, order.Index, timer.Elapsed);
			}
		}
	}
}
