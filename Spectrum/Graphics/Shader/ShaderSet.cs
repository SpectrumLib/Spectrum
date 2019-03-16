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
		/// Builds a new shader by combing multiple modules. Note that no validity checking is made for the set of
		/// modules, outside of ensuring that it at least has a vertex shader. If any of the modules specified do not
		/// exist in the shader set, an exception is thrown. Any null module is not included in the shader.
		/// </summary>
		/// <param name="name">The name of the new shader.</param>
		/// <param name="vert">The name of the module to use as the vertex shader stage. Cannot be null.</param>
		/// <param name="frag">The name of the module to use as the fragment shader stage. Can be null.</param>
		/// <param name="tesc">The name of the module to use as the tessellation control stage. Can be null.</param>
		/// <param name="tese">The name of the module to use as the tesselation evaluation stage. Can be null.</param>
		/// <param name="geom">The name of the module to use as the geometry shader stage. Can be null.</param>
		/// <param name="cache">If this shader set should cache this new shader for later use.</param>
		/// <returns>An object describing the new shader that should be used with <see cref="PipelineBuilder"/> instances.</returns>
		public PipelineShader CreateShader(string name, string vert, string frag, string tesc = null, string tese = null, string geom = null, bool cache = false)
		{
			if (cache && String.IsNullOrWhiteSpace(name))
				throw new ArgumentException($"A shader cannot have a name that is empty or whitespace.", nameof(name));
			if (cache && _shaders.ContainsKey(name))
				throw new ArgumentException($"A shader with the name '{name}' already exists in this shader set.", nameof(name));
			if (String.IsNullOrEmpty(vert))
				throw new ArgumentException($"A shader is required to have a vertex shading stage.", nameof(vert));

			ShaderStages stages = ShaderStages.Vertex |
				(String.IsNullOrEmpty(frag) ? 0 : ShaderStages.Fragment) |
				(String.IsNullOrEmpty(tesc) ? 0 : ShaderStages.TessControl) |
				(String.IsNullOrEmpty(tese) ? 0 : ShaderStages.TessEval) |
				(String.IsNullOrEmpty(geom) ? 0 : ShaderStages.Geometry);
			var modCount = stages.StageCount();
			Vk.ShaderModule[] vkmods = new Vk.ShaderModule[modCount];
			(string, ShaderStages)[] mods = new (string, ShaderStages)[modCount];

			int mi = 0;
			if ((stages & ShaderStages.Vertex) > 0)
			{
				var mod = getAndValidateModule(vert, ShaderStages.Vertex);
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if ((stages & ShaderStages.TessControl) > 0)
			{
				var mod = getAndValidateModule(tesc, ShaderStages.TessControl);
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if ((stages & ShaderStages.TessEval) > 0)
			{
				var mod = getAndValidateModule(tese, ShaderStages.TessEval);
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if ((stages & ShaderStages.Geometry) > 0)
			{
				var mod = getAndValidateModule(geom, ShaderStages.Geometry);
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}
			if ((stages & ShaderStages.Fragment) > 0)
			{
				var mod = getAndValidateModule(frag, ShaderStages.Fragment);
				vkmods[mi] = mod.Module;
				mods[mi++] = (mod.Name, mod.Stage);
			}

			if (cache)
			{
				_shaders.Add(name, new SSShader {
					Name = name,
					Stages = stages,
					Vert = stages.HasStages(ShaderStages.Vertex) ? (uint)_modules.FindIndex(md => md.Name == vert) : (uint?)null,
					Tesc = stages.HasStages(ShaderStages.TessControl) ? (uint)_modules.FindIndex(md => md.Name == tesc) : (uint?)null,
					Tese = stages.HasStages(ShaderStages.TessEval) ? (uint)_modules.FindIndex(md => md.Name == tese) : (uint?)null,
					Geom = stages.HasStages(ShaderStages.Geometry) ? (uint)_modules.FindIndex(md => md.Name == geom) : (uint?)null,
					Frag = stages.HasStages(ShaderStages.Fragment) ? (uint)_modules.FindIndex(md => md.Name == frag) : (uint?)null
				});
			}

			return new PipelineShader(name, stages, vkmods, mods);
		}

		private SSModule getAndValidateModule(string name, ShaderStages stage)
		{
			var idx = _modules.FindIndex(md => md.Name == name);
			if (idx == -1)
				throw new ArgumentException($"A module with the name '{name}' does not exist in this shader set.");
			var mod = _modules[idx];
			if (mod.Stage != stage)
				throw new ArgumentException($"The module '{name}' cannot be used as a {stage} stage.");
			return mod;
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
