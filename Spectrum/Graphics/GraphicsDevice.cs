using System;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;
using VkKhr = VulkanCore.Khr;

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
		private readonly Vk.Instance _vkInstance;
		private readonly VkExt.DebugReportCallbackExt _vkDebugReport;
		private readonly Vk.PhysicalDevice _vkPhysicalDevice;
		private readonly Vk.Device _vkDevice;

		// Swapchain
		internal readonly Swapchain Swapchain;
		// Queues
		internal readonly DeviceQueues Queues;

		/// <summary>
		/// The set of features and extensions that the device supports.
		/// </summary>
		public readonly DeviceFeatures Features;
		/// <summary>
		/// The set of limits for the device.
		/// </summary>
		public readonly DeviceLimits Limits;
		/// <summary>
		/// High level information about the device.
		/// </summary>
		public readonly DeviceInfo Info;

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsDevice(SpectrumApp app)
		{
			Application = app;

			createVulkanInstance(out _vkInstance, out _vkDebugReport);
			openVulkanDevice(_vkInstance, out _vkPhysicalDevice, out _vkDevice, out Features, out Limits, out Info, out Queues);

			Swapchain = new Swapchain(this, _vkInstance, _vkPhysicalDevice, _vkDevice);
		}
		~GraphicsDevice()
		{
			dispose(false);
		}

		#region Frame Functions
		// Called at the beginning of a render frame to prepare the render subsystem
		internal void BeginFrame()
		{
			Swapchain.BeginFrame();
		}

		// Called at the end of a render frame to present the frame
		internal void EndFrame()
		{
			Swapchain.EndFrame();
		}
		#endregion // Frame Functions

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed && disposing)
			{
				Swapchain.Dispose();
				destroyGlobalVulkanObjects(_vkInstance, _vkDebugReport, _vkDevice);
			}

			IsDisposed = true;
		}
		#endregion // IDisposable
	}

	/// <summary>
	/// Contains the set of supported features and extensions for a device.
	/// </summary>
	public struct DeviceFeatures
	{

	}

	/// <summary>
	/// Contains the set of limits for a device.
	/// </summary>
	public struct DeviceLimits
	{

	}

	/// <summary>
	/// Contains high level information about a physical device.
	/// </summary>
	public struct DeviceInfo
	{
		/// <summary>
		/// The human-readable name of the device.
		/// </summary>
		public string Name;
		/// <summary>
		/// If the device is a discrete GPU, false implies an integrated GPU.
		/// </summary>
		public bool IsDiscrete;
		/// <summary>
		/// The human-readable name of the manufacturer of the device driver.
		/// </summary>
		public string VendorName;
		/// <summary>
		/// The version of the active Vulkan driver.
		/// </summary>
		public AppVersion DriverVersion;
	}

	// Contains the queues that the device uses
	internal struct DeviceQueues
	{
		// The main graphics/present queue
		public Vk.Queue Graphics;
	}
}
