using System;
using Vulkan;

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
		private VKObjects _vkObjects;

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsDevice(SpectrumApp app)
		{
			Application = app;

			createVulkanInstance(out _vkObjects.Instance, out _vkObjects.DebugReportCallback);
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
				destroyGlobalVulkanObjects(_vkObjects.Instance, _vkObjects.DebugReportCallback);
			}

			IsDisposed = true;
		}
		#endregion // IDisposable
	}


	// Holds the top-level vulkan objects used by a device
	internal struct VKObjects
	{
		public VkInstance Instance;
		public VkDebugReportCallbackEXT DebugReportCallback;
	}
}
