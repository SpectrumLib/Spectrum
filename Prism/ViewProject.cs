using System;
using System.Linq;

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
				Console.WriteLine($"INFO: Loaded project file at '{project.FilePath}'.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"ERROR: Could not load content project file, reason: {e.Message}.");
				if (verbose && (e.InnerException != null))
				{
					Console.WriteLine($"EXCEPTION: ({e.InnerException.GetType().Name})");
					Console.WriteLine(e.InnerException.StackTrace);
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
			Console.WriteLine($"  Item Count:          {project.Items.Count}");
			Console.WriteLine();

			// Function for printing an item
			void __printItem(ContentItem item)
			{
				Console.WriteLine($"  > {item.ItemPath}");
				if (verbose)
				{
					Console.WriteLine($"      Full Path:   {item.Paths.SourcePath}");
					Console.WriteLine($"      Importer:    {item.ImporterName}");
					Console.WriteLine($"                     {String.Join(";", item.ImporterArgs.Select(pair => $"{pair.Key}={pair.Value}"))}");
					Console.WriteLine($"      Processor:   {item.ProcessorName}");
					Console.WriteLine($"                     {String.Join(";", item.ProcessorArgs.Select(pair => $"{pair.Key}={pair.Value}"))}");
				}
				else
				{
					Console.WriteLine($"      Importer:    {item.ImporterName} ({item.ImporterArgs.Count})");
					Console.WriteLine($"      Processor:   {item.ProcessorName} ({item.ProcessorArgs.Count})");
				}
			}

			// Items info
			Console.WriteLine($"=========== ITEMS SUMMARY ===========");
			foreach (var item in project.Items)
				__printItem(item.Value);
			Console.WriteLine($"");

			// All went well
			return 0;
		}
	}
}
