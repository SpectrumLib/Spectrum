using System;
using System.Collections.Generic;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{ 
	/// <summary>
	/// Lightweight type for communicating shader information between <see cref="Pipeline"/> instances and
	/// <see cref="ShaderSet"/> instances. Does not interact with the graphics device and does not contain information
	/// related to active shaders.
	/// </summary>
	public sealed class PipelineShader
	{
		#region Fields
		/// <summary>
		/// The name of the shader.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The stages present in the shader.
		/// </summary>
		public readonly ShaderStages Stages;
		/// <summary>
		/// The set of stage modules in this shader.
		/// </summary>
		public readonly IReadOnlyList<(string Name, ShaderStages Stage)> Modules;

		internal readonly Vk.ShaderModule[] VkModules;
		#endregion // Fields

		internal PipelineShader(ShaderSet.SSShader shader, Vk.ShaderModule[] vkMods, (string, ShaderStages)[] mods)
		{
			Name = shader.Name;
			Stages = shader.Stages;

			VkModules = vkMods;
			Modules = mods;
		}
	}
}
