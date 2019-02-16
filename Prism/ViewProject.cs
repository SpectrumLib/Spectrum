using System;
using System.Linq;
using Prism.Content;

namespace Prism
{
	// Implements the command line 'view' action
	public static class ViewProject
	{
		// Summarizes the content project
		public static int Summarize(string[] args, bool verbose)
		{
			// Try to load the content project file
			string filePath = args[1];
			ContentProject project = null;
			try
			{
				project = ContentProject.LoadFromFile(filePath);
			}
			catch (Exception e)
			{
				CConsole.Error($"Could not load content project file, reason: {e.Message}.");
				if (verbose && (e.InnerException != null))
				{
					CConsole.Error($"{e.InnerException.GetType().Name}");
					CConsole.Error(e.InnerException.StackTrace);
				}
				return -1;
			}

			// Project Info
			Console.WriteLine();
			Console.WriteLine($"========== PROJECT SUMMARY ==========");
			Console.WriteLine($"  Project File:        {project.FilePath}");
			Console.WriteLine($"  Content Root:        {project.Paths.ContentRoot}");
			Console.WriteLine($"  Intermediate Path:   {project.Paths.IntermediateRoot}");
			Console.WriteLine($"  Output Path:         {project.Paths.OutputRoot}");
			Console.WriteLine($"  Compress:            {project.Properties.Compress}");
			Console.WriteLine($"  Pack Size:           {project.Properties.PackSize / ProjectProperties.SIZE_TO_BYTES} MB");
			Console.WriteLine();

			// Function for printing an item
			void __printItem(ContentItem item)
			{
				Console.WriteLine($"  > {item.ItemPath}");
				if (verbose)
				{
					Console.WriteLine($"      Full Path:   {item.Paths.SourcePath}");
					Console.WriteLine($"      Importer:    {item.ImporterName}");
					Console.WriteLine($"      Processor:   {item.ProcessorName} ({item.ProcessorArgs.Count})");
					Console.WriteLine($"                     {(item.ProcessorArgs.Count > 0 ? String.Join(";", item.ProcessorArgs.Select(pair => $"{pair.Key}={pair.Value}")) : "No Args")}");
				}
				else
				{
					Console.WriteLine($"      Importer:    {item.ImporterName}");
					Console.WriteLine($"      Processor:   {item.ProcessorName} ({item.ProcessorArgs.Count})");
				}
			}

			// Items info
			Console.WriteLine($"=========== ITEMS SUMMARY ===========");
			Console.WriteLine($"  Total Item Count:    {project.Items.Count}");
			foreach (var item in project.Items)
				__printItem(item.Value);
			Console.WriteLine($"");

			// All went well
			return 0;
		}
	}
}
