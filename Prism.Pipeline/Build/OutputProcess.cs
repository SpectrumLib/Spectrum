using System;

namespace Prism.Build
{
	// Manages outputting the content files to their final form, both when packed and when not packed
	internal class OutputProcess
	{
		#region Fields
		public readonly BuildEngine Engine;
		#endregion // Fields

		public OutputProcess(BuildEngine engine)
		{
			Engine = engine;
		}

		public void BuildMetadata()
		{

		}
	}
}
