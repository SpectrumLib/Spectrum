/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Audio
{
	// Contains the OpenAL constants
	internal sealed partial class OpenAL : IDisposable
	{
		public static partial class AL
		{
			public const int NONE = 0;
			public const int FALSE = 0;
			public const int TRUE = 1;
			public const int SOURCE_RELATIVE = 0x202;
			public const int CONE_INNER_ANGLE = 0x1001;
			public const int CONE_OUTER_ANGLE = 0x1002;
			public const int PITCH = 0x1003;
			public const int POSITION = 0x1004;
			public const int DIRECTION = 0x1005;
			public const int VELOCITY = 0x1006;
			public const int LOOPING = 0x1007;
			public const int BUFFER = 0x1009;
			public const int GAIN = 0x100A;
			public const int MIN_GAIN = 0x100D;
			public const int MAX_GAIN = 0x100E;
			public const int ORIENTATION = 0x100F;
			public const int SOURCE_STATE = 0x1010;
			public const int INITIAL = 0x1011;
			public const int PLAYING = 0x1012;
			public const int PAUSED = 0x1013;
			public const int STOPPED = 0x1014;
			public const int BUFFERS_QUEUED = 0x1015;
			public const int BUFFERS_PROCESSED = 0x1016;
			public const int REFERENCE_DISTANCE = 0x1020;
			public const int ROLLOFF_FACTOR = 0x1021;
			public const int CONE_OUTER_GAIN = 0x1022;
			public const int MAX_DISTANCE = 0x1023;
			public const int SEC_OFFSET = 0x1024;
			public const int SAMPLE_OFFSET = 0x1025;
			public const int BYTE_OFFSET = 0x1026;
			public const int SOURCE_TYPE = 0x1027;
			public const int STATIC = 0x1028;
			public const int STREAMING = 0x1029;
			public const int UNDETERMINED = 0x1030;
			public const int FORMAT_MONO8 = 0x1100;
			public const int FORMAT_MONO16 = 0x1101;
			public const int FORMAT_STEREO8 = 0x1102;
			public const int FORMAT_STEREO16 = 0x1103;
			public const int FREQUENCY = 0x2001;
			public const int BITS = 0x2002;
			public const int CHANNELS = 0x2003;
			public const int SIZE = 0x2004;
			public const int UNUSED = 0x2010;
			public const int PENDING = 0x2011;
			public const int PROCESSED = 0x2012;
			public const int NO_ERROR = 0;
			public const int INVALID_NAME = 0xA001;
			public const int INVALID_ENUM = 0xA002;
			public const int INVALID_VALUE = 0xA003;
			public const int INVALID_OPERATION = 0xA004;
			public const int OUT_OF_MEMORY = 0xA005;
			public const int VENDOR = 0xB001;
			public const int VERSION = 0xB002;
			public const int RENDERER = 0xB003;
			public const int EXTENSIONS = 0xB004;
			public const int DOPPLER_FACTOR = 0xC000;
			public const int DOPPLER_VELOCITY = 0xC001;
			public const int SPEED_OF_SOUND = 0xC003;
			public const int DISTANCE_MODEL = 0xD000;
			public const int INVERSE_DISTANCE = 0xD001;
			public const int INVERSE_DISTANCE_CLAMPED = 0xD002;
			public const int LINEAR_DISTANCE = 0xD003;
			public const int LINEAR_DISTANCE_CLAMPED = 0xD004;
			public const int EXPONENT_DISTANCE = 0xD005;
			public const int EXPONENT_DISTANCE_CLAMPED = 0xD006;
		}

		public static partial class ALC
		{
			public const int FALSE = 0;
			public const int TRUE = 1;
			public const int FREQUENCY = 0x1007;
			public const int REFRESH = 0x1008;
			public const int SYNC = 0x1009;
			public const int MONO_SOURCES = 0x1010;
			public const int STEREO_SOURCES = 0x1011;
			public const int NO_ERROR = 0;
			public const int INVALID_DEVICE = 0xA001;
			public const int INVALID_CONTEXT = 0xA002;
			public const int INVALID_ENUM = 0xA003;
			public const int INVALID_VALUE = 0xA004;
			public const int OUT_OF_MEMORY = 0xA005;
			public const int MAJOR_VERSION = 0x1000;
			public const int MINOR_VERSION = 0x1001;
			public const int ATTRIBUTES_SIZE = 0x1002;
			public const int ALL_ATTRIBUTES = 0x1003;
			public const int DEFAULT_DEVICE_SPECIFIER = 0x1004;
			public const int DEVICE_SPECIFIER = 0x1005;
			public const int EXTENSIONS = 0x1006;
			public const int EXT_CAPTURE = 1;
			public const int CAPTURE_DEVICE_SPECIFIER = 0x310;
			public const int CAPTURE_DEFAULT_DEVICE_SPECIFIER = 0x311;
			public const int CAPTURE_SAMPLES = 0x312;
			public const int ENUMERATE_ALL_EXT = 1;
			public const int DEFAULT_ALL_DEVICES_SPECIFIER = 0x1012;
			public const int ALL_DEVICES_SPECIFIER = 0x1013;
		}

		public static partial class Ext
		{
			public const int FORMAT_MONO_FLOAT32 = 0x10010;
			public const int FORMAT_STEREO_FLOAT32 = 0x10011;
		}
	}
}
