using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Exception thrown when Vulkan encounters an error.
	/// </summary>
	public sealed class VkException : Exception
	{
		internal VkException(string msg) :
			base(msg)
		{ }
		internal VkException(string msg, Exception inner) :
			base(msg, inner)
		{ }
	}
}
