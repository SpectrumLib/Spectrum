using System;
using System.Collections.Generic;
using System.Linq;
using Spectrum.Content;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Contains a set of shaders loaded as content from the disk.
	/// </summary>
	public class ShaderSet : IDisposableContent
	{
		#region Fields
		// Shaders and modules
		private readonly Dictionary<string, SSShader> _shaders;
		private readonly Dictionary<string, SSModule> _modules;

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal ShaderSet(SSShader[] ss, SSModule[] ms)
		{
			_shaders = ss.ToDictionary(sh => sh.Name);
			_modules = ms.ToDictionary(mod => mod.Name);
		}
		~ShaderSet()
		{
			dispose(false);
		}

		/// <summary>
		/// Checks if this shader set contains a shader with the given name.
		/// </summary>
		/// <param name="name">The shader name to check for.</param>
		/// <returns>If the shader set contains the name.</returns>
		public bool HasShader(string name) => _shaders.ContainsKey(name);

		/// <summary>
		/// Gets the shader stages present in the shader with the given name. Throws an exception if a shader with
		/// the name does not exist in the shader set.
		/// </summary>
		/// <param name="name">The shader name to check.</param>
		/// <returns>The stages that are present in the shader.</returns>
		public ShaderStages GetShaderStages(string name) =>
			_shaders.ContainsKey(name) ? _shaders[name].Stages :
			throw new ArgumentException($"A shader with the name {name} does not exist in this shader set.", nameof(name));

		/// <summary>
		/// Checks if this shader set contains a module with the given name, and optionally if the module is for
		/// the given shader stage.
		/// </summary>
		/// <param name="name">The name of the module to search for.</param>
		/// <param name="stage">If not null, also checks if the module is for the given stage.</param>
		/// <returns>If a matching module exists in this shader set.</returns>
		public bool HasModule(string name, ShaderStages? stage = null) =>
			_modules.ContainsKey(name) ? (stage.HasValue ? _modules[name].Stage == stage : true) : false;

		/// <summary>
		/// Gets the shader stage implemented by the module with the given name. Throws an exception if a modules with
		/// the name does not exist in the shader set.
		/// </summary>
		/// <param name="name">The module name to check.</param>
		/// <returns>The stage implemented by the module.</returns>
		public ShaderStages GetModuleStage(string name) =>
			_modules.ContainsKey(name) ? _modules[name].Stage :
			throw new ArgumentException($"A module with the name {name} does not exist in this shader set.", nameof(name));

		/// <summary>
		/// Gets an enumerator over the shaders in this shader set, giving the name and stages for each shader.
		/// </summary>
		/// <returns>An enumerator over the shaders.</returns>
		public IEnumerable<(string Name, ShaderStages Stages)> GetShaders() => _shaders.Select(pair => (pair.Key, pair.Value.Stages));

		/// <summary>
		/// Gets an enumerator over the modules in this shader set, giving the name and stage for each module.
		/// </summary>
		/// <returns>An enumerator over the modules.</returns>
		public IEnumerable<(string Name, ShaderStages Stage)> GetModules() => _modules.Select(pair => (pair.Key, pair.Value.Stage));

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed && disposing)
			{
				foreach (var mod in _modules.Values)
					mod.Module.Dispose();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Holds information about a shader from a shader set
		internal struct SSShader
		{
			public string Name;
			public ShaderStages Stages;
			public uint Vert;
			public uint Tesc;
			public uint Tese;
			public uint Geom;
			public uint Frag;
		}

		// Holds information about a module
		internal struct SSModule
		{
			public string Name;
			public string EntryPoint;
			public ShaderStages Stage;
			public Vk.ShaderModule Module;
		}
	}
}
