using System;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the processing logic of a
	/// <see cref="ContentProcessor{Tin, Tout, Twriter}"/> instance.
	/// </summary>
	public sealed class ProcessorContext
	{
		#region Fields
		/// <summary>
		/// The logger to use to report messages inside of ContentProcessor instances.
		/// </summary>
		public readonly PipelineLogger Logger;

		/// <summary>
		/// If the build has requested statistics.
		/// </summary>
		public readonly bool UseStats;
		#endregion // Fields

		internal ProcessorContext(PipelineLogger logger, bool stats)
		{
			Logger = logger;
			UseStats = stats;
		}
	}
}
