using System;
using System.Reflection;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;
using static Spectrum.InternalLog;
using System.Collections.Generic;
using System.Linq;

namespace Spectrum.Graphics
{
	// Implements Vulkan instance & device object management functions
	public sealed partial class GraphicsDevice
	{
		// Creates a VkInstance
		private (Vk.Instance, VkExt.DebugReportCallbackExt) createVulkanInstance()
		{
			var appVersion = Application.AppParameters.Version;
			var engVersion = Assembly.GetExecutingAssembly().GetName().Version;

			// Build the app info
			Vk.ApplicationInfo aInfo = new Vk.ApplicationInfo(
				Application.AppParameters.Name,
				new Vk.Version((int)appVersion.Major, (int)appVersion.Minor, (int)appVersion.Revision),
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
			Vk.Instance instance = new Vk.Instance(iInfo, null);
			LINFO("Created Vulkan instance.");

			// Create the debug callback if needed
			VkExt.DebugReportCallbackExt debugReport = null;
			if (hasDebug)
			{
				VkExt.DebugReportCallbackCreateInfoExt dbInfo = new VkExt.DebugReportCallbackCreateInfoExt(
					(VkExt.DebugReportFlagsExt.All & ~VkExt.DebugReportFlagsExt.Debug), // All levels except debug
					_DebugReportCallback,
					IntPtr.Zero
				);
				debugReport = VkExt.InstanceExtensions.CreateDebugReportCallbackExt(instance, dbInfo, null);
				LINFO("Created Vulkan debug report callback.");
			}

			return (instance, debugReport);
		}

		// Destroys the global vulkan objects
		private unsafe void destroyGlobalVulkanObjects(Vk.Instance inst, VkExt.DebugReportCallbackExt debugReport)
		{
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
