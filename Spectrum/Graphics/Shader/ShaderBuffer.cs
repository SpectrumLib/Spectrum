using System;

namespace Spectrum.Graphics
{
	// Maintains a monolithic buffer on the graphics device that is used to source shader uniform data
	// Each pipeline instance is given part of this buffer to use for its shader, and the shader uploads
	//   information into their portion to be sourced as uniforms.
	internal static class ShaderBuffer
	{
		#region Fields
		#endregion // Fields

		// Called at the beginning of the program to set up the buffer
		public static void CreateResources()
		{

		}

		// Called at the end of the program to release the resources
		public static void Cleanup()
		{

		}
	}
}
