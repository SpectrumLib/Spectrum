/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using static Spectrum.InternalLog;

namespace Spectrum.Audio
{
	internal sealed partial class OpenAL : IDisposable
	{
		#region Fields
		private IntPtr _library = IntPtr.Zero;

		#region AL Functions
		public readonly AL.DopplerFactor DopplerFactor;
		public readonly AL.DopplerVelocity DopplerVelocity;
		public readonly AL.SpeedOfSound SpeedOfSound;
		public readonly AL.DistanceModel DistanceModel;
		public readonly AL.Enable Enable;
		public readonly AL.Disable Disable;
		public readonly AL.IsEnabled IsEnabled;
		public readonly AL.GetString GetString;
		public readonly AL.GetBooleanv GetBooleanv;
		public readonly AL.GetIntegerv GetIntegerv;
		public readonly AL.GetFloatv GetFloatv;
		public readonly AL.GetDoublev GetDoublev;
		public readonly AL.GetBoolean GetBoolean;
		public readonly AL.GetInteger GetInteger;
		public readonly AL.GetFloat GetFloat;
		public readonly AL.GetDouble GetDouble;
		public readonly AL.GetError GetError;
		public readonly AL.IsExtensionPresent IsExtensionPresent;
		public readonly AL.GetProcAddress GetProcAddress;
		public readonly AL.GetEnumValue GetEnumValue;
		public readonly AL.Listenerf Listenerf;
		public readonly AL.Listener3f Listener3f;
		public readonly AL.Listenerfv Listenerfv;
		public readonly AL.Listeneri Listeneri;
		public readonly AL.Listener3i Listener3i;
		public readonly AL.Listeneriv Listeneriv;
		public readonly AL.GetListenerf GetListenerf;
		public readonly AL.GetListener3f GetListener3f;
		public readonly AL.GetListenerfv GetListenerfv;
		public readonly AL.GetListeneri GetListeneri;
		public readonly AL.GetListener3i GetListener3i;
		public readonly AL.GetListeneriv GetListeneriv;
		public readonly AL.GenSources GenSources;
		public readonly AL.DeleteSources DeleteSources;
		public readonly AL.IsSource IsSource;
		public readonly AL.Sourcef Sourcef;
		public readonly AL.Source3f Source3f;
		public readonly AL.Sourcefv Sourcefv;
		public readonly AL.Sourcei Sourcei;
		public readonly AL.Source3i Source3i;
		public readonly AL.Sourceiv Sourceiv;
		public readonly AL.GetSourcef GetSourcef;
		public readonly AL.GetSource3f GetSource3f;
		public readonly AL.GetSourcefv GetSourcefv;
		public readonly AL.GetSourcei GetSourcei;
		public readonly AL.GetSource3i GetSource3i;
		public readonly AL.GetSourceiv GetSourceiv;
		public readonly AL.SourcePlayv SourcePlayv;
		public readonly AL.SourceStopv SourceStopv;
		public readonly AL.SourceRewindv SourceRewindv;
		public readonly AL.SourcePausev SourcePausev;
		public readonly AL.SourcePlay SourcePlay;
		public readonly AL.SourceStop SourceStop;
		public readonly AL.SourceRewind SourceRewind;
		public readonly AL.SourcePause SourcePause;
		public readonly AL.SourceQueueBuffers SourceQueueBuffers;
		public readonly AL.SourceUnqueueBuffers SourceUnqueueBuffers;
		public readonly AL.GenBuffers GenBuffers;
		public readonly AL.DeleteBuffers DeleteBuffers;
		public readonly AL.IsBuffer IsBuffer;
		public readonly AL.BufferData BufferData;
		public readonly AL.Bufferf Bufferf;
		public readonly AL.Buffer3f Buffer3f;
		public readonly AL.Bufferfv Bufferfv;
		public readonly AL.Bufferi Bufferi;
		public readonly AL.Buffer3i Buffer3i;
		public readonly AL.Bufferiv Bufferiv;
		public readonly AL.GetBufferf GetBufferf;
		public readonly AL.GetBuffer3f GetBuffer3f;
		public readonly AL.GetBufferfv GetBufferfv;
		public readonly AL.GetBufferi GetBufferi;
		public readonly AL.GetBuffer3i GetBuffer3i;
		public readonly AL.GetBufferiv GetBufferiv;
		#endregion // AL Functions

