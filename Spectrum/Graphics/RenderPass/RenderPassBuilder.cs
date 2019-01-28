using System;
using System.Collections.Generic;
using System.Linq;
using Vk = VulkanCore;
using static Spectrum.Utilities.CollectionUtils;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Creates instances that are used to build render passes, either from scratch, or derived from existing 
	/// render passes. Also assists in building multiple similar render passes quickly.
	/// <para>
	/// A render pass is first and foremost described by the attachments it contains, which are texture resources
	/// that are used within the render pass. Any two render passes derived from the same attachment set are considered
	/// compatible. Once the attachments are described, a render pass then as subpasses within it. Each subpass
	/// uses a single pipeline, shader, and buffer set, but must describe which attachments it will use, and in
	/// what way.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Creating render passes is very verbose and time consuming, but the tradeoff is fully defined rendering states
	/// that can be strongly optimized by the driver when they is created, and quickly and efficiently bound at runtime
	/// with minimal backend work. The render passes must be described in a specific order, which will greatly affect
	/// how they work. This order is:
	/// <list type="number">
	///		<item>A new builder is created with <see cref="New()"/> or <see cref="New(RenderPass)"/>.</item>
	///		<item>
	///			If the builder was not derived from an existing render pass, and if it was not created with no
	///			attachments specified, its attachments must be specified using the <c>AddAttachment</c> functions.
	///		</item>
	///		<item>
	///			Add subpasses with the <see cref="AddSubpass(SubpassInfo)"/> function. The first time this function is
	///			called, the attachments are finalized and cannot be edited or added to any further. The subpasses will
	///			execute in the order that they are added.
	///		</item>
	/// </list>
	/// Attempting to add attachments after subpasses are added, or adding a subpass without attachments described,
	/// will result in an exception being thrown.
	/// </para>
	/// </remarks>
	public sealed class RenderPassBuilder : IDisposable
	{
		#region Fields
		// Cached attachment info
		private readonly List<AttachmentInfo> _attachmentCache = new List<AttachmentInfo>();
		private readonly bool _hasAttachments; // If the user has specified that the render pass has attachments
		
		// Cached subpass info
		private readonly List<SubpassInfo> _subpassCache = new List<SubpassInfo>();

		// Attachment Vulkan info
		private Vk.AttachmentDescription[] _attDescriptions = null;

		/// <summary>
		/// If the attachments in this subpass have been finalized, and it is ready to add subpasses.
		/// </summary>
		public bool AttachmentsFinalized => (_attDescriptions != null);
		
		private bool _isDisposed = false;
		#endregion // Fields

		private RenderPassBuilder(bool hasAttachments)
		{
			_hasAttachments = hasAttachments;
			if (!hasAttachments)
				FinalizeAttachments();
		}
		~RenderPassBuilder()
		{
			dispose(false);
		}

		/// <summary>
		/// Creates a brand new builder without attachments or subpasses specified. The attachments must be specified
		/// before subpasses are added.
		/// </summary>
		/// <param name="hasAttachments">
		/// If <c>true</c>, then the render passes from this builder will have attachments, and the attachments must be
		/// specified before subpasses. Defaults to true.
		/// </param>
		/// <returns>A new render pass builder, with no descriptive information.</returns>
		public static RenderPassBuilder New(bool hasAttachments = true)
		{
			return new RenderPassBuilder(hasAttachments);
		}

		/// <summary>
		/// Creates a new builder without subpasses, but inherits an identical attachment set from the provided render
		/// pass.
		/// </summary>
		/// <param name="pass">The pass to derive the attachments in this builder on.</param>
		/// <returns>A new render pass builder, with attachments already specified.</returns>
		public static RenderPassBuilder New(RenderPass pass)
		{
			var rpb = new RenderPassBuilder(true /* TODO */);
			// TODO: Add attachment description from existing render pass
			rpb.FinalizeAttachments();
			return rpb;
		}

		#region Attachment Specification
		/// <summary>
		/// Adds an attachment for the render pass builder to include. Attempting to add attachments after starting to
		/// add subpasses will result in an exception.
		/// </summary>
		/// <param name="name">The name of the attachment. Must be non-null, not empty, and unique within a render pass instance.</param>
		/// <param name="format">The texel format for the attachment.</param>
		/// <param name="op">The load operation for the attachment.</param>
		/// <param name="preserve">If the attachment's contents should be preserved past the end of the render pass.</param>
		/// <returns>The builder instance, for function chaining.</returns>
		public RenderPassBuilder AddAttachment(string name, TexelFormat format, AttachmentOp op, bool preserve) =>
			AddAttachment(new AttachmentInfo(name, format, op, preserve));

		/// <summary>
		/// Adds an attachment for the render pass builder to include. Attempting to add attachments after starting to
		/// add subpasses will result in an exception.
		/// </summary>
		/// <param name="info">The description of the attachment to add.</param>
		/// <returns>The builder instance, for function chaining.</returns>
		public RenderPassBuilder AddAttachment(AttachmentInfo info)
		{
			// Check if we have finalized the attachments
			if (_attDescriptions != null)
				throw new InvalidOperationException("Cannot specify new render pass attachments after subpasses are added");
			// Check for no attachments
			if (!_hasAttachments)
				throw new InvalidOperationException("Cannot add an attachment to a render pass builder that does not use attachments");
			// Ensure the name is unique
			if (_attachmentCache.Any(att => att.Name == info.Name))
				throw new InvalidOperationException($"The render pass builder already contains an attachment with the name '{info.Name}'");

			_attachmentCache.Add(info);
			return this;
		}

		/// <summary>
		/// Uses the current set of added attachments as the final set for this builder. Attempting to add attachments
		/// after this is called will result in exceptions. This function does not need to be called on builders that
		/// do not use attachments, or those created from existing render passes.
		/// </summary>
		/// <returns>The builder instance, for function chaining.<returns>
		public RenderPassBuilder FinalizeAttachments()
		{
			if (!AttachmentsFinalized)
			{
				_attDescriptions = new Vk.AttachmentDescription[_attachmentCache.Count];
				_attachmentCache.ForEach((att, idx) => {
					_attDescriptions[idx] = new Vk.AttachmentDescription(
						Vk.AttachmentDescriptions.MayAlias,
						(Vk.Format)att.Format,
						Vk.SampleCounts.Count1,
						(Vk.AttachmentLoadOp)att.LoadOp,
						att.Preserve ? Vk.AttachmentStoreOp.Store : Vk.AttachmentStoreOp.DontCare,
						(Vk.AttachmentLoadOp)att.LoadOp,
						att.Preserve ? Vk.AttachmentStoreOp.Store : Vk.AttachmentStoreOp.DontCare,
						AttachmentInfo.GetInitialLayout(att),
						AttachmentInfo.GetFinalLayout(att)
					);
				});
			}

			return this;
		}
		#endregion // Attachment Specification

		#region Subpass Specification
		/// <summary>
		/// Adds a subpass to this render pass. Attachments must be specified before calling this function.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public RenderPassBuilder AddSubpass(SubpassInfo info)
		{
			// Make sure the attachments are ready
			if (!AttachmentsFinalized)
				throw new InvalidOperationException("Cannot add a subpass to a render pass until attachments are finalized");

			return this;
		}
		#endregion // Subpas Specification

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
	}
}
