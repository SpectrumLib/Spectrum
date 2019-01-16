using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Vulkan;
using static Vulkan.VulkanNative;
using Spectrum.Utility;
using static Spectrum.InternalLog;
using System.Runtime.InteropServices;

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
		private static readonly FixedUtfString VK_KHR_SWAPCHAIN_EXTENSION_NAME = "VK_KHR_swapchain";
		private static readonly FixedUtfString VK_EXT_DEBUG_REPORT_EXTENSION_NAME = "VK_EXT_debug_report";
		private static readonly FixedUtfString VK_STANDARD_VALIDATION_LAYER_NAME = "VK_LAYER_LUNARG_standard_validation";
		private static readonly FixedUtfString VK_CREATE_DEBUG_REPORT_CALLBACK_NAME = "vkCreateDebugReportCallbackEXT";
		private static readonly FixedUtfString VK_DESTROY_DEBUG_REPORT_CALLBACK_NAME = "vkDestroyDebugReportCallbackEXT";

		// Creates a VkInstance
		private unsafe void createVulkanInstance(out VkInstance inst, out VkDebugReportCallbackEXT debugReport)
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

			// Request extra features if validation is requested
			List<FixedUtfString> instLay = new List<FixedUtfString>();
			bool hasDebug = false;
			if (Application.AppParameters.EnableValidationLayers)
			{
				if (aExts.Contains(VK_EXT_DEBUG_REPORT_EXTENSION_NAME.ToString()))
				{
					uint layerCount = 0;
					vkEnumerateInstanceLayerProperties(ref layerCount, IntPtr.Zero);

					if (layerCount > 0)
					{
						VkLayerProperties[] alayp = new VkLayerProperties[layerCount];
						fixed (VkLayerProperties* layPtr = &alayp[0]) { vkEnumerateInstanceLayerProperties(ref layerCount, layPtr); }
						string[] aLays = alayp.Select(lay => PointerUtils.RawToString(lay.layerName, Encoding.UTF8)).ToArray();
						LDEBUG($"Available Layers:  {String.Join(", ", aLays)}");

						if (aLays.Contains(VK_STANDARD_VALIDATION_LAYER_NAME.ToString()))
						{
							instExt.Add(VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
							instLay.Add(VK_STANDARD_VALIDATION_LAYER_NAME);
							hasDebug = true;
						}
						else
							LERROR("Application requested Vulkan validation layers, but the standard layers are not available.");
					}
					else
						LERROR("Application requested Vulkan validation layers, but there are no validation layers available.");
				}
				else
					LERROR("Application requested Vulkan validation layers, but the debug extension is not available.");
			}
			instLay.Add(null); // This is to prevent the second fixed statement below from potentially exploding if no layers are present

			// Build the instance create info
			IntPtr[] extPtrs = instExt.Select(ext => ext.AsIntPtr()).ToArray();
			IntPtr[] layPtrs = instLay.Select(lay => lay?.AsIntPtr() ?? IntPtr.Zero).ToArray();
			VkInstanceCreateInfo cInfo = VkInstanceCreateInfo.New();
			cInfo.pApplicationInfo = &aInfo;
			cInfo.enabledLayerCount = (uint)layPtrs.Length - 1; // Adjust for the extra null above
			cInfo.enabledExtensionCount = (uint)extPtrs.Length;

			// Create the instance
			fixed (IntPtr *extPtr = &extPtrs[0])
			fixed (IntPtr *layPtr = &layPtrs[0])
			{
				cInfo.ppEnabledLayerNames = (byte**)layPtr;
				cInfo.ppEnabledExtensionNames = (byte**)extPtr;
				VkUtils.CheckCall(vkCreateInstance(&cInfo, IntPtr.Zero, out inst));
			}
			LINFO("Created Vulkan instance.");

			// If available, and requested, create the messenger callback
			if (hasDebug)
			{
				VkDebugReportCallbackCreateInfoEXT drcInfo = VkDebugReportCallbackCreateInfoEXT.New();
				drcInfo.flags = VkDebugReportFlagsEXT.WarningEXT | VkDebugReportFlagsEXT.ErrorEXT; // Report warns and errs only
				drcInfo.pfnCallback = (new FunctionPointer<PFN_vkDebugReportCallbackEXT>(_DebugReportCallback)).Pointer;

				IntPtr createFn = vkGetInstanceProcAddr(inst, VK_CREATE_DEBUG_REPORT_CALLBACK_NAME.Data);
				if (createFn != IntPtr.Zero)
				{
					var createDel = Marshal.GetDelegateForFunctionPointer<vkCreateDebugReportCallbackEXT_t>(createFn);
					VkUtils.CheckCall(createDel(inst, &drcInfo, IntPtr.Zero, out debugReport));
					LINFO("Debug report callback created.");
				}
				else
				{
					LERROR("Application requested Vulkan validation layers, but the debug report handler could not be created.");
					debugReport = new VkDebugReportCallbackEXT(0);
				}
			}
			else
				debugReport = new VkDebugReportCallbackEXT(0);

			// Free the fixed strings
			appName.Dispose();
		}

		// Destroys the global vulkan objects
		private unsafe void destroyGlobalVulkanObjects(in VkInstance inst, in VkDebugReportCallbackEXT debugReport)
		{
			if (debugReport.Handle != 0)
			{
				IntPtr destroyFn = vkGetInstanceProcAddr(inst, VK_DESTROY_DEBUG_REPORT_CALLBACK_NAME.Data);
				var destroyDel = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugReportCallbackEXT_t>(destroyFn);
				destroyDel(inst, debugReport, IntPtr.Zero);
				LINFO("Debug report callback destroyed.");
			}

			vkDestroyInstance(inst, IntPtr.Zero);
			LINFO("Destroyed Vulkan instance.");
		}

		// The debug report callback
		private unsafe static uint _DebugReportCallback
		(
			uint flags,
			VkDebugReportObjectTypeEXT objectType,
			ulong @object,
			UIntPtr location,
			int messageCode,
			byte *pLayerPrefix, // const
			byte *pMessage, // const
			void *pUserData
		)
		{
			LINFO("Called debug report.");
			return VkBool32.False;
		}

		private unsafe delegate VkResult vkCreateDebugReportCallbackEXT_t(
			VkInstance instance, 
			VkDebugReportCallbackCreateInfoEXT *createInfo, 
			IntPtr allocatorPtr, 
			out VkDebugReportCallbackEXT ret
		);

		private unsafe delegate void vkDestroyDebugReportCallbackEXT_t(
			VkInstance instance,
			VkDebugReportCallbackEXT callback,
			IntPtr allocatorPtr
		);
	}
}
