using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;
using VkKhr = VulkanCore.Khr;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	// Implements Vulkan instance & device object management functions
	public sealed partial class GraphicsDevice
	{
		// List of venders
		private static readonly Dictionary<int, string> VENDOR_ID_LIST = new Dictionary<int, string> {
			{ 0x1002, "AMD" }, { 0x1010, "ImgTec" }, { 0x10DE, "NVIDIA" }, { 0x13B5, "ARM" }, { 0x5143, "Qualcomm" }, { 0x8086, "INTEL" }
		};

		// Creates a VkInstance
		private void createVulkanInstance(out Vk.Instance instance, out VkExt.DebugReportCallbackExt debugReport)
		{
			var appVersion = Application.AppParameters.Version;
			var engVersion = Assembly.GetExecutingAssembly().GetName().Version;

			// Build the app info
			Vk.ApplicationInfo aInfo = new Vk.ApplicationInfo(
				Application.AppParameters.Name,
				appVersion.ToVkVersion(),
				"Spectrum",
				new Vk.Version(engVersion.Major, engVersion.Minor, engVersion.Revision),
				new Vk.Version(1, 0, 0)
			);

			// Get available instance extensions, and ensure the required ones are present
			var availExt = Vk.Instance.EnumerateExtensionProperties().Select(ext => ext.ExtensionName).ToArray();
			List<string> reqExt = new List<string>();
			reqExt.Add(Vk.Constant.InstanceExtension.KhrSurface);
			switch (Platform.OS)
			{
				case PlatformOS.Windows: reqExt.Add(Vk.Constant.InstanceExtension.KhrWin32Surface); break;
				case PlatformOS.Linux: reqExt.Add(Vk.Constant.InstanceExtension.KhrXlibSurface); break;
				case PlatformOS.OSX: reqExt.Add(Vk.Constant.InstanceExtension.MvkMacOSSurface); break;
			}
			{
				var missingExts = reqExt.FindAll(extName => !availExt.Contains(extName));
				if (missingExts.Count > 0)
				{
					string msg = $"Required Vulkan extensions are missing: {String.Join(", ", missingExts)}";
					LFATAL(msg);
					throw new PlatformNotSupportedException(msg);
				}
			}

			// Check for validation layers, if requested
			bool hasDebug = false;
			if (Application.AppParameters.EnableValidationLayers)
			{
				if (availExt.Contains(Vk.Constant.InstanceExtension.ExtDebugReport))
				{
					var availLay = Vk.Instance.EnumerateLayerProperties().Select(layer => layer.LayerName).ToArray();

					hasDebug = availLay.Contains(Vk.Constant.InstanceLayer.LunarGStandardValidation);
					if (hasDebug)
						reqExt.Add(Vk.Constant.InstanceExtension.ExtDebugReport);
					else
						LERROR("Application requested Vulkan validation layers, but the standard layers are not available.");
				}
				else
					LERROR("Application requested Vulkan validation layers, but the debug report extension is not available.");
			}

			// Create the instance
			Vk.InstanceCreateInfo iInfo = new Vk.InstanceCreateInfo(
				aInfo,
				hasDebug ? new[] { Vk.Constant.InstanceLayer.LunarGStandardValidation } : null,
				reqExt.ToArray(),
				IntPtr.Zero
			);
			instance = new Vk.Instance(iInfo, null);
			LINFO("Created Vulkan instance.");

			// Create the debug callback if needed
			debugReport = null;
			if (hasDebug)
			{
				VkExt.DebugReportCallbackCreateInfoExt dbInfo = new VkExt.DebugReportCallbackCreateInfoExt(
					(VkExt.DebugReportFlagsExt.PerformanceWarning | VkExt.DebugReportFlagsExt.Warning | VkExt.DebugReportFlagsExt.Error),
					_DebugReportCallback,
					IntPtr.Zero
				);
				debugReport = VkExt.InstanceExtensions.CreateDebugReportCallbackExt(instance, dbInfo, null);
				LINFO("Created Vulkan debug report callback.");
			}
		}

		// Selects and opens the device
		private void openVulkanDevice(Vk.Instance instance, out Vk.PhysicalDevice pDevice, out Vk.Device lDevice,
			out DeviceFeatures features, out DeviceLimits limits, out DeviceInfo info, out DeviceQueues queues)
		{
			// Enumerate the physical devices, and score and sort them, then remove invalid ones
			var devices = instance.EnumeratePhysicalDevices()
				.Select(dev => {
					var score = scoreDevice(dev,
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
					LDEBUG($"Ignoring invalid physical device: {dev.props.DeviceName}.");
					return true;
				}
				return false;
			});
			if (devices.Count == 0)
				throw new PlatformNotSupportedException("This system does not have any valid physical devices.");
			var bestDev = devices[0];

			// Ensure extension support
			var aExts = bestDev.device.EnumerateExtensionProperties().Select(ext => ext.ExtensionName).ToArray();
			var rExts = new List<string> { Vk.Constant.DeviceExtension.KhrSwapchain };
			{
				var missing = rExts.FindAll(ext => !aExts.Contains(ext));
				if (missing.Count > 0)
				{
					string msg = $"Required Vulkan device extensions are missing: {String.Join(", ", missing)}";
					LFATAL(msg);
					throw new PlatformNotSupportedException(msg);
				}
			}

			// Prepare the queue families (we need to ensure a single queue for graphics and present)
			// In the future, we will operate with a separate transfer queue, if possible, as well as a separate compute queue
			Vk.DeviceQueueCreateInfo[] qInfos;
			{
				var qfams = bestDev.queues
					.Select((queue, idx) => (queue, present: Glfw.GetPhysicalDevicePresentationSupport(instance, bestDev.device, (uint)idx), family: idx))
					.ToArray();
				var gFam = qfams.FirstOrDefault(fam => (fam.queue.QueueFlags & Vk.Queues.Graphics) > 0 && fam.present);
				if (gFam.queue.QueueCount == 0 && gFam.family == 0)
					throw new PlatformNotSupportedException("The selected device does not support a graphics queue with present capabilities.");
				qInfos = new Vk.DeviceQueueCreateInfo[] {
					new Vk.DeviceQueueCreateInfo(gFam.family, 1, 1.0f)
				};
			}

			// Populate the limits and features
			features = default;
			limits = default;
			Vk.PhysicalDeviceFeatures enFeats = default;

			// Create the device
			Vk.DeviceCreateInfo dInfo = new Vk.DeviceCreateInfo(
				qInfos,
				rExts.ToArray(),
				enFeats,
				IntPtr.Zero
			);
			pDevice = bestDev.device;
			lDevice = pDevice.CreateDevice(dInfo, null);
			LINFO($"Created Vulkan logical device.");
			info = new DeviceInfo
			{
				Name = bestDev.props.DeviceName,
				IsDiscrete = (bestDev.props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu),
				VendorName = VENDOR_ID_LIST.ContainsKey(bestDev.props.VendorId) ? VENDOR_ID_LIST[bestDev.props.VendorId] : "unknown",
				DriverVersion = new AppVersion(bestDev.props.DriverVersion)
			};
			LINFO($"Device Info: {info.Name} (S:{bestDev.score} D:{info.IsDiscrete} V:{info.VendorName} DV:{info.DriverVersion.ToString()}).");

			// Retrieve the queues
			queues.Graphics = lDevice.GetQueue(qInfos[0].QueueFamilyIndex, 0);
		}

		// Scores a physical device (somewhat arbitrarily, make this better later), score of zero is unsuitable
		private uint scoreDevice(Vk.PhysicalDevice device, out Vk.PhysicalDeviceProperties props,
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

		// Destroys the global vulkan objects
		private void destroyGlobalVulkanObjects(Vk.Instance inst, VkExt.DebugReportCallbackExt debugReport, Vk.Device device)
		{
			device?.Dispose();
			LINFO("Destroyed Vulkan device.");

			if (debugReport != null)
			{
				debugReport.Dispose();
				LINFO("Destroyed Vulkan debug report callback.");
			}

			inst.Dispose();
			LINFO("Destroyed Vulkan instance.");
		}

		// The debug report callback
		private static bool _DebugReportCallback(VkExt.DebugReportCallbackInfo info)
		{
			LINFO("Called debug report.");
			return false;
		}
	}
}
