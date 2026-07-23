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

namespace MetalFatiguePatcher
{
    public sealed class Patcher
    {
        readonly string _exePath;
        readonly string _bakPath;

        public Patcher(string exePath)
        {
            _exePath = exePath;
            _bakPath = exePath + ".bak";
        }

        public bool BackupExists => File.Exists(_bakPath);

        // Marker so an external uninstaller (Inno Setup) can find & restore the patched game.
        static string MarkerPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MetalFatiguePatcher", "lastgame.txt");

        void RecordPatched()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath));
                File.WriteAllText(MarkerPath, _exePath);
            }
            catch { /* non-fatal */ }
        }

        static void ClearRecord()
        {
            try { if (File.Exists(MarkerPath)) File.Delete(MarkerPath); }
            catch { }
        }

        /// <summary>True if the file's key sites still hold the original bytes.</summary>
        public static bool LooksPristine(string path)
        {
            try { return MatchesPristine(File.ReadAllBytes(path)); }
            catch { return false; }
        }

        static bool MatchesPristine(byte[] data)
        {
            foreach (var s in PatchData.PristineSignature)
            {
                if (s.Offset + s.Original.Length > data.LongLength) return false;
                for (int i = 0; i < s.Original.Length; i++)
                    if (data[s.Offset + i] != s.Original[i]) return false;
            }
            return true;
        }

        /// <summary>True if the file already carries exactly this profile's patched bytes.</summary>
        static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>Remove sites that are exact duplicates (same offset and identical patched bytes).</summary>
        static void DedupeIdentical(System.Collections.Generic.List<PatchSite> sites)
        {
            for (int i = sites.Count - 1; i > 0; i--)
                for (int j = 0; j < i; j++)
                    if (sites[i].Offset == sites[j].Offset && SameBytes(sites[i].Patched, sites[j].Patched))
                    { sites.RemoveAt(i); break; }
        }

        static bool MatchesProfile(byte[] data, Profile p)
        {
            foreach (var s in p.Sites)
            {
                if (s.Offset + s.Patched.Length > data.LongLength) return false;
                for (int i = 0; i < s.Patched.Length; i++)
                    if (data[s.Offset + i] != s.Patched[i]) return false;
            }
            return true;
        }

        public static string Sha256(byte[] data)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        public enum Compat
        {
            Missing,        // file not found / unreadable
            Pristine,       // supported build, unpatched -> ready
            PatchedByUs,    // already patched by this tool and a clean backup exists -> can switch/restore
            Unsupported     // unknown build (or patched with no clean backup) -> refuse
        }

        /// <summary>
        /// Determines whether the given MFatigue.exe can be patched by this tool.
        /// Uses an exact SHA-256 match against the known build, falling back to the
        /// byte signature at every patch site, then to detecting our own patched output.
        /// </summary>
        public static Compat Check(string exePath, out string detectedProfileKey, out bool exactHash)
        {
            detectedProfileKey = null;
            exactHash = false;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return Compat.Missing;

            byte[] data;
            try { data = File.ReadAllBytes(exePath); }
            catch { return Compat.Missing; }

            // Only bother hashing when the size already matches the known build.
            if (data.LongLength == PatchData.KnownSize)
                try { exactHash = Sha256(data) == PatchData.KnownSha256; } catch { }

            if (exactHash || MatchesPristine(data)) return Compat.Pristine;

            // Cheat profiles are supersets of "unleashed", so prefer the most specific match.
            Profile best = null;
            foreach (var p in PatchData.Profiles)
                if (MatchesProfile(data, p) && (best == null || p.Sites.Count > best.Sites.Count))
                    best = p;
            if (best != null) detectedProfileKey = best.Key;

            var bak = exePath + ".bak";
            bool cleanBackup = File.Exists(bak) && LooksPristine(bak);

            // Patched (by us or otherwise) but we still hold a clean original -> recoverable.
            if (cleanBackup) return Compat.PatchedByUs;

            return Compat.Unsupported;
        }

        /// <summary>True if the file already carries the shared-vision add-on.</summary>
        public static bool HasSharedVision(string exePath)
        {
            try
            {
                var data = File.ReadAllBytes(exePath);
                foreach (var s in PatchData.SharedVisionSites())
                {
                    if (s.Offset + s.Patched.Length > data.LongLength) return false;
                    for (int i = 0; i < s.Patched.Length; i++)
                        if (data[s.Offset + i] != s.Patched[i]) return false;
                }
                return true;
            }
            catch { return false; }
        }

        public void Apply(Profile profile, bool sharedVision, Action<string> log) =>
            Apply(profile, sharedVision, null, log);

        /// <summary>
        /// Apply a version profile plus any number of composed extra sites (individually selected
        /// cheats, part/superweapon unlocks). All sites are collision-checked before anything is
        /// written, so two features can never silently overwrite each other.
        /// </summary>
        public void Apply(Profile profile, bool sharedVision, System.Collections.Generic.List<PatchSite> extra, Action<string> log)
        {
            if (!File.Exists(_exePath))
                throw new FileNotFoundException(Lang.T("err.notFound"), _exePath);

            // 1) Ensure a pristine backup exists.
            if (!BackupExists)
            {
                if (!LooksPristine(_exePath))
                    throw new InvalidOperationException(Lang.T("err.notPristine"));
                File.Copy(_exePath, _bakPath);
                log(string.Format(Lang.T("log.backup"), Path.GetFileName(_bakPath)));
            }

            // 2) Always patch from the pristine backup so profiles are switchable.
            if (!LooksPristine(_bakPath))
                throw new InvalidOperationException(Lang.T("err.badBackup"));

            var data = File.ReadAllBytes(_bakPath);
            log(string.Format(Lang.T("log.applying"), profile.Title));

            // Optional add-ons patched on top of the chosen profile.
            var sites = new System.Collections.Generic.List<PatchSite>(profile.Sites);
            if (sharedVision)
            {
                sites.AddRange(PatchData.SharedVisionSites());
                log("  + " + Lang.T("sv.label"));
            }
            if (extra != null && extra.Count > 0)
                sites.AddRange(extra);

            // The Maximum version and the "unlimited elite crews" cheat both request the identical
            // one-byte crew-name fix. Drop exact duplicates (same offset AND same bytes) so picking
            // both is not mistaken for a collision; conflicting overlaps still fail below.
            DedupeIdentical(sites);

            // Never let two composed features write over each other - fail loudly first.
            PatchData.EnsureNoCollisions(sites);

            foreach (var s in sites)
            {
                if (s.Offset + s.Patched.Length > data.LongLength)
                    throw new InvalidOperationException(string.Format(Lang.T("err.outOfRange"), s.Name));

                if (s.Original != null)
                    for (int i = 0; i < s.Original.Length; i++)
                        if (data[s.Offset + i] != s.Original[i])
                            throw new InvalidOperationException(
                                string.Format(Lang.T("err.unexpected"), s.Name, s.Offset));

                for (int i = 0; i < s.Patched.Length; i++)
                    data[s.Offset + i] = s.Patched[i];

                log(string.Format(Lang.T("log.patched"), s.Name, s.Offset));
            }

            File.WriteAllBytes(_exePath, data);

            // 3) Verify.
            var check = File.ReadAllBytes(_exePath);
            foreach (var s in sites)
                for (int i = 0; i < s.Patched.Length; i++)
                    if (check[s.Offset + i] != s.Patched[i])
                        throw new InvalidOperationException(string.Format(Lang.T("err.verify"), s.Name));

            RecordPatched();
            log(Lang.T("log.verified"));
        }

        /// <summary>A backup that exists AND still verifies as an untouched original.</summary>
        public static bool HasValidBackup(string exePath)
        {
            if (string.IsNullOrEmpty(exePath)) return false;
            var bak = exePath + ".bak";
            return File.Exists(bak) && LooksPristine(bak);
        }

        public void Restore(Action<string> log)
        {
            if (!BackupExists)
                throw new InvalidOperationException(Lang.T("err.noBackup"));

            // Never copy an unverified backup over the game: a truncated or corrupt .bak
            // (e.g. from an interrupted backup copy) would destroy the last clean original.
            if (!LooksPristine(_bakPath))
                throw new InvalidOperationException(Lang.T("err.badBackup"));

            File.Copy(_bakPath, _exePath, true);
            ClearRecord();
            log(Lang.T("log.restored"));
        }
    }
}
