using System;

namespace Spectrum.Graphics
{
	// Contains information about the uniforms, bindings, and buffer for a single shader uniform set
	internal class UniformSet
	{
		#region Fields
		public readonly Block[] Blocks;
		#endregion // Fields

		public UniformSet(Block[] blocks)
		{
			Blocks = blocks;
		}

		// Holds information for a uniform
		public struct Uniform
		{
			public string Name;
			public uint Offset;
			public uint Size;
			public uint Index;
		}

		// Holds information for a uniform block
		public struct Block
		{
			public string Name;
			public uint Binding;
			public uint Offset;
			public uint Size;
		}
	}
}
