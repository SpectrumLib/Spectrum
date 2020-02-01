/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4.Streams;


namespace Spectrum.Content
{
	// Wrapper around a potentially compressed stream, including file subsections
	// Note that for compressed streams, seeking backward is a very expensive operation
	internal sealed class ContentStream : Stream
	{
		private const int LZ4_EXTRA_MEM = 4096;

		#region Fields
		public readonly FileInfo File;
		public readonly ContentPack.Entry Item;
		public ulong FileOffset => Item.Offset; // Offset into source file (bytes)
		public ulong DataSize => Item.DataSize; // Total uncompressed data size (bytes)
		public ulong BinSize => Item.BinSize; // Total compressed data size, as it exists in the file (bytes)
		public bool IsCompressed => Item.Compress; // If the item data is compressed

		private readonly FileStream _fileStream;
		private LZ4DecoderStream _codeStream;
		private ulong _position;

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => (long)DataSize;
		public override long Position
		{
			get => (long)_position;
			set => Seek(value, SeekOrigin.Begin);
		}

		private bool _isDisposed = false;
		#endregion // Fields

		public ContentStream(bool release, string root, ContentPack.Entry item)
		{
			Item = item;

			var path = Path.Combine(root, release ? $"{item.BinIndex}.cbin" : $"{item.Name}.cbin");
			if (!PathUtils.TryGetFileInfo(path, out File))
				throw new ContentLoadException(item.Name, "Failed to touch content item file");

			try
			{
				_fileStream = File.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
				_fileStream.Position = (long)item.Offset;
				_codeStream = item.Compress ? LZ4Stream.Decode(_fileStream, LZ4_EXTRA_MEM, true) : null;
			}
			catch (Exception e)
			{
				throw new ContentLoadException(Item.Name, "Failed to open stream to content item", e);
			}

			_position = 0;
		}
		private ContentStream(FileInfo finfo, ContentPack.Entry item)
		{
			Item = item;
			File = finfo;

			try
			{
				_fileStream = File.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
				_fileStream.Position = (long)item.Offset;
				_codeStream = item.Compress ? LZ4Stream.Decode(_fileStream, LZ4_EXTRA_MEM, true) : null;
			}
			catch (Exception e)
			{
				throw new ContentLoadException(Item.Name, "Failed to duplicate stream to content item", e);
			}

			_position = 0;
		}
		~ContentStream()
		{
			Dispose(false);
		}

		// Creates a copy of this content stream, open to the beginning of the content data
		public ContentStream Duplicate()
		{
			checkDisposed();
			return new ContentStream(new FileInfo(File.FullName), Item);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			checkDisposed();
			if ((_position + (ulong)count) >= DataSize)
				throw new ContentLoadException(Item.Name, "Attempted to read past end of content item data");

			try
			{
				var amt = ((Stream)_codeStream ?? _fileStream).Read(buffer, offset, count);
				_position += (ulong)amt;
				return amt;
			}
			catch (Exception e)
			{
				throw new ContentLoadException(Item.Name, "Failed to read content data", e);
			}
		}

		public override int Read(Span<byte> buffer)
		{
			checkDisposed();
			if ((_position + (ulong)buffer.Length) >= DataSize)
				throw new ContentLoadException(Item.Name, "Attempted to read past end of content item data");

			try
			{
				var amt = ((Stream)_codeStream ?? _fileStream).Read(buffer);
				_position += (ulong)amt;
				return amt;
			}
			catch (Exception e)
			{
				throw new ContentLoadException(Item.Name, "Failed to read content data", e);
			}
		}

		public override int ReadByte()
		{
			checkDisposed();
			if (_position == DataSize)
				throw new ContentLoadException(Item.Name, "Read past end of content item data");

			try
			{
				var val = ((Stream)_codeStream ?? _fileStream).ReadByte();
				_position += 1;
				return val;
			}
			catch (Exception e)
			{
				throw new ContentLoadException(Item.Name, "Failed to read content data", e);
			}
		}

		public unsafe override long Seek(long offset, SeekOrigin origin)
		{
			checkDisposed();

			// Check and calculate offsets
			(long diff, long pos) = origin switch { 
				SeekOrigin.Begin => (offset - (long)_position, offset),
				SeekOrigin.End => ((long)DataSize + offset - (long)_position, (long)DataSize + offset),
				SeekOrigin.Current => (offset, (long)_position + offset),
				_ => (0, 0)
			};
			if (diff == 0)
				return (long)_position;
			if (pos < 0)
				throw new ContentLoadException(Item.Name, "Seek past beginning of content item data");
			if (pos > (long)DataSize)
				throw new ContentLoadException(Item.Name, "Seek past end of content item data");

			if (IsCompressed)
			{
				if (diff < 0) // Reset stream first, for backwards seeks
				{
					Reset();
					diff = pos;
				}

				Span<byte> buffer = stackalloc byte[512];
				while (diff >= buffer.Length)
					diff -= _codeStream.Read(buffer);
				if (diff > 0)
					_codeStream.Read(buffer.Slice(0, (int)diff));
			}
			else
				_fileStream.Seek(diff, SeekOrigin.Current);

			_position = (ulong)pos;
			return (long)_position;
		}

		public override void Close()
		{
			checkDisposed();
			_codeStream?.Close();
			_fileStream.Close();
			base.Close();
		}

		// Seeks to the beginning of the content data
		public void Reset()
		{
			checkDisposed();
			_codeStream?.Dispose();
			_fileStream.Position = (long)Item.Offset;
			_codeStream = IsCompressed ? LZ4Stream.Decode(_fileStream, LZ4_EXTRA_MEM, true) : null;
			_position = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void checkDisposed()
		{
			if (_isDisposed)
				throw new ContentLoadException(Item.Name, "The content stream was disposed");
		}

		#region Unsupported/Nop
		public override void SetLength(long value) => throw new NotImplementedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
		public override void Flush() { }
		public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		#endregion // Unsupported/Nop

		protected override void Dispose(bool disposing)
		{
			if (!_isDisposed && disposing)
			{
				_codeStream?.Dispose();
				_fileStream.Dispose();
			}
			_isDisposed = true;
			base.Dispose(disposing);
		}
	}
}
