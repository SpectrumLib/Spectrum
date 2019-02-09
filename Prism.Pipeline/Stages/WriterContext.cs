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
		#endregion // Fields

		internal WriterContext(PipelineLogger logger)
		{
			Logger = logger;
		}
	}
}
