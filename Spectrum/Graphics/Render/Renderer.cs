/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The core rendering type. Contains a set of <see cref="RenderTarget"/>s that can be used as attachments in 
	/// multiple rendering passes, which are each described by a <see cref="Pipeline"/> object.
	/// </summary>
	public sealed class Renderer : IDisposable
	{
		#region Fields
		// Attachment info
		private Attachment[] _attachments;
		private uint? _depthStencilIndex = null;

		private bool _isDisposed = false;
		#endregion // Fields
		
		/// <summary>
		/// Creates a new renderer using the given framebuffer and pipeline passes.
		/// </summary>
		/// <param name="framebuffer">The attachments to use in the renderer.</param>
		/// <param name="pipelines">The pipelines that describe the render passes.</param>
		public Renderer(Framebuffer framebuffer, Pipeline[] pipelines)
		{
			var dev = Core.Instance.GraphicsDevice;
			if (framebuffer == null)
				throw new ArgumentNullException(nameof(framebuffer));
			if (pipelines == null)
				throw new ArgumentNullException(nameof(pipelines));
			if (pipelines.Length == 0)
				throw new ArgumentException("Renderer must have at least one pipeline.");

			// Validate the attachments
			var adescs = CreateAttachments(framebuffer, out _attachments, out _depthStencilIndex);

			// Create the framebuffer
			foreach (var at in _attachments)
				at.Target.IncRefCount();
		}
		~Renderer()
		{
			dispose(false);
		}

		#region Creation
		private static Vk.AttachmentDescription[] CreateAttachments(Framebuffer fb, out Attachment[] atts, out uint? _depthIdx)
		{
			// Validate and copy
			if (!fb.Validate(out var verr))
				throw new ArgumentException($"Invalid renderer attachments: {verr}.");
			_depthIdx = fb.CopyAttachments(out atts);

			// Generate attachment descriptions
			return atts.Select(at => at.GetDescription()).ToArray();
		}
		#endregion // Creation

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
				foreach (var at in _attachments)
					at.Target.DecRefCount();

				if (disposing)
				{
					
				}
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
