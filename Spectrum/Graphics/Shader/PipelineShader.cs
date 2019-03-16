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

		internal readonly (Vk.ShaderModule Module, string Entry)[] VkModules;
		#endregion // Fields

		internal PipelineShader(ShaderSet.SSShader shader, (Vk.ShaderModule, string)[] vkMods, (string, ShaderStages)[] mods) :
			this(shader.Name, shader.Stages, vkMods, mods)
		{ }

		internal PipelineShader(string name, ShaderStages stages, (Vk.ShaderModule, string)[] vkMods, (string, ShaderStages)[] mods)
		{
			Name = name;
			Stages = stages;

			VkModules = vkMods;
			Modules = mods;
		}

		internal Vk.PipelineShaderStageCreateInfo[] ToCreateInfos()
		{
			var scis = new Vk.PipelineShaderStageCreateInfo[VkModules.Length];

			for (int i = 0; i < VkModules.Length; ++i)
			{
				scis[i].Name = VkModules[i].Entry;
				scis[i].Module = VkModules[i].Module;
				scis[i].SpecializationInfo = null;
				scis[i].Stage = (Vk.ShaderStages)Modules[i].Stage;
			}

			return scis;
		}
	}
}
