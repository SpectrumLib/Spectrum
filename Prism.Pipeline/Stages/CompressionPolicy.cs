using System;

namespace Prism
{
	/// <summary>
	/// Per-<see cref="ContentWriter{Tin}"/> options for compressing content data.
	/// </summary>
	public enum CompressionPolicy
	{
		/// <summary>
		/// The content will be compressed only in Release builds, and only if the content project has not disabled
		/// compression.
		/// </summary>
		Default,
		/// <summary>
		/// The content will always be compressed, even in Debug mode, and even if the content project has disabled 
		/// compression.
		/// </summary>
		Always,
		/// <summary>
		/// The content will always be compressed in Release mode, even if the content project has disabled compression.
		/// </summary>
		ReleaseOnly,
		/// <summary>
		/// The content will never be compressed, regardless of build mode or project settings. If the
		/// <see cref="ContentWriter{Tin}"/> type applies its own compression, use this setting.
		/// </summary>
		Never
	}
}
