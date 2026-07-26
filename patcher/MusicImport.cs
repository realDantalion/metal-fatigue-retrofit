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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MetalFatiguePatcher
{
    /// <summary>
    /// Imports the CD 1 (Rimtech) music the re-release is missing. The patcher ships no audio and
    /// links to no source — the player supplies ten OGG files and this puts them where the game looks.
    ///
    /// The game addresses music by track NUMBER, so the files have to land on the right slot:
    /// Track24..28 are the quiet set, Track29..33 the action set, each in the order they had on the
    /// disc. Filenames cannot be trusted for that — every source names them differently — so the
    /// mapping is derived from playing time, which survives re-encoding. The player confirms (and can
    /// correct) the result before anything is written.
    /// </summary>
    public static class MusicImport
    {
        /// <summary>Playing time of each slot, in seconds, in CD order: 24..33.</summary>
        public static readonly double[] SlotSeconds =
        {
            161.999, 116.886,  98.026, 118.715, 116.024,   // 24..28  quiet
             67.488,  85.774,  55.211,  58.711,  70.701,   // 29..33  action
        };

        /// <summary>How far a file's length may sit from a slot's reference and still match.
        /// The closest two references are 0.862 s apart, so 0.4 s cannot produce an ambiguity;
        /// re-encoding a file typically shifts its length by well under 100 ms.</summary>
        public const double ToleranceSeconds = 0.4;

        public enum Confidence { Exact, Duration, Uncertain }

        public sealed class Candidate
        {
            public string Path;
            public string Name => System.IO.Path.GetFileName(Path);
            public double Seconds;
            public int Channels;
            public int SampleRate;
            public long Bytes;
            public int Slot = -1;              // 24..33 once assigned
            public Confidence Confidence = Confidence.Uncertain;

            public double SlotSeconds => Slot < 0 ? 0 : MusicImport.SlotSeconds[Slot - PatchData.MusicFirstSlot];
            public double Deviation => Slot < 0 ? 0 : Math.Abs(Seconds - SlotSeconds);
        }

        public sealed class Scan
        {
            public List<Candidate> Tracks = new List<Candidate>();
            public List<string> OtherFormats = new List<string>();   // mp3/wav/... found instead
            public string TempDir;                                   // set when a zip was extracted
            public string Error;                                     // localised key, null if fine
        }

        static readonly string[] OtherAudio = { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".wma" };

        /// <summary>
        /// Collect the OGG files from a folder or zip. Recurses, so an archive with a subfolder
        /// works too. Other audio formats are noted separately: the game reads only OGG, and saying
        /// so beats reporting "nothing found" when a source shipped MP3s.
        /// </summary>
        public static Scan Collect(string folderOrZip)
        {
            var scan = new Scan();
            string root = folderOrZip;

            try
            {
                if (File.Exists(folderOrZip) &&
                    string.Equals(Path.GetExtension(folderOrZip), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    scan.TempDir = Path.Combine(Path.GetTempPath(), "MFRetrofitMusic_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(scan.TempDir);
                    System.IO.Compression.ZipFile.ExtractToDirectory(folderOrZip, scan.TempDir);
                    root = scan.TempDir;
                }
                else if (!Directory.Exists(folderOrZip))
                {
                    scan.Error = "music.err.noSource";
                    return scan;
                }

                foreach (var f in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".ogg")
                    {
                        var c = Probe(f);
                        if (c != null) scan.Tracks.Add(c);
                    }
                    else if (Array.IndexOf(OtherAudio, ext) >= 0)
                        scan.OtherFormats.Add(f);
                }
            }
            catch (Exception ex)
            {
                scan.Error = "music.err.read";
                System.Diagnostics.Debug.WriteLine(ex);
                return scan;
            }

            if (scan.Tracks.Count == 0)
                scan.Error = scan.OtherFormats.Count > 0 ? "music.err.wrongFormat" : "music.err.noOgg";
            else if (scan.Tracks.Count != PatchData.MusicSlotCount)
                scan.Error = "music.err.count";

            return scan;
        }

        /// <summary>
        /// Read an Ogg Vorbis file's channel count, sample rate and playing time without decoding it:
        /// the identification header gives the format, and the granule position on the last page is
        /// the total sample count. Returns null if the file is not Ogg Vorbis.
        /// </summary>
        public static Candidate Probe(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (info.Length < 64) return null;

                byte[] head = Read(path, 0, (int)Math.Min(info.Length, 8192));
                if (head.Length < 4 || head[0] != 'O' || head[1] != 'g' || head[2] != 'g' || head[3] != 'S')
                    return null;

                int id = IndexOf(head, new byte[] { 0x01, (byte)'v', (byte)'o', (byte)'r', (byte)'b', (byte)'i', (byte)'s' });
                if (id < 0 || id + 16 > head.Length) return null;

                int channels = head[id + 11];
                int rate = BitConverter.ToInt32(head, id + 12);
                if (rate <= 0 || channels <= 0) return null;

                // The last page starts with "OggS" and carries the final granule position at +6.
                int tailLen = (int)Math.Min(info.Length, 65536);
                byte[] tail = Read(path, info.Length - tailLen, tailLen);
                int last = LastIndexOf(tail, new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' });
                if (last < 0 || last + 14 > tail.Length) return null;

                ulong granule = BitConverter.ToUInt64(tail, last + 6);

                return new Candidate
                {
                    Path = path,
                    Seconds = granule / (double)rate,
                    Channels = channels,
                    SampleRate = rate,
                    Bytes = info.Length,
                };
            }
            catch { return null; }
        }

        /// <summary>
        /// Assign each file to a slot: every slot taken exactly once, total deviation minimised.
        /// Ten files means ten factorial permutations is far too many to brute-force, but the
        /// references are so far apart that a greedy pass over the globally closest pairs finds the
        /// optimum; anything it cannot place within tolerance is flagged to be sorted out by hand.
        /// </summary>
        public static void Assign(List<Candidate> tracks)
        {
            foreach (var t in tracks) { t.Slot = -1; t.Confidence = Confidence.Uncertain; }

            var free = Enumerable.Range(0, SlotSeconds.Length).ToList();
            var open = new List<Candidate>(tracks);

            while (open.Count > 0 && free.Count > 0)
            {
                Candidate bestT = null; int bestS = -1; double best = double.MaxValue;
                foreach (var t in open)
                    foreach (var s in free)
                    {
                        double d = Math.Abs(t.Seconds - SlotSeconds[s]);
                        if (d < best) { best = d; bestT = t; bestS = s; }
                    }
                if (bestT == null) break;

                bestT.Slot = PatchData.MusicFirstSlot + bestS;
                bestT.Confidence = best <= ToleranceSeconds ? Confidence.Duration : Confidence.Uncertain;
                open.Remove(bestT);
                free.Remove(bestS);
            }

            // Leftovers (only possible if the count is off) get the remaining slots in file order.
            foreach (var t in open)
            {
                if (free.Count == 0) break;
                t.Slot = PatchData.MusicFirstSlot + free[0];
                t.Confidence = Confidence.Uncertain;
                free.RemoveAt(0);
            }
        }

        /// <summary>
        /// The game's MUSIC folder, or null when the caller has no usable game path yet — which is
        /// the normal state until an installation is selected. Path.GetDirectoryName throws on an
        /// empty or malformed path rather than returning null, so every read-only caller below has
        /// to be able to answer "nothing there" without the exception escaping.
        /// </summary>
        public static string MusicDir(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath)) return null;
            try { return Path.Combine(Path.GetDirectoryName(exePath) ?? ".", "MUSIC"); }
            catch { return null; }
        }

        /// <summary>Copy the assigned files into MUSIC\TrackNN.ogg and verify each one landed.</summary>
        public static void Install(string exePath, IEnumerable<Candidate> tracks, Action<string> log)
        {
            var dir = MusicDir(exePath);
            if (dir == null) throw new ArgumentException("no game path to install into", "exePath");
            Directory.CreateDirectory(dir);

            foreach (var t in tracks.Where(x => x.Slot >= 0).OrderBy(x => x.Slot))
            {
                var dst = Path.Combine(dir, string.Format("Track{0:00}.ogg", t.Slot));
                File.Copy(t.Path, dst, true);

                var check = Probe(dst);
                if (check == null || Math.Abs(check.Seconds - t.Seconds) > 0.05)
                    throw new InvalidOperationException(string.Format(Lang.T("music.err.verify"), Path.GetFileName(dst)));

                log(string.Format(Lang.T("music.log.copied"), Path.GetFileName(dst), t.Name));
            }
        }

        /// <summary>
        /// Remove only the files this feature adds — never touch the game's own tracks.
        /// Reports what it could not delete instead of swallowing it: a file held open by the preview
        /// stays behind, and silently pretending otherwise leaves the list showing tracks the player
        /// just asked to be rid of.
        /// </summary>
        public static int Remove(string exePath, out List<string> failed)
        {
            failed = new List<string>();
            var dir = MusicDir(exePath);
            if (dir == null) return 0;
            int n = 0;
            for (int i = 0; i < PatchData.MusicSlotCount; i++)
            {
                var p = Path.Combine(dir, string.Format("Track{0:00}.ogg", PatchData.MusicFirstSlot + i));
                if (!File.Exists(p)) continue;
                try { File.Delete(p); n++; }
                catch { failed.Add(Path.GetFileName(p)); }
            }
            return n;
        }

        public static int Remove(string exePath)
        {
            List<string> ignored;
            return Remove(exePath, out ignored);
        }

        /// <summary>True if all ten files are present — the table patch is only valid together with them.</summary>
        public static bool FilesPresent(string exePath)
        {
            var dir = MusicDir(exePath);
            if (dir == null) return false;
            for (int i = 0; i < PatchData.MusicSlotCount; i++)
                if (!File.Exists(Path.Combine(dir, string.Format("Track{0:00}.ogg", PatchData.MusicFirstSlot + i))))
                    return false;
            return true;
        }

        /// <summary>
        /// Read back what is already installed, so the tab can show the real arrangement instead of
        /// just "something is installed". Each file's slot comes from its name, and its confidence is
        /// re-judged against that slot's reference length — which is what surfaces an order the player
        /// (or an earlier import) got wrong.
        /// </summary>
        public static List<Candidate> LoadInstalled(string exePath)
        {
            var dir = MusicDir(exePath);
            var list = new List<Candidate>();
            if (dir == null) return list;
            for (int i = 0; i < PatchData.MusicSlotCount; i++)
            {
                int slot = PatchData.MusicFirstSlot + i;
                var p = Path.Combine(dir, string.Format("Track{0:00}.ogg", slot));
                if (!File.Exists(p)) continue;
                var c = Probe(p);
                if (c == null) continue;
                c.Slot = slot;
                c.Confidence = Math.Abs(c.Seconds - SlotSeconds[i]) <= ToleranceSeconds
                    ? Confidence.Duration : Confidence.Uncertain;
                list.Add(c);
            }
            return list;
        }

        /// <summary>
        /// Move already-installed files to the slots just confirmed. Everything is staged
        /// under temporary names first: a straight rename would clobber a file whenever two tracks
        /// swap places, and half of the set would be gone before anyone noticed.
        /// </summary>
        public static void Rearrange(string exePath, IEnumerable<Candidate> tracks, Action<string> log)
        {
            var dir = MusicDir(exePath);
            if (dir == null) throw new ArgumentException("no game path to rearrange in", "exePath");
            var staged = new List<string[]>();

            foreach (var t in tracks.Where(x => x.Slot >= 0))
            {
                var target = Path.Combine(dir, string.Format("Track{0:00}.ogg", t.Slot));
                // Only the files that actually move. Copying all ten on every arrow click would mean
                // ~50 MB of disk writes to swap two tracks.
                if (string.Equals(t.Path, target, StringComparison.OrdinalIgnoreCase)) continue;

                var tmp = Path.Combine(dir, string.Format("Track{0:00}.reorder", t.Slot));
                File.Copy(t.Path, tmp, true);
                staged.Add(new[] { tmp, target });
            }

            foreach (var s in staged)
            {
                if (File.Exists(s[1])) File.Delete(s[1]);
                File.Move(s[0], s[1]);
                log(string.Format(Lang.T("music.log.copied"), Path.GetFileName(s[1]), ""));
            }
        }

        public static void CleanTemp(Scan scan)
        {
            if (string.IsNullOrEmpty(scan?.TempDir)) return;
            try { Directory.Delete(scan.TempDir, true); } catch { }
        }

        // ---- helpers ----

        static byte[] Read(string path, long offset, int count)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(Math.Max(0, offset), SeekOrigin.Begin);
                var buf = new byte[count];
                int got = 0, r;
                while (got < count && (r = fs.Read(buf, got, count - got)) > 0) got += r;
                if (got != count) Array.Resize(ref buf, got);
                return buf;
            }
        }

        static int IndexOf(byte[] hay, byte[] needle)
        {
            for (int i = 0; i + needle.Length <= hay.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && hay[i + j] == needle[j]) j++;
                if (j == needle.Length) return i;
            }
            return -1;
        }

        static int LastIndexOf(byte[] hay, byte[] needle)
        {
            for (int i = hay.Length - needle.Length; i >= 0; i--)
            {
                int j = 0;
                while (j < needle.Length && hay[i + j] == needle[j]) j++;
                if (j == needle.Length) return i;
            }
            return -1;
        }
    }
}
