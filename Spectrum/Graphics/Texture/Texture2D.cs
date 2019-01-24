using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Texture type that holds a single 2D plane of pixel data. The most common type of texture.
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
	}
}
