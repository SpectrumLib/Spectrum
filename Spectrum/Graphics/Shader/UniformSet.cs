using System;

namespace Spectrum.Graphics
{
	// Contains information about the uniforms, bindings, and buffer for a single shader uniform set
	internal class UniformSet
	{

		// Holds information for a uniform binding, which maps to an offset and range in a uniform buffer
		public struct Binding
		{
			public string Name;
			public uint Offset;
			public uint Size;
			public uint Index;
			public bool IsBlock; // true = block, false = opaque handle
		}
	}
}
