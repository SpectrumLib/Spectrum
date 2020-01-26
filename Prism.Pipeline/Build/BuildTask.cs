/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Prism.Pipeline
{
	// Encapsultes the pipeline process on a single thread, managing the building of a single item at a time
	internal class BuildTask
	{
		#region Fields
		public readonly BuildEngine Engine;
		public bool ShouldStop => Engine.ShouldStop;
		public bool IsRelease => Engine.IsRelease;
		public bool Compress => Engine.Compress;

		private readonly Dictionary<string, ContentProcessor> _procCache = 
			new Dictionary<string, ContentProcessor>();

		private readonly List<ItemResult> _results = new List<ItemResult>();
		public IReadOnlyList<ItemResult> Results => _results;

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

		private ContentProcessor getProcessor(string ctype, out string pname, out string err)
		{
			pname = null;
			err = null;
			if (_procCache.ContainsKey(ctype))
				return _procCache[ctype];

			var proctype = Engine.ProcessorTypes.FindContentType(ctype);
			if (proctype == null)
			{
				err = $"no content processor registered for type '{ctype}'";
				return null;
			}

			try
			{
				var proc = proctype.Ctor.Invoke(null) as ContentProcessor;
				_procCache.Add(ctype, proc);
				pname = proctype.Attr.DisplayName;
				return proc;
			}
			catch (Exception e)
			{
				err = $"could not construct content processor ({e.Message}).";
				return null;
			}
		}

		private void _thread_func(bool rebuild)
		{
			Stopwatch timer = Stopwatch.StartNew();
			_results.Clear();

			// Iterate over build orders
			while (!ShouldStop && Engine.GetNextOrder(out var order))
			{
				// Report start
				Engine.Logger.ItemStart(order.Item, order.Index);
				timer.Restart();

				// Prepare the item results
				var res = new ItemResult(order);
				_results.Add(res);

				// Refresh file status
				try
				{
					order.Item.InputFile.Refresh();
					order.Item.CacheFile.Refresh();
					order.Item.OutputFile.Refresh();
				}
				catch
				{
					Engine.Logger.ItemFailed(order.Item, order.Index, "Could not refresh content file status.");
					continue;
				}

				// Check the source file exists
				if (!order.Item.InputFile.Exists)
				{
					Engine.Logger.ItemFailed(order.Item, order.Index, "Source file does not exist.");
					continue;
				}

				// Check the build cache
				if (!rebuild && !order.NeedsRebuild())
				{
					Engine.Logger.ItemSkipped(order.Item, order.Index);
					res.Complete(TimeSpan.Zero, (ulong)order.Item.OutputFile.Length);
					continue;
				}

				// Get the processor instance
				if (getProcessor(order.Item.Type, out var pname, out var err) is var pinst && (pinst is null))
				{
					Engine.Logger.ItemFailed(order.Item, order.Index, $"Content processor failed - {err}.");
					continue;
				}
				try
				{
					pinst.Reset();
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(order.Item, order.Index, $"{pname} reset failed - {e.Message}.");
					continue;
				}

				// Open the file streams
				FileStream inStream = null, outStream = null;
				try
				{
					inStream = order.Item.InputFile.Open(FileMode.Open, FileAccess.Read, FileShare.None);
					outStream = order.Item.OutputFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
				}
				catch (Exception e)
				{
					inStream?.Dispose();
					outStream?.Dispose();
					Engine.Logger.ItemFailed(order.Item, order.Index, $"Unable to open file stream - {e.Message}.");
					continue;
				}

				// Run through the pipeline
				ulong outSize = 0;
				try
				{
					// Exit check
					if (ShouldStop)
					{
						Engine.Logger.ItemFailed(order.Item, order.Index, "Build cancelled.");
						break;
					}

					// Create the streams and context
					using BinaryReader reader = new BinaryReader(inStream);
					using BinaryWriter writer = new BinaryWriter(outStream);
					PipelineContext ctx = new PipelineContext(this, order);

					// Begin the pipeline
					pinst.Begin(ctx, reader);

					// Perform the processing loop
					while (!ShouldStop && pinst.Read(ctx, reader))
					{
						pinst.Process(ctx);
						pinst.Write(ctx, writer);
						writer.Flush();
						ctx.LoopIndex += 1;
					}

					// Exit check
					if (ShouldStop)
					{
						Engine.Logger.ItemFailed(order.Item, order.Index, "Build cancelled.");
						break;
					}

					// End the pipeline
					pinst.End(ctx, writer);
					writer.Flush();
					outSize = (ulong)outStream.Length;
				}
				catch (PipelineItemException e)
				{
					Engine.Logger.ItemFailed(order.Item, order.Index, $"[{e.CallingMethod}:{e.CallingLine}] - {e.Message}.");
					continue;
				}
				catch (Exception e)
				{
					var sline = e.StackTrace.Substring(0, e.StackTrace.IndexOf('\n')).Trim();
					Engine.Logger.ItemFailed(order.Item, order.Index, $"[{e.GetType().Name}] - {e.Message} ({sline}).");
					continue;
				}
				finally
				{
					inStream?.Dispose();
					outStream?.Dispose();
				}

				// Report the end of the item build, write the cache
				res.Complete(timer.Elapsed, outSize);
				if (!order.WriteCacheFile(res))
					Engine.Logger.ItemWarn(order.Item, order.Index, "Unable to write build cache file.");
				Engine.Logger.ItemFinished(order.Item, order.Index, timer.Elapsed);
			}
		}
	}
}
