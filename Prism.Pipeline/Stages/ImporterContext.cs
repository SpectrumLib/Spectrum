using System;
using System.Collections.Generic;
using System.IO;
using Prism.Build;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the content importing logic of a <see cref="ContentImporter{Tout}"/>
	/// instance.
	/// </summary>
	public sealed class ImporterContext : StageContext
	{
		#region Fields
		private readonly FileInfo _fileInfo;
		/// <summary>
		/// The name of the current content file (without any directory info).
		/// </summary>
		public string FileName => _fileInfo.Name;
		/// <summary>
		/// The absolute path to the directory that the content file is in.
		/// </summary>
		public string FileDirectory => _fileInfo.DirectoryName;
		/// <summary>
		/// The absolute path to the input file.
		/// </summary>
		public string FilePath => _fileInfo.FullName;
		/// <summary>
		/// The extension of the current content file, with the period.
		/// </summary>
		public string FileExtension => _fileInfo.Extension;
		/// <summary>
		/// The length of the current content file, in bytes.
		/// </summary>
		public uint FileLength => (uint)_fileInfo.Length;
		/// <summary>
		/// The date and time that the current content file was last changed.
		/// </summary>
		public DateTime LastWriteTime => _fileInfo.LastWriteTime;

		private readonly List<string> _dependencies;
		/// <summary>
		/// The list of file dependencies currently added to this content item.
		/// </summary>
		public IReadOnlyList<string> Dependencies => _dependencies;
		#endregion // Fields

		internal ImporterContext(BuildTask task, PipelineLogger logger, FileInfo finfo) :
			base(task, logger)
		{
			_fileInfo = finfo;
			_dependencies = new List<string>();
		}

		/// <summary>
		/// Adds an external file as a dependency for this content item. External file dependencies will also be checked
		/// to see if they have been edited since the last build, and will trigger a rebuild if they have.
		/// </summary>
		/// <param name="path">The path to the external file dependency, can be relative or absolute.</param>
		/// <returns>If the dependency file exists and could be added.</returns>
		public bool AddDependency(string path)
		{
			if (!PathUtils.TryGetFullPath(path, out string abs, FileDirectory))
				throw new ArgumentException($"The dependency path '{path}' is invalid.", nameof(path));

			if (!File.Exists(abs))
				return false;

			if (!_dependencies.Contains(abs))
				_dependencies.Add(abs);
			return true;
		}
	}
}
