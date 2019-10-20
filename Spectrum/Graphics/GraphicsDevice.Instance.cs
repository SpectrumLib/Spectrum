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
		private static readonly Dictionary<int, string> VENDOR_ID_LIST = new Dictionary<int, string> {
			{ 0x1002, "AMD" }, { 0x1010, "ImgTec" }, { 0x10DE, "NVIDIA" }, { 0x13B5, "ARM" }, { 0x5143, "Qualcomm" }, { 0x8086, "INTEL" }
		};
		private const string DEBUG_LAYER = "VK_LAYER_KHRONOS_validation";

		// Creates a VkInstance
		private static void CreateVulkanInstance(out Vk.Instance instance)
		{
			var appVersion = Core.Instance.Version;
			var engVersion = Assembly.GetExecutingAssembly().GetName().Version;

			// Build the app info
			var aInfo = new Vk.ApplicationInfo()
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
			var dbInfo = new VkExt.DebugReportCallbackCreateInfo()
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
