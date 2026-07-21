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

using System.Collections.Generic;

namespace MetalFatiguePatcher
{
    public sealed class PatchSite
    {
        public string Name;
        public long Offset;
        public byte[] Original; // expected pristine bytes (optional sanity check)
        public byte[] Patched;
    }

    public sealed class Profile
    {
        public string Key;
        public List<PatchSite> Sites;
        public bool Hidden;   // easter-egg profiles are only shown once unlocked

        // Display strings are localized — see Lang.
        public string Title       => Lang.ProfileTitle(Key);
        public string Description => Lang.ProfileDesc(Key);
    }

    /// <summary>
    /// Patch definitions for the Nightdive re-release of MFatigue.exe (GOG and Steam are
    /// byte-identical). This file is the single source of truth for the patch bytes.
    /// file_offset == RVA in this build. Little-endian byte order below.
    /// </summary>
    public static class PatchData
    {
        public const string TargetFileName = "MFatigue.exe";

        /// <summary>
        /// SHA-256 of the supported build: the Nightdive 2021 re-release.
        /// GOG and Steam ship this exact same file (verified byte-identical).
        /// </summary>
        public const string KnownSha256 = "26d428f1ee8da3c2c499d85f2c3f002fa19e4cd63f4ae9cdc82c08d9b8547cde";

        /// <summary>Exact byte size of that build — a cheap pre-check before hashing.</summary>
        public const long KnownSize = 1191989;

        public static byte[] H(string hex)
        {
            hex = hex.Replace(" ", "");
            var b = new byte[hex.Length / 2];
            for (int i = 0; i < b.Length; i++)
                b[i] = System.Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return b;
        }

        // Fingerprint of a pristine GOG MFatigue.exe (original bytes at key patch sites).
        public static readonly PatchSite[] PristineSignature =
        {
            new PatchSite { Name = "arena_size",     Offset = 0xd231,  Original = H("0000A000") },
            new PatchSite { Name = "arena_sentinel", Offset = 0xd243,  Original = H("F4FF9F00") },
            new PatchSite { Name = "threshold",      Offset = 0xd5b4,  Original = H("00008000") },
            new PatchSite { Name = "crew_redirect",  Offset = 0x7b948, Original = H("5F5E5D33C05B59C20C00") },
            new PatchSite { Name = "crew_cave",      Offset = 0xd1cd2, Original = H(Zeros(36)) },
        };

        static string Zeros(int n) => new string('0', n * 2);

        static PatchSite Mem(long off, string orig, string patched) =>
            new PatchSite { Name = "mem_" + off.ToString("x"), Offset = off, Original = H(orig), Patched = H(patched) };

        // GetNextCrewName cyclic-reuse cave (36 bytes) + redirect at the NULL epilogue.
        const string CrewCave = "a1f0a057008d500183fa32720233d28915f0a057006bc0188b4406205f5e5d5b59c20c00";

        // --- dev/cheat sites (easter-egg profile only) ---
        // CFogOfWar::Process hook -> cave that calls MakeAllSeen(layer,0) for layers 0/1/2 each frame.
        const string FogCave = "6a006a00e8675df3ff83c4086a006a01e85b5df3ff83c4086a006a02e84f5df3ff83c408a100a05700e9475ff3ff";

        static List<PatchSite> UnleashedSites() => new List<PatchSite>
        {
            Mem(0xd231, "0000A000", "00000008"),
            Mem(0xd243, "F4FF9F00", "F4FFFF07"),
            Mem(0xd5b4, "00008000", "00008007"),
            new PatchSite { Name = "crew_redirect", Offset = 0x7b948, Original = H("5F5E5D33C05B59C20C00"), Patched = H("E9856305009090909090") },
            new PatchSite { Name = "crew_cave",     Offset = 0xd1cd2, Original = H(Zeros(36)),               Patched = H(CrewCave) },
        };

        // Player-only cheat caves. Each hooks the function, checks whether the owning
        // CPlayerManager is the LOCAL player's — [PlayerIndex 0x5229b8] indexed into the
        // manager table 0x571a78 — and only then applies the cheat; otherwise it runs the
        // original code. So the AI keeps playing by the normal rules.
        const string MjCave  = "a1b82952008b0485781a57003bc1740bd981c0000000e9fb85faffb801000000c20400";
        const string ResCave = "a1b82952008b0485781a57003bc1740bd981cc000000e9cb82faffb801000000c20800";
        const string BtCave  = "8b81500100008b15b82952008b1495781a57003bc2740b8b8950010000e96454fdffd9e8c20400";

