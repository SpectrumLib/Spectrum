using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Texture type that holds a 3D space of pixel data.
	/// </summary>
	public sealed class Texture3D : Texture
	{
		/// <summary>
		/// Creates a new 2D texture.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="depth">The depth of the texture.</param>
		public Texture3D(uint width, uint height, uint depth) :
			base(TextureType.Texture3D, width, height, depth, 1)
		{ }
	}
}
