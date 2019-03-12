using System;
using Prism.Build;

namespace Prism
{
	/// <summary>
	/// Contains information and objects related to the processing logic of a
	/// <see cref="ContentProcessor{Tin, Tout, Twriter}"/> instance.
	/// </summary>
	public sealed class ProcessorContext : StageContext
	{
		#region Fields
		#endregion // Fields

		internal ProcessorContext(BuildTask task, PipelineLogger logger) :
			base(task, logger)
		{

		}
	}
}
