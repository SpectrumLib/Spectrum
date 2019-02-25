using System;

namespace Prism.Builtin
{
	// Contains RLAD ("run-length accumulating difference") formatted data from raw PCM data
	internal class RLADAudio : IDisposable
	{
		#region Fields
		private bool _isDisposed = false;
		#endregion // Fields

		~RLADAudio()
		{
			Dispose();
		}

		public void Dispose()
		{
			_isDisposed = false;
		}
	}
}
