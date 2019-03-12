using System;
using Prism.Build;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the processing logic of a <see cref="ContentWriter{Tin}"/> instance.
	/// </summary>
	public sealed class WriterContext : StageContext
	{
		#region Fields
		#endregion // Fields

		internal WriterContext(BuildTask task, PipelineLogger logger) :
			base(task, logger)
		{

		}
	}
}
