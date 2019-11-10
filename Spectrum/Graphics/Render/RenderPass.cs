/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the set of objects that define a single pass within a <see cref="Renderer"/> instance.
	/// </summary>
	public sealed class RenderPass
	{
		#region Fields
		/// <summary>
		/// The name of this render pass, for identification and debugging. This name must be unique within a single
		/// <see cref="Renderer"/> instance.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Sets if the render pass requires the depth/stencil buffer.
		/// </summary>
		public readonly bool UseDepthStencil;

		/// <summary>
		/// A list of color attachment names to use as write-only color attachments in the pass. The order of this 
		/// array must match the order of the attachments in the render pass shader(s).
		/// </summary>
		public IReadOnlyList<string> ColorAttachments => _colorAttachments;
		private readonly string[] _colorAttachments;

		/// <summary>
		/// A list of color attachment names to use as read-only subpass input attachments in the pass. The order of 
		/// this array should match the order of the attachments in the render pass shader(s).
		/// </summary>
		public IReadOnlyList<string> InputAttachments => _inputAttachments;
		private readonly string[] _inputAttachments = null;
		#endregion // Fields

		/// <summary>
		/// Creates a new render pass description.
		/// </summary>
		/// <param name="name">The name of the render pass.</param>
		/// <param name="depthStencil">If the pass requires the depth/stencil attachment.</param>
		/// <param name="colors">The names of the color attachments for this pass.</param>
		/// <param name="inputs">The names of the subpass input attachments for this pass.</param>
		public RenderPass(string name, bool depthStencil, string[] colors, string[] inputs)
		{
			Name = !String.IsNullOrEmpty(name) ? name :
				throw new ArgumentException("Pipeline cannot have null or empty name.", nameof(name));
			UseDepthStencil = depthStencil;
			_colorAttachments = colors ?? new string[0];
			_inputAttachments = inputs ?? new string[0];

			// Validate
			var dev = Core.Instance.GraphicsDevice;
			if (_colorAttachments.Any(n => String.IsNullOrWhiteSpace(n)))
				throw new ArgumentException("RenderPass null or empty color attachment name.", nameof(colors));
			if (_inputAttachments.Any(n => String.IsNullOrWhiteSpace(n)))
				throw new ArgumentException("RenderPass null or empty input attachment name.", nameof(inputs));
			if (_colorAttachments.Concat(_inputAttachments).GroupBy(n => n).FirstOrDefault(g => g.Count() > 1) is var bname && bname != null)
				throw new ArgumentException($"RenderPass duplicate attachment name \"{bname.Key}\".");
			if (_colorAttachments.Length > dev.Limits.ColorAttachments)
				throw new ArgumentException("RenderPass color attachment count exceeds device limits.", nameof(colors));
			if (_inputAttachments.Length > dev.Limits.InputAttachments)
				throw new ArgumentException("RenderPass input attachment count exceeds device limits.", nameof(inputs));
		}

		/// <summary>
		/// Checks that the render pass is compatible with the attachments in the <see cref="Framebuffer"/>.
		/// </summary>
		/// <param name="fb">The framebuffer to check.</param>
		/// <returns>If the renderpass and framebuffer are compatible.</returns>
		public bool IsCompatible(Framebuffer fb) => CheckCompatibility(fb) == null;

		internal string CheckCompatibility(Framebuffer fb)
		{
			// Ensure depth/stencil settings and support
			if (UseDepthStencil && !fb.HasDepthStencil)
				return "depth operations are not supported";

			// Ensure valid attachment indices
			if ((_inputAttachments.Any() || _colorAttachments.Any()) && !fb.Color.Any())
				return "color attachments are not available";
			if (_colorAttachments.FirstOrDefault(an => !fb.Color.Any(at => at.Name == an)) is var mname && mname != null)
				return $"color attachment \"{mname}\" not in framebuffer";
			if ((mname = _inputAttachments.FirstOrDefault(an => !fb.Color.Any(at => at.Name == an))) != null)
				return $"input attachment \"{mname}\" not in framebuffer";

			return null;
		}
	}
}
