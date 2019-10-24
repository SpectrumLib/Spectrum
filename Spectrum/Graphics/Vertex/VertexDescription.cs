/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Fully describes vertex buffer input data, across all elements, binding buffers, and per-vertex and
	/// per-instance objects.
	/// </summary>
	public readonly struct VertexDescription
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
				throw new ArgumentException("Cannot create a vertex description with zero bindings.");
			var binds = new VertexBinding[bindings.Length];
			var ec = 0;
			bindings.ForEach((b, idx) => {
				binds[idx] = new VertexBinding(b.Stride, b.PerInstance, b.Elements);
				ec += b.Elements.Length;
			});
			Bindings = binds;
			ElementCount = (uint)ec;
		}

		/// <summary>
		/// Provides a quick way to describe a vertex, assuming tightly packed elements all sourced from the same 
		/// buffer.
		/// </summary>
		/// <param name="fmts">Tightly packed formats that make up the vertex.</param>
		public VertexDescription(params VertexElementFormat[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Cannot create a vertex description with zero bindings.");
			Bindings = new VertexBinding[] { new VertexBinding(fmts) };
			ElementCount = (uint)fmts.Length;
		}

		/// <summary>
		/// Provides a quick way to describe a vertex, assuming tightly packed elements all sourced from the same 
		/// buffer.
		/// </summary>
		/// <param name="fmts">Tightly packed formats that make up the vertex.</param>
		public VertexDescription(params (VertexElementFormat, uint)[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Cannot create a vertex description with zero bindings.");
			Bindings = new VertexBinding[] { new VertexBinding(fmts) };
			ElementCount = (uint)fmts.Length;
		}

		internal Vk.PipelineVertexInputStateCreateInfo ToCreateInfo()
		{
			var bds = Bindings.Select((b, idx) =>
				new Vk.VertexInputBindingDescription((uint)idx, b.Stride, b.PerInstance ? Vk.VertexInputRate.Instance : Vk.VertexInputRate.Vertex)
			).ToArray();
			var ats = new Vk.VertexInputAttributeDescription[ElementCount];
			int aidx = 0;
			Bindings.ForEach((b, bidx) => b.Elements.ForEach(elem => {
				ats[aidx++] = new Vk.VertexInputAttributeDescription(elem.Location, (uint)bidx, (Vk.Format)elem.Format, elem.Offset);
			}));
			return new Vk.PipelineVertexInputStateCreateInfo
			{
				VertexBindingDescriptions = bds,
				VertexAttributeDescriptions = ats,
				Flags = Vk.PipelineVertexInputStateCreateFlags.None
			};
		}
	}

	/// <summary>
	/// Describes a vertex layout within a single vertex buffer (all attributes with the same binding number). All
	/// <see cref="VertexElement"/>s must be either complete or incomplete, no mixing is allowed. Mixing may cause
	/// rendering issues.
	/// </summary>
	public readonly struct VertexBinding : IEquatable<VertexBinding>
	{
		#region Fields
		/// <summary>
		/// The data size for a single vertex in the backing buffer.
		/// </summary>
		public readonly uint Stride;
		/// <summary>
		/// The elements that make up this vertex.
		/// </summary>
		public readonly VertexElement[] Elements;
		/// <summary>
		/// If this data is per-instance instead of per-vertex.
		/// </summary>
		public readonly bool PerInstance;
		#endregion // Fields

		#region Constructors
		/// <summary>
		/// Describes a new vertex, calculating the correct stride.
		/// </summary>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(params VertexElement[] elems)
		{
			if (elems.Length == 0)
				throw new ArgumentException("Cannot create a vertex binding with zero elements.");
			Elements = CompleteElements(elems);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the correct stride.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(bool perInstance, params VertexElement[] elems)
		{
			if (elems.Length == 0)
				throw new ArgumentException("Cannot create a vertex binding with zero elements.");
			Elements = CompleteElements(elems);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(params VertexElementFormat[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Cannot create a vertex binding with zero elements.");
			Elements = PackFormats(fmts.Select(fmt => (fmt, 1u)).ToArray());
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(bool perInstance, params VertexElementFormat[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Cannot create a vertex binding with zero elements.");
			Elements = PackFormats(fmts.Select(fmt => (fmt, 1u)).ToArray());
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = perInstance;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(params (VertexElementFormat, uint)[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Cannot create a vertex binding with zero elements.");
			Elements = PackFormats(fmts);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride, element locations, and element offsets assuming tight 
		/// packing in both buffer data and shader binding slots.
		/// </summary>
		/// <param name="perInstance">If this data is per-instance instead of per-vertex.</param>
		/// <param name="fmts">The elements of the vertex, in order.</param>
		public VertexBinding(bool perInstance, params (VertexElementFormat, uint)[] fmts)
		{
			if (fmts.Length == 0)
				throw new ArgumentException("Cannot create a vertex binding with zero elements.");
			Elements = PackFormats(fmts);
			Stride = Elements.Max(e => e.Offset + e.Format.GetSize(e.Count));
			PerInstance = perInstance;
		}
		#endregion // Constructors

		internal VertexBinding(uint stride, bool perInstance, VertexElement[] elems)
		{
			Stride = stride;
			Elements = new VertexElement[elems.Length];
			Array.Copy(elems, Elements, elems.Length);
			PerInstance = perInstance;
		}

		/// <summary>
		/// Creates a copy of this vertex binding.
		/// </summary>
		/// <returns>An identical vertex layout.</returns>
		public readonly VertexBinding Copy() => new VertexBinding(Stride, PerInstance, Elements);

		public readonly override int GetHashCode() => (int)(~(Stride * 55009) ^ (Elements.Length << 18)); // Really not ideal

		public readonly override bool Equals(object obj) => (obj is VertexBinding) && (((VertexBinding)obj) == this);

		readonly bool IEquatable<VertexBinding>.Equals(VertexBinding other) =>
			other.Stride == Stride && other.PerInstance == PerInstance && other.Elements.SequenceEqual(Elements);

		public static bool operator == (in VertexBinding l, in VertexBinding r) =>
			l.Stride == r.Stride && l.PerInstance == r.PerInstance && l.Elements.SequenceEqual(r.Elements);

		public static bool operator != (in VertexBinding l, in VertexBinding r) =>
			l.Stride != r.Stride || l.PerInstance != r.PerInstance || !l.Elements.SequenceEqual(r.Elements);

		// For incomplete elements, this will complete them into a new array, otherwise it will just copy the array
		private static VertexElement[] CompleteElements(VertexElement[] elems)
		{
			var elements = new VertexElement[elems.Length];
			if (elems[0].IsComplete)
				Array.Copy(elems, elements, elems.Length);
			else
			{
				uint loc = 0, off = 0;
				elems.ForEach((el, idx) => {
					elements[idx] = new VertexElement(loc, el.Format, off);
					loc += el.Format.GetBindingSize(el.Count);
					off += el.Format.GetSize(el.Count);
				});
			}
			return elements;
		}

		// This will build a tightly-packed vertex element array from formats
		private static VertexElement[] PackFormats(in (VertexElementFormat fmt, uint count)[] fmts)
		{
			var elements = new VertexElement[fmts.Length];
			uint loc = 0, off = 0;
			fmts.ForEach((fmt, idx) => {
				elements[idx] = new VertexElement(loc, fmt.fmt, off);
				loc += fmt.fmt.GetBindingSize(fmt.count);
				off += fmt.fmt.GetSize(fmt.count);
			});
			return elements;
		}
	}
}
