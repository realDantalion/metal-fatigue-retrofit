// Metal Fatigue Retrofit
// Copyright (C) 2026 Dantalion (github.com/realDantalion)
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetalFatiguePatcher
{
    /// <summary>
    /// Plays an OGG file so the user can tell one track from another while sorting them.
    ///
    /// Decoding uses the game's OWN libvorbisfile.dll, loaded from the selected installation. That
    /// keeps the patcher a single dependency-free exe — the deliberate design of this project — and
    /// has a useful side effect: if a file plays here, it will play in the game, so the preview
    /// doubles as a format check. It also means the patcher must be a 32-bit process; those
    /// libraries are 32-bit, which is why the project sets PlatformTarget=x86.
    ///
    /// Output goes through waveOut from winmm, a Windows API, so nothing third-party is involved.
    /// Every failure path is silent-but-safe: the preview is an aid, never a gate on the import.
    /// </summary>
    internal sealed class MusicPreview : IDisposable
    {
        // ---- vorbisfile ----
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibrary(string path);

        // OggVorbis_File is passed as a raw pointer, never as a managed byte[]. The struct holds
        // pointers INTO ITSELF; a managed array is only pinned for the duration of one call, so the
        // GC may move it between them and those internal pointers then dangle. The symptom is
        // wonderfully misleading: the first second of audio plays, then ov_read quietly returns 0 as
        // if the file had ended.
        [DllImport("libvorbisfile.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int ov_fopen([MarshalAs(UnmanagedType.LPStr)] string path, IntPtr vf);
        [DllImport("libvorbisfile.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr ov_info(IntPtr vf, int link);
        [DllImport("libvorbisfile.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern double ov_time_total(IntPtr vf, int i);
        [DllImport("libvorbisfile.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int ov_time_seek(IntPtr vf, double s);
        [DllImport("libvorbisfile.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int ov_read(IntPtr vf, byte[] buffer, int length, int bigendian, int word, int signed, out int bitstream);
        [DllImport("libvorbisfile.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int ov_clear(IntPtr vf);

        // ---- winmm ----
        [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr h, int dev, byte[] fmt, IntPtr cb, IntPtr inst, int flags);
        [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr h, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr h, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr h, IntPtr hdr, int size);
        [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr h);
        [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr h);
        [DllImport("winmm.dll")] static extern int waveOutGetPosition(IntPtr h, byte[] mmtime, int size);
        [DllImport("winmm.dll")] static extern int waveOutSetVolume(IntPtr h, uint volume);

        // Buffer state lives in WAVEHDR.dwFlags. Read it, never write it: waveOutPrepareHeader sets
        // WHDR_PREPARED there, and overwriting the field clears that bit — waveOutWrite then rejects
        // every buffer with WAVERR_UNPREPARED and nothing plays, silently. A buffer is ours to refill
        // exactly when the driver is not holding it, i.e. WHDR_INQUEUE is clear.
        const int WHDR_INQUEUE = 0x10;
        const int BufferCount = 4;
        const int BufferBytes = 32768;      // ~0.19 s each at 44.1 kHz stereo 16-bit

        static bool _loaded;
        static readonly object LoadLock = new object();

        readonly string _gameDir;
        IntPtr _vf;                             // OggVorbis_File is opaque; 8 KB is comfortably large
        const int VfBytes = 8192;
        IntPtr _wave;
        IntPtr[] _hdrs = new IntPtr[BufferCount];
        IntPtr[] _data = new IntPtr[BufferCount];
        Thread _thread;
        volatile bool _run;
        int _rate, _channels;
        double _seekBase;
        long _bytesAtSeek;

        public double Length { get; private set; }
        public bool IsPlaying => _run;

        /// <summary>0.0 – 1.0. Kept across tracks, and applied again whenever a device is opened.</summary>
        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = Math.Max(0, Math.Min(1, value));
                ApplyVolume();
            }
        }
        double _volume = 0.7;

        void ApplyVolume()
        {
            if (_wave == IntPtr.Zero) return;
            // One 16-bit level per channel, right in the high word.
            uint one = (uint)Math.Round(_volume * 0xFFFF);
            try { waveOutSetVolume(_wave, (one << 16) | one); } catch { }
        }
        /// <summary>Set when playback gave up, so the caller can say why instead of just going quiet.</summary>
        public string LastError { get; private set; }

        public double Position
        {
            get
            {
                if (_wave == IntPtr.Zero || _rate <= 0) return 0;
                double played = (BytesPlayed() - _bytesAtSeek) / (double)(_rate * _channels * 2);
                return Math.Max(0, Math.Min(Length, _seekBase + played));
            }
        }

        public MusicPreview(string exePath)
        {
            _gameDir = Path.GetDirectoryName(exePath) ?? ".";
        }

        /// <summary>Load the game's codec DLLs by full path so the later DllImports resolve to them.</summary>
        void EnsureLoaded()
        {
            lock (LoadLock)
            {
                if (_loaded) return;
                foreach (var dll in new[] { "libogg.dll", "libvorbis.dll", "libvorbisfile.dll" })
                {
                    var p = Path.Combine(_gameDir, dll);
                    if (!File.Exists(p)) throw new FileNotFoundException(dll);
                    if (LoadLibrary(p) == IntPtr.Zero) throw new InvalidOperationException(dll);
                }
                _loaded = true;
            }
        }

        public void Play(string oggPath)
        {
            Stop();
            EnsureLoaded();

            if (_vf == IntPtr.Zero) _vf = Marshal.AllocHGlobal(VfBytes);
            for (int b = 0; b < VfBytes; b++) Marshal.WriteByte(_vf, b, 0);

            if (ov_fopen(oggPath, _vf) != 0) throw new InvalidOperationException("ov_fopen");

            var info = ov_info(_vf, -1);
            if (info == IntPtr.Zero) { ov_clear(_vf); throw new InvalidOperationException("ov_info"); }
            _channels = Marshal.ReadInt32(info, 4);
            _rate = Marshal.ReadInt32(info, 8);
            Length = ov_time_total(_vf, -1);
            _seekBase = 0;

            if (_rate <= 0 || _channels <= 0) { ov_clear(_vf); throw new InvalidOperationException("format"); }

            var fmt = WaveFormat(_channels, _rate);
            if (waveOutOpen(out _wave, -1, fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0)
            { ov_clear(_vf); _wave = IntPtr.Zero; throw new InvalidOperationException("waveOutOpen"); }
            ApplyVolume();   // a fresh device starts at whatever the driver felt like

            for (int i = 0; i < BufferCount; i++)
            {
                _data[i] = Marshal.AllocHGlobal(BufferBytes);
                _hdrs[i] = Marshal.AllocHGlobal(32);
                for (int b = 0; b < 32; b++) Marshal.WriteByte(_hdrs[i], b, 0);
                Marshal.WriteIntPtr(_hdrs[i], 0, _data[i]);
                Marshal.WriteInt32(_hdrs[i], 4, BufferBytes);
                waveOutPrepareHeader(_wave, _hdrs[i], 32);
                // No flag write here — after preparing, WHDR_INQUEUE is already clear, which is
                // exactly what the pump reads as "free".
            }

            _bytesAtSeek = 0;
            _run = true;
            _thread = new Thread(Pump) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
            _thread.Start();
        }

        void Pump()
        {
            var pcm = new byte[BufferBytes];
            try
            {
                while (_run)
                {
                    bool wrote = false;
                    for (int i = 0; i < BufferCount && _run; i++)
                    {
                        if ((Marshal.ReadInt32(_hdrs[i], 16) & WHDR_INQUEUE) != 0) continue;

                        // ov_read hands back one packet at a time, so fill the buffer in a loop.
                        int got = 0, bs;
                        while (got < BufferBytes)
                        {
                            int n = ov_read(_vf, pcm, BufferBytes - got, 0, 2, 1, out bs);
                            if (n <= 0) break;                       // 0 = end of stream
                            Marshal.Copy(pcm, 0, IntPtr.Add(_data[i], got), n);
                            got += n;
                        }
                        if (got == 0) { _run = false; break; }       // played to the end

                        // The header was prepared once in Play(); only the length changes per write.
                        Marshal.WriteInt32(_hdrs[i], 4, got);
                        int rc = waveOutWrite(_wave, _hdrs[i], 32);
                        if (rc != 0) { LastError = "waveOutWrite=" + rc; _run = false; break; }
                        wrote = true;
                    }
                    Thread.Sleep(wrote ? 5 : 20);
                }
            }
            catch { /* a failed preview must not take the dialog with it */ }
            _run = false;
        }

        public void SeekTo(double seconds)
        {
            if (_wave == IntPtr.Zero) return;
            try
            {
                // waveOutReset returns every queued buffer, which clears WHDR_INQUEUE on all of them —
                // so the pump sees them as free again without us touching the flags.
                waveOutReset(_wave);
                ov_time_seek(_vf, Math.Max(0, Math.Min(Length, seconds)));
                _seekBase = seconds;
                _bytesAtSeek = BytesPlayed();
            }
            catch { }
        }

        public void Stop()
        {
            _run = false;
            var t = _thread; _thread = null;
            if (t != null && t.IsAlive) t.Join(400);

            if (_wave != IntPtr.Zero)
            {
                try { waveOutReset(_wave); } catch { }
                for (int i = 0; i < BufferCount; i++)
                {
                    if (_hdrs[i] != IntPtr.Zero)
                    {
                        try { waveOutUnprepareHeader(_wave, _hdrs[i], 32); } catch { }
                        Marshal.FreeHGlobal(_hdrs[i]); _hdrs[i] = IntPtr.Zero;
                    }
                    if (_data[i] != IntPtr.Zero) { Marshal.FreeHGlobal(_data[i]); _data[i] = IntPtr.Zero; }
                }
                try { waveOutClose(_wave); } catch { }
                _wave = IntPtr.Zero;
                if (_vf != IntPtr.Zero) { try { ov_clear(_vf); } catch { } }
            }
            Length = 0; _seekBase = 0; _bytesAtSeek = 0;
        }

        const int TIME_BYTES = 4;
        const int MMTIME_SIZE = 12;   // UINT wType + an 8-byte union (its largest member is smpte)

        long BytesPlayed()
        {
            // The size has to be exactly sizeof(MMTIME). Passing 8 — which is what the union looks
            // like if you only count the DWORD members — makes waveOutGetPosition fail outright, and
            // then the position reads as zero forever: a still seek bar and a clock stuck at 0:00.
            var mm = new byte[MMTIME_SIZE];
            mm[0] = TIME_BYTES;
            if (waveOutGetPosition(_wave, mm, MMTIME_SIZE) != 0) return 0;
            if (BitConverter.ToUInt32(mm, 0) != TIME_BYTES) return 0;   // driver answered in other units
            return BitConverter.ToUInt32(mm, 4);
        }

        static byte[] WaveFormat(int channels, int rate)
        {
            var f = new byte[18];
            int blockAlign = channels * 2;
            BitConverter.GetBytes((ushort)1).CopyTo(f, 0);                       // PCM
            BitConverter.GetBytes((ushort)channels).CopyTo(f, 2);
            BitConverter.GetBytes(rate).CopyTo(f, 4);
            BitConverter.GetBytes(rate * blockAlign).CopyTo(f, 8);
            BitConverter.GetBytes((ushort)blockAlign).CopyTo(f, 12);
            BitConverter.GetBytes((ushort)16).CopyTo(f, 14);
            BitConverter.GetBytes((ushort)0).CopyTo(f, 16);
            return f;
        }

        public void Dispose()
        {
            Stop();
            if (_vf != IntPtr.Zero) { Marshal.FreeHGlobal(_vf); _vf = IntPtr.Zero; }
        }
    }
}