		#region ALC Functions
		public readonly ALC.CreateContext AlcCreateContext;
		public readonly ALC.MakeContextCurrent AlcMakeContextCurrent;
		public readonly ALC.ProcessContext AlcProcessContext;
		public readonly ALC.SuspendContext AlcSuspendContext;
		public readonly ALC.DestroyContext AlcDestroyContext;
		public readonly ALC.GetCurrentContext AlcGetCurrentContext;
		public readonly ALC.GetContextsDevice AlcGetContextsDevice;
		public readonly ALC.OpenDevice AlcOpenDevice;
		public readonly ALC.CloseDevice AlcCloseDevice;
		public readonly ALC.GetError AlcGetError;
		public readonly ALC.IsExtensionPresent AlcIsExtensionPresent;
		public readonly ALC.GetProcAddress AlcGetProcAddress;
		public readonly ALC.GetEnumValue AlcGetEnumValue;
		public readonly ALC.GetString AlcGetString;
		public readonly ALC.GetIntegerv AlcGetIntegerv;
		public readonly ALC.CaptureOpenDevice AlcCaptureOpenDevice;
		public readonly ALC.CaptureCloseDevice AlcCaptureCloseDevice;
		public readonly ALC.CaptureStart AlcCaptureStart;
		public readonly ALC.CaptureStop AlcCaptureStop;
		public readonly ALC.CaptureSamples AlcCaptureSamples;
		#endregion // ALC Functions
		#endregion // Fields

