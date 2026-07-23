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
            new PatchSite { Name = "crew_search",    Offset = 0x7b81d, Original = H("7457") },
        };

        static string Zeros(int n) => new string('0', n * 2);

        static PatchSite Mem(long off, string orig, string patched) =>
            new PatchSite { Name = "mem_" + off.ToString("x"), Offset = off, Original = H(orig), Patched = H(patched) };

        // Crew-name fix: one byte at GetNextCrewName's search loop (je -> jmp) so it reuses names
        // WITHIN each tier's block instead of failing. Supersedes the old 46-byte cyclic-reuse cave
        // — smaller, no code cave, and it keeps each crew's tier label correct. Shared by the
        // Maximum version (lift the ~50 combot name limit) and the "unlimited elite crews" cheat;
        // identical sites are de-duplicated at apply time so selecting both is not a collision.
        public static PatchSite CrewNameFix() =>
            new PatchSite { Name = "crew_name_fix", Offset = 0x7b81d, Original = H("7457"), Patched = H("eb57") };

        // CFogOfWar::Process hook -> cave that calls MakeAllSeen(layer,0) for layers 0/1/2 each frame.
        const string FogCave = "6a006a00e8675df3ff83c4086a006a01e85b5df3ff83c4086a006a02e84f5df3ff83c408a100a05700e9475ff3ff";

        static List<PatchSite> UnleashedSites() => new List<PatchSite>
        {
            Mem(0xd231, "0000A000", "00000008"),
            Mem(0xd243, "F4FF9F00", "F4FFFF07"),
            Mem(0xd5b4, "00008000", "00008007"),
            CrewNameFix(),
        };

        // Player-only cheat caves. Each hooks the function, checks whether the owning
        // CPlayerManager is the LOCAL player's — [PlayerIndex 0x5229b8] indexed into the
        // manager table 0x571a78 — and only then applies the cheat; otherwise it runs the
        // original code. So the AI keeps playing by the normal rules.
        const string MjCave  = "a1b82952008b0485781a57003bc1740bd981c0000000e9fb85faffb801000000c20400";
        const string ResCave = "a1b82952008b0485781a57003bc1740bd981cc000000e9cb82faffb801000000c20800";
        const string BtCave  = "8b81500100008b15b82952008b1495781a57003bc2740b8b8950010000e96454fdffd9e8c20400";

        // (The old bundled CheatSites/CheatSitesAll profiles were removed in 1.1.0 — cheats are now
        //  individually composable via CheatFeatureSites. MjCave/ResCave/BtCave are still used there
        //  for the player-only variants.)

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

        // --- Combot part + superweapon unlock ------------------------------------------------
        // Found via Cheat Engine (see docs/plan-2.0.md): the assembly-bay build-list gate at
        // fileoff 0x4bade9 reads a part's availability mask at descriptor+0x4c and skips the part
        // if the local player's bit is absent. ORing the bit in makes it buildable WITH its icon
        // (merely bypassing the test leaves the icon logic seeing it unavailable).
        //
        // We hook the 11-byte gate into a cave that walks a table of the chosen descriptor
        // addresses; on a match it ORs the player bit, then either way re-runs the original
        // mov/test/je. Loop-over-table (not inline cmp per part) keeps the cave small and the
        // jumps short regardless of how many parts are selected.
        // All FILE offsets (file_offset == RVA in this build). VAs are formed by adding IMAGE_BASE
        // where a jump/hook target is computed; do not bake IMAGE_BASE into these constants.
        const long GATE_HOOK = 0xbade9;     // 11 bytes overwritten: mov ecx,[esi+0x4c]; test eax,ecx; je
        const long UNLOCK_CAVE = 0xd1e20;   // free space after the shared-vision cave (ends 0xd1e10)
        const long GATE_SKIP = 0xbaec0;     // original je target (part unavailable)
        const long GATE_CONT = 0xbadf4;     // original fall-through
        const uint IMAGE_BASE = 0x400000;

        static uint CaveVA(long fileoff) => (uint)(IMAGE_BASE + fileoff);

        /// <summary>
        /// Build the hook + cave that unlock exactly the given descriptor addresses (parts and/or
        /// superweapons share the same mask, so they go in one table). Empty input yields no sites.
        /// </summary>
        public static List<PatchSite> PartsUnlockSites(IReadOnlyList<uint> descriptorAddrs, bool allPlayers)
        {
            var sites = new List<PatchSite>();
            if (descriptorAddrs == null || descriptorAddrs.Count == 0) return sites;

            uint caveVa = CaveVA(UNLOCK_CAVE);
            var c = new List<byte>();
            void U32(uint v) { c.Add((byte)v); c.Add((byte)(v >> 8)); c.Add((byte)(v >> 16)); c.Add((byte)(v >> 24)); }

            //  0: push ebx                       (ebx is live in the gate function -> save it)
            c.Add(0x53);
            //  1: mov ebx, TABLE                 (imm32 patched once TABLE offset is known)
            c.Add(0xBB); int tableAt = c.Count; U32(0);
            int scan = c.Count;                     // 6
            //  scan: cmp dword [ebx], 0 ; je done
            c.Add(0x83); c.Add(0x3B); c.Add(0x00);
            int jeDone = c.Count; c.Add(0x74); c.Add(0x00);
            //  cmp esi,[ebx] ; je hit
            c.Add(0x3B); c.Add(0x33);
            int jeHit = c.Count; c.Add(0x74); c.Add(0x00);
            //  add ebx,4 ; jmp scan
            c.Add(0x83); c.Add(0xC3); c.Add(0x04);
            c.Add(0xEB); c.Add((byte)(scan - (c.Count + 1)));
            int hit = c.Count;
            //  hit: unlock this descriptor. "Me only" ORs the local player's bit (already in eax
            //  from 0x4bade4); "all players" ORs 0xFF so every player's bit is set - AI assembly
            //  bays run this same gate, so it propagates to them too.
            if (allPlayers)
            {
                c.Add(0x80); c.Add(0x4E); c.Add(0x4C); c.Add(0xFF);   // or byte [esi+0x4c], 0xFF
            }
            else
            {
                c.Add(0x09); c.Add(0x46); c.Add(0x4C);                // or [esi+0x4c], eax
            }
            int done = c.Count;
            c[jeDone + 1] = (byte)(done - (jeDone + 2));
            c[jeHit + 1] = (byte)(hit - (jeHit + 2));
            //  done: pop ebx                     ; then the original 11 bytes, retargeted
            c.Add(0x5B);
            c.Add(0x8B); c.Add(0x4E); c.Add(0x4C);              // mov ecx,[esi+0x4c]
            c.Add(0x85); c.Add(0xC8);                           // test eax,ecx
            c.Add(0x0F); c.Add(0x84);                           // je GATE_SKIP
            uint jeAt = caveVa + (uint)c.Count - 2;
            U32((uint)(GATE_SKIP + IMAGE_BASE - (jeAt + 6)));
            c.Add(0xE9);                                        // jmp GATE_CONT
            uint jmpAt = caveVa + (uint)c.Count - 1;
            U32((uint)(GATE_CONT + IMAGE_BASE - (jmpAt + 5)));
            //  TABLE: dd addr..., 0
            uint tableVa = caveVa + (uint)c.Count;
            c[tableAt] = (byte)tableVa; c[tableAt + 1] = (byte)(tableVa >> 8);
            c[tableAt + 2] = (byte)(tableVa >> 16); c[tableAt + 3] = (byte)(tableVa >> 24);
            foreach (var a in descriptorAddrs) U32(a);
            U32(0);   // null terminator

            long caveEnd = UNLOCK_CAVE + c.Count;
            if (caveEnd > 0xd2000)
                throw new System.InvalidOperationException(
                    string.Format("Parts-unlock cave overflows .text free space ({0} bytes, max {1}).",
                                  c.Count, 0xd2000 - UNLOCK_CAVE));

            // hook: jmp cave (5) + 6 nops = 11 bytes
            var hook = new List<byte> { 0xE9 };
            uint rel = (uint)(caveVa - (IMAGE_BASE + GATE_HOOK + 5));
            hook.Add((byte)rel); hook.Add((byte)(rel >> 8)); hook.Add((byte)(rel >> 16)); hook.Add((byte)(rel >> 24));
            for (int i = 0; i < 6; i++) hook.Add(0x90);

            sites.Add(new PatchSite { Name = "parts_unlock_cave", Offset = UNLOCK_CAVE, Original = H(Zeros(c.Count)), Patched = c.ToArray() });
            sites.Add(new PatchSite { Name = "parts_unlock_hook", Offset = GATE_HOOK, Original = H("8b4e4c85c80f84cc000000"), Patched = hook.ToArray() });
            return sites;
        }

        // --- Individually selectable cheat features ------------------------------------------
        // Keys used by the UI checkboxes. Each maps to a set of patch sites; the "all players"
        // scope swaps stubs (apply to everyone, AI included) for the player-only caves that check
        // the local CPlayerManager first. All bytes here are the ones verified in-game via
        // research/build_aitest.py.
        public const string CheatFog = "fog";
        public const string CheatFreeBuild = "freebuild";   // metajoules + resources + manpower
        public const string CheatTurbo = "turbo";           // instant build
        public const string CheatCrews = "crews";           // unlimited high-tier crews + name reuse

        public static readonly string[] CheatKeys = { CheatFog, CheatFreeBuild, CheatTurbo, CheatCrews };

        /// <summary>Assemble the patch sites for the chosen cheat features and scope.</summary>
        public static List<PatchSite> CheatFeatureSites(ICollection<string> features, bool allPlayers)
        {
            var l = new List<PatchSite>();
            if (features == null || features.Count == 0) return l;

            // Fog of war off - view-only, so identical in both scopes.
            if (features.Contains(CheatFog))
            {
                l.Add(new PatchSite { Name = "fog_redirect", Offset = 0x7c70,  Original = H("a100a05700"), Patched = H("e98ba00c00") });
                l.Add(new PatchSite { Name = "fog_cave",     Offset = 0xd1d00, Original = H(Zeros(46)),    Patched = H(FogCave) });
            }

            // Free building: MetaJoules + Resources (+ ManPower, the third gate the old profiles
            // never touched). "All" stubs each gate to return 1; "player" uses the owner-checking caves.
            if (features.Contains(CheatFreeBuild))
            {
                if (allPlayers)
                {
                    l.Add(new PatchSite { Name = "free_metajoules_all", Offset = 0x7a350, Original = H("d981c0000000d85c"), Patched = H("b801000000c20400") });
                    l.Add(new PatchSite { Name = "free_resource_all",   Offset = 0x7a050, Original = H("d981cc000000d881"), Patched = H("b801000000c20800") });
                    l.Add(new PatchSite { Name = "free_manpower_all",   Offset = 0x7a270, Original = H("8b4424045625ffff"), Patched = H("b801000000c20400") });
                }
                else
                {
                    l.Add(new PatchSite { Name = "free_metajoules_hook", Offset = 0x7a350, Original = H("d981c0000000"), Patched = H("e9eb79050090") });
                    l.Add(new PatchSite { Name = "free_metajoules_cave", Offset = 0xd1d40, Original = H(Zeros(35)),      Patched = H(MjCave) });
                    l.Add(new PatchSite { Name = "free_resource_hook",   Offset = 0x7a050, Original = H("d981cc000000"), Patched = H("e91b7d050090") });
                    l.Add(new PatchSite { Name = "free_resource_cave",   Offset = 0xd1d70, Original = H(Zeros(35)),      Patched = H(ResCave) });
                }
            }

            // Instant build: BuildTime -> 1.0 (fld1). NOT 0 - zero hangs the production queue.
            if (features.Contains(CheatTurbo))
            {
                if (allPlayers)
                    l.Add(new PatchSite { Name = "fast_build_all",  Offset = 0xa7220, Original = H("8b89500100"),   Patched = H("d9e8c20400") });
                else
                {
                    l.Add(new PatchSite { Name = "fast_build_hook", Offset = 0xa7220, Original = H("8b8950010000"), Patched = H("e97bab020090") });
                    l.Add(new PatchSite { Name = "fast_build_cave", Offset = 0xd1da0, Original = H(Zeros(39)),      Patched = H(BtCave) });
                }
            }

            // Unlimited high-tier combot crews: lift both build-quota gates, plus the one-byte
            // crew-name fix (search reuses names within the same tier, so higher tiers stop running
            // out of names). Quota is per crew type, not per player, so there is no scope variant.
            if (features.Contains(CheatCrews))
            {
                l.Add(new PatchSite { Name = "crew_tier",     Offset = 0x7c220, Original = H("668b81a800000066"), Patched = H("b863000000c20400") });
                l.Add(new PatchSite { Name = "mp_crew_gate",  Offset = 0x7c150, Original = H("568bf18b0d28055200"), Patched = H("b801000000c3") });
                l.Add(CrewNameFix());   // same site the Maximum version uses; de-duplicated at apply time
            }

            return l;
        }

        /// <summary>
        /// Reject any two sites whose byte ranges overlap. Composing cheats must never silently
        /// write over each other; a collision is a bug in the feature set, surfaced loudly.
        /// </summary>
        public static void EnsureNoCollisions(IEnumerable<PatchSite> sites)
        {
            var ordered = new List<PatchSite>(sites);
            ordered.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var cur = ordered[i];
                if (cur.Offset < prev.Offset + prev.Patched.Length)
                    throw new System.InvalidOperationException(
                        string.Format("Patch sites '{0}' and '{1}' overlap at 0x{2:x}.",
                                      prev.Name, cur.Name, cur.Offset));
            }
        }

        /// <summary>What cheats / part unlocks a patched exe currently carries. Read back purely
        /// from the bytes so the UI can restore every setting on load and never silently drop one.</summary>
        public sealed class Installed
        {
            public bool Fog, FreeBuild, Turbo, Crews;
            public bool CheatScopeAll;                 // player vs all for free-build / instant-build
            public bool PartsUnlock, PartsScopeAll;
            public readonly List<uint> UnlockedAddrs = new List<uint>();
        }

        static bool Has(byte[] d, long off, string hex)
        {
            var b = H(hex);
            if (off < 0 || off + b.Length > d.LongLength) return false;
            for (int i = 0; i < b.Length; i++) if (d[off + i] != b[i]) return false;
            return true;
        }

        static uint U32(byte[] d, long off) =>
            (uint)(d[off] | (d[off + 1] << 8) | (d[off + 2] << 16) | (d[off + 3] << 24));

        public static Installed DetectInstalled(byte[] d)
        {
            var r = new Installed();

            r.Fog = Has(d, 0x7c70, "e98ba00c00");                          // fog redirect (both scopes)
            // free building: all = stub at 0x7a350, player = hook there
            bool fbAll = Has(d, 0x7a350, "b801000000c20400");
            bool fbPlayer = Has(d, 0x7a350, "e9eb79050090");
            r.FreeBuild = fbAll || fbPlayer;
            // instant build: all = fld1 stub, player = hook
            bool tbAll = Has(d, 0xa7220, "d9e8c20400");
            bool tbPlayer = Has(d, 0xa7220, "e97bab020090");
            r.Turbo = tbAll || tbPlayer;
            // scope is shared by the two; read it from whichever is present
            r.CheatScopeAll = fbAll || (tbAll && !fbPlayer);

            r.Crews = Has(d, 0x7c220, "b863000000c20400");

            // parts unlock: hook present at the gate, then read the cave's address table
            if (d.LongLength > GATE_HOOK && d[GATE_HOOK] == 0xE9)
            {
                r.PartsUnlock = true;
                // "or byte [esi+0x4c], 0xFF" (all) somewhere in the first bytes of the cave = all-players
                for (long i = UNLOCK_CAVE; i < UNLOCK_CAVE + 48 && i + 4 <= d.LongLength; i++)
                    if (d[i] == 0x80 && d[i + 1] == 0x4E && d[i + 2] == 0x4C && d[i + 3] == 0xFF)
                    { r.PartsScopeAll = true; break; }
                // mov ebx, TABLE  is at cave+2; walk the table until the 0 terminator
                long tableOff = U32(d, UNLOCK_CAVE + 2) - IMAGE_BASE;
                for (int n = 0; n < 256 && tableOff >= 0 && tableOff + 4 <= d.LongLength; n++, tableOff += 4)
                {
                    uint a = U32(d, tableOff);
                    if (a == 0) break;
                    r.UnlockedAddrs.Add(a);
                }
            }
            return r;
        }

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
        };

        public static Profile ByKey(string key) => Profiles.Find(p => p.Key == key);
    }
}
