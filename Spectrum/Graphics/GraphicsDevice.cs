using Spectrum.Utilities;
using System;
using System.Linq;
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
		internal Vk.Device VkDevice => _vkDevice;

		// Swapchain
		internal readonly Swapchain Swapchain;
		// Queues
		internal readonly DeviceQueues Queues;
		// Memory types
		internal readonly Vk.PhysicalDeviceMemoryProperties Memory;

		/// <summary>
		/// The set of features and extensions that the device supports, and are additionally enabled by the application.
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
			openVulkanDevice(_vkInstance, out _vkPhysicalDevice, out _vkDevice, out Features, out Limits, out Info, out Queues, out Memory);

			Swapchain = new Swapchain(this, _vkInstance, _vkPhysicalDevice, _vkDevice);
		}
		~GraphicsDevice()
		{
			dispose(false);
		}

		// Initializes the various graphics resources found throughout the library
		internal void InitializeResources()
		{
			TransferBuffer.CreateResources();
		}

		#region Frame Functions
		// Called at the beginning of a render frame to prepare the render subsystem
		internal void BeginFrame()
		{
			Swapchain.BeginFrame();

			// Reset the global state
			setInitialState();
		}

		// Called at the end of a render frame to present the frame
		internal void EndFrame()
		{
			Swapchain.EndFrame();
		}
		#endregion // Frame Functions

		// Finds the best type of memory for the given constraints
		// TODO: In the future, we will probably cache the best indices for all common property flag combinations,
		//       and check against that and make sure the memory types are valid, before performing the expensive
		//       calculation to find the best
		internal int FindMemoryTypeIndex(int bits, Vk.MemoryProperties props)
		{
			int? index = null;
			Memory.MemoryTypes.ForEach((type, idx) => {
				// If: (not already found) AND (valid memory type) AND (all required properties are present)
				if (!index.HasValue && (bits & (0x1 << idx)) > 0 && (type.PropertyFlags & props) == props)
				{
					index = idx;
				}
			});
			return index.HasValue ? index.Value : -1;
		}

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
				// Resources scattered thorughout the library
				Sampler.Samplers.ForEach(pair => pair.Value.Dispose());
				TransferBuffer.Cleanup();

				// Base objects
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
		// NOTE: the openVulkanDevice function must be updated whenever this one is

		/// <summary>
		/// If the device supports rendering in line or point fill mode.
		/// </summary>
		public bool FillModeNonSolid;
		/// <summary>
		/// If the device supports line widths other than 1.0.
		/// </summary>
		public bool WideLines;
		/// <summary>
		/// If the device supports clamping depth fragments instead of discarding them.
		/// </summary>
		public bool DepthClamp;
		/// <summary>
		/// If the device supports anisotropic filtering for image samplers.
		/// </summary>
		public bool AnisotropicFiltering;
		/// <summary>
		/// If the device supports geometry shader stages.
		/// </summary>
		public bool GeometryShaders;
		/// <summary>
		/// If the device supports tessellation control and evaluation shader stages.
		/// </summary>
		public bool TessellationShaders;
	}

	/// <summary>
	/// Contains the set of limits for a device.
	/// </summary>
	public struct DeviceLimits
	{
		// NOTE: the openVulkanDevice function must be updated whenever this one is

		/// <summary>
		/// The maximum dimensions for a 1D texture.
		/// </summary>
		public uint MaxTextureSize1D;
		/// <summary>
		/// The maximum dimensions for a 2D texture.
		/// </summary>
		public uint MaxTextureSize2D;
		/// <summary>
		/// The maximum dimensions for a 3D texture.
		/// </summary>
		public uint MaxTextureSize3D;
		/// <summary>
		/// The maximum number of layers that a texture can have.
		/// </summary>
		public uint MaxTextureLayers;
	}

	/// <summary>
	/// Contains high level information about a physical device.
	/// </summary>
	public struct DeviceInfo
	{
		// NOTE: the openVulkanDevice function must be updated whenever this one is

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
		// The queue dedicated to transfering data for images and buffers between the host and device
		//   Note that this queue may be the same as the graphics queue, and depends on if there is more than one queue
		//   available for the same family as the graphics queue. We require them to be the same family all the resources
		//   are exclusive and sharing is a level of complexity to avoid.
		public Vk.Queue Transfer;

		// If the graphics and transfer queues are separate.
		public bool SeparateTransfer => !ReferenceEquals(Graphics, Transfer);
	}
}
