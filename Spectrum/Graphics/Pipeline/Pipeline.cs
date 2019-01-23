using System;
using Vk = VulkanCore;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Represents the total parameter set of a rendering pipeline.
	/// </summary>
	public sealed class Pipeline
	{
		#region Fields
		/// <summary>
		/// The name of this pipeline.
		/// </summary>
		public readonly string Name;

		// The pipeline object (created by PipelineBuilder, but this class is responsible for disposal)
		private readonly Vk.Pipeline _vkPipeline;
		// The pipeline layout object (needs to be disposed)
		private readonly Vk.PipelineLayout _vkLayout;

		private bool _isDisposed = false;
		#endregion // Fields

		internal Pipeline(string name, Vk.Pipeline pipeline, Vk.PipelineLayout layout)
		{
			Name = name;
			_vkPipeline = pipeline;
			_vkLayout = layout;

			LDEBUG($"Created new pipeline '{name}'.");
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
			if (!_isDisposed && disposing)
			{
				// Null checking is temporary, remove once pipelines are well defined in builders
				_vkLayout?.Dispose();
				_vkPipeline?.Dispose();
				LDEBUG($"Destroyed pipeline '{Name}'.");
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
