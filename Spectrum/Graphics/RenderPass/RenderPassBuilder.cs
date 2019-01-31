using System;
using System.Collections.Generic;
using System.Linq;
using Vk = VulkanCore;
using static Spectrum.Utilities.CollectionUtils;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Creates instances that are used to build render passes, either from scratch, or derived from existing 
	/// render passes.
	/// <para>
	/// A render pass is defined by the texture resources it accesses as attachments, and the subpasses that
	/// act on those attachments in a well-defined fashion. This type allows these descriptive objects to be
	/// built up and swapped out to generate render pass instances.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Creating render passes is very verbose and time consuming, but the tradeoff is fully defined rendering states
	/// that can be strongly optimized by the driver when they are created, and quickly and efficiently bound at runtime
	/// with minimal backend work. The render passes must be described in a specific order, which will greatly affect
	/// how they work. All attachments must first be specified, then subpasses can be added. Attempting to add 
	/// attachments after subpasses are added, or adding a subpass without attachments described, will result in an 
	/// exception being thrown.
	/// </para>
	/// </remarks>
	public sealed class RenderPassBuilder : IDisposable
	{
		#region Fields
		// The list of attachments added to the builder so far
		private readonly List<Attachment> _attachments = new List<Attachment>();
		// The framebuffer size that attachments must be compatible with, set by the first attachment added
		private Point _validSize = Point.Zero;
		// If the attachments are marked as finalized
		private bool _attachmentsComplete = false;

		// Cached framebuffer create info
		private Vk.FramebufferCreateInfo _fbci = default;
		// Cached attachment info
		private Vk.AttachmentDescription[] _descriptions = null;

		private bool _isDisposed = false;
		#endregion // Fields

		private RenderPassBuilder()
		{

		}
		~RenderPassBuilder()
		{
			dispose(false);
		}

		/// <summary>
		/// Creates a new incomplete render pass builder. This object must be disposed.
		/// </summary>
		/// <returns>A new builder instance.</returns>
		public static RenderPassBuilder New()
		{
			return new RenderPassBuilder();
		}

		#region Attachment Specification
		/// <summary>
		/// Adds an attachment resource from a framebuffer for use in this builder.
		/// </summary>
		/// <param name="att">The attachment to add. It must be compatible with any previously added attachments.</param>
		/// <param name="loadOp">The operation to perform on the attachment when it is loaded for a render pass.</param>
		/// <param name="preserve">If the attachment contents should be preserved at the end of a render pass.</param>
		public void AddAttachment(FramebufferAttachment att, AttachmentOp loadOp, bool preserve)
		{
			// Cannot add attachments after they are finalized
			if (_attachmentsComplete)
				throw new InvalidOperationException("Cannot add attachments to render pass builder after they are finalized");

			// Validate the attachment itself
			if (att.Name == null || att.Framebuffer == null || att.View == null)
				throw new ArgumentException("The framebuffer attachment added to the render pass builder is invalid", nameof(att));
			// Check for name overlap
			if (_attachments.FindIndex(a => a.Attach.Name == att.Name) != -1)
				throw new ArgumentException($"The render pass builder already has an attachment with the name '{att.Name}'", nameof(att));

			// Set the valid size (if first attachment), otherwise check against valid size
			if (_validSize.X == 0)
				_validSize = new Point((int)att.Framebuffer.Width, (int)att.Framebuffer.Height);
			else if (_validSize.X != att.Framebuffer.Width || _validSize.Y != att.Framebuffer.Height)
				throw new ArgumentException($"The attachment '{att.Name}' is not a compatible size with the render pass builder", nameof(att));

			// Save the attachment for use
			_attachments.Add(new Attachment(att, loadOp, preserve));
		}

		/// <summary>
		/// Marks the current set of added attachments as the final attachments to use to build render passes with.
		/// Subpasses can only be added after this function is called.
		/// </summary>
		public void FinalizeAttachments()
		{
			if (_attachmentsComplete)
				return;
			if (_attachments.Count == 0)
				throw new InvalidOperationException("Render pass builder must have attachments specified");

			// Create the cached framebuffer create info
			_fbci = new Vk.FramebufferCreateInfo(
				_attachments.Select(att => att.Attach.View).ToArray(),
				_validSize.X,
				_validSize.Y,
				layers: 1
			);

			// Build the attachment descriptions
			_descriptions = new Vk.AttachmentDescription[_attachments.Count];
			_attachments.ForEach((att, idx) => {
				_descriptions[idx] = new Vk.AttachmentDescription(
					Vk.AttachmentDescriptions.MayAlias,
					(Vk.Format)att.Attach.Format,
					Vk.SampleCounts.Count1,
					(Vk.AttachmentLoadOp)att.LoadOp,
					att.Preserve ? Vk.AttachmentStoreOp.Store : Vk.AttachmentStoreOp.DontCare,
					(Vk.AttachmentLoadOp)att.LoadOp,
					att.Preserve ? Vk.AttachmentStoreOp.Store : Vk.AttachmentStoreOp.DontCare,
					GetInitialLayout(att),
					GetFinalLayout(att)
				);
			});

			_attachmentsComplete = true;
		}
		#endregion // Attachment Specification

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{

			}

			_isDisposed = true;
		}
		#endregion // IDisposable

		// Gets the initial layout for an attachment
		private static Vk.ImageLayout GetInitialLayout(in Attachment att)
		{
			if (att.LoadOp == AttachmentOp.Clear || att.LoadOp == AttachmentOp.Discard)
				return Vk.ImageLayout.Undefined;
			else if (att.Attach.Format.IsDepthFormat())
				return Vk.ImageLayout.DepthStencilAttachmentOptimal;
			return Vk.ImageLayout.ColorAttachmentOptimal;
		}

		// Gets the final layout for an attachment
		private static Vk.ImageLayout GetFinalLayout(in Attachment att)
		{
			if (att.Attach.Format.IsDepthFormat())
				return Vk.ImageLayout.DepthStencilAttachmentOptimal;
			return Vk.ImageLayout.ColorAttachmentOptimal;
		}

		// Object describing a framebuffer attachment used in a render pass, and descriptive info about it
		private struct Attachment
		{
			public readonly FramebufferAttachment Attach;
			public readonly AttachmentOp LoadOp;
			public readonly bool Preserve;

			public Attachment(in FramebufferAttachment a, AttachmentOp l, bool p)
			{
				Attach = a;
				LoadOp = l;
				Preserve = p;
			}
		}
	}

	/// <summary>
	/// Operations that can be performed on render pass attachments when they are loaded for use.
	/// </summary>
	public enum AttachmentOp
	{
		/// <summary>
		/// The existing contents of the attachment are preserved.
		/// </summary>
		Preserve = Vk.AttachmentLoadOp.Load,
		/// <summary>
		/// All texels in the attachment are cleared to a constant value.
		/// </summary>
		Clear = Vk.AttachmentLoadOp.Clear,
		/// <summary>
		/// The existing contents of the attachment are not important, Vulkan is allowed to do whatever it wants with
		/// them before we write to or read from the attachment.
		/// </summary>
		Discard = Vk.AttachmentLoadOp.DontCare
	}
}