		public OpenAL()
		{
			_library = Native.NativeLoader.LoadLibrary("openal", "libopenal.so.1",
				(lib, @new, time) => IINFO($"Loaded {(@new ? "new" : "existing")} native library '{lib}' in {time.TotalMilliseconds:.000}ms."));

			Stopwatch timer = Stopwatch.StartNew();

			DopplerFactor = loadAlFunc<AL.DopplerFactor>();
			DopplerVelocity = loadAlFunc<AL.DopplerVelocity>();
			SpeedOfSound = loadAlFunc<AL.SpeedOfSound>();
			DistanceModel = loadAlFunc<AL.DistanceModel>();
			Enable = loadAlFunc<AL.Enable>();
			Disable = loadAlFunc<AL.Disable>();
			IsEnabled = loadAlFunc<AL.IsEnabled>();
			GetString = loadAlFunc<AL.GetString>();
			GetBooleanv = loadAlFunc<AL.GetBooleanv>();
			GetIntegerv = loadAlFunc<AL.GetIntegerv>();
			GetFloatv = loadAlFunc<AL.GetFloatv>();
			GetDoublev = loadAlFunc<AL.GetDoublev>();
			GetBoolean = loadAlFunc<AL.GetBoolean>();
			GetInteger = loadAlFunc<AL.GetInteger>();
			GetFloat = loadAlFunc<AL.GetFloat>();
			GetDouble = loadAlFunc<AL.GetDouble>();
			GetError = loadAlFunc<AL.GetError>();
			IsExtensionPresent = loadAlFunc<AL.IsExtensionPresent>();
			GetProcAddress = loadAlFunc<AL.GetProcAddress>();
			GetEnumValue = loadAlFunc<AL.GetEnumValue>();
			Listenerf = loadAlFunc<AL.Listenerf>();
			Listener3f = loadAlFunc<AL.Listener3f>();
			Listenerfv = loadAlFunc<AL.Listenerfv>();
			Listeneri = loadAlFunc<AL.Listeneri>();
			Listener3i = loadAlFunc<AL.Listener3i>();
			Listeneriv = loadAlFunc<AL.Listeneriv>();
			GetListenerf = loadAlFunc<AL.GetListenerf>();
			GetListener3f = loadAlFunc<AL.GetListener3f>();
			GetListenerfv = loadAlFunc<AL.GetListenerfv>();
			GetListeneri = loadAlFunc<AL.GetListeneri>();
			GetListener3i = loadAlFunc<AL.GetListener3i>();
			GetListeneriv = loadAlFunc<AL.GetListeneriv>();
			GenSources = loadAlFunc<AL.GenSources>();
			DeleteSources = loadAlFunc<AL.DeleteSources>();
			IsSource = loadAlFunc<AL.IsSource>();
			Sourcef = loadAlFunc<AL.Sourcef>();
			Source3f = loadAlFunc<AL.Source3f>();
			Sourcefv = loadAlFunc<AL.Sourcefv>();
			Sourcei = loadAlFunc<AL.Sourcei>();
			Source3i = loadAlFunc<AL.Source3i>();
			Sourceiv = loadAlFunc<AL.Sourceiv>();
			GetSourcef = loadAlFunc<AL.GetSourcef>();
			GetSource3f = loadAlFunc<AL.GetSource3f>();
			GetSourcefv = loadAlFunc<AL.GetSourcefv>();
			GetSourcei = loadAlFunc<AL.GetSourcei>();
			GetSource3i = loadAlFunc<AL.GetSource3i>();
			GetSourceiv = loadAlFunc<AL.GetSourceiv>();
			SourcePlayv = loadAlFunc<AL.SourcePlayv>();
			SourceStopv = loadAlFunc<AL.SourceStopv>();
			SourceRewindv = loadAlFunc<AL.SourceRewindv>();
			SourcePausev = loadAlFunc<AL.SourcePausev>();
			SourcePlay = loadAlFunc<AL.SourcePlay>();
			SourceStop = loadAlFunc<AL.SourceStop>();
			SourceRewind = loadAlFunc<AL.SourceRewind>();
			SourcePause = loadAlFunc<AL.SourcePause>();
			SourceQueueBuffers = loadAlFunc<AL.SourceQueueBuffers>();
			SourceUnqueueBuffers = loadAlFunc<AL.SourceUnqueueBuffers>();
			GenBuffers = loadAlFunc<AL.GenBuffers>();
			DeleteBuffers = loadAlFunc<AL.DeleteBuffers>();
			IsBuffer = loadAlFunc<AL.IsBuffer>();
			BufferData = loadAlFunc<AL.BufferData>();
			Bufferf = loadAlFunc<AL.Bufferf>();
			Buffer3f = loadAlFunc<AL.Buffer3f>();
			Bufferfv = loadAlFunc<AL.Bufferfv>();
			Bufferi = loadAlFunc<AL.Bufferi>();
			Buffer3i = loadAlFunc<AL.Buffer3i>();
			Bufferiv = loadAlFunc<AL.Bufferiv>();
			GetBufferf = loadAlFunc<AL.GetBufferf>();
			GetBuffer3f = loadAlFunc<AL.GetBuffer3f>();
			GetBufferfv = loadAlFunc<AL.GetBufferfv>();
			GetBufferi = loadAlFunc<AL.GetBufferi>();
			GetBuffer3i = loadAlFunc<AL.GetBuffer3i>();
			GetBufferiv = loadAlFunc<AL.GetBufferiv>();

			AlcCreateContext = loadAlcFunc<ALC.CreateContext>();
			AlcMakeContextCurrent = loadAlcFunc<ALC.MakeContextCurrent>();
			AlcProcessContext = loadAlcFunc<ALC.ProcessContext>();
			AlcSuspendContext = loadAlcFunc<ALC.SuspendContext>();
			AlcDestroyContext = loadAlcFunc<ALC.DestroyContext>();
			AlcGetCurrentContext = loadAlcFunc<ALC.GetCurrentContext>();
			AlcGetContextsDevice = loadAlcFunc<ALC.GetContextsDevice>();
			AlcOpenDevice = loadAlcFunc<ALC.OpenDevice>();
			AlcCloseDevice = loadAlcFunc<ALC.CloseDevice>();
			AlcGetError = loadAlcFunc<ALC.GetError>();
			AlcIsExtensionPresent = loadAlcFunc<ALC.IsExtensionPresent>();
			AlcGetProcAddress = loadAlcFunc<ALC.GetProcAddress>();
			AlcGetEnumValue = loadAlcFunc<ALC.GetEnumValue>();
			AlcGetString = loadAlcFunc<ALC.GetString>();
			AlcGetIntegerv = loadAlcFunc<ALC.GetIntegerv>();
			AlcCaptureOpenDevice = loadAlcFunc<ALC.CaptureOpenDevice>();
			AlcCaptureCloseDevice = loadAlcFunc<ALC.CaptureCloseDevice>();
			AlcCaptureStart = loadAlcFunc<ALC.CaptureStart>();
			AlcCaptureStop = loadAlcFunc<ALC.CaptureStop>();
			AlcCaptureSamples = loadAlcFunc<ALC.CaptureSamples>();

			IINFO($"Loaded OpenAL functions in {timer.Elapsed.TotalMilliseconds:.000}ms.");
		}
		~OpenAL()
		{
			Dispose();
		}

		#region IDisposable
		public void Dispose()
		{
			if (_library != IntPtr.Zero)
			{
				Native.NativeLoader.FreeLibrary(_library);
				_library = IntPtr.Zero;
			}
		}
		#endregion // IDisposable
	}
}
