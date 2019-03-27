using System;

namespace Spectrum.Graphics
{
	// Contains information about the uniforms, bindings, and buffer for a single shader uniform set
	internal class UniformSet
	{
		#region Fields
		public readonly Block[] Blocks;
		public readonly uint BufferSize;
		public readonly Uniform[] Uniforms;
		#endregion // Fields

		public UniformSet(Block[] blocks, uint bsize, Uniform[] uniforms)
		{
			Blocks = blocks;
			BufferSize = bsize;
			Uniforms = uniforms;
		}

		public bool TryGetBlock(string name, out Block block)
		{
			foreach (var b in Blocks)
			{
				if (String.CompareOrdinal(b.Name, name) == 0)
				{
					block = b;
					return true;
				}
			}

			block = default;
			return false;
		}

		public bool TryGetUniform(string name, out Uniform uniform)
		{
			foreach (var u in Uniforms)
			{
				if (String.CompareOrdinal(u.Name, name) == 0)
				{
					uniform = u;
					return true;
				}
			}

			uniform = default;
			return false;
		}

		// Holds information for a uniform
		public struct Uniform
		{
			public string Name;       // The name of the uniform
			public uint Binding;      // The binding of the uniform (block binding for uniforms in blocks)
			public uint Offset;       // The offset of the uniform into the uniform buffer for the set (0 if not a block uniform)
			public uint BlockOffset;  // The offset of the uniform into the original block (0 if not a block uniform)
			public bool IsHandle;     // `true` if the uniform is outside of a block (handle type), `false` otherwise (data type)
		}

		// Holds information for a uniform block
		public struct Block
		{
			public string Name;   // Block name
			public uint Binding;  // Block binding point
			public uint Offset;   // Offset into the full uniform buffer allocated for this set layout
			public uint Size;     // Size of the block in the full uniform buffer
		}
	}
}
