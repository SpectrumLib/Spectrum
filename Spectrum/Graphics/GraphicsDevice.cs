/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;
using VkExt = SharpVk.Multivendor;

namespace Spectrum.Graphics
{
	/// <summary>
	///	Represents a connection to a physical rendering device on the system. Contains and manages backend graphics
	///	objects and all communications to and from the physical device.
	/// </summary>
	public sealed partial class GraphicsDevice : IDisposable
	{
		#region Fields
		// Top level vulkan objects
		private readonly Vk.Instance _vkInstance;
		private readonly Vk.PhysicalDevice _vkPhysicalDevice;
		internal Vk.Device VkDevice { get; private set; }
		
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

		internal GraphicsDevice()
		{
			CreateVulkanInstance(out _vkInstance);
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
			if (!IsDisposed && disposing)
			{
				// Wait for all current GPU processes to complete
				//VkDevice.WaitIdle();

				// Clean the internal resources
				//cleanResources();

				// Base objects
				//Swapchain.Dispose();
				DestroyVulkanObjects(_vkInstance, VkDevice);
			}

			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
