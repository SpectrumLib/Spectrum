using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Prism.Content;

namespace Prism.Build
{
	// Represents a single thread in a build process, which can operate on a single content item at a time
	//   A task is responsible for guiding a single content item through the build pipeline stages
	internal class BuildTask
	{
		#region Fields
		public readonly BuildTaskManager Manager;
		public BuildEngine Engine => Manager.Engine;
		public BuildLogger Logger => Manager.Engine.Logger;
		public ContentProject Project => Manager.Engine.Project;

		// Objects for tracking run state
		private readonly object _runLock = new object();
		private bool _running = false;
		public bool Running
		{
			get { lock (_runLock) { return _running; } }
			private set { lock (_runLock) { _running = value; } }
		}

		// Holds the importer and processors instances used by this task
		private readonly Dictionary<string, ImporterInstance> _importers;
		private readonly Dictionary<string, ProcessorInstance> _processors;

		// The output stream for the content
		private ContentStream _contentStream;

		// The thread instance
		private Thread _thread = null;
		#endregion // Fields

		public BuildTask(BuildTaskManager manager)
		{
			Manager = manager;

			_importers = new Dictionary<string, ImporterInstance>();
			_processors = new Dictionary<string, ProcessorInstance>();
		}

		// Starts the thread and begins processing the content items
		public void Start(bool rebuild)
		{
			if (Running)
				throw new InvalidOperationException("Cannot start a build task while it is already running");

			_thread = new Thread(() => {
				Running = true;
				_thread_func(rebuild);
				_thread = null;
				Running = false;
			});
			_thread.Start();
		}
		
		// Waits until this task has exited
		public void Join() => _thread?.Join();

		// The function that runs on the thread
		private void _thread_func(bool rebuild)
		{
			Stopwatch _timer = new Stopwatch();
			PipelineLogger _logger = new PipelineLogger(Engine);

			// Create the content stream
			if (_contentStream == null)
				_contentStream = new ContentStream();

			// Iterate over the tasks
			while (!Manager.ShouldStop && Manager.GetTaskItem(out ContentItem currItem, out uint currIdx))
			{
				// Report start
				Engine.Logger.ItemStart(currItem, currIdx);
				_timer.Restart();
				_logger.UpdateItem(currItem, currIdx);

				// Check for the requested importer and processor
				if (!_importers.ContainsKey(currItem.ImporterName))
				{
					if (Engine.StageCache.Importers.ContainsKey(currItem.ImporterName))
						_importers.Add(currItem.ImporterName, new ImporterInstance(Engine.StageCache.Importers[currItem.ImporterName]));
					else
					{
						Engine.Logger.ItemFailed(currItem, currIdx, "The item requested an importer type that does not exist");
						continue;
					}
				}
				if (!_processors.ContainsKey(currItem.ProcessorName))
				{
					if (Engine.StageCache.Processors.ContainsKey(currItem.ProcessorName))
						_processors.Add(currItem.ProcessorName, new ProcessorInstance(Engine.StageCache.Processors[currItem.ProcessorName]));
					else
					{
						Engine.Logger.ItemFailed(currItem, currIdx, "The item requested an processor type that does not exist");
						continue;
					}
				}
				var importer = _importers[currItem.ImporterName];
				var processor = _processors[currItem.ProcessorName];

				// Validate stage compatibility
				if (!processor.Type.InputType.IsAssignableFrom(importer.Type.OutputType))
				{
					Engine.Logger.ItemFailed(currItem, currIdx, "The item specified incompatible stages");
					continue;
				}

				// Make sure the source file exists
				if (!File.Exists(currItem.Paths.SourcePath))
				{
					Engine.Logger.ItemFailed(currItem, currIdx, "Could not find the source file for the item");
					continue;
				}

				// Delete the intermediate file
				try
				{
					if (File.Exists(currItem.Paths.OutputPath))
						File.Delete(currItem.Paths.OutputPath);
				}
				catch
				{
					Engine.Logger.ItemFailed(currItem, currIdx, "Could not delete the output file to rebuild the item");
					continue;
				}

				// Early stop check
				if (Manager.ShouldStop)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, "The build process was stopped while the item was being built");
					break;
				}

				// Run the importer
				FileStream importStream = null;
				FileInfo importInfo = null;
				try
				{
					importInfo = new FileInfo(currItem.Paths.SourcePath);
					importStream = importInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, $"The item source file could not be opened, {e.Message}");
					continue;
				}
				object importedData = null;
				try
				{
					_logger.UpdateStageName(currItem.ImporterName);
					ImporterContext ctx = new ImporterContext(importInfo, _logger);
					importedData = importer.Instance.Import(importStream, ctx);
					if (importedData == null)
					{
						Engine.Logger.ItemFailed(currItem, currIdx, "The importer for the item did not produce any data");
						continue;
					}
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, $"Unhandled exception in importer, {e.Message} ({e.GetType().Name})");
					continue;
				}
				finally
				{
					importStream.Dispose();
				}

				// Early stop check
				if (Manager.ShouldStop)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, "The build process was stopped while the item was being built");
					break;
				}

				// Run the processor
				Engine.Logger.ItemContinue(currItem, currIdx, BuildLogger.ContinueStage.Processing);
				object processedData = null;
				try
				{
					_logger.UpdateStageName(currItem.ProcessorName);
					ProcessorContext ctx = new ProcessorContext(_logger);
					processor.UpdateFields(Engine, currItem, currIdx);
					processedData = processor.Instance.Process(importedData, ctx);
					if (processedData == null)
					{
						Engine.Logger.ItemFailed(currItem, currIdx, "The processor for the item did not produce any data");
						continue;
					}
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, $"Unhandled exception in processor, {e.Message} ({e.GetType().Name})");
					continue;
				}

				// Early stop check
				if (Manager.ShouldStop)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, "The build process was stopped while the item was being built");
					break;
				}

				// Run the writer
				Engine.Logger.ItemContinue(currItem, currIdx, BuildLogger.ContinueStage.Writing);
				try
				{
					_logger.UpdateStageName(processor.Type.WriterType.Name);
					_contentStream.Reset(currItem.Paths.OutputPath);
					WriterContext ctx = new WriterContext(_logger);
					processor.WriterInstance.Write(processedData, _contentStream, ctx);
					_contentStream.Flush();
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(currItem, currIdx, $"Unhandled exception in writer, {e.Message} ({e.GetType().Name})");
					continue;
				}

				// Report end
				Engine.Logger.ItemFinished(currItem, currIdx, _timer.Elapsed);
			}

			// Wait for the final output to be complete
			_contentStream.Reset(null);
		}
	}
}
