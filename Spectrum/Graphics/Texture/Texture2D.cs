using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Texture type that holds a single 2D plane of pixel data.
	/// </summary>
	public sealed class Texture2D : Texture
	{
		/// <summary>
		/// Creates a new 2D texture.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		public Texture2D(uint width, uint height) :
			base(TextureType.Texture2D, width, height, 1, 1)
		{

		}

		/// <summary>
		/// Sets the data for the entire texture at once.
		/// </summary>
		/// <typeparam name="T">The type of the source texel data.</typeparam>
		/// <param name="data">The source data.</param>
		/// <param name="offset">The optional offset into the source array.</param>
		public void SetData<T>(T[] data, uint offset = 0)
			where T : struct =>
			SetData(data, offset, (0, 0, Width, Height), 0, 1);

		/// <summary>
		/// Sets the data for a subset region of the texture.
		/// </summary>
		/// <typeparam name="T">The type of the source texel data.</typeparam>
		/// <param name="data">The source data.</param>
		/// <param name="region">The region of the image to set the data in.</param>
		/// <param name="offset">The optional offset into the source array.</param>
		public void SetData<T>(T[] data, in TextureRegion region, uint offset = 0)
			where T : struct =>
			SetData(data, offset, region, 0, 1);

		/// <summary>
		/// Retrieves the data of the entire image at once into a buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination texel data.</typeparam>
		/// <param name="data">
		/// The array to place the data into, or null to have a correctly-size array created automatically. Any new
		/// array will be large enough to take into account passed offset.
		/// </param>
		/// <param name="offset">The optional offset into the destination array.</param>
		public void GetData<T>(ref T[] data, uint offset = 0)
			where T : struct =>
			GetData(ref data, offset, (0, 0, Width, Height), 0, 1);

		/// <summary>
		/// Retrieves the data of a subset of the image into a buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination texel data.</typeparam>
		/// <param name="data">
		/// The array to place the data into, or null to have a correctly-size array created automatically. Any new
		/// array will be large enough to take into account passed offset.
		/// </param>
		/// <param name="region">The subset region of the image to pull data from.</param>
		/// <param name="offset">The optional offset into the destination array.</param>
		public void GetData<T>(ref T[] data, in TextureRegion region, uint offset = 0)
			where T : struct =>
			GetData(ref data, offset, region, 0, 1);
	}
}
