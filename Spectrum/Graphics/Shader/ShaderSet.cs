using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Contains a set of shaders loaded as content from the disk.
	/// </summary>
	public class ShaderSet
	{

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
			public byte[] ByteCode;
		}
	}
}
