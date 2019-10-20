/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Contains the definitions for structs used with the graphics device
	public sealed partial class GraphicsDevice : IDisposable
	{
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
			public bool SeparateTransfer => Graphics.RawHandle.ToUInt64() != Transfer.RawHandle.ToUInt64();
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
			public Version DriverVersion;
		}
	}
}
