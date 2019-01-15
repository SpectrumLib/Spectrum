using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vulkan;

namespace Spectrum.Graphics
{
	// Contains utility functionality for Vulkan
	internal static class VkUtils
	{
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void CheckCall
		(
			VkResult res,
			[CallerMemberName] string name = "",
			[CallerLineNumber] int line = 0
		)
		{
			if (res != VkResult.Success)
			{
				string msg = $"Vulkan call failed with error {res} at {name}:{line}";
				InternalLog.LERROR(msg);
				throw new VkException(msg);
			}
		}

		// Implements the VK_MAKE_VERSION macro present in the C API
		public static uint MakeVersion(uint major, uint minor, uint patch) => (major << 22) | (minor << 12) | patch;
		public static uint MakeVersion(in AppVersion av) => (av.Major << 22) | (av.Minor << 12) | av.Revision;
	}
}
