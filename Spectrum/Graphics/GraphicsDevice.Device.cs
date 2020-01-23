/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Vk = SharpVk;
using VkExt = SharpVk.Multivendor;
using VkKhr = SharpVk.Khronos;
using VkMvk = SharpVk.MoltenVk;
using static Spectrum.InternalLog;
using System.Reflection;

namespace Spectrum.Graphics
{
	// Contains the logic for operating on Vulkan instances and devices
	public sealed partial class GraphicsDevice : IDisposable
	{
		private const string DEBUG_LAYER_NAME = "VK_LAYER_KHRONOS_validation";
		private static readonly string[] REQUIRED_DEVICE_EXTENSIONS = { 
			VkKhr.KhrExtensions.Swapchain
		};

		// Initializes a VkInstance object, and selects the best physical device available
		private static void InitializeVulkan(out Vk.Instance inst, out Vk.PhysicalDevice pdevice)
		{
			// Query and select instance extensions
			var avext = Vk.Instance.EnumerateExtensionProperties().Select(ext => ext.ExtensionName).ToArray();
			var rqext = new List<string>();
			rqext.Add(VkKhr.KhrExtensions.Surface);
			rqext.Add(
				Runtime.OS.IsWindows ? VkKhr.KhrExtensions.Win32Surface :
				Runtime.OS.IsLinux ? VkKhr.KhrExtensions.XlibSurface :
				VkMvk.MvkExtensions.MacosSurface
			);
			{
				var missing = rqext.FindAll(ext => !avext.Contains(ext));
				if (missing.Count > 0)
				{
					string msg = $"Missing Vulkan Extensions: {String.Join('|', missing)}";
					IERROR(msg);
					throw new PlatformNotSupportedException(msg);
				}
			}

			// Check for the validation layers
			bool hasDebug = false;
			if (Core.Instance.Params.EnableValidationLayers)
			{
				if (avext.Contains(VkExt.ExtExtensions.DebugReport))
				{
					var avlay = Vk.Instance.EnumerateLayerProperties().Select(lay => lay.LayerName).ToArray();
					hasDebug = avlay.Contains(DEBUG_LAYER_NAME);
					if (hasDebug)
						rqext.Add(VkExt.ExtExtensions.DebugReport);
					else
						IWARN($"Requested debug layer '{DEBUG_LAYER_NAME}' not available.");
				}
				else
					IWARN("Requested debug report extension not available.");
			}

			// Create the instance
			var appv = Core.Instance.Version;
			var engv = Assembly.GetExecutingAssembly().GetName().Version;
			inst = Vk.Instance.Create(
				enabledLayerNames: hasDebug ? new [] { DEBUG_LAYER_NAME } : null,
				enabledExtensionNames: rqext.ToArray(),
				flags: Vk.InstanceCreateFlags.None,
				applicationInfo: new Vk.ApplicationInfo {
					ApplicationName = Core.Instance.Name,
					ApplicationVersion = new Vk.Version(appv.Major, appv.Minor, appv.Build),
					EngineName = "Spectrum",
					EngineVersion = new Vk.Version(engv.Major, engv.Minor, engv.Build),
					ApiVersion = new Vk.Version(1, 0, 0)
				},
				debugReportCallbackCreateInfoExt: hasDebug ? new VkExt.DebugReportCallbackCreateInfo {
					Flags = (VkExt.DebugReportFlags.Warning | VkExt.DebugReportFlags.PerformanceWarning | VkExt.DebugReportFlags.Error),
					Callback = _DebugReportCallback,
					UserData = IntPtr.Zero
				} : (VkExt.DebugReportCallbackCreateInfo?)null
			);
			IINFO("Created Vulkan instance.");

			// Select the physical device
			var pdevs = inst.EnumeratePhysicalDevices()
							.Select(dev => (dev, score: ScoreDevice(dev)))
							.Where(pair => pair.score > 0)
							.OrderByDescending(pair => pair.score)
							.ToArray();
			if (pdevs.Length == 0)
			{
				var msg = "No devices on the system support Vulkan.";
				IERROR(msg);
				throw new PlatformNotSupportedException(msg);
			}
			pdevice = pdevs[0].dev;
		}

