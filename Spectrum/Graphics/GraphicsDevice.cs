using System;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Represents a physical rendering device on the current system. Contains and manages backend graphics objects
	/// and all communications to and from the physical device.
	/// </summary>
	public sealed partial class GraphicsDevice : IDisposable
	{
		#region Fields
		/// <summary>
		/// The application using this device.
		/// </summary>
		public readonly SpectrumApp Application;

		// Top level vulkan objects
		private Vk.Instance _vkInstance;
		private VkExt.DebugReportCallbackExt _vkDebugReport;

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsDevice(SpectrumApp app)
		{
			Application = app;

			(_vkInstance, _vkDebugReport) = createVulkanInstance();
		}
		~GraphicsDevice()
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
			if (!IsDisposed)
			{
				destroyGlobalVulkanObjects(_vkInstance, _vkDebugReport);
			}

			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
