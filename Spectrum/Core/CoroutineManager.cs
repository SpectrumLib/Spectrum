using System;

namespace Spectrum
{
	// Internal class that controls the lifetimes and ticking of coroutine instances
	internal static class CoroutineManager
	{
		public static void AddCoroutine(Coroutine c)
		{
			c.Running = true;
		}
	}
}
