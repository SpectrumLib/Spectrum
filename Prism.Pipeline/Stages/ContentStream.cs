using System;
using System.IO;

namespace Prism
{
	/// <summary>
	/// Stream type used be <see cref="ContentWriter{Tin}"/> instances to write the processed content data out to 
	/// a content file in binary format.
	/// </summary>
	public sealed class ContentStream
	{
		// Internally, a single instance of this type is provided to each BuildTask for a build engine. After a content
		//   file is imported and processed, the underlying stream in this type is reset so the same buffer can be used
		//   to write the new data. After the write is complete, the buffer is flushed to the file asynchronously so
		//   the task can move onto the import and processing stages for the next file. The task will wait on the
		//   async flush from the previous file before starting the write process for the next file.

		private const uint BUFFER_SIZE = 33_554_432; // 32MB for the backing buffer size

		#region Fields
		private byte[] _memBuffer = null;
		private MemoryStream _baseStream = null;
		private BinaryWriter _writer = null;
		#endregion // Fields

		internal ContentStream()
		{
			_memBuffer = new byte[BUFFER_SIZE];
			_baseStream = new MemoryStream(_memBuffer, 0, (int)BUFFER_SIZE, true);
			_writer = new BinaryWriter(_baseStream);
		}
	}
}
