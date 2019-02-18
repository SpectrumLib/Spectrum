using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;

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
		private readonly byte[] _memBuffer;
		private readonly MemoryStream _memStream;
		private uint _bufferPos => (uint)_memStream.Position;
		private readonly BinaryWriter _writer;

		// The size (in bytes) of the file written by this stream, not valid until the final Flush() is called
		internal uint OutputSize { get; private set; } = 0;

		// The UTF8 encoding and associated encoder used to get string and character bytes
		private readonly Encoding _encoding;
		private readonly Encoder _encoder;

		// The compression type to use
		internal readonly bool Compress;
		internal bool SkipCompress = false;

		// The current async write task, if any. The task will return the compressed size of the file
		private Task<uint> _writeTask = null;
		// The absolute path to the current output file
		private string _currentFile = null;

		// The underlying streams (need to be declared here, as we cannot close a LZ4 stream part-way through
		//   or else the compression will fail)
		private FileStream _file;
		private LZ4EncoderStream _compressor;
		#endregion // Fields

		internal ContentStream(bool compress)
		{
			_memBuffer = new byte[BUFFER_SIZE];
			_memStream = new MemoryStream(_memBuffer);
			_writer = new BinaryWriter(_memStream);
			_encoding = new UTF8Encoding(
				encoderShouldEmitUTF8Identifier: false,
				throwOnInvalidBytes: true
			);
			Compress = compress;
			_encoder = _encoding.GetEncoder();
		}

		// Waits on the old write task, then resets the type to start recoding at the beginning of the buffer
		// This will return the compressed size of the previous item, or zero if it was not compressed
		internal uint Reset(string path, bool skipCompression)
		{
			uint usize = _writeTask?.Result ?? 0; // The "Result" property blocks until the task is finished
			_memStream.Seek(0, SeekOrigin.Begin);
			OutputSize = 0;
			_currentFile = path;
			SkipCompress = skipCompression;

			// Create the new streams (if there is a file)
			if (path != null)
			{
				_file = File.Open(_currentFile, FileMode.Create, FileAccess.Write, FileShare.None);
				if (Compress && !SkipCompress)
					_compressor = LZ4Stream.Encode(_file, LZ4Level.L00_FAST, leaveOpen: true);
				else
					_compressor = null; 
			}
			else
			{
				_file = null;
				_compressor = null;
			}

			return usize;
		}

		// Called by the task when it is done with this writer, launches the async write
		internal void Flush()
		{
			OutputSize += _bufferPos;

			_writeTask = Task<uint>.Factory.StartNew(() => {
				if (Compress && !SkipCompress)
				{
					_compressor.Write(_memBuffer, 0, (int)_bufferPos);
					_compressor.Flush();
					_compressor.Close();
					_file.Flush();
				}
				else
				{
					_file.Write(_memBuffer, 0, (int)_bufferPos);
					_file.Flush();
				}

				var size = (uint)_file.Position;
				_compressor?.Dispose();
				_file?.Dispose();

				return size;
			});
		}

		// Called by write functions when the backing buffer is full to synchronously write the buffer out and reset it
		private void flushInternal()
		{
			OutputSize += _bufferPos;

			// Synchronously write
			if (Compress && !SkipCompress)
			{
				_compressor.Write(_memBuffer, 0, (int)_bufferPos);
				_compressor.Flush();
			}
			else
			{
				_file.Write(_memBuffer, 0, (int)_bufferPos);
				_file.Flush();
			}

			// Reset
			_memStream.Seek(0, SeekOrigin.Begin);
		}

		// Used to directly flush very large arrays directly to the file
		//  !!! flushInternal() should be called before this or the data will get out of order !!!
		private unsafe void flushDirect(byte* data, uint length)
		{
			OutputSize += length;

			// Synchronously write
			using (var buffer = new UnmanagedMemoryStream(data, length))
			{
				if (Compress && !SkipCompress)
				{
					buffer.CopyTo(_compressor);
					_compressor.Flush();
				}
				else
				{
					buffer.CopyTo(_file);
					_file.Flush();
				}
			}

			// Reset
			_memStream.Seek(0, SeekOrigin.Begin);
		}

		// Used to directly flush very large strings directly to the file, this function will only be called in instances
		//  where millions of characters are being written at once
		//  !!! flushInternal() should be called before this or the data will get out of order !!!
		private void flushDirect(string str)
		{
			// Synchronously write
			bool c = (Compress && !SkipCompress);
			uint size = 0;
			using (var directWriter = new BinaryWriter(c ? (Stream)_compressor : (Stream)_file))
			{
				uint pos = (uint)directWriter.BaseStream.Position;
				directWriter.Write(str);
				directWriter.BaseStream.Flush();
				size = (uint)directWriter.BaseStream.Position - pos;
			}
			OutputSize += size;

			// Reset
			_memStream.Seek(0, SeekOrigin.Begin);
		}

		// Used to directly flush very large character arrays directly to the file, this function will only be called in instances
		//  where millions of characters are being written at once
		//  !!! flushInternal() should be called before this or the data will get out of order !!!
		private void flushDirect(char[] chars, int off, int len)
		{
			// Synchronously write
			bool c = (Compress && !SkipCompress);
			uint size = 0;
			using (var directWriter = new BinaryWriter(c ? (Stream)_compressor : (Stream)_file))
			{
				uint pos = (uint)directWriter.BaseStream.Position;
				directWriter.Write(chars, off, len);
				directWriter.BaseStream.Flush();
				size = (uint)directWriter.BaseStream.Position - pos;
			}
			OutputSize += size;

			// Reset
			_memStream.Seek(0, SeekOrigin.Begin);
		}
	}
}
