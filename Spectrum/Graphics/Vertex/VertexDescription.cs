/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Fully describes shader vertex input data, across all elements, binding buffers, and per-vertex and
	/// per-instance objects.
	/// </summary>
	public struct VertexDescription
	{
		#region Fields
		/// <summary>
		/// The vertex data descriptions for each binding buffer.
		/// </summary>
		public readonly VertexBinding[] Bindings;
		/// <summary>
		/// The total number of elements in this description across all bindings.
		/// </summary>
		public readonly uint ElementCount;
		#endregion // Fields

		/// <summary>
		/// Describes a new vertex from a set of bindings.
		/// </summary>
		/// <param name="bindings">The bindings describing the vertex.</param>
		public VertexDescription(params VertexBinding[] bindings)
		{
			if (bindings.Length == 0)
				throw new ArgumentException("Vertex description with zero bindings.");
			Bindings = bindings.Select(b => b.Copy()).ToArray();
			ElementCount = (uint)Bindings.Sum(b => b.Elements.Length);
		}

		/// <summary>
		/// Simple vertex description, assuming tightly packed elements all sourced from the same buffer.
		/// </summary>
		/// <param name="fmts">Tightly packed formats that make up the vertex.</param>
		public VertexDescription(params VertexElementFormat[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Vertex description with zero bindings.");
			Bindings = new VertexBinding[] { new VertexBinding(fmts) };
			ElementCount = (uint)fmts.Length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal readonly Vk.PipelineVertexInputStateCreateInfo ToVulkanType()
		{
			var bds = Bindings.Select((b, idx) =>
				new Vk.VertexInputBindingDescription((uint)idx, b.Stride, b.PerInstance ? Vk.VertexInputRate.Instance : Vk.VertexInputRate.Vertex)
			).ToArray();
			var ats = new Vk.VertexInputAttributeDescription[ElementCount];
			uint aidx = 0;
			Bindings.ForEach((b, bidx) => b.Elements.ForEach(elem => {
				ats[aidx] = new Vk.VertexInputAttributeDescription(elem.Location, (uint)bidx, (Vk.Format)elem.Format, elem.Offset);
			}));
			return new Vk.PipelineVertexInputStateCreateInfo {
				VertexBindingDescriptions = bds,
				VertexAttributeDescriptions = ats
			};
		}
	}
}