        static List<PatchSite> CheatSites()
        {
            var l = UnleashedSites();
            // free building (local player only): NeedMetaJoules / NeedResource
            l.Add(new PatchSite { Name = "free_metajoules_hook", Offset = 0x7a350, Original = H("d981c0000000"), Patched = H("e9eb79050090") });
            l.Add(new PatchSite { Name = "free_metajoules_cave", Offset = 0xd1d40, Original = H(Zeros(35)),      Patched = H(MjCave) });
            l.Add(new PatchSite { Name = "free_resource_hook",   Offset = 0x7a050, Original = H("d981cc000000"), Patched = H("e91b7d050090") });
            l.Add(new PatchSite { Name = "free_resource_cave",   Offset = 0xd1d70, Original = H(Zeros(35)),      Patched = H(ResCave) });
            // fast build (local player only): BuildTime -> 1.0 (fld1). NOT 0/fldz - zero hangs the queue.
            l.Add(new PatchSite { Name = "fast_build_hook",      Offset = 0xa7220, Original = H("8b8950010000"), Patched = H("e97bab020090") });
            l.Add(new PatchSite { Name = "fast_build_cave",      Offset = 0xd1da0, Original = H(Zeros(39)),      Patched = H(BtCave) });
            // fog of war off (inherently local view only)
            l.Add(new PatchSite { Name = "fog_redirect",         Offset = 0x7c70,  Original = H("a100a05700"),   Patched = H("e98ba00c00") });
            l.Add(new PatchSite { Name = "fog_cave",             Offset = 0xd1d00, Original = H(Zeros(46)),      Patched = H(FogCave) });
            return l;
        }

        // Unconditional variant: the cheats apply to EVERY player, AI included.
        static List<PatchSite> CheatSitesAll()
        {
            var l = UnleashedSites();
            l.Add(new PatchSite { Name = "free_metajoules_all", Offset = 0x7a350, Original = H("d981c0000000d85c"), Patched = H("b801000000c20400") });
            l.Add(new PatchSite { Name = "free_resource_all",   Offset = 0x7a050, Original = H("d981cc000000d881"), Patched = H("b801000000c20800") });
            l.Add(new PatchSite { Name = "fast_build_all",      Offset = 0xa7220, Original = H("8b89500100"),       Patched = H("d9e8c20400") });
            l.Add(new PatchSite { Name = "fog_redirect",        Offset = 0x7c70,  Original = H("a100a05700"),       Patched = H("e98ba00c00") });
            l.Add(new PatchSite { Name = "fog_cave",            Offset = 0xd1d00, Original = H(Zeros(46)),          Patched = H(FogCave) });
            return l;
        }

        // --- Optional add-on: shared vision with allies ---
        // At the end of CFogOfWar::UpdateVisible the vision source's owner is loaded
        // (mov edi,[esi+0x1c] @ 0x407b6c) and handed to the render engine, which clears
        // that player's fog (call [pRendEng+0x8c] @ 0x407b90). The cave keeps the owner
        // unless it is an ALLY of the local player — then it substitutes the local player
        // id, so the ally's vision clears OUR fog instead. Asks the game's own static
        // CPlayerManager::IsAlly, so in-game alliance changes are honoured automatically.
        // eax (already masked for the call) is saved across the cdecl call; esi/edi are ours.
        const string SharedVisionCave =
            "8b7e1c25ff0000003b3db8295200741b50ff35b829520057e8f36afaff83c40885c05874068b3db8295200e9645df3ff";

        public static List<PatchSite> SharedVisionSites() => new List<PatchSite>
        {
            new PatchSite { Name = "shared_vision_hook", Offset = 0x7b6c,  Original = H("8b7e1c25ff000000"), Patched = H("e96fa20c00909090") },
            new PatchSite { Name = "shared_vision_cave", Offset = 0xd1de0, Original = H(Zeros(48)),          Patched = H(SharedVisionCave) },
        };

        public static readonly List<Profile> Profiles = new List<Profile>
        {
            new Profile
            {
                Key = "unleashed",
                Sites = UnleashedSites()
            },
            // --- Native ~50 combot limit, only the unit budget scales ---
            new Profile
            {
                Key = "balanced2x",
                Sites = new List<PatchSite>
                {
                    Mem(0xd231, "0000A000", "00004001"), // arena 20 MB
                    Mem(0xd243, "F4FF9F00", "F4FF3F01"), // sentinel = size - 0xC
                    Mem(0xd5b4, "00008000", "00000001"), // threshold 16 MB  (2x vanilla)
                }
            },
            new Profile
            {
                Key = "balanced4x",
                Sites = new List<PatchSite>
                {
                    Mem(0xd231, "0000A000", "00008002"), // arena 40 MB
                    Mem(0xd243, "F4FF9F00", "F4FF7F02"), // sentinel
                    Mem(0xd5b4, "00008000", "00000002"), // threshold 32 MB  (4x vanilla)
                }
            },
            new Profile
            {
                Key = "balanced8x",
                Sites = new List<PatchSite>
                {
                    Mem(0xd231, "0000A000", "00000005"), // arena 80 MB
                    Mem(0xd243, "F4FF9F00", "F4FFFF04"), // sentinel
                    Mem(0xd5b4, "00008000", "00000004"), // threshold 64 MB  (8x vanilla)
                }
            },
            new Profile
            {
                Key = "cheats",
                Hidden = true,
                Sites = CheatSites()
            },
            new Profile
            {
                Key = "cheats_all",
                Hidden = true,
                Sites = CheatSitesAll()
            },
        };

        public static Profile ByKey(string key) => Profiles.Find(p => p.Key == key);
    }
}
