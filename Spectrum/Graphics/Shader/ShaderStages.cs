using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Represents the different shader stages for graphics shaders. Can be used as a set of flags.
	/// </summary>
	[Flags]
	public enum ShaderStages : byte
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

	/// <summary>
	/// Contains extension functionality for <see cref="ShaderStages"/> values.
	/// </summary>
	public static class ShaderStagesExtensions
	{
		/// <summary>
		/// Checks if the set of shader stages contains the specific set of shader stages.
		/// </summary>
		/// <param name="flags">The set of stage flags to check.</param>
		/// <param name="stages">The stages to check the flags for.</param>
		/// <returns>If the stages are all contained in the flags. Returns false for partial matches.</returns>
		public static bool HasStages(this ShaderStages flags, ShaderStages stages) => (flags & stages) == stages;

		/// <summary>
		/// Checks if the set of shader stages contains at least one from the set of shader stages.
		/// </summary>
		/// <param name="flags">The set of stage flags to check.</param>
		/// <param name="stages">The stages to check the flags for.</param>
		/// <returns>If the stages are all contained in the flags. Returns true for partial matches.</returns>
		public static bool HasAnyStages(this ShaderStages flags, ShaderStages stages) => (flags & stages) > 0;

		/// <summary>
		/// Gets the number of stages present in the set of stage flags.
		/// </summary>
		/// <param name="stages">The stage flags to count.</param>
		/// <returns>The number of stages present in the stage set.</returns>
		public static uint StageCount(this ShaderStages stages) =>
			(uint)(((byte)stages & 0x01) + (((byte)stages & 0x02) >> 1) + (((byte)stages & 0x04) >> 2) + (((byte)stages & 0x08) >> 3) + (((byte)stages & 0x10) >> 4));

		/// <summary>
		/// Checks if the shader stages is valid (contains at least the vertex shader).
		/// </summary>
		/// <param name="stages">The stage flags to check.</param>
		/// <returns>If the set of stages represents a valid Vulkan shader.</returns>
		public static bool IsValid(this ShaderStages stages) => (stages & ShaderStages.Vertex) > 0;
	}
}
