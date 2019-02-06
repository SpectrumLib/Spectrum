using System;
using System.IO;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the content importing logic of a <see cref="ContentImporter{Tout}"/>
	/// instance.
	/// </summary>
	public class ImporterContext
	{
		#region Fields
		private readonly FileInfo _fileInfo;
		/// <summary>
		/// The name of the current content file (without any directory info).
		/// </summary>
		public string FileName => _fileInfo.Name;
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
		#endregion // Fields

		internal ImporterContext(FileInfo finfo)
		{
			_fileInfo = finfo;
		}
	}
}
