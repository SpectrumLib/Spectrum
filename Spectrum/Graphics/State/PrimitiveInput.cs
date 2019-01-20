using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the topology of vertex input for a render pipeline.
	/// </summary>
	public struct PrimitiveInput
	{
		/// <summary>
		/// Vertices are assembled as a point list without restart.
		/// </summary>
		public static readonly PrimitiveInput PointList = new PrimitiveInput(PrimitiveType.PointList, false);
		/// <summary>
		/// Vertices are assembled as a line list without restart.
		/// </summary>
		public static readonly PrimitiveInput LineList = new PrimitiveInput(PrimitiveType.LineList, false);
		/// <summary>
		/// Vertices are assembled as a line strip without restart.
		/// </summary>
		public static readonly PrimitiveInput LineStrip = new PrimitiveInput(PrimitiveType.LineStrip, false);
		/// <summary>
		/// Vertices are assembled as a triangle list without restart.
		/// </summary>
		public static readonly PrimitiveInput TriangleList = new PrimitiveInput(PrimitiveType.TriangleList, false);
		/// <summary>
		/// Vertices are assembled as a triangle strip without restart.
		/// </summary>
		public static readonly PrimitiveInput TriangleStrip = new PrimitiveInput(PrimitiveType.TriangleStrip, false);
		/// <summary>
		/// Vertices are assembled as a triangle fan without restart.
		/// </summary>
		public static readonly PrimitiveInput TriangleFan = new PrimitiveInput(PrimitiveType.TriangleFan, false);

		#region Fields
		/// <summary>
		/// The type that the vertices will be assembled into.
		/// </summary>
		public PrimitiveType Type;
		/// <summary>
		/// If the primitive assembly type can be restarted with a special index value of 0xFF or 0xFFFF.
		/// </summary>
		public bool Restart;
		#endregion // Fields

		/// <summary>
		/// Creates a new vertex assembly description.
		/// </summary>
		/// <param name="type">The primitive type to assemble the vertices into.</param>
		/// <param name="restart">If primitive restarting should be enabled.</param>
		public PrimitiveInput(PrimitiveType type, bool restart = false)
		{
			Type = type;
			Restart = restart;
		}

		// Easy casting to the pipeline creation type
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vk.PipelineInputAssemblyStateCreateInfo (in PrimitiveInput pi)
			=> new Vk.PipelineInputAssemblyStateCreateInfo((Vk.PrimitiveTopology)pi.Type, pi.Restart);

		// Casting from topology enums
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator PrimitiveInput (PrimitiveType type) => new PrimitiveInput(type, false);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator PrimitiveInput (Vk.PrimitiveTopology topo) => new PrimitiveInput((PrimitiveType)topo, false);
	}

	/// <summary>
	/// Describes the different primitives that vertices can be assembled into.
	/// </summary>
	public enum PrimitiveType : uint
	{
		/// <summary>
		/// Vertices are assembled as a list of points.
		/// </summary>
		PointList = Vk.PrimitiveTopology.PointList,
		/// <summary>
		/// Vertices are assembled as a list of disconnected lines.
		/// </summary>
		LineList = Vk.PrimitiveTopology.LineList,
		/// <summary>
		/// Vertices are assembled as a continuous set of lines.
		/// </summary>
		LineStrip = Vk.PrimitiveTopology.LineStrip,
		/// <summary>
		/// Vertices are assembled as a list of disconnected triangles.
		/// </summary>
		TriangleList = Vk.PrimitiveTopology.TriangleList,
		/// <summary>
		/// Vertices are assembled as a list of triangles, each new triangle sharing an edge with the last.
		/// </summary>
		TriangleStrip = Vk.PrimitiveTopology.TriangleStrip,
		/// <summary>
		/// Vertices are assembled as a fan of triangles, all sharing the first vertex.
		/// </summary>
		TriangleFan = Vk.PrimitiveTopology.TriangleFan
	}
}
