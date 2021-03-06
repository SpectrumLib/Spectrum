﻿using System;

namespace Spectrum.Graphics
{
	// Contains the drawing and drawing state function for GraphicsDevice
	public sealed partial class GraphicsDevice
	{
		#region State
		/// <summary>
		/// The current viewport to render to.
		/// </summary>
		public Viewport Viewport;
		/// <summary>
		/// The current render scissor limits.
		/// </summary>
		public Scissor Scissor;
		#endregion // State

		// Used to set the initial state for each frame.
		private void setInitialState()
		{
			var winSize = Application.Window.Size;
			Viewport = new Viewport(0, 0, (uint)winSize.X, (uint)winSize.Y);
		}
	}
}
