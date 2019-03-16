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
		private readonly List<SSModule> _modules;

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal ShaderSet(SSShader[] ss, SSModule[] ms)
		{
			_shaders = ss.ToDictionary(sh => sh.Name);
			_modules = new List<SSModule>(ms);
		}
		~ShaderSet()
		{
			dispose(false);
		}

		/// <summary>
		/// Gets an object which describes a shader that can be used in a <see cref="PipelineBuilder"/> to create
		/// pipelines with. If a shader with the name does not exist, an exception is thrown.
		/// </summary>
		/// <param name="name">The name of the shader to get.</param>
		/// <returns>An object that should be passed to a <see cref="PipelineBuilder"/> instance.</returns>
		public PipelineShader GetShader(string name)
		{
			if (!_shaders.ContainsKey(name))
				throw new ArgumentException($"A shader with the name '{name}' does not exist in this shader set.", nameof(name));

			var shader = _shaders[name];
			var modCount = shader.Stages.StageCount();
			Vk.ShaderModule[] vkmods = new Vk.ShaderModule[modCount];
			(string, ShaderStages)[] mods = new (string, ShaderStages)[modCount];

			int mi = 0;
			if (shader.Vert.HasValue)
			{
				var mod = _modules[(int)shader.Vert.Value];
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if (shader.Tesc.HasValue)
			{
				var mod = _modules[(int)shader.Tesc.Value];
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if (shader.Tese.HasValue)
			{
				var mod = _modules[(int)shader.Tese.Value];
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if (shader.Geom.HasValue)
			{
				var mod = _modules[(int)shader.Geom.Value];
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if (shader.Frag.HasValue)
			{
				var mod = _modules[(int)shader.Frag.Value];
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}

			return new PipelineShader(shader, vkmods, mods);
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
			throw new ArgumentException($"A shader with the name '{name}' does not exist in this shader set.", nameof(name));

		/// <summary>
		/// Checks if this shader set contains a module with the given name, and optionally if the module is for
		/// the given shader stage.
		/// </summary>
		/// <param name="name">The name of the module to search for.</param>
		/// <param name="stage">If not null, also checks if the module is for the given stage.</param>
		/// <returns>If a matching module exists in this shader set.</returns>
		public bool HasModule(string name, ShaderStages? stage = null)
		{
			var found = _modules.FirstOrDefault(mod => {
				if (mod.Name == name)
					return stage.HasValue ? (mod.Stage == stage.Value) : true;
				return false;
			});
			return (found.Name != null); // A null name is the default = not found
		}

		/// <summary>
		/// Gets the shader stage implemented by the module with the given name. Throws an exception if a modules with
		/// the name does not exist in the shader set.
		/// </summary>
		/// <param name="name">The module name to check.</param>
		/// <returns>The stage implemented by the module.</returns>
		public ShaderStages GetModuleStage(string name)
		{
			var found = _modules.FirstOrDefault(mod => (mod.Name == name));
			return (found.Name != null) ? found.Stage :
				throw new ArgumentException($"A module with the name '{name}' does not exist in this shader set.", nameof(name));
		}

		/// <summary>
		/// Gets an enumerator over the shaders in this shader set, giving the name and stages for each shader.
		/// </summary>
		/// <returns>An enumerator over the shaders.</returns>
		public IEnumerable<(string Name, ShaderStages Stages)> GetShaders() => _shaders.Select(pair => (pair.Key, pair.Value.Stages));

		/// <summary>
		/// Gets an enumerator over the modules in this shader set, giving the name and stage for each module.
		/// </summary>
		/// <returns>An enumerator over the modules.</returns>
		public IEnumerable<(string Name, ShaderStages Stage)> GetModules() => _modules.Select(mod => (mod.Name, mod.Stage));

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
				foreach (var mod in _modules)
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
			public uint? Vert;
			public uint? Tesc;
			public uint? Tese;
			public uint? Geom;
			public uint? Frag;
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
