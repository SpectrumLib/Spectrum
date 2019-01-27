using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes how a texture resource is used as a <see cref="RenderPass"/> attachment.
	/// </summary>
	public struct AttachmentInfo
	{
		#region Fields
		/// <summary>
		/// The name of the attachment, used to uniquely identify it within a renderpass.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The format of the attachment's texels.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// The operation to perform on an attachment when it is prepared for a render pass.
		/// </summary>
		public readonly AttachmentOp LoadOp;
		/// <summary>
		/// If the contents of the attachment need to be preserved after the render pass completes.
		/// </summary>
		public readonly bool Preserve;
		#endregion // Fields

		/// <summary>
		/// Creates a new description for a render pass attachment.
		/// </summary>
		/// <param name="name">The name of the attachment. Must be non-null, not empty, and unique within a render pass instance.</param>
		/// <param name="format">The texel format for the attachment.</param>
		/// <param name="op">The load operation for the attachment.</param>
		/// <param name="preserve">If the attachment's contents should be preserved past the end of the render pass.</param>
		public AttachmentInfo(string name, TexelFormat format, AttachmentOp op, bool preserve)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The name for an attachment cannot be null or whitespace");

			Name = name;
			Format = format;
			LoadOp = op;
			Preserve = preserve;
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
