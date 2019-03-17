using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the objects and attachments used in a subpass within a <see cref="RenderPass"/> instance.
	/// </summary>
	public struct SubpassInfo
	{
		#region Fields
		/// <summary>
		/// The name of this subpass, used for identification and debugging. Must be unique within a RenderPass instance.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The list of names of attachments that will be used as read-only input attachments in this subpass.
		/// </summary>
		public string[] InputAttachments;
		/// <summary>
		/// The list of names of attachments that will be used as output color attachments in this subpass.
		/// </summary>
		public string[] ColorAttachments;
		/// <summary>
		/// The name of the attachment that will be used as the output depth/stencil attachment in this subpass.
		/// </summary>
		public string DepthStencilAttachment;
		#endregion // Fields

		/// <summary>
		/// Creates a new named subpass with no attachments specified.
		/// </summary>
		/// <param name="name">The name of the subpass.</param>
		/// <param name="color">The names of the color attachments to use in this subpass.</param>
		/// <param name="depthStencil">The name of the depth-stencil attachment to use in this subpass.</param>
		/// <param name="input">The names of the input attachments to use in this subpass.</param>
		public SubpassInfo(string name, string[] color = null, string depthStencil = null, string[] input = null)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("A subpass name cannot be null or empty", nameof(name));

			Name = name;
			InputAttachments = input;
			ColorAttachments = color;
			DepthStencilAttachment = depthStencil;
		}
	}
}
