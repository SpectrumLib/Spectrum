using System;
using System.Diagnostics;
using System.Threading;

namespace Spectrum
{
	// Contains code for accurate sleeping to implement framerate limiting
	internal static class TimestepUtils
	{
		// This is the *assumed* precision of the Thread.Sleep function in milliseconds.
		public const float SLEEP_ACCURACY_MS = 1;

		// Assuming some small level of function call overhead and scheduling burps, this is the absolute minimum time
		// that will be passed to Thread.Sleep
		public const float MIN_SLEEP_TIME_MS = SLEEP_ACCURACY_MS - 0.05f;

		// Used to track sleep amounts
		private static readonly Stopwatch s_sw = Stopwatch.StartNew();

		// Performs a wait, taking into account the already elapsed time, and the target time to wait
		public static void WaitFor(float target, float elapsed)
		{
			float diff = target - elapsed;
			//if (diff <= 0.05) // We dont have the capacity (or want, really) to deal with waits less than 50 us 
			//	return;

			s_sw.Restart();
			//if (diff >= SLEEP_ACCURACY_MS)
			//{
			//	int sleepcount = (int)(diff / SLEEP_ACCURACY_MS);
			//	float sleepamt = sleepcount * MIN_SLEEP_TIME_MS;
			//	Thread.Sleep((int)sleepamt);
			//}

			while ((float)s_sw.Elapsed.TotalMilliseconds < diff) ;
		}
	}
}
