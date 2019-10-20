/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vk = SharpVk;
using VkExt = SharpVk.Multivendor;
using VkKhr = SharpVk.Khronos;
using VkMvk = SharpVk.MoltenVk;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	// Contains the functions for working with vulkan instances and devices
	public sealed partial class GraphicsDevice : IDisposable
	{
		// List of venders
		private static readonly Dictionary<uint, string> VENDOR_ID_LIST = new Dictionary<uint, string> {
			{ 0x1002, "AMD" }, { 0x1010, "ImgTec" }, { 0x10DE, "NVIDIA" }, { 0x13B5, "ARM" }, { 0x5143, "Qualcomm" }, { 0x8086, "INTEL" }
		};
		private const string DEBUG_LAYER = "VK_LAYER_KHRONOS_validation";

		// Creates a VkInstance
		private static void CreateVulkanInstance(out Vk.Instance instance)
		{
			var appVersion = Core.Instance.Version;
			var engVersion = Assembly.GetExecutingAssembly().GetName().Version;

			// Build the app info
			var aInfo = new Vk.ApplicationInfo
			{
				ApplicationName = Core.Instance.Name,
				ApplicationVersion = new Vk.Version(appVersion.Major, appVersion.Minor, appVersion.Build),
				EngineName = "Spectrum",
				EngineVersion = new Vk.Version(engVersion.Major, engVersion.Minor, engVersion.Build),
				ApiVersion = new Vk.Version(1, 0, 0)
			};

			// Get available instance extensions, and ensure the required ones are present
			var availExt = Vk.Instance.EnumerateExtensionProperties().Select(ext => ext.ExtensionName).ToArray();
			List<string> reqExt = new List<string>();
			reqExt.Add(VkKhr.KhrExtensions.Surface);
			switch (Runtime.OS.Family)
			{
				case OSFamily.Windows: reqExt.Add(VkKhr.KhrExtensions.Win32Surface); break;
				case OSFamily.Linux: reqExt.Add(VkKhr.KhrExtensions.XlibSurface); break;
				case OSFamily.OSX: reqExt.Add(VkMvk.MvkExtensions.MacosSurface); break;
			}
			{
				var missingExts = reqExt.FindAll(extName => !availExt.Contains(extName));
				if (missingExts.Count > 0)
				{
					string msg = $"Missing Vulkan Extensions: {String.Join(", ", missingExts)}";
					IERROR(msg);
					throw new PlatformNotSupportedException(msg);
				}
			}

			// Check for validation layers, if requested
			bool hasDebug = false;
			if (Core.Instance.Params.EnableValidationLayers)
			{
				if (availExt.Contains(VkExt.ExtExtensions.DebugReport))
				{
					var availLay = Vk.Instance.EnumerateLayerProperties().Select(layer => layer.LayerName).ToArray();

					hasDebug = availLay.Contains(DEBUG_LAYER);
					if (hasDebug)
						reqExt.Add(VkExt.ExtExtensions.DebugReport);
					else
						IERROR("Vulkan validation layers requested, but are not available.");
				}
				else
					IERROR("Vulkan validation layers requested, but the extension is not available.");
			}

			// Prepare the debug report callback info
			var dbInfo = new VkExt.DebugReportCallbackCreateInfo
			{
				Flags = (VkExt.DebugReportFlags.PerformanceWarning | VkExt.DebugReportFlags.Warning | VkExt.DebugReportFlags.Error),
				Callback = _DebugReportCallback,
				UserData = IntPtr.Zero
			};

			// Create the instance (and debug report callback, if requested
			instance = Vk.Instance.Create(
				enabledLayerNames: hasDebug ? new [] { DEBUG_LAYER } : null,
				enabledExtensionNames: reqExt.ToArray(),
				flags: Vk.InstanceCreateFlags.None,
				applicationInfo: aInfo,
				debugReportCallbackCreateInfoExt: hasDebug ? dbInfo : (VkExt.DebugReportCallbackCreateInfo?)null
			);
			IINFO("Vulkan instance created.");
		}

		// Create a vulkan device
		private static void CreateVulkanDevice(
			Vk.Instance inst, out Vk.PhysicalDevice pdevice, out Vk.Device device,
			out DeviceFeatures feats, out DeviceLimits limits, out DeviceInfo info, out DeviceQueues queues,
			out Vk.PhysicalDeviceMemoryProperties memory)
		{
			// Enumerate the physical devices, and score and sort them, then remove invalid ones
			var devices = inst.EnumeratePhysicalDevices()
				.Select(dev => {
					var score = ScoreDevice(dev,
						out Vk.PhysicalDeviceProperties props,
						out Vk.PhysicalDeviceFeatures feats,
						out Vk.PhysicalDeviceMemoryProperties memProps,
						out Vk.QueueFamilyProperties[] qfams);
					return (device: dev, props, feats, memProps, queues: qfams, score);
				})
				.OrderByDescending(dev => dev.score)
				.ToList();
			devices.RemoveAll(dev => {
				if (dev.score == 0)
				{
					IINFO($"Ignoring invalid physical device: {dev.props.DeviceName}.");
					return true;
				}
				return false;
			});
			if (devices.Count == 0)
			{
				IERROR("No valid Vulkan physical devices detected.");
				throw new PlatformNotSupportedException("No valid Vulkan physical devices detected.");
			}
			var bestDev = devices[0];

			// Ensure extension support
			var aExts = bestDev.device.EnumerateDeviceExtensionProperties().Select(ext => ext.ExtensionName).ToArray();
			var rExts = new List<string> { VkKhr.KhrExtensions.Swapchain };
			{
				var missing = rExts.FindAll(ext => !aExts.Contains(ext));
				if (missing.Count > 0)
				{
					string msg = $"Missing Vulkan Extensions: {String.Join(", ", missing)}";
					IERROR(msg);
					throw new PlatformNotSupportedException(msg);
				}
			}

			// Prepare the queue families (we need to ensure a single queue for graphics and present)
			Vk.DeviceQueueCreateInfo[] qInfos;
			bool sepTrans = false; // If there is a separate transfer queue
			{
				var qfams = bestDev.queues
					.Select((queue, idx) => (queue, 
						present: Core.Instance.Window.Glfw.GetPhysicalDevicePresentationSupport(inst, bestDev.device, (uint)idx), family: idx))
					.ToArray();
				var gFam = qfams.FirstOrDefault(fam => (fam.queue.QueueFlags & Vk.QueueFlags.Graphics) > 0 && fam.present);
				if (gFam.queue.QueueCount == 0 && gFam.family == 0)
					throw new PlatformNotSupportedException("The Vulkan device does not support presenting.");
				sepTrans = gFam.queue.QueueCount > 1;
				qInfos = new Vk.DeviceQueueCreateInfo[] {
					new Vk.DeviceQueueCreateInfo
					{
						QueueFamilyIndex = (uint)gFam.family,
						QueuePriorities = sepTrans ? new [] { 1.0f, 0.5f } : new [] { 1.0f },
						Flags = Vk.DeviceQueueCreateFlags.None
					}
				};
			}

			// Populate the limits
			limits = new DeviceLimits
			{
				MaxTextureSize1D = bestDev.props.Limits.MaxImageDimension1D,
				MaxTextureSize2D = bestDev.props.Limits.MaxImageDimension2D,
				MaxTextureSize3D = bestDev.props.Limits.MaxImageDimension3D,
				MaxTextureLayers = bestDev.props.Limits.MaxImageArrayLayers
			};

			// Populate the features
			feats = default;
			Vk.PhysicalDeviceFeatures enFeats = default;
			var rFeats = Core.Instance.Params.EnabledGraphicsFeatures;
			var strict = Core.Instance.Params.StrictGraphicsFeatures;
			bool _enableFeature(bool avail, string name)
			{
				if (!avail)
				{
					if (strict) throw new PlatformNotSupportedException($"Required device feature '{name}' not available.");
					else IERROR($"Requested device feature '{name}' not available.");
					return false;
				}
				return true;
			}
			if (rFeats.FillModeNonSolid)
				enFeats.FillModeNonSolid = feats.FillModeNonSolid = _enableFeature(bestDev.feats.FillModeNonSolid, "FillModeNonSolid");
			if (rFeats.WideLines)
				enFeats.WideLines = feats.WideLines = _enableFeature(bestDev.feats.WideLines, "WideLines");
			if (rFeats.DepthClamp)
				enFeats.DepthClamp = feats.DepthClamp = _enableFeature(bestDev.feats.DepthClamp, "DepthClamp");
			if (rFeats.AnisotropicFiltering)
				enFeats.SamplerAnisotropy = feats.AnisotropicFiltering = _enableFeature(bestDev.feats.SamplerAnisotropy, "AnisotropicFiltering");

			// Create the device
			Vk.DeviceCreateInfo dInfo = new Vk.DeviceCreateInfo
			{
				QueueCreateInfos = qInfos,
				EnabledExtensionNames = rExts.ToArray(),
				EnabledFeatures = enFeats,
				Flags = Vk.DeviceCreateFlags.None
			};
			pdevice = bestDev.device;
			device = pdevice.CreateDevice(
				queueCreateInfos: qInfos,
				enabledLayerNames: default,
				enabledExtensionNames: rExts.ToArray(),
				flags: Vk.DeviceCreateFlags.None,
				enabledFeatures: enFeats
			);
			IINFO($"Vulkan device created.");
			info = new DeviceInfo
			{
				Name = bestDev.props.DeviceName,
				IsDiscrete = (bestDev.props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu),
				VendorName = VENDOR_ID_LIST.ContainsKey(bestDev.props.VendorID) ? VENDOR_ID_LIST[bestDev.props.VendorID] : "unknown",
				VendorId = bestDev.props.VendorID,
				DriverVersion = new Version(bestDev.props.DriverVersion.Major, bestDev.props.DriverVersion.Minor, bestDev.props.DriverVersion.Patch)
			};

			// Retrieve the queues
			var qfi = qInfos[0].QueueFamilyIndex;
			queues.Graphics = device.GetQueue(qfi, 0);
			queues.Transfer = sepTrans ? device.GetQueue(qfi, 1) : queues.Graphics;
			queues.FamilyIndex = qfi;

			// Save the memory info
			memory = bestDev.memProps;

			// Report
			IINFO($"Device Info: {info.Name} (S:{bestDev.score} D:{info.IsDiscrete} V:{info.VendorName} DV:{info.DriverVersion.ToString()}).");
			IINFO($"Queue Info: G={qfi}:0 T={qfi}:{(sepTrans ? 1 : 0)}.");
		}

		// Scores a physical device (somewhat arbitrarily, make this better later), score of zero is unsuitable
		private static uint ScoreDevice(Vk.PhysicalDevice device, out Vk.PhysicalDeviceProperties props,
			out Vk.PhysicalDeviceFeatures feats, out Vk.PhysicalDeviceMemoryProperties memProps,
			out Vk.QueueFamilyProperties[] queues)
		{
			props = device.GetProperties();
			feats = device.GetFeatures();
			memProps = device.GetMemoryProperties();
			queues = device.GetQueueFamilyProperties();

			uint score = 0;

			// Strongly prefer discrete GPUS
			if (props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu)
				score += 10000;

			return score;
		}

		// Destroys the top level vulkan objects used by GraphicsDevice
		private static void DestroyVulkanObjects(Vk.Instance inst, Vk.Device dev)
		{
			dev?.Dispose();
			IINFO("Disposed Vulkan Device");

			inst?.Dispose();
			IINFO("Disposed Vulkan Instance");
		}

		// The debug report callback
		private static Vk.Bool32 _DebugReportCallback(
			VkExt.DebugReportFlags flags,
			VkExt.DebugReportObjectType objType,
			ulong obj,
			Vk.HostSize location,
			int msgCode,
			string layerPrefix,
			string msg,
			IntPtr userData
		)
		{
			if (flags == VkExt.DebugReportFlags.Error)
				IERROR($"Vulkan ({flags}): [{layerPrefix}:{msgCode}] - {msg}");
			else
				IWARN($"Vulkan ({flags}): [{layerPrefix}:{msgCode}] - {msg}");
			return false;
		}
	}
}