		// Open the device and populate device into
		private static void OpenDevice(Vk.Instance inst, Vk.PhysicalDevice pdev, out Vk.Device device, out DeviceInfo dinfo, 
			out DeviceFeatures dfeats, out DeviceLimits dlims, out DeviceQueues dqueues, out DeviceMemory dmem)
		{
			// Get the physical device info
			var props = pdev.GetProperties();
			var feats = pdev.GetFeatures();
			var memp  = pdev.GetMemoryProperties();
			var qfams = pdev.GetQueueFamilyProperties();
			dinfo = new DeviceInfo(props);
			dfeats = Core.Instance.Params.EnabledGraphicsFeatures;
			dlims = new DeviceLimits(props.Limits);
			dmem = new DeviceMemory(memp);
			IINFO($"Selected device '{props.DeviceName}'.");

			// Check the features
			dfeats.Check(feats);

			// Prepare the queue information
			Vk.DeviceQueueCreateInfo dqcis;
			bool sepTrans = false;
			{
				uint qidx = 0;
				var valid = qfams
					.Select(qp => (
						queue: qp,
						present: Core.Instance.Window.Glfw.GetPhysicalDevicePresentationSupport(inst, pdev, qidx),
						family: qidx++))
					.Where(queue => queue.present && ((queue.queue.QueueFlags & Vk.QueueFlags.Graphics) > 0)) // Ensure present and graphics abilities
					.OrderByDescending(queue => queue.queue.QueueCount)										  // Try to find the queue with the highest count
					.ToArray();
				if (valid.Length == 0)
				{
					string msg = "The selected device does not support presenting to a window.";
					IERROR(msg);
					throw new PlatformNotSupportedException(msg);
				}
				sepTrans = valid[0].queue.QueueCount > 1;
				dqcis = new Vk.DeviceQueueCreateInfo { 
					QueueFamilyIndex = valid[0].family,
					QueuePriorities = sepTrans ? new [] { 1.0f, 0.5f } : new [] { 1.0f },
					Flags = Vk.DeviceQueueCreateFlags.None
				};
			}

			// Create the device
			device = pdev.CreateDevice(
				queueCreateInfos: new [] { dqcis },
				enabledLayerNames: null,
				enabledExtensionNames: REQUIRED_DEVICE_EXTENSIONS,
				flags: Vk.DeviceCreateFlags.None,
				enabledFeatures: dfeats.ToVulkanType()
			);
			IINFO("Created Vulkan device.");

			// Retrieve the queues
			dqueues.Graphics = device.GetQueue(dqcis.QueueFamilyIndex, 0);
			dqueues.Transfer = sepTrans ? device.GetQueue(dqcis.QueueFamilyIndex, 1) : dqueues.Graphics;
			dqueues.FamilyIndex = dqcis.QueueFamilyIndex;

			// Report
			IINFO($"\tDevice: V=0x{props.VendorID:X} Driver={props.DriverVersion} Api={props.ApiVersion}");
			IINFO($"\tQueues: F={dqueues.FamilyIndex} S={sepTrans}");
		}

		// Calculates a numeric score for the device (this needs to be done better)
		private static uint ScoreDevice(Vk.PhysicalDevice device)
		{
			// Get device info
			var props = device.GetProperties();
			var feats = device.GetFeatures();
			var memp = device.GetMemoryProperties();
			var queues = device.GetQueueFamilyProperties();
			var limits = props.Limits;
			var exts = device.EnumerateDeviceExtensionProperties().Select(ext => ext.ExtensionName).ToArray();

			// Check for the required extensions
			var mext = Array.FindAll(REQUIRED_DEVICE_EXTENSIONS, ext => !exts.Contains(ext));
			if (mext.Length > 0)
			{
				IINFO($"Ignoring device '{props.DeviceName}', missing required extensions: {String.Join(", ", mext)}.");
				return 0;
			}

			// Calculate a simplistic score
			uint score = 0;

			// Strongly prefer discrete devices
			if (props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu)
				score += 10000;

			// Megabytes of device local memory
			{
				ulong tmem = 0;
				foreach (var heap in memp.MemoryHeaps)
				{
					if ((heap.Flags & Vk.MemoryHeapFlags.DeviceLocal) > 0)
						tmem += heap.Size;
				}
				score += (uint)(tmem / (1024 * 1024));
			}

			IINFO($"Discovered device '{props.DeviceName}' (score: {score}).");
			return score;
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
