using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;
using Spectrum.Utility;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	// Implements Vulkan instance & device object management functions
	public sealed partial class GraphicsDevice
	{
		// Important strings for working with Vulkan
		private static readonly FixedUtfString ENGINE_NAME = "Spectrum";
		private static readonly FixedUtfString VK_KHR_SURFACE_EXTENSION_NAME = "VK_KHR_surface";
		private static readonly FixedUtfString VK_KHR_WIN32_SURFACE_EXTENSION_NAME = "VK_KHR_win32_surface";
		private static readonly FixedUtfString VK_KHR_XLIB_SURFACE_EXTENSION_NAME = "VK_KHR_xlib_surface";
		private static readonly FixedUtfString VK_MVK_MACOS_SURFACE_EXTENSION_NAME = "VK_MVK_macos_surface";

		// Creates a VkInstance
		private unsafe void createVulkanInstance(out VkInstance inst)
		{
			var appName = new FixedUtfString(Application.AppParameters.Name);

			// Build the app info
			VkApplicationInfo aInfo = VkApplicationInfo.New();
			aInfo.pApplicationName = appName.Data;
			aInfo.pEngineName = ENGINE_NAME.Data;
			aInfo.applicationVersion = VkUtils.MakeVersion(Application.AppParameters.Version);
			aInfo.apiVersion = VkUtils.MakeVersion(1, 0, 0);
			aInfo.engineVersion = VkUtils.MakeVersion(0, 1, 0);

			// Enumerate available instance extensions
			uint extCount = 0;
			vkEnumerateInstanceExtensionProperties((byte*)0, ref extCount, IntPtr.Zero);
			VkExtensionProperties[] aextp = new VkExtensionProperties[extCount];
			fixed (VkExtensionProperties *extPtr = &aextp[0]) { vkEnumerateInstanceExtensionProperties((byte*)0, ref extCount, extPtr); }
			string[] aExts = aextp.Select(ext => PointerUtils.RawToString(ext.extensionName, Encoding.UTF8)).ToArray();
			LDEBUG($"Available Extensions:  {String.Join(", ", aExts)}");

			// Create a list of the required extensions, and ensure that they are all available
			List<FixedUtfString> instExt = new List<FixedUtfString>();
			instExt.Add(VK_KHR_SURFACE_EXTENSION_NAME);
			switch (Platform.OS)
			{
				case PlatformOS.Windows: instExt.Add(VK_KHR_WIN32_SURFACE_EXTENSION_NAME); break;
				case PlatformOS.Linux: instExt.Add(VK_KHR_XLIB_SURFACE_EXTENSION_NAME); break;
				case PlatformOS.OSX: instExt.Add(VK_MVK_MACOS_SURFACE_EXTENSION_NAME); break;
			}
			{
				var missing = instExt.FindAll(ext => !aExts.Contains(ext.ToString()));
				if (missing.Count > 0)
				{
					string msg = $"Missing required Vulkan extensions: {String.Join(", ", missing)}";
					LFATAL(msg);
					throw new PlatformNotSupportedException(msg);
				}
			}
			IntPtr[] extPtrs = instExt.Select(ext => ext.AsIntPtr()).ToArray();

			// Build the instance create info
			VkInstanceCreateInfo cInfo = VkInstanceCreateInfo.New();
			cInfo.pApplicationInfo = &aInfo;
			cInfo.enabledLayerCount = 0;
			cInfo.ppEnabledLayerNames = (byte**)0;
			cInfo.enabledExtensionCount = (uint)extPtrs.Length;

			// Create the instance
			fixed (IntPtr *extPtr = &extPtrs[0])
			{
				cInfo.ppEnabledExtensionNames = (byte**)extPtr;
				VkUtils.CheckCall(vkCreateInstance(&cInfo, IntPtr.Zero, out inst));
			}
			LINFO("Created Vulkan instance.");

			// Free the fixed strings
			appName.Dispose();
		}

		// Destroys the global vulkan objects
		private unsafe void destroyGlobalVulkanObjects(in VkInstance inst)
		{
			vkDestroyInstance(inst, IntPtr.Zero);
			LINFO("Destroyed Vulkan instance.");
		}
	}
}
