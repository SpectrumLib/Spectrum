/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The core rendering type. Contains a set of <see cref="RenderTarget"/>s that can be used as attachments in 
	/// multiple rendering passes, which are each described by a <see cref="Pipeline"/> object.
	/// </summary>
	public sealed class Renderer : IDisposable
	{
		#region Fields
		private bool _isDisposed = false;
		#endregion // Fields
		
		public Renderer()
		{

		}
		~Renderer()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}
		
		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{

			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
