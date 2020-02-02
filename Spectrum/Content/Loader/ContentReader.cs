/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Text;

namespace Spectrum.Content
{
	/// <summary>
	/// Extension of the <see cref="BinaryReader"/> type for loading content item data from streams. Note that seeking
	/// is supported, but is expensive for compressed streams (particularly backwards seeking).
	/// </summary>
	public sealed class ContentReader : BinaryReader
	{
		#region Fields
		internal ContentStream ContentStream => BaseStream as ContentStream;

		/// <summary>
		/// The total number of bytes available in the content item.
		/// </summary>
		public ulong DataSize => ContentStream.DataSize;
		/// <summary>
		/// The current offset of the reader into the content item data stream.
		/// </summary>
		public ulong Position
		{
			get => (ulong)BaseStream.Position;
			set => ContentStream.Position = (long)value;
		}
		/// <summary>
		/// If the content item data is compressed.
		/// </summary>
		public bool IsCompressed => ContentStream.IsCompressed;
		/// <summary>
		/// The number of bytes remaining in the content item stream.
		/// </summary>
		public ulong Remaining => ContentStream.DataSize - (ulong)ContentStream.Position;
		#endregion // Fields

		internal ContentReader(ContentStream stream) :
			base(stream, Encoding.UTF8, false)
		{ }

		/// <summary>
		/// Creates a new reader instance which reads from the same content item. This function can be used to allow
		/// delayed read operations, such as streaming from the disk.
		/// </summary>
		/// <returns>A duplicate instance of this reader, set to the beginning of the stream.</returns>
		public ContentReader Duplicate() => new ContentReader(ContentStream.Duplicate());

		/// <summary>
		/// Seeks the underlying stream. Note that seeks are expensive in compressed data.
		/// </summary>
		/// <param name="offset">The offset, in bytes, of the seek.</param>
		/// <param name="origin">The origin point of the seek offset.</param>
		/// <returns>The new position within the stream after the seek.</returns>
		public ulong Seek(long offset, SeekOrigin origin) => (ulong)ContentStream.Seek(offset, origin);

		/// <summary>
		/// Resets the stream to the beginning. This is a slightly faster <c>Seek(0, SeekOrigin.Begin)</c>.
		/// </summary>
		public void Reset() => ContentStream.Reset();

		/// <summary>
		/// Reads a sequence of value types from the stream, and advances the stream.
		/// </summary>
		/// <typeparam name="T">The value type to read.</typeparam>
		/// <param name="buffer">The buffer to read data into.</param>
		/// <returns>The number of bytes read.</returns>
		public int Read<T>(Span<T> buffer) where T : struct => Read(buffer.AsBytes());

		/// <summary>
		/// Not implemented, due to how expensive seeking is in compressed data.
		/// </summary>
		[Obsolete("PeekChar is not implemented for ContentReader", true)]
		public new int PeekChar() => throw new NotImplementedException();
	}
}
