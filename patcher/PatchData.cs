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

        // Crew-name fix: one byte in the name allocator
        // ?GetNextCrewName@CPlayerManager@@QAEPBDAAH00@Z (0x47b670) — je -> jmp at the "name in use"
        // test (0x7b81d, 7457 -> eb57).
        //
        // The allocator keeps each crew TIER on its own rotating iteration: tier 1 hands out only
        // tier-1 names, tier 2 only tier-2 names, etc. Vanilla only gives a name bound to the
        // requested tier AND currently free, else it reports "names used up". This flip makes a
        // tier REUSE its own names cyclically instead of failing, which lifts the ~50-combot name
        // limit WITHOUT disturbing the per-tier partitioning. The tier-match test at 0x47b816 must
        // never be touched — NOP-ing it collapses every tier onto the same names (tried & reverted).
        //
        // KNOWN edge (deliberately NOT patched): under Maximum + unlimited-crews, if you build ~50
        // crews across tiers 1-3 WITHOUT ever building a tier-4/5 crew, the shared name pool empties
        // and a tier that still has zero names of its own will fail on its first crew. A "starve
        // rescue" that reused an arbitrary slot was tried and REVERTED (2026-07-25): the 3 name
        // "styles" per record are per-CORPORATION (Rimtech callsigns "Alpha-2" / MilAgro "Mad Dogs"
        // / Neuropa "Benevolence"), NOT per-tier — tiers differ only by which SLICE of the 50 names
        // they hold — so reusing a foreign slot would surface another tier's name, which the user
        // forbids ("ein tier darf AUSSCHLIESSLICH die eigenen tier namen haben"). A correct fix would
        // have to pre-assign each tier a guaranteed disjoint slice at setup (SetupCrewNames rework).
        //
        // Shared by the Maximum version and the "unlimited elite crews" cheat; exact-duplicate sites
        // are de-duplicated at apply time, so picking both is fine.
        public static PatchSite CrewNameFix() =>
            new PatchSite { Name = "crew_name_fix", Offset = 0x7b81d, Original = H("7457"), Patched = H("eb57") };

        // --- Relocatable cave assembly -------------------------------------------------------------
        // A cave body may branch back into the game, and a hook must branch into its cave, so both
        // carry rel32 displacements that depend on where the cave sits. Baking those into literal hex
        // pins every cave to one address: the zone could never be repacked without silently
        // redirecting a jump into the middle of nowhere. These two helpers keep each displacement a
        // function of the cave offset, so moving a cave is a one-constant change.

        /// <summary>Builds a cave body from literal byte runs plus rel32 branches to absolute targets.</summary>
        sealed class CaveAsm
        {
            readonly List<byte> _b = new List<byte>();
            readonly long _caveOff;
            public CaveAsm(long caveOff) { _caveOff = caveOff; }
            public CaveAsm Raw(string hex) { _b.AddRange(H(hex)); return this; }
            public CaveAsm Call(uint target) { return Branch(0xE8, target); }
            public CaveAsm Jmp(uint target) { return Branch(0xE9, target); }
            CaveAsm Branch(byte op, uint target)
            {
                uint at = (uint)(IMAGE_BASE + _caveOff + _b.Count);
                uint rel = target - (at + 5);
                _b.Add(op);
                _b.Add((byte)rel); _b.Add((byte)(rel >> 8)); _b.Add((byte)(rel >> 16)); _b.Add((byte)(rel >> 24));
                return this;
            }
            public byte[] Bytes { get { return _b.ToArray(); } }
        }

        /// <summary>A 5-byte jmp from a hook site into a cave, NOP-padded to cover whole instructions.</summary>
        static byte[] HookJmp(long hookOff, long caveOff, int width)
        {
            uint rel = (uint)(IMAGE_BASE + caveOff) - (uint)(IMAGE_BASE + hookOff + 5);
            var b = new byte[width];
            b[0] = 0xE9;
            b[1] = (byte)rel; b[2] = (byte)(rel >> 8); b[3] = (byte)(rel >> 16); b[4] = (byte)(rel >> 24);
            for (int i = 5; i < width; i++) b[i] = 0x90;
            return b;
        }

        // CFogOfWar::Process hook -> cave that calls MakeAllSeen(layer,0) for layers 0/1/2 each frame.
        const long FOG_HOOK = 0x7c70;
        static byte[] FogCaveBody() => new CaveAsm(FOG_CAVE)
            .Raw("6a006a00").Call(0x407a70)                  // MakeAllSeen(0, 0)
            .Raw("83c4086a006a01").Call(0x407a70)            // MakeAllSeen(1, 0)
            .Raw("83c4086a006a02").Call(0x407a70)            // MakeAllSeen(2, 0)
            .Raw("83c408a100a05700").Jmp(0x407c75)           // relocated mov eax,[0x57a000]; back
            .Bytes;

        static List<PatchSite> UnleashedSites()
        {
            var l = new List<PatchSite>
            {
                Mem(0xd231, "0000A000", "00000008"),
                Mem(0xd243, "F4FF9F00", "F4FFFF07"),
                Mem(0xd5b4, "00008000", "00008007"),
            };
            l.Add(CrewNameFix());
            return l;
        }

        // Player-only cheat caves. Each hooks the function, checks whether the owning
        // CPlayerManager is the LOCAL player's — [PlayerIndex 0x5229b8] indexed into the
        // manager table 0x571a78 — and only then applies the cheat; otherwise it runs the
        // original code. So the AI keeps playing by the normal rules.
        const long MJ_HOOK = 0x7a350, RES_HOOK = 0x7a050, BT_HOOK = 0xa7220;

        static byte[] MjCaveBody() => new CaveAsm(MJ_CAVE)
            .Raw("a1b82952008b0485781a57003bc1740bd981c0000000").Jmp(0x47a356)   // not mine -> original
            .Raw("b801000000c20400").Bytes;                                      // mine -> return 1

        static byte[] ResCaveBody() => new CaveAsm(RES_CAVE)
            .Raw("a1b82952008b0485781a57003bc1740bd981cc000000").Jmp(0x47a056)
            .Raw("b801000000c20800").Bytes;

        static byte[] BtCaveBody() => new CaveAsm(BT_CAVE)
            .Raw("8b81500100008b15b82952008b1495781a57003bc2740b8b8950010000").Jmp(0x4a7226)
            .Raw("d9e8c20400").Bytes;                                            // mine -> fld1

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
        const long SV_HOOK = 0x7b6c;

        static byte[] SharedVisionCaveBody() => new CaveAsm(SV_CAVE)
            .Raw("8b7e1c25ff0000003b3db8295200741b50ff35b829520057").Call(0x4788f0)   // IsAlly(owner, local)
            .Raw("83c40885c05874068b3db8295200").Jmp(0x407b74)
            .Bytes;

        public static List<PatchSite> SharedVisionSites()
        {
            var cave = SharedVisionCaveBody();
            return new List<PatchSite>
            {
                new PatchSite { Name = "shared_vision_hook", Offset = SV_HOOK,  Original = H("8b7e1c25ff000000"), Patched = HookJmp(SV_HOOK, SV_CAVE, 8) },
                new PatchSite { Name = "shared_vision_cave", Offset = SV_CAVE,  Original = H(Zeros(cave.Length)), Patched = cave },
            };
        }

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
        const long UNLOCK_CAVE = 0xd1dd0;   // free space right after the shared-vision cave
        const long GATE_SKIP = 0xbaec0;     // original je target (part unavailable)
        const long GATE_CONT = 0xbadf4;     // original fall-through
        const uint IMAGE_BASE = 0x400000;

        // The whole usable code-cave zone is .text's tail padding, 0xd1cd2..0xd2000 (814 bytes) —
        // verified to be the ONLY run of free space in the section. (0xd1cd1 is not free: it is the
        // last byte of the jmp dword ptr [0x4d2048] at 0x4d1ccc.)
        //
        // Every cave here is position-independent — bodies branch via CaveAsm and hooks via HookJmp,
        // both of which derive their rel32 from the offsets below — so the zone can be repacked by
        // editing these constants alone. It was repacked once already (see LegacyLayouts): the 1.1.x
        // layout left ~130 bytes stranded in alignment gaps, which the tail features had grown into.
        //
        //   fog          0xd1ce0   46          mj      0xd1d10   35
        //   resources    0xd1d40   35          turbo   0xd1d70   39
        //   shared-vis   0xd1da0   48          parts   0xd1dd0  256 reserved (worst case 245)
        //   crew names   0xd1ed0   96 reserved (92 used)
        //   move speed   0xd1f30  100 worst case -> ends 0xd1f94, leaving 108 bytes spare
        //
        // Parts worst case is every part + superweapon in "all players" scope: 51 listed descriptors,
        // one of which (the shared JetPack torso) is a duplicate PartsUnlockSites folds away, so 50
        // entries = 245 bytes. Before that dedupe it came to 249 and the old 248-byte bound made
        // exactly that combination throw instead of patching.
        const long FOG_CAVE = 0xd1ce0;
        const long MJ_CAVE = 0xd1d10;
        const long RES_CAVE = 0xd1d40;
        const long BT_CAVE = 0xd1d70;
        const long SV_CAVE = 0xd1da0;
        const long UNLOCK_CAVE_END = 0xd1ed0;   // parts must not reach the crew-name cave
        const long CAVE_ZONE_END = 0xd2000;

        static uint CaveVA(long fileoff) => (uint)(IMAGE_BASE + fileoff);

        // --- Superseded layouts --------------------------------------------------------------------
        // An exe patched by an older release carries caves at offsets this build no longer writes, so
        // re-patching would leave that dead code behind. Patcher.Apply always rebuilds from the
        // pristine .bak, which wipes it — the case that needs a word to the user is a patched exe with
        // no clean backup, where we cannot undo the old layout ourselves. Detecting it lets us say so
        // instead of failing with a generic "unsupported build".
        //
        // A layout is identified by where its HOOKS point, not by cave contents. Cave bytes are not
        // discriminating: the metajoules and resources caves share their first 12 bytes, so after the
        // repack the new resources cave sits exactly where the old metajoules cave was and matches it
        // byte for byte. A hook's rel32, by contrast, encodes the cave address itself, so it names the
        // layout exactly. Hooks live at fixed offsets in the game's own code and never move.
        //
        // To retire a layout: add one row per hook whose cave it placed differently.
        public sealed class LegacyLayout
        {
            public string Release;      // which release produced it, for the message
            public long HookOffset;     // fixed site in the game's code carrying the jmp
            public long CaveOffset;     // where that release put the cave
        }

        static readonly LegacyLayout[] LegacyLayouts =
        {
            // 1.1.x — cave zone before it was repacked; caves sat on 0x40-byte grid points, which
            // stranded ~130 bytes in the alignment gaps between them.
            new LegacyLayout { Release = "1.1.x", HookOffset = FOG_HOOK,  CaveOffset = 0xd1d00 },
            new LegacyLayout { Release = "1.1.x", HookOffset = MJ_HOOK,   CaveOffset = 0xd1d40 },
            new LegacyLayout { Release = "1.1.x", HookOffset = RES_HOOK,  CaveOffset = 0xd1d70 },
            new LegacyLayout { Release = "1.1.x", HookOffset = BT_HOOK,   CaveOffset = 0xd1da0 },
            new LegacyLayout { Release = "1.1.x", HookOffset = SV_HOOK,   CaveOffset = 0xd1de0 },
            new LegacyLayout { Release = "1.1.x", HookOffset = GATE_HOOK, CaveOffset = 0xd1e20 },
        };

        /// <summary>Where a 5-byte E9 at <paramref name="hookOff"/> lands, as a file offset, or -1.</summary>
        static long JmpTargetAt(byte[] data, long hookOff)
        {
            if (hookOff + 5 > data.LongLength || data[hookOff] != 0xE9) return -1;
            int rel = data[hookOff + 1] | (data[hookOff + 2] << 8) | (data[hookOff + 3] << 16) | (data[hookOff + 4] << 24);
            return hookOff + 5 + rel;
        }

        /// <summary>The release whose cave layout this file still carries, or null if none does.</summary>
        public static string DetectLegacyLayout(byte[] data)
        {
            if (data == null) return null;
            foreach (var l in LegacyLayouts)
                if (JmpTargetAt(data, l.HookOffset) == l.CaveOffset) return l.Release;
            return null;
        }

        /// <summary>Convenience overload for a path; false/null on any read error.</summary>
        public static string DetectLegacyLayout(string exePath)
        {
            try { return DetectLegacyLayout(System.IO.File.ReadAllBytes(exePath)); }
            catch { return null; }
        }

        /// <summary>
        /// Build the hook + cave that unlock exactly the given descriptor addresses (parts and/or
        /// superweapons share the same mask, so they go in one table). Empty input yields no sites.
        /// </summary>
        public static List<PatchSite> PartsUnlockSites(IReadOnlyList<uint> descriptorAddrs, bool allPlayers)
        {
            var sites = new List<PatchSite>();
            if (descriptorAddrs == null || descriptorAddrs.Count == 0) return sites;

            // Some descriptors are shared by more than one faction — the JetPack torso (CJetPackTorso,
            // 0x5731b0) is listed under both MilAgro and Neuropa — so ticking both boxes would emit the
            // same address twice. The scan loop stops at the first match, making the duplicate pure
            // dead weight that pushed the table past its bound. Dedupe here, at the point that owns the
            // size limit, so no caller can reintroduce it.
            var seen = new HashSet<uint>();
            var unique = new List<uint>(descriptorAddrs.Count);
            foreach (var a in descriptorAddrs)
                if (seen.Add(a)) unique.Add(a);
            descriptorAddrs = unique;

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
            if (caveEnd > UNLOCK_CAVE_END)
                throw new System.InvalidOperationException(
                    string.Format("Parts-unlock cave overflows its .text free space ({0} bytes, max {1}).",
                                  c.Count, UNLOCK_CAVE_END - UNLOCK_CAVE));

            // hook: jmp cave (5) + 6 nops = 11 bytes
            var hook = new List<byte> { 0xE9 };
            uint rel = (uint)(caveVa - (IMAGE_BASE + GATE_HOOK + 5));
            hook.Add((byte)rel); hook.Add((byte)(rel >> 8)); hook.Add((byte)(rel >> 16)); hook.Add((byte)(rel >> 24));
            for (int i = 0; i < 6; i++) hook.Add(0x90);

            sites.Add(new PatchSite { Name = "parts_unlock_cave", Offset = UNLOCK_CAVE, Original = H(Zeros(c.Count)), Patched = c.ToArray() });
            sites.Add(new PatchSite { Name = "parts_unlock_hook", Offset = GATE_HOOK, Original = H("8b4e4c85c80f84cc000000"), Patched = hook.ToArray() });
            return sites;
        }

        // --- Experimental: unit movement speed ------------------------------------------------
        // [CMover+0x14] is MaxVelocity — the game says so itself: ?GetMaxVelocity@CMover@@QAEMXZ
        // is literally "fld dword [ecx+0x14]; ret". (That export is a dead stub with no callers,
        // so it is useless as a hook, but it documents the field.) Confirmed in-game with Cheat
        // Engine: every HTH-legs combot reads 15.5, a Rimtech hovertruck 28.0, and editing the
        // value up or down changes that unit's speed.
        //
        // The movement integrator at 0x438bf0 is what consumes it, reading the field three times:
        // the step itself, the braking limit and the arrival test. Scaling all three by the same
        // factor is equivalent to the unit simply having a higher MaxVelocity — crucially it keeps
        // the braking/arrival logic consistent with the new speed, so units still stop on target
        // instead of overshooting. Nothing stored is modified, so savegames stay vanilla and the
        // value the AI plans with is untouched.
        //
        // [CMover+0x28] is the owner: SetPlayer@CMover writes exactly that field, and elsewhere the
        // engine feeds it straight into ?GetPlayerManager@CPlayerManager@@SAPAV1@G@Z, which does
        // "mov eax,[eax*4+0x571a78]" — the same player-id space as the local player index at
        // 0x5229b8. So "my units only" is a plain dword compare against that global.
        //
        // Hook placement was verified against the whole image: nothing branches into any of the
        // three overwritten ranges and no data word points inside them. Note site 2 hooks the fld
        // BEFORE the fmul: a je at 0x438c5d targets 0x438c6a, so a 5-byte jmp written at the fmul
        // itself would be jumped into mid-instruction.
        const long SPEED_CAVE = 0xd1f30;   // right after the crew-name cave's reserved 96 bytes
        const long SPEED_HOOK1 = 0x38bf1; const uint SPEED_RET1 = 0x438bf7;   // fld [ecx+0x14]; mov eax,[ecx+0xc]
        const long SPEED_HOOK2 = 0x38c61; const uint SPEED_RET2 = 0x438c6a;   // fld [0x4d7ea4]; fmul [ecx+0x14]
        const long SPEED_HOOK3 = 0x38c77; const uint SPEED_RET3 = 0x438c80;   // fmul [ecx+0x14]; fld [ecx+0xa0]
        const uint LOCAL_PLAYER = 0x5229b8;

        /// <summary>The movement-speed multipliers the UI offers. 6x puts the fastest vehicle
        /// (28.0) at 168, just past the ~160 that already feels extreme in play.</summary>
        public static readonly double[] SpeedFactors = { 1.5, 2.0, 3.0, 4.0, 5.0, 6.0 };

        /// <summary>
        /// Hook the three MaxVelocity reads in the movement integrator into a cave that multiplies
        /// each by <paramref name="factor"/>, optionally only for movers owned by the local player.
        /// A factor of 1 or less means "off" and yields no sites.
        /// </summary>
        public static List<PatchSite> MoveSpeedSites(double factor, bool allPlayers)
        {
            var sites = new List<PatchSite>();
            if (factor <= 1.0) return sites;

            uint caveVa = CaveVA(SPEED_CAVE);
            var c = new List<byte>();
            void U32(uint v) { c.Add((byte)v); c.Add((byte)(v >> 8)); c.Add((byte)(v >> 16)); c.Add((byte)(v >> 24)); }
            void Emit(string hex) { c.AddRange(H(hex)); }

            // The factor lives at the head of the cave so all three stubs share one constant.
            uint factorVa = caveVa;
            c.AddRange(System.BitConverter.GetBytes((float)factor));

            // Multiply st(0) by the factor. In "my units only" scope this is gated on the mover's
            // owner; POP does not touch the flags, so the compare survives restoring eax.
            void Scale()
            {
                if (!allPlayers)
                {
                    Emit("50");                        // push eax
                    Emit("8b4128");                    // mov  eax,[ecx+0x28]     ; this mover's owner
                    Emit("3b05"); U32(LOCAL_PLAYER);   // cmp  eax,[localPlayer]
                    Emit("58");                        // pop  eax
                    Emit("7506");                      // jne  +6                 ; skip the fmul
                }
                Emit("d80d"); U32(factorVa);           // fmul dword [factor]
            }

            void JmpBack(uint target)
            {
                c.Add(0xE9);
                uint at = caveVa + (uint)c.Count - 1;
                U32((uint)(target - (at + 5)));
            }

            uint stub1 = caveVa + (uint)c.Count;
            Emit("d94114");                            // fld  dword [ecx+0x14]
            Scale();
            Emit("8b410c");                            // mov  eax,[ecx+0xc]      (relocated)
            JmpBack(SPEED_RET1);

            uint stub2 = caveVa + (uint)c.Count;
            Emit("d905a47e4d00");                      // fld  dword [0x4d7ea4]   (relocated)
            Emit("d84914");                            // fmul dword [ecx+0x14]
            Scale();
            JmpBack(SPEED_RET2);

            uint stub3 = caveVa + (uint)c.Count;
            Emit("d84914");                            // fmul dword [ecx+0x14]
            Scale();
            Emit("d981a0000000");                      // fld  dword [ecx+0xa0]   (relocated)
            JmpBack(SPEED_RET3);

            if (SPEED_CAVE + c.Count > CAVE_ZONE_END)
                throw new System.InvalidOperationException(
                    string.Format("Movement-speed cave overflows .text free space ({0} bytes, max {1}).",
                                  c.Count, CAVE_ZONE_END - SPEED_CAVE));

            sites.Add(new PatchSite { Name = "move_speed_cave", Offset = SPEED_CAVE, Original = H(Zeros(c.Count)), Patched = c.ToArray() });
            sites.Add(SpeedHook("move_speed_hook1", SPEED_HOOK1, stub1, "d941148b410c"));
            sites.Add(SpeedHook("move_speed_hook2", SPEED_HOOK2, stub2, "d905a47e4d00d84914"));
            sites.Add(SpeedHook("move_speed_hook3", SPEED_HOOK3, stub3, "d84914d981a0000000"));
            return sites;
        }

        /// <summary>jmp into the cave, padded with nops to exactly cover the original instructions.</summary>
        static PatchSite SpeedHook(string name, long fileoff, uint target, string original)
        {
            int len = original.Length / 2;
            var b = new List<byte> { 0xE9 };
            uint rel = (uint)(target - (IMAGE_BASE + fileoff + 5));
            b.Add((byte)rel); b.Add((byte)(rel >> 8)); b.Add((byte)(rel >> 16)); b.Add((byte)(rel >> 24));
            while (b.Count < len) b.Add(0x90);
            return new PatchSite { Name = name, Offset = fileoff, Original = H(original), Patched = b.ToArray() };
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

        // --- Deterministic crew-name cave (elite-crew cheat only) ---------------------------------
        // The vanilla allocator ?GetNextCrewName@CPlayerManager@@…@Z hands out names from one shared
        // 50-name pool, stamping each slot with the level it was first handed out for, and hard-fails
        // (spamming "Couldn't find a name for this crew! cl:N") once an internal ci==100 gate is hit —
        // which is exactly what starves AI players' higher tiers.
        //
        // The cave replaces only the *allocation search*, hooking at 0x47b7c8 rather than the function
        // entry. That is deliberate: the two early paths above it must survive, because they are what
        // answers "what is this existing crew called / what level is it" —
        //   0x47b6c7  cc != 0            -> name of an existing special crew (table 0x512380)
        //   0x47b728  cc == 0 && ci < 50 -> existing crew; also does *cl = slot.level
        // Hooking the entry (as an earlier revision did) killed both, so the game read back an
        // unstamped level and rendered the wrong number: the UI prints "%s(%d)" with %d = level+1
        // (0x4bc3ba / 0x4bc5ac both 'inc' right before pushing it), so an unstamped slot showed "(1)".
        //
        // In the cave: slot = tier*10 + cursor, cursor cycling 0..9, so each of the 5 tiers permanently
        // owns 10 of the faction's 50 names (column = [this+0xa4]). It then stamps the slot exactly like
        // vanilla does — slot.used = 1 at [esi+slot*0x18+0x10], slot.level = tier at +0x14 — and returns
        // slot in *ci, so name and displayed level stay in step. The cursor is vanilla's own per-player
        // cursor at [esi+tier*4+0x5bc] (esi = per-player state), so players no longer share a counter.
        // Both cl and the cursor are range-clamped, capping the table index at 49*3+2. All 3 faction
        // columns are full 50-name lists (verified), so the uniform slot index is correct everywhere.
        // 90-byte cave; sits between the parts-unlock and movement-speed caves.
        const long CREW_CAVE = 0xd1ed0;
        const long CREW_CAVE_MAX = 96;    // reserved; movement-speed cave starts at CREW_CAVE + this
        const long CREW_HOOK = 0x7b7c8;   // allocation search: 10 bytes overwritten by jmp + nop padding
        static readonly byte[] CrewCaveBody = H(
            "8b54241c8b2a83fd05720431ed892a8dbcaebc0500008b0783f80a720231c08d500183" +
            "fa0a720231d289178d4cad008d0c488d1449c744d61001000000896cd6148b44242089" +
            "080393a40000008b0495282151005f5e5d5b59c20c00");

        /// <summary>Hook + cave that deterministically name each crew tier from its own 10-name slice.
        /// Used by the unlimited-elite-crews cheat only (the Maximum version keeps the lighter
        /// one-byte je->jmp fix). Placed right after the parts-unlock cave's bounded end.
        /// <para>MUST be installed together with the <c>crew_tier</c> site (0x7c220). The cave skips
        /// vanilla's decrements of the per-level pool counters at [player+lvl*4+0x5d0] and +0x5f8;
        /// their only reader in the whole image is CanBuildCrew (0x47c220), which <c>crew_tier</c>
        /// stubs out to "return 99". Ship the cave without it and those counters become a stale gate
        /// on crew production.</para></summary>
        public static List<PatchSite> CrewNameCaveSites()
        {
            if (CrewCaveBody.Length > CREW_CAVE_MAX)
                throw new System.InvalidOperationException(
                    string.Format("Crew-name cave overflows its slot ({0} bytes, max {1}).",
                                  CrewCaveBody.Length, CREW_CAVE_MAX));

            // 5-byte jmp plus nops over the two replaced instructions, so nothing is left half-erased.
            var hook = HookJmp(CREW_HOOK, CREW_CAVE, 10);
            return new List<PatchSite>
            {
                new PatchSite { Name = "crew_name_cave", Offset = CREW_CAVE, Original = H(Zeros(CrewCaveBody.Length)), Patched = CrewCaveBody },
                new PatchSite { Name = "crew_name_hook", Offset = CREW_HOOK, Original = H("8b54241c8b8ba4000000"),     Patched = hook },
                new PatchSite { Name = "crew_level_from_slot", Offset = LEVEL_SITE, Original = H(LevelOriginal), Patched = H(LevelPatched) },
            };
        }

        // --- Crew level read back from the slot index instead of a mutable field -------------------
        // CPlayerManager::GetLevel (0x47ba90) answers "what level is this crew". For a normal crew it
        // returned dword [player + 0x14 + ci*0x18] — the level the allocator stamped into that slot.
        // That field is per-OWNER and mutable: InitPlayerDataForLevelStart zeroes it at mission start
        // (0x479a99), PATH B rewrites it (0x47b75d), and the whole block is snapshotted and restored
        // wholesale by CycleBackupData. It is also read with the DISPLAYED combot's owner, while the
        // name was allocated under the crew's owner, so the two can drift apart — the reported
        // "Tango-3 (1)" on a genuine tier-5 crew.
        //
        // The cave makes that lookup unnecessary: it hands out slot = level*10 + n, so the level is
        // recoverable from the slot index alone, as ci / 10 — a value nothing can zero. ci*205 >> 11
        // is that division, exact for every ci in range. ci >= 50 (the ctor's 100 = "not named yet",
        // or a negative from a corrupt save) returns 0 instead of vanilla's read past the array.
        //
        // This is reached by 13 call sites — 3 render, 10 gameplay — and all of them get the crew's
        // true tier, including the cases where they used to read a zeroed field. Only the cc == 0
        // branch is touched; hero crews (0x47bae7) and non-crew objects (0x47bb25) keep their code.
        // Only meaningful together with the cave, so it ships in the same feature block.
        const long LEVEL_SITE = 0x7bb03;
        const string LevelOriginal = "8b89080500008bc6c1e00703c68d14495f5e8d0440c1e0028b84d024ea5600c20400";
        const string LevelPatched  = "8b89080500005f5e83f932730c69c1cd000000c1e80bc2040031c0c2040090909090";

        /// <summary>Assemble the patch sites for the chosen cheat features and scope.</summary>
        public static List<PatchSite> CheatFeatureSites(ICollection<string> features, bool allPlayers)
        {
            var l = new List<PatchSite>();
            if (features == null || features.Count == 0) return l;

            // Fog of war off - view-only, so identical in both scopes.
            if (features.Contains(CheatFog))
            {
                var fog = FogCaveBody();
                l.Add(new PatchSite { Name = "fog_redirect", Offset = FOG_HOOK, Original = H("a100a05700"),      Patched = HookJmp(FOG_HOOK, FOG_CAVE, 5) });
                l.Add(new PatchSite { Name = "fog_cave",     Offset = FOG_CAVE, Original = H(Zeros(fog.Length)), Patched = fog });
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
                    var mj = MjCaveBody(); var res = ResCaveBody();
                    l.Add(new PatchSite { Name = "free_metajoules_hook", Offset = MJ_HOOK,  Original = H("d981c0000000"),   Patched = HookJmp(MJ_HOOK, MJ_CAVE, 6) });
                    l.Add(new PatchSite { Name = "free_metajoules_cave", Offset = MJ_CAVE,  Original = H(Zeros(mj.Length)), Patched = mj });
                    l.Add(new PatchSite { Name = "free_resource_hook",   Offset = RES_HOOK, Original = H("d981cc000000"),   Patched = HookJmp(RES_HOOK, RES_CAVE, 6) });
                    l.Add(new PatchSite { Name = "free_resource_cave",   Offset = RES_CAVE, Original = H(Zeros(res.Length)),Patched = res });
                }
            }

            // Instant build: BuildTime -> 1.0 (fld1). NOT 0 - zero hangs the production queue.
            if (features.Contains(CheatTurbo))
            {
                if (allPlayers)
                    l.Add(new PatchSite { Name = "fast_build_all",  Offset = 0xa7220, Original = H("8b89500100"),   Patched = H("d9e8c20400") });
                else
                {
                    var bt = BtCaveBody();
                    l.Add(new PatchSite { Name = "fast_build_hook", Offset = BT_HOOK, Original = H("8b8950010000"),   Patched = HookJmp(BT_HOOK, BT_CAVE, 6) });
                    l.Add(new PatchSite { Name = "fast_build_cave", Offset = BT_CAVE, Original = H(Zeros(bt.Length)), Patched = bt });
                }
            }

            // Unlimited high-tier combot crews: lift both build-quota gates, plus the deterministic
            // crew-name cave (each tier gets its own fixed slice of 10 names, so higher tiers never
            // run out and the AI's late crews stop spamming "Couldn't find a name for this crew").
            // Always global (AI included): the quota is per crew type, not per player.
            if (features.Contains(CheatCrews))
            {
                l.Add(new PatchSite { Name = "crew_tier",     Offset = 0x7c220, Original = H("668b81a800000066"), Patched = H("b863000000c20400") });
                l.Add(new PatchSite { Name = "mp_crew_gate",  Offset = 0x7c150, Original = H("568bf18b0d28055200"), Patched = H("b801000000c3") });
                l.AddRange(CrewNameCaveSites());   // deterministic per-tier naming (supersedes the je->jmp here)
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
            public bool MoveSpeed, MoveSpeedScopeAll;  // experimental: unit movement speed
            public double MoveSpeedFactor;
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

            // movement speed: hook at the first MaxVelocity read, factor stored at the cave head.
            // Scope is told apart by the owner test ("mov eax,[ecx+0x28]") the player-only stubs carry.
            if (d.LongLength > SPEED_HOOK1 && d[SPEED_HOOK1] == 0xE9 && SPEED_CAVE + 4 <= d.LongLength)
            {
                r.MoveSpeed = true;
                r.MoveSpeedFactor = System.BitConverter.ToSingle(d, (int)SPEED_CAVE);
                r.MoveSpeedScopeAll = true;
                for (long i = SPEED_CAVE; i < SPEED_CAVE + 128 && i + 3 <= d.LongLength; i++)
                    if (d[i] == 0x8B && d[i + 1] == 0x41 && d[i + 2] == 0x28) { r.MoveSpeedScopeAll = false; break; }
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
