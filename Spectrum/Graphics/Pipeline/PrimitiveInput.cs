/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the topology of vertex input for a pipeline.
	/// </summary>
	public struct PrimitiveInput
	{
		#region Predefined States
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
		#endregion // Predefined States

		#region Fields
		/// <summary>
		/// The type that the vertices will be assembled into.
		/// </summary>
		public PrimitiveType Type;
		/// <summary>
		/// If primitive restart is enabled for indexed rendering (using 0xFFFF or 0xFFFFFFFF).
		/// </summary>
		public bool Restart;

		/// <summary>
		/// If the topology is a list type. If <c>true</c>, then <see cref="Restart"/> must be <c>false</c>.
		/// </summary>
		public readonly bool IsListType =>
			Type == PrimitiveType.PointList || Type == PrimitiveType.LineList || Type == PrimitiveType.TriangleList;
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.PipelineInputAssemblyStateCreateInfo ToVulkanType() => new Vk.PipelineInputAssemblyStateCreateInfo { 
			Topology = (Vk.PrimitiveTopology)Type,
			PrimitiveRestartEnable = Restart
		};

		// Casting from topology enums
		public static implicit operator PrimitiveInput(PrimitiveType type) => new PrimitiveInput(type, false);
		public static implicit operator PrimitiveInput(Vk.PrimitiveTopology topo) => new PrimitiveInput((PrimitiveType)topo, false);
	}

	/// <summary>
	/// Describes the different primitives that vertices can be assembled into.
	/// </summary>
	public enum PrimitiveType
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
