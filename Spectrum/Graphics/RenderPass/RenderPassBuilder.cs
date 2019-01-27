using System;
using System.Collections.Generic;
using System.Linq;
using Vk = VulkanCore;

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
	/// </list>
	/// Attempting to add attachments after subpasses are added, or adding a subpass without attachments described,
	/// will result in an exception being thrown.
	/// </para>
	/// </remarks>
	public sealed class RenderPassBuilder : IDisposable
	{
		#region Fields

		// Cached attachment info
		private readonly List<AttachmentInfo> _attachments = new List<AttachmentInfo>();
		private readonly bool _hasAttachments; // If the user has specified that the render pass has attachments
		public bool AttachmentsSpecified => !_hasAttachments || (_attachments.Count > 0);

		private bool _isDisposed = false;
		#endregion // Fields

		private RenderPassBuilder(bool hasAttachments)
		{
			_hasAttachments = hasAttachments;
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
		//public static RenderPassBuilder New(RenderPass pass)
		//{
		//	var rpb = new RenderPassBuilder();
		//	// TODO: Add attachment description from existing render pass
		//	return rpb;
		//}

		#region Attachment Specification
		/// <summary>
		/// Adds an attachment for the render pass builder to include. Attempting to add render passes after 
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public RenderPassBuilder AddAttachment(AttachmentInfo info)
		{
			// TODO: check for subpass specification

			// Check for no attachments
			if (!_hasAttachments)
				throw new InvalidOperationException("Cannot add an attachment to a render pass builder that does not use attachments");
			// Ensure the name is unique
			if (_attachments.Any(att => att.Name == info.Name))
				throw new InvalidOperationException($"The render pass builder already contains an attachment with the name '{info.Name}'");

			_attachments.Add(info);
			return this;
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
	}
}
