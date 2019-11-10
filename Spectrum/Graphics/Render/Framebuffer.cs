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
	/// Describes a collection of <see cref="RenderTarget"/>s to use as attachments in a <see cref="Renderer"/> 
	/// instance.
	/// </summary>
	public sealed class Framebuffer
	{
		#region Fields
		/// <summary>
		/// The render target to use as the depth stencil buffer. <see cref="RenderTarget.IsDepthTarget"/> must be
		/// <c>true</c>. If <c>null</c>, the renderer will not have depth/stencil functionality available.
		/// </summary>
		public readonly Attachment DepthStencil;
		/// <summary>
		/// The render target(s) to use as color attachments. <see cref="RenderTarget.IsColorTarget"/> must be 
		/// <c>true</c> for all instances.
		/// </summary>
		public readonly Attachment[] Color;

		/// <summary>
		/// Gets if the framebuffer has a depth/stencil attachment.
		/// </summary>
		public bool HasDepthStencil => (DepthStencil != null);
		/// <summary>
		/// Gets the number of attachments (depth/stencil and color) in this set.
		/// </summary>
		public uint Count => (uint)Color.Length + (DepthStencil != null ? 1u : 0u);
		/// <summary>
		/// The size of the render targets in this framebuffer.
		/// </summary>
		public Extent Size => DepthStencil?.Target.Size ?? Color[0].Target.Size;
		#endregion // Fields

		/// <summary>
		/// Create a new framebuffer with the given attachments.
		/// </summary>
		/// <param name="depthStencil">The depth/stencil attachment, or <c>null</c> for no attachment.</param>
		/// <param name="color">
		/// The color attachments. If an attachment is unnamed, it will named "ColorX", where X is the attachment index.
		/// </param>
		public Framebuffer(Attachment depthStencil, params Attachment[] color)
		{
			DepthStencil = depthStencil;
			if (DepthStencil != null && DepthStencil.Target == null)
				throw new ArgumentException("Invalid framebuffer: depth/stencil target is null.");

			if (color?.Any(cat => cat == null || cat.Target == null) ?? false)
				throw new ArgumentException("Invalid framebuffer: null color attachment or color target.");
			Color = color?.Select((cat, cidx) => cat.Name != null ? cat : new Attachment($"Color{cidx}", cat.Target, cat.Preserve)).ToArray() 
				?? new Attachment[0];

			if (validate() is var verr && verr != null)
				throw new ArgumentException($"Invalid framebuffer: {verr}.");
		}

		/// <summary>
		/// Gets the attachment index of the color attachment with the given name, or <c>null</c>.
		/// </summary>
		/// <param name="name">The name of the color attachment to get the index for.</param>
		/// <returns>The color attachment index, or <c>null</c> if there is not an attachment with the name.</returns>
		public uint? GetColorIndex(string name)
		{
			var i = Color.IndexOf(at => at.Name == name);
			return (i != -1) ? (uint)i : (uint?)null;
		}

		/// <summary>
		/// Gets the attachment index of the depth/stencil attachment, or <c>null</c> if there is no depth/stencil.
		/// </summary>
		/// <returns>The depth/stencil index, or <c>null</c>.</returns>
		public uint? GetDepthStencilIndex() => HasDepthStencil ? Count - 1 : (uint?)null;

		private string validate()
		{
			if (Count == 0)
				return "no attachments specified";

			// Check types
			if (!(DepthStencil?.Target.IsDepthTarget ?? true))
				return $"depth/stencil attachment has invalid format";
			if (Color.IndexOf(at => !at.Target.IsColorTarget) is var bi && bi != -1)
				return $"color attachment {bi} has invalid format";

			// Check for duplicate names
			if (Color.GroupBy(at => at.Name).FirstOrDefault(g => g.Count() > 1) is var bname && bname != null)
				return $"duplicate color attachment name \"{bname.Key}\"";

			// Check for aliasing (technically allowed in Vulkan, but complex to support, so we dont) 
			if (Color.GroupBy(at => at.Target).Any(g => g.Count() > 1))
				return $"duplicate color attachment target";
			if (HasDepthStencil && Color.Any(at => ReferenceEquals(at.Target, DepthStencil.Target)))
				return $"duplicate color attachment as depth/stencil attachment";

			// Check sizes
			var te = DepthStencil?.Target.Size ?? Color[0].Target.Size;
			if ((bi = Color.IndexOf(at => at.Target.Size != te)) != -1)
				return $"invalid render target size at index {bi}";

			// Check multisample counts (TODO)

			return null;
		}

		// Copies the render targets into an array, putting the depth/stencil target at the end
		//   It returns the index of the depth/stencil attachment, if there is one
		internal void CopyAttachments(out Attachment[] targs)
		{
			targs = new Attachment[Count];
			if (Color.Length > 0)
				Array.Copy(Color, targs, Color.Length);
			if (HasDepthStencil)
				targs[targs.Length - 1] = DepthStencil;
		}
	}

	/// <summary>
	/// Describes an attachment for a <see cref="Renderer"/> instance, and if the attachment should be preserved or
	/// cleared when the <see cref="Renderer"/> begins.
	/// </summary>
	public sealed class Attachment
	{
		#region Fields
		/// <summary>
		/// The identifying name of the attachment, used to reference the attachment in a <see cref="RenderPass"/>.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The <see cref="RenderTarget"/> to use as the attachment.
		/// </summary>
		public readonly RenderTarget Target;
		/// <summary>
		/// If the attachment should be preserved then the renderer starts, <c>false</c> will clear the attachment.
		/// </summary>
		public readonly bool Preserve;
		#endregion // Fields

		/// <summary>
		/// Describe a new attachment with the given name, render target, and preserve setting.
		/// </summary>
		/// <param name="name">The name of the attachment. Required to be non-null for color attachments.</param>
		/// <param name="targ">The target to use as the attachment.</param>
		/// <param name="preserve">If the attachment should be preserved by the renderer.</param>
		public Attachment(string name, RenderTarget targ, bool preserve)
		{
			if (targ.IsDepthTarget)
				Name = name ?? "DepthStencil";
			else
			{
				Name = !String.IsNullOrWhiteSpace(name) ? name :
					throw new ArgumentException("A framebuffer color attachment cannot have a null or empty name.");
			}
			Target = targ ?? throw new ArgumentNullException("Cannot create attachment from null render target.");
			Preserve = preserve;
		}

		/// <summary>
		/// Creates an unnamed attachment, which will be given a name when passed to a <see cref="Framebuffer"/>.
		/// </summary>
		/// <param name="targ">The target to use as the attachment.</param>
		/// <param name="preserve">If the attachment should be preserved by the renderer.</param>
		public Attachment(RenderTarget targ, bool preserve)
		{
			Name = targ.IsDepthTarget ? "DepthStencil" : null;
			Target = targ ?? throw new ArgumentNullException("Cannot create attachment from null render target.");
			Preserve = preserve;
		}

		/// <summary>
		/// Allows attachments to be created with (string, <see cref="RenderTarget"/>, bool) tuples, as a shortcut for
		/// the <see cref="Attachment(string, RenderTarget, bool)"/> constructor.
		/// </summary>
		/// <param name="tup">The tuple with the name, <see cref="RenderTarget"/>, and preserve flag.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Attachment (in (string, RenderTarget, bool) tup) => 
			new Attachment(tup.Item1, tup.Item2, tup.Item3);

		/// <summary>
		/// Creates an unnamed attachment, which will be given a name when passed to a <see cref="Framebuffer"/>.
		/// </summary>
		/// <param name="tup">The RenderTarget and preserve setting for the attachment.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Attachment (in (RenderTarget, bool) tup) =>
			new Attachment(tup.Item1, tup.Item2);

		// Creates the default description for this attachment
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.AttachmentDescription GetDescription() => new Vk.AttachmentDescription(
			flags: Vk.AttachmentDescriptionFlags.None,
			format: (Vk.Format)Target.Format,
			samples: Vk.SampleCountFlags.None, // TODO: Make this correct once we support multisampling
			loadOp: Preserve ? Vk.AttachmentLoadOp.Load : Vk.AttachmentLoadOp.Clear,
			storeOp: Vk.AttachmentStoreOp.Store,
			stencilLoadOp: Preserve ? Vk.AttachmentLoadOp.Load : Vk.AttachmentLoadOp.Clear,
			stencilStoreOp: Vk.AttachmentStoreOp.Store,
			initialLayout: Target.DefaultImageLayout,
			finalLayout: Target.DefaultImageLayout
		);
	}
}
