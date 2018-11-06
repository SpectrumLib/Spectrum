using System;
using System.Collections.Generic;
using System.Linq;
using OpenAL;

namespace Spectrum.Audio
{
	/// <summary>
	/// Descriptor for devices on the host system that support audio playback.
	/// </summary>
	/// <remarks>
	/// Currently only the name of the device is used. In the future we will support checking device extensions and
	/// other stats.
	/// </remarks>
	public struct PlaybackDevice
	{
		private static readonly List<PlaybackDevice> s_devices = new List<PlaybackDevice>();
		/// <summary>
		/// A list of all of the audio playback devices on the current system.
		/// </summary>
		public static IReadOnlyList<PlaybackDevice> Devices => s_devices;

		#region Fields
		/// <summary>
		/// The human-readable name of the playback device.
		/// </summary>
		public readonly string Name;
		// Full device name used to specify it to OpenAL
		internal readonly string Identifier;
		#endregion // Fields

		internal PlaybackDevice(string name)
		{
			Name = name.Substring(15);
			Identifier = name;
		}

		internal static void PopulateDeviceList()
		{
			string[] dnames = ALUtils.GetALCString(ALC11.ALC_ALL_DEVICES_SPECIFIER, 0).Split('\n');
			s_devices.AddRange(dnames.Select(name => new PlaybackDevice(name)));
		}
	}
}
