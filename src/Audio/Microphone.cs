#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2015 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	public class Microphone
	{
		#region Public Static Properties

		public static ReadOnlyCollection<Microphone> All
		{
			get;
			internal set;
		}

		public static Microphone Default
		{
			get
			{
				if (All.Count == 0)
				{
					return null;
				}
				return All[0];
			}
		}

		#endregion

		#region Public Properties

		public TimeSpan BufferDuration
		{
			get
			{
				return bufferDuration;
			}
			set
			{
				if (	value.Milliseconds < 100 ||
					value.Milliseconds > 1000 ||
					value.Milliseconds % 10 != 0	)
				{
					throw new ArgumentOutOfRangeException();
				}
				bufferDuration = value;
			}
		}

		public bool IsHeadset
		{
			get
			{
				// FIXME: I think this is just for Windows Phone? -flibit
				return false;
			}
		}

		public int SampleRate
		{
			get
			{
				return SAMPLERATE;
			}
		}

		public MicrophoneState State
		{
			get;
			private set;
		}

		#endregion

		#region Public Variables

		public readonly string Name;

		#endregion

		#region Private Variables

		private TimeSpan bufferDuration;
		private IntPtr nativeMic;

		#endregion

		#region Events

		// TODO: Hook this up! -flibit
#pragma warning disable 0067
		public event EventHandler<EventArgs> BufferReady;
#pragma warning restore 0067

		#endregion

		#region Private Constants

		/* FIXME: This is what XNA4 aims for, but it _could_ be lower.
		 * Something work looking at is falling back to lower sample rates in
		 * powers of two, i.e. 44100, 22050, 11025, etc.
		 * -flibit
		 */
		private const int SAMPLERATE = 44100;

		#endregion

		#region Internal Constructor

		internal Microphone(string name)
		{
			Name = name;
			bufferDuration = TimeSpan.FromSeconds(1.0);
			State = MicrophoneState.Stopped;
		}

		#endregion

		#region Public Methods

		public int GetData(byte[] buffer)
		{
			return GetData(buffer, 0, buffer.Length);
		}

		public int GetData(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentException("buffer is null!");
			}
			if (offset < 0 || offset > buffer.Length)
			{
				throw new ArgumentException("offset");
			}
			if (count <= 0 || (offset + count) > buffer.Length)
			{
				throw new ArgumentException("count");
			}
			GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			int read = AudioDevice.ALDevice.CaptureSamples(
				nativeMic,
				handle.AddrOfPinnedObject() + offset,
				count
			);
			handle.Free();
			System.Console.WriteLine(read);
			return read;
		}

		public TimeSpan GetSampleDuration(int sizeInBytes)
		{
			return SoundEffect.GetSampleDuration(
				sizeInBytes,
				SampleRate,
				AudioChannels.Mono
			);
		}

		public int GetSampleSizeInBytes(TimeSpan duration)
		{
			return SoundEffect.GetSampleSizeInBytes(
				duration,
				SampleRate,
				AudioChannels.Mono
			);
		}

		public void Start()
		{
			if (State == MicrophoneState.Stopped)
			{
				nativeMic = AudioDevice.ALDevice.StartDeviceCapture(
					Name,
					SampleRate,
					GetSampleSizeInBytes(bufferDuration)
				);
				if (nativeMic == IntPtr.Zero)
				{
					throw new NoMicrophoneConnectedException(Name);
				}
				State = MicrophoneState.Started;
			}
		}

		public void Stop()
		{
			if (State == MicrophoneState.Started)
			{
				AudioDevice.ALDevice.StopDeviceCapture(nativeMic);
				nativeMic = IntPtr.Zero;
				State = MicrophoneState.Stopped;
			}
		}

		#endregion
	}
}