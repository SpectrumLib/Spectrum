/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Prism.Pipeline
{
	// Contains information about a content project item to be built
	internal class BuildOrder
	{
		#region Fields
		// The item to build for the order
		public readonly ContentItem Item;
		// The item index
		public readonly uint Index;
		#endregion // Fields

		public BuildOrder(ContentItem item, uint idx)
		{
			Item = item;
			Index = idx;
		}
	}
}
