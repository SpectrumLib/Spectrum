/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes a set of <see cref="VertexElement"/> instances that are sourced from the same vertex buffer.
	/// </summary>
	public struct VertexBinding : IEquatable<VertexBinding>
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

		#region Ctor
		/// <summary>
		/// Describes a new vertex, calculating the stride from the passed elements.
		/// </summary>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(params VertexElement[] elems)
		{
			if (elems.Length == 0)
				throw new ArgumentException("Vertex binding with zero elements.");
			Array.Copy(elems, Elements = new VertexElement[elems.Length], elems.Length);
			Stride = Elements.Max(e => e.Offset + (e.Format.GetSize() * e.ArraySize.GetValueOrDefault(1)));
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride from the passed elements.
		/// </summary>
		/// <param name="inst">If the data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(bool inst, params VertexElement[] elems)
		{
			if (elems.Length == 0)
				throw new ArgumentException("Vertex binding with zero elements.");
			Array.Copy(elems, Elements = new VertexElement[elems.Length], elems.Length);
			Stride = Elements.Max(e => e.Offset + (e.Format.GetSize() * e.ArraySize.GetValueOrDefault(1)));
			PerInstance = inst;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride from the passed elements.
		/// </summary>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(params VertexElementFormat[] elems)
		{
			if (elems.Length == 0)
				throw new ArgumentException("Vertex binding with zero elements.");
			uint loc = 0, off = 0;
			Elements = elems.Select(vef => {
				var roff = off;
				off += vef.GetSize();
				return new VertexElement(vef, loc++, roff);
			}).ToArray();
			Stride = off;
			PerInstance = false;
		}

		/// <summary>
		/// Describes a new vertex, calculating the stride from the passed elements.
		/// </summary>
		/// <param name="inst">If the data is per-instance instead of per-vertex.</param>
		/// <param name="elems">The elements of the vertex to describe.</param>
		public VertexBinding(bool inst, params VertexElementFormat[] elems)
		{
			if (elems.Length == 0)
				throw new ArgumentException("Vertex binding with zero elements.");
			uint loc = 0, off = 0;
			Elements = elems.Select(vef => {
				var roff = off;
				off += vef.GetSize();
				return new VertexElement(vef, loc++, roff);
			}).ToArray();
			Stride = off;
			PerInstance = inst;
		}
		#endregion // Ctor

		internal VertexBinding(uint s, VertexElement[] e, bool pi)
		{
			Stride = s;
			Elements = e;
			PerInstance = pi;
		}

		internal readonly VertexBinding Copy()
		{
			VertexElement[] elems;
			Array.Copy(Elements, elems = new VertexElement[Elements.Length], Elements.Length);
			return new VertexBinding(Stride, elems, PerInstance);
		}

		#region Overrides
		public readonly override int GetHashCode() => (int)(~(Stride * 55009) | (uint)(Elements.Length << 18)); // Really not ideal

		public readonly override bool Equals(object obj) => (obj is VertexBinding) && (((VertexBinding)obj) == this);

		readonly bool IEquatable<VertexBinding>.Equals(VertexBinding other) =>
			other.Stride == Stride && other.PerInstance == PerInstance && other.Elements.SequenceEqual(Elements);
		#endregion // Overrides

		public static bool operator == (in VertexBinding l, in VertexBinding r) =>
			l.Stride == r.Stride && l.PerInstance == r.PerInstance && l.Elements.SequenceEqual(r.Elements);

		public static bool operator != (in VertexBinding l, in VertexBinding r) =>
			l.Stride != r.Stride || l.PerInstance != r.PerInstance || !l.Elements.SequenceEqual(r.Elements);
	}
}
