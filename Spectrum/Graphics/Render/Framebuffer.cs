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
		public Attachment? DepthStencil
		{
			get => _depthStencil;
			set { _depthStencil = value; _isValid = null; }
		}
		private Attachment? _depthStencil;
		/// <summary>
		/// The render target(s) to use as color attachments. <see cref="RenderTarget.IsColorTarget"/> must be 
		/// <c>true</c> for all instances.
		/// </summary>
		public Attachment[] Color
		{
			get => _color;
			set { _color = value; _isValid = null; }
		}
		private Attachment[] _color;

		/// <summary>
		/// Gets the number of attachments (depth/stencil and color) in this set.
		/// </summary>
		public uint Count => (uint)(_color?.Length ?? 0) + (_depthStencil.HasValue ? 1u : 0u);

		// Cached validity values
		private bool? _isValid = null;
		private string _validError = null;
		#endregion // Fields

		/// <summary>
		/// Create a new framebuffer with the given attachments.
		/// </summary>
		/// <param name="depthStencil">The depth/stencil attachment, or <c>null</c> for no attachment.</param>
		/// <param name="color">The color attachments, order is important.</param>
		public Framebuffer(Attachment? depthStencil, params Attachment[] color)
		{
			_depthStencil = depthStencil;
			_color = color;
		}

		/// <summary>
		/// Create a new framebuffer with the given attachments.
		/// </summary>
		/// <param name="depthStencil">The depth/stencil attachment, or <c>null</c> for no attachment.</param>
		/// <param name="color">The color attachments, order is important.</param>
		public Framebuffer(RenderTarget depthStencil, params Attachment[] color)
		{
			_depthStencil = (depthStencil != null) ? new Attachment(depthStencil) : (Attachment?)null;
			_color = color;
		}

		/// <summary>
		/// Checks that the render targets are valid. All render targets must be non-null, 
		/// </summary>
		/// <param name="error">Gets set to a human readable error message about the invalid state.</param>
		/// <returns>If all render targets are valid.</returns>
		public bool Validate(out string err)
		{
			// Attempt to get cached results
			if (_isValid.HasValue)
			{
				err = _validError;
				return _isValid.Value;
			}
			if (Count == 0)
			{
				err = _validError = null;
				return (_isValid = true).Value;
			}

			// Check for null
			var bi = _color?.IndexOf(at => at.Target == null) ?? -1;
			if (bi != -1)
			{
				_validError = err = $"null color attachment [{bi}]";
				return (_isValid = false).Value;
			}
			if (_depthStencil.HasValue && _depthStencil.Value.Target == null)
			{
				_validError = err = "null depth/stencil attachment";
				return (_isValid = false).Value;
			}

			// Check types
			if (_depthStencil.HasValue && !_depthStencil.Value.Target.IsDepthTarget)
			{
				_validError = err = $"depth/stencil attachment has invalid format";
				return (_isValid = false).Value;
			}
			bi = _color?.IndexOf(at => !at.Target.IsColorTarget) ?? -1;
			if (bi != -1)
			{
				_validError = err = $"color attachment {bi} has invalid format";
				return (_isValid = false).Value;
			}

			// Check for aliasing (technically allowed in Vulkan, but complex to support, so we dont) 
			if (_color?.GroupBy(at => at).Any(g => g.Count() > 1) ?? false)
			{
				_validError = err = $"duplicate color attachment";
				return (_isValid = false).Value;
			}
			if (_depthStencil.HasValue && (_color?.Any(at => at == _depthStencil.Value) ?? false))
			{
				_validError = err = $"duplicate color attachment as depth/stencil attachment";
				return (_isValid = false).Value;
			}

			// Check sizes
			var te = _depthStencil?.Target.Size ?? _color[0].Target.Size;
			bi = _color?.IndexOf(at => at.Target.Size != te) ?? -1;
			if (bi != -1)
			{
				_validError = err = $"invalid render target size at index {bi}";
				return (_isValid = false).Value;
			}

			// Check multisample counts (TODO)

			_validError = err = null;
			return (_isValid = true).Value;
		}

		// Copies the render targets into an array, putting the depth/stencil target at the end
		//   It returns the index of the depth/stencil attachment, if there is one
		internal uint? CopyAttachments(out Attachment[] targs)
		{
			targs = Count > 0 ? new Attachment[Count] : null;
			if (targs != null)
			{
				if (_color != null && _color.Length > 0)
					Array.Copy(_color, targs, _color.Length);
				if (_depthStencil.HasValue)
					targs[targs.Length - 1] = _depthStencil.Value;
				return _depthStencil.HasValue ? (uint)targs.Length - 1 : (uint?)null;
			}
			return null;
		}
	}

	/// <summary>
	/// Describes an attachment for a <see cref="Renderer"/> instance, and if the attachment should be preserved or
	/// cleared when the <see cref="Renderer"/> begins.
	/// </summary>
	public readonly struct Attachment : IEquatable<Attachment>
	{
		#region Fields
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
		/// Describe a new attachment with the given render target, 
		/// </summary>
		/// <param name="targ">The target to use as the attachment.</param>
		/// <param name="preserve">If the attachment should be preserved by the renderer.</param>
		public Attachment(RenderTarget targ, bool preserve = false)
		{
			Target = targ ?? throw new ArgumentNullException("Cannot create attachment from null render target.");
			Preserve = preserve;
		}

		readonly bool IEquatable<Attachment>.Equals(Attachment other) => ReferenceEquals(Target, other.Target);

		public readonly override bool Equals(object obj) => (obj is Attachment) && ReferenceEquals(Target, ((Attachment)obj).Target);

		public readonly override int GetHashCode() => Target?.GetHashCode() ?? 0;

		public static bool operator == (in Attachment l, in Attachment r) => ReferenceEquals(l.Target, r.Target);

		public static bool operator != (in Attachment l, in Attachment r) => !ReferenceEquals(l.Target, r.Target);

		/// <summary>
		/// Implicit cast of a <see cref="RenderTarget"/> to a attachment that is cleared by the <see cref="Renderer"/>.
		/// </summary>
		/// <param name="targ">The <see cref="RenderTarget"/> to use as the attachment.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Attachment (RenderTarget targ) => new Attachment(targ);

		/// <summary>
		/// Allows attachments to be created with (<see cref="RenderTarget"/>, bool) tuples, as a shortcut for the
		/// <see cref="Attachment(RenderTarget, bool)"/> constructor.
		/// </summary>
		/// <param name="tup">The tuple with the <see cref="RenderTarget"/> and preserve flag.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Attachment (in (RenderTarget, bool) tup) => new Attachment(tup.Item1, tup.Item2);

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
