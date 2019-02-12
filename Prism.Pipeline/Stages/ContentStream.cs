using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Prism.Content;

namespace Prism
{
	/// <summary>
	/// Stream type used be <see cref="ContentWriter{Tin}"/> instances to write the processed content data out to 
	/// a content file in binary format.
	/// </summary>
	public sealed partial class ContentStream
	{
		// Internally, a single instance of this type is provided to each BuildTask for a build engine. After a content
		//   file is imported and processed, the underlying stream in this type is reset so the same buffer can be used
		//   to write the new data. After the write is complete, the buffer is flushed to the file asynchronously so
		//   the task can move onto the import and processing stages for the next file. The task will wait on the
		//   async flush from the previous file before starting the write process for the next file. If the internal
		//   memory buffer gets full before writing ends, then the stream will synchronously write the buffer out to
		//   the file, and then continue writing from the beginning of the memory buffer.

		private const uint BUFFER_SIZE = 33_554_432; // 32MB for the backing buffer size
		private const uint DIRECT_WRITE_THRESHOLD = (uint)(BUFFER_SIZE * 0.9); // Arbitrary 90% threshold for direct write to disk

		#region Fields
		private readonly byte[] _memBuffer = null;
		private uint _bufferPos = 0;

		// The size (in bytes) of the file written by this stream, not valid until the final Flush() is called
		internal uint OutputSize { get; private set; } = 0;

		// The UTF8 encoding and associated encoder used to get string and character bytes
		private readonly Encoding _encoding;
		private readonly Encoder _encoder;

		// The compression level to use
		internal readonly bool Compress;
		internal bool SkipCompress = false;

		// The current async write task, if any
		private Task _writeTask = null;
		// The absolute path to the current output file
		private string _currentFile = null;
		#endregion // Fields

		internal ContentStream(bool compress)
		{
			_memBuffer = new byte[BUFFER_SIZE];
			_encoding = new UTF8Encoding(
				encoderShouldEmitUTF8Identifier: false,
				throwOnInvalidBytes: true
			);
			Compress = compress;
			_encoder = _encoding.GetEncoder();
		}

		// Waits on the old write task, then resets the type to start recoding at the beginning of the buffer
		internal void Reset(string path, bool skipCompression)
		{
			_writeTask?.Wait();
			_bufferPos = 0;
			OutputSize = 0;
			_currentFile = path;
			SkipCompress = skipCompression;
		}

		// Called by the task when it is done with this writer, launches the async write
		internal void Flush()
		{
			OutputSize += _bufferPos;

			_writeTask = new Task(() => {
				using (var file = File.Open(_currentFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				{
					if (Compress && !SkipCompress)
					{
						using (var writer = LZ4Stream.Encode(file, LZ4Level.L00_FAST))
						{
							writer.Write(_memBuffer, 0, (int)_bufferPos);
							writer.Flush();
						}
					}
					else
					{
						file.Write(_memBuffer, 0, (int)_bufferPos);
						file.Flush();
					}
				}
			});
			_writeTask.Start();
		}

		// Called by write functions when the backing buffer is full to synchronously write the buffer out and reset it
		private void flushInternal()
		{
			OutputSize += _bufferPos;

			// Synchronously write
			using (var file = File.Open(_currentFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			{
				if (Compress && !SkipCompress)
				{
					using (var writer = LZ4Stream.Encode(file, LZ4Level.L00_FAST))
					{
						writer.Write(_memBuffer, 0, (int)_bufferPos);
						writer.Flush();
					}
				}
				else
				{
					file.Write(_memBuffer, 0, (int)_bufferPos);
					file.Flush();
				}
			}

			// Reset
			_bufferPos = 0;
		}

		// Used to directly flush very large arrays directly to the file
		//  !!! flushInternal() should be called before this or the data will get out of order !!!
		private unsafe void flushDirect(byte* data, uint length)
		{
			OutputSize += length;

			// Synchronously write
			using (var file = File.Open(_currentFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			using (var buffer = new UnmanagedMemoryStream(data, length))
			{
				if (Compress && !SkipCompress)
				{
					using (var writer = LZ4Stream.Encode(file, LZ4Level.L00_FAST))
					{
						buffer.CopyTo(writer);
						writer.Flush();
					}
				}
				else
				{
					buffer.CopyTo(file);
					file.Flush();
				}
			}

			// Reset
			_bufferPos = 0;
		}
	}
}
