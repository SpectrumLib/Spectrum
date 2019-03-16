using System;
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
		private readonly SSShader[] _shaders;
		private readonly SSModule[] _modules;

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal ShaderSet(SSShader[] ss, SSModule[] ms)
		{
			_shaders = ss;
			_modules = ms;
		}
		~ShaderSet()
		{
			dispose(false);
		}

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
			public ShaderStage Stages;
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
			public ShaderStage Stage;
			public Vk.ShaderModule Module;
		}
	}
}
