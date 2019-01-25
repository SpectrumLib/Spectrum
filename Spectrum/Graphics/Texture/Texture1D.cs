using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Texture type that holds a single 1D strip of pixel data.
	/// </summary>
	public sealed class Texture1D : Texture
	{
		/// <summary>
		/// Creates a new 1D texture.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		public Texture1D(uint width) :
			base(TextureType.Texture1D, width, 1, 1, 1)
		{ }
	}
}
