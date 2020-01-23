/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Prism.Pipeline
{
	// Encapsultes the pipeline process on a single thread, managing the building of a single item at a time
	internal class BuildTask
	{
		#region Fields
		public readonly BuildEngine Engine;
		#endregion // Fields

		public BuildTask(BuildEngine engine)
		{
			Engine = engine;
		}

		public void Join()
		{

		}
	}
}
