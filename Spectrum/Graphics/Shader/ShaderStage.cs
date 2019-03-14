using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Represents the different shader stages for graphics shaders. Can be used as a set of flags.
	/// </summary>
	[Flags]
	public enum ShaderStage : byte
	{
		/// <summary>
		/// Vertex shader stage.
		/// </summary>
		Vertex = 0x01,
		/// <summary>
		/// Tessellation control stage.
		/// </summary>
		TessControl = 0x02,
		/// <summary>
		/// Tessellation evaluation stage.
		/// </summary>
		TessEval = 0x04,
		/// <summary>
		/// Geometry shader stage.
		/// </summary>
		Geometry = 0x08,
		/// <summary>
		/// Fragment shader stage.
		/// </summary>
		Fragment = 0x10
	}
}
