/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Collection of objects that fully describe how to process rendering commands within a specific 
	/// <see cref="Renderer"/> pass.
	/// </summary>
	public sealed class Pipeline : IDisposable
	{
		#region Fields
		/// <summary>
		/// The renderer that this pipeline was created from.
		/// </summary>
		public readonly Renderer Renderer;
		/// <summary>
		/// The name of the <see cref="RenderPass"/> that this pipeline is compatible with.
		/// </summary>
		public readonly string PassName;
		/// <summary>
		/// The index of the <see cref="RenderPass"/> within its <see cref="Renderer"/> that the pipeline is compatible
		/// with.
		/// </summary>
		public readonly uint PassIndex;

		// Internal objects
		internal readonly Vk.Pipeline VkPipeline;
		internal readonly Vk.PipelineLayout VkLayout;

		private bool _isDisposed = false;
		#endregion // Fields

		internal Pipeline(Renderer rdr, Renderer.PassInfo pass, Vk.Pipeline pipeline, Vk.PipelineLayout layout)
		{
			Renderer = rdr;
			PassName = pass.Name;
			PassIndex = pass.Index;
			VkPipeline = pipeline;
			VkLayout = layout;
		}
		~Pipeline()
		{
			dispose(false);
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
				if (disposing)
				{
					VkPipeline?.Dispose();
					VkLayout?.Dispose();
				}
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
