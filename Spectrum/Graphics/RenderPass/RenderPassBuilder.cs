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
		// A mapping of attachment names to indices (mostly for convenience)
		private readonly Dictionary<string, int> _attachPoints = new Dictionary<string, int>();
		// The framebuffer size that attachments must be compatible with, set by the first attachment added
		private Point _validSize = Point.Zero;
		// If the attachments are marked as finalized
		private bool _attachmentsComplete = false;

		// The list of subpass descriptions added to the builder
		private readonly List<Subpass> _subpasses = new List<Subpass>();

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
			_attachPoints.Add(att.Name, _attachments.Count - 1);
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

		#region Subpass Specification
		/// <summary>
		/// Adds a subpass to the builder. Subpasses will be executed in the order that they are added.
		/// </summary>
		/// <param name="info">The descriptive information about the subpass.</param>
		public void AddSubpass(SubpassInfo info)
		{
			if (info.Name == null)
				throw new ArgumentException($"Invalid subpass added to render pass builder (no name)", nameof(info));
			if (_subpasses.IndexOf(i => i.Info.Name == info.Name) != -1)
				throw new ArgumentException($"The render pass builder already contains a subpass with the name '{info.Name}'", nameof(info));

			List<string> used = new List<string>(); // Ones that have already been used (to prevent double usage)

			// Create the input attachment refs
			Vk.AttachmentReference[] inputRefs = null;
			if ((info.InputAttachments?.Length ?? 0) > 0)
			{
				validateAttachments(used, info.InputAttachments, info.Name, "input", TexelFormatExtensions.IsValidInputFormat);

				inputRefs = new Vk.AttachmentReference[info.InputAttachments.Length];
				info.InputAttachments.ForEach((aname, idx) => {
					used.Add(aname);
					int aidx = _attachPoints[aname];
					inputRefs[idx] = new Vk.AttachmentReference(
						aidx,
						_attachments[aidx].Attach.Format.IsDepthFormat() ? Vk.ImageLayout.DepthStencilReadOnlyOptimal : Vk.ImageLayout.ShaderReadOnlyOptimal
					);
				});
			}

			// Create the color attachment refs
			Vk.AttachmentReference[] colorRefs = null;
			if ((info.ColorAttachments?.Length ?? 0) > 0)
			{
				validateAttachments(used, info.ColorAttachments, info.Name, "color", TexelFormatExtensions.IsColorFormat);

				colorRefs = new Vk.AttachmentReference[info.ColorAttachments.Length];
				info.ColorAttachments.ForEach((aname, idx) => {
					used.Add(aname);
					inputRefs[idx] = new Vk.AttachmentReference(
						_attachPoints[aname],
						Vk.ImageLayout.ColorAttachmentOptimal
					);
				});
			}

			// Create the depth/stencil attachment
			Vk.AttachmentReference dsRef = new Vk.AttachmentReference(-1, Vk.ImageLayout.General);
			if (info.DepthStencilAttachment != null)
			{
				validateAttachments(used, new[] { info.DepthStencilAttachment }, info.Name, "depth/stencil", TexelFormatExtensions.IsDepthFormat);
				used.Add(info.DepthStencilAttachment);
				dsRef = new Vk.AttachmentReference(
					_attachPoints[info.DepthStencilAttachment],
					Vk.ImageLayout.DepthStencilAttachmentOptimal
				);
			}

			// Generate the preserve indices
			var remaining = _attachPoints.Keys.Where(aname => !used.Contains(aname));
			int[] preserve = remaining.Select(aname => _attachPoints[aname]).ToArray();

			// Save the subpass
			_subpasses.Add(new Subpass(info, inputRefs, colorRefs, dsRef, preserve));
		}

		// Validates that all of the specified attachments are available and have not been used already
		private void validateAttachments(List<string> used, string[] atts, string spname, string typeName, Func<TexelFormat, bool> formatCheck)
		{
			var navail = atts.FirstOrDefault(aname => used.Contains(aname));
			if (navail != null)
				throw new InvalidOperationException($"The subpass '{spname}' has specified the attachment '{navail}' for use more than once");
			var missing = atts.FirstOrDefault(aname => !_attachPoints.ContainsKey(aname));
			if (missing != null)
				throw new InvalidOperationException($"The subpass '{spname}' has specified the attachment '{missing}', which does not exist in the render pass");
			var badFormat = atts.FirstOrDefault(aname => !formatCheck(_attachments[_attachPoints[aname]].Attach.Format));
			if (badFormat != null)
				throw new InvalidOperationException($"The subpass '{spname}' has specified an invalid format {typeName} attachment '{badFormat}'");
		}
		#endregion // Subpass Specification

		/// <summary>
		/// Builds a new <see cref="RenderPass"/> instance using the information so far provided to the builder.
		/// </summary>
		/// <returns>A new render pass object.</returns>
		public RenderPass Build()
		{
			if (!_attachmentsComplete)
				throw new InvalidOperationException("Cannot build a render pass without finalized attatchments");
			if (_subpasses.Count == 0)
				throw new InvalidOperationException("Cannot build a render pass with zero subpasses");

			return null;
		}

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

		// Object describing a subpass and its cached creation info
		private struct Subpass
		{
			public readonly SubpassInfo Info;
			public readonly Vk.AttachmentReference[] InputRefs;
			public readonly Vk.AttachmentReference[] ColorRefs;
			public readonly Vk.AttachmentReference DepthStencilRef;
			public readonly int[] PreserveIndices;

			public Subpass(SubpassInfo info, Vk.AttachmentReference[] i, Vk.AttachmentReference[] c, Vk.AttachmentReference ds, int[] p)
			{
				Info = info;
				InputRefs = i;
				ColorRefs = c;
				DepthStencilRef = ds;
				PreserveIndices = p;
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
