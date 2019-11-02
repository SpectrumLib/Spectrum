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
	/// Manages an instantiated connection to a physical rendering device, and the objects and states that manage and
	/// control the connection.
	/// </summary>
	public sealed partial class GraphicsDevice : IDisposable
	{
		#region Fields
		// Core vulkan objects
		internal readonly Vk.Instance VkInstance;
		internal readonly Vk.PhysicalDevice VkPhysicalDevice;
		internal readonly Vk.Device VkDevice;

		// Device information
		/// <summary>
		/// Information about the graphics device.
		/// </summary>
		public readonly DeviceInfo Info;
		/// <summary>
		/// Enabled features on the device.
		/// </summary>
		public readonly DeviceFeatures Features;
		internal readonly Vk.PhysicalDeviceLimits Limits;
		internal readonly DeviceQueues Queues;

		// Disposal state
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsDevice()
		{
			InitializeVulkan(out VkInstance, out VkPhysicalDevice);
			OpenDevice(VkInstance, VkPhysicalDevice, out VkDevice, out Info, out Features, out Limits, out Queues);
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
				// Destroy top level objects
				VkDevice?.Dispose();
				VkInstance?.Dispose();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
