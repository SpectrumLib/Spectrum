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

		// The results of the last build
		public readonly TaskResults Results;

		// The thread instance
		private Thread _thread = null;
		#endregion // Fields

		public BuildTask(BuildTaskManager manager)
		{
			Manager = manager;

			_importers = new Dictionary<string, ImporterInstance>();
			_processors = new Dictionary<string, ProcessorInstance>();

			Results = new TaskResults();
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
			Results.Reset();

			// Create the content stream
			if (_contentStream == null)
				_contentStream = new ContentStream(Engine.Project.Properties.Compress);

			// Iterate over the tasks
			while (!Manager.ShouldStop && Manager.GetTaskItem(out BuildEvent current))
			{
				// Report start
				Engine.Logger.ItemStart(current);
				_timer.Restart();
				_logger.UseEvent(current);
				Results.UseItem(current.Item.ItemPath);

				// Check the source file exists
				if (current.InputTime == BuildEvent.ERROR_TIME)
				{
					Engine.Logger.ItemFailed(current, "Could not find the source file for the item");
					continue;
				}

				// Check for the requested importer and processor
				if (!_importers.ContainsKey(current.ImporterName))
				{
					if (Engine.StageCache.Importers.ContainsKey(current.ImporterName))
						_importers.Add(current.ImporterName, new ImporterInstance(Engine.StageCache.Importers[current.ImporterName]));
					else
					{
						Engine.Logger.ItemFailed(current, "The item requested an importer type that does not exist");
						continue;
					}
				}
				if (!_processors.ContainsKey(current.ProcessorName))
				{
					if (Engine.StageCache.Processors.ContainsKey(current.ProcessorName))
						_processors.Add(current.ProcessorName, new ProcessorInstance(Engine.StageCache.Processors[current.ProcessorName]));
					else
					{
						Engine.Logger.ItemFailed(current, "The item requested an processor type that does not exist");
						continue;
					}
				}
				var importer = _importers[current.ImporterName];
				var processor = _processors[current.ProcessorName];

				// Validate stage compatibility
				if (!processor.Type.InputType.IsAssignableFrom(importer.Type.OutputType))
				{
					Engine.Logger.ItemFailed(current, "The item specified incompatible stages");
					continue;
				}

				// Compare the current and cached build events to see if we can skip the build
				//   If we are forcing a rebuild we have to build so we can skip the check
				if (!rebuild)
				{
					var cached = BuildEvent.FromCacheFile(Engine, current.Item);
					if (!current.NeedsBuild(cached, processor.Instance))
					{
						Engine.Logger.ItemSkipped(current);
						Results.PassItem(current.OutputSize);
						continue;
					}
				}

				// Early stop check
				if (Manager.ShouldStop)
				{
					Engine.Logger.ItemFailed(current, "The build process was stopped while the item was being built");
					break;
				}

				// Run the importer
				FileStream importStream = null;
				FileInfo importInfo = null;
				try
				{
					importInfo = new FileInfo(current.Paths.SourcePath);
					importStream = importInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(current, $"The item source file could not be opened, {e.Message}");
					continue;
				}
				object importedData = null;
				try
				{
					_logger.UpdateStageName(current.ImporterName);
					ImporterContext ctx = new ImporterContext(importInfo, _logger);
					importedData = importer.Instance.Import(importStream, ctx);
					if (importedData == null)
					{
						Engine.Logger.ItemFailed(current, "The importer for the item did not produce any data");
						continue;
					}
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(current, $"Unhandled exception in importer, {e.Message} ({e.GetType().Name})");
					continue;
				}
				finally
				{
					importStream.Dispose();
				}

				// Early stop check
				if (Manager.ShouldStop)
				{
					Engine.Logger.ItemFailed(current, "The build process was stopped while the item was being built");
					break;
				}

				// Run the processor
				Engine.Logger.ItemContinue(current, BuildLogger.ContinueStage.Processing);
				object processedData = null;
				try
				{
					_logger.UpdateStageName(current.ProcessorName);
					ProcessorContext ctx = new ProcessorContext(_logger);
					processor.UpdateFields(Engine, current);
					processedData = processor.Instance.Process(importedData, ctx);
					if (processedData == null)
					{
						Engine.Logger.ItemFailed(current, "The processor for the item did not produce any data");
						continue;
					}
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(current, $"Unhandled exception in processor, {e.Message} ({e.GetType().Name})");
					continue;
				}

				// Early stop check
				if (Manager.ShouldStop)
				{
					Engine.Logger.ItemFailed(current, "The build process was stopped while the item was being built");
					break;
				}

				// Delete the output and cache files
				try
				{
					if (File.Exists(current.Paths.OutputPath))
						File.Delete(current.Paths.OutputPath);
					if (File.Exists(current.CachePath))
						File.Decrypt(current.CachePath);
				}
				catch
				{
					Engine.Logger.ItemFailed(current, "Could not delete the output file to rebuild the item");
					continue;
				}

				// Run the writer
				Engine.Logger.ItemContinue(current, BuildLogger.ContinueStage.Writing);
				try
				{
					_logger.UpdateStageName(processor.Type.WriterType.Name);
					_contentStream.Reset(current.Paths.OutputPath, processor.Instance.SkipCompression);
					WriterContext ctx = new WriterContext(_logger);
					processor.WriterInstance.Write(processedData, _contentStream, ctx);
					_contentStream.Flush();
				}
				catch (Exception e)
				{
					Engine.Logger.ItemFailed(current, $"Unhandled exception in writer, {e.Message} ({e.GetType().Name})");
					continue;
				}

				// Save the cache
				current.SaveCache(Engine);

				// Report end
				Engine.Logger.ItemFinished(current, _timer.Elapsed);
				Results.PassItem(_contentStream.OutputSize);
			}

			// Wait for the final output to be complete
			_contentStream.Reset(null, false);
			Results.UseItem(null); // In the case that the last item fails
		}
	}
}
