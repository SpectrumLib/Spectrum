using System;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the processing logic of a <see cref="ContentWriter{Tin}"/> instance.
	/// </summary>
	public sealed class WriterContext
	{
		#region Fields
		/// <summary>
		/// The logger to use to report messages inside of ContentWriter instances.
		/// </summary>
		public readonly PipelineLogger Logger;

		/// <summary>
		/// If the build has requested statistics.
		/// </summary>
		public readonly bool UseStats;
		#endregion // Fields

		internal WriterContext(PipelineLogger logger, bool stats)
		{
			Logger = logger;
			UseStats = stats;
		}
	}
}
