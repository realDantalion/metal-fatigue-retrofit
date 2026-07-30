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
        public double Factor;   // unit budget as a multiple of vanilla
        public List<PatchSite> Sites;

        // Display strings are localized — see Lang.
        // A factor needs no translation. Only Maximum keeps a localised name, because
        // "12.8x" would be a worse label than the word for what it is.
        public string Title => Factor == PatchData.MaximumFactor
            ? Lang.ProfileTitle("unleashed")
            : Factor.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "×";
        // Maximum keeps its own text; every other step differs only by a number, so one
        // sentence with the factor filled in beats nine near-identical translations.
        public string Description => Factor == PatchData.MaximumFactor
            ? Lang.ProfileDesc("unleashed")
            : string.Format(Lang.T("prof.units.desc"), Title);
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
        //
        // The version stamp is the load-bearing one, and it is here rather than only in the read-out
        // for a reason: everything above it belongs to one feature, so the check is only as strong as
        // "a unit-limit profile was always written". That happens to hold today, but it is an accident
        // of Apply always taking a profile, and the cost of it ever ceasing to hold is a contaminated
        // backup - Apply copies a file that looks pristine over the .bak and then treats it as the
        // original forever, silently. The stamp is written unconditionally by every Apply since 1.4.0
        // and is eight zero bytes in an untouched exe, so it catches a patched file no matter which
        // boxes were ticked.
        public static readonly PatchSite[] PristineSignature =
        {
            new PatchSite { Name = "arena_size",     Offset = 0xd231,   Original = H("0000A000") },
            new PatchSite { Name = "arena_sentinel", Offset = 0xd243,   Original = H("F4FF9F00") },
            new PatchSite { Name = "threshold",      Offset = 0xd5b4,   Original = H("00008000") },
            new PatchSite { Name = "crew_search",    Offset = 0x7b81d,  Original = H("7457") },
            new PatchSite { Name = "version_stamp",  Offset = STAMP_SITE, Original = H(Zeros(8)) },
            new PatchSite { Name = "rdata_flags",    Offset = 0x22f,      Original = H("40") },
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
        // they hold — so reusing a foreign slot would surface another tier's name. A tier must only
        // ever show names from its own slice; that rule is not negotiable. A correct fix would
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
                // A branch target is an absolute VA. Passing IMAGE_BASE + someVA - i.e. adding the
                // base to an address that already carries it - lands 0x400000 past the end of the
                // image, and the game dies with an invalid unwind target the first time the cave
                // runs. That shipped once (2026-07-27) because the pre-flight check only compared
                // the expected ORIGINAL bytes at each site and never asked where the cave jumps.
                if (target <= IMAGE_BASE || target >= IMAGE_BASE + KnownSize)
                    throw new System.InvalidOperationException(string.Format(
                        "Cave at 0x{0:x} branches to 0x{1:x8}, which is outside the image " +
                        "(0x{2:x8}..0x{3:x8}). A VA with the image base added twice looks exactly like this.",
                        _caveOff, target, IMAGE_BASE, IMAGE_BASE + KnownSize));

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
        // Until 1.5.0 this called MakeAllSeen three times, once per layer. MakeAllSeen is only a
        // shim: it picks the "a vision source is standing here" sentinel and tail-calls the
        // renderer's FOWFill, whose per-cell body writes the display slot and then rep-movsd's the
        // same value into all eight player slots. There is no player argument anywhere in that
        // chain, so the reveal reached every AI as well - and the AI reads its own slot both for
        // target acquisition and for the world model behind its build decisions. The cheat was
        // handing the opponent a free map.
        //
        // So the walk is ours now, and it writes exactly two of the nine slots per cell:
        //   cell+0x10          the display slot - terrain shading and the renderer's object cull
        //   cell+0x10+4*P      the local player's slot - minimap terrain and every check the EXE
        //                      makes on the player's behalf
        // Everything else keeps whatever the fog system put there, so an AI's map stays honest.
        //
        // Grid shape, all of it verified in both GlideRendEng and SGLRendEng, which agree offset
        // for offset: pRendEng -> +0xa0 CRendBackground; +0x10 W, +0x12 H, +0x18 layer count;
        // layer structs are 0x24 bytes from +0x30 with the grid pointer at +0xc, so the first grid
        // is at +0x3c; cells are 0x34 bytes.
        //
        // The walk covers rows 3..H-4, which is the region FOWFill covers. The engine deliberately
        // leaves a 3-cell frame at the map edge dark, and lighting it would show as a lit border on
        // the map and the minimap - the near edge is not clipped away, only the far one is.
        //
        // Both loops are guarded before they run. The engine guards this same layer count in its
        // own two loops over it, and a count of zero here would mean a dec/jnz pair walking 2^32
        // cells through whatever follows the grid.
        const long FOG_CAVE_END = MJ_CAVE;

        static byte[] FogCaveBody() => new CaveAsm(FOG_CAVE)
            .Raw("53565755")                    // push ebx/esi/edi/ebp - the hook is Process's first
                                                //   byte, so these still belong to its caller
            .Raw("a1bc295200")                  // mov   eax,[pRendEng]
            .Raw("8bb0a0000000")                // mov   esi,[eax+0xa0]        CRendBackground
            .Raw("0fb74610")                    // movzx eax,word [esi+0x10]   W
            .Raw("0fb74e12")                    // movzx ecx,word [esi+0x12]   H
            .Raw("83e906")                      // sub   ecx,6                 drop the 3-cell frame
            .Raw("0fafc8")                      // imul  ecx,eax               cells to write
            .Raw("69c09c000000")                // imul  eax,eax,0x9c          three rows, in bytes
            .Raw("50")                          // push  eax                   no register left for it
            .Raw("85c9")                        // test  ecx,ecx
            .Raw("7e2f")                        // jle   done                  tiny or absent grid
            .Raw("8b3db8295200")                // mov   edi,[PlayerIndex]     0..8, so the scaled
            .Raw("8b6e18")                      // mov   ebp,[esi+0x18]        store cannot leave the cell
            .Raw("85ed")                        // test  ebp,ebp
            .Raw("7422")                        // jz    done
            .Raw("83c63c")                      // add   esi,0x3c              &layer[0].pGrid
            .Raw("bb9976967e")                  // mov   ebx,1e38              MakeAllSeen's own constant
                                                // layer:
            .Raw("8b16")                        // mov   edx,[esi]             this layer's grid
            .Raw("031424")                      // add   edx,[esp]             skip the frame rows
            .Raw("8bc1")                        // mov   eax,ecx
                                                // cell:
            .Raw("895a10")                      // mov   [edx+0x10],ebx        display slot
            .Raw("895cba10")                    // mov   [edx+edi*4+0x10],ebx  local player's slot
            .Raw("83c234")                      // add   edx,0x34
            .Raw("48")                          // dec   eax
            .Raw("75f3")                        // jnz   cell
            .Raw("83c624")                      // add   esi,0x24
            .Raw("4d")                          // dec   ebp
            .Raw("75e6")                        // jnz   layer
                                                // done:
            .Raw("58")                          // pop   eax                   drop the row offset
            .Raw("5d5f5e5b")                    // pop   ebp/edi/esi/ebx
            .Raw("a100a05700").Jmp(0x407c75)    // relocated mov eax,[0x57a000]; back
            .Bytes;

        static List<PatchSite> UnleashedSites()
        {
            var l = new List<PatchSite>
            {
                Mem(0xd231, "0000A000", "00000008"),
                Mem(0xd243, "F4FF9F00", "F4FFFF07"),
                Mem(0xd5b4, "00008000", "00008007"),
            };
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
        // id, so the ally's vision clears the local player's fog instead. Asks the game's own static
        // CPlayerManager::IsAlly, so in-game alliance changes are honoured automatically.
        // eax (already masked for the call) is saved across the cdecl call; esi/edi are free to use.
        // ?UpdateVisible@CFogOfWar@@QAEXXZ (0x407ae0) is called once per vision source on every
        // fog pass, and ends by handing the renderer five arguments - position, layer, radius,
        // "is a structure", and the OWNER at [esi+0x1c]. The renderer writes that owner's fog slot,
        // cell+0x10+4*owner, and additionally the display slot 0 when the owner happens to be the
        // local player.
        //
        // Up to 1.5.0 this feature hooked the owner load at 0x7b6c and, for an allied source,
        // REPLACED the owner with the local index. That gave the local player his ally's vision by
        // taking it away: the ally's own slot stopped being written anywhere on the map. For a
        // human ally in multiplayer that only affects the patched machine's copy of his units, but
        // an allied AI reads its own slot for target acquisition and for its world model, so it
        // went effectively blind - the patch made the ally useless as an ally.
        //
        // The hook now sits on the render call itself at 0x7b90, which is what makes the fix fit:
        // after the call eax is dead, so nothing has to be preserved across IsAlly, and the second
        // pass can be a tail jump back into the game's own argument setup instead of eighteen bytes
        // of re-pushed arguments. Pass one runs verbatim vanilla with the true owner. Only then, if
        // the source belongs to an ally, does the cave run the setup a second time with the local
        // index, so the local player's slot is filled as well. Recursion terminates on its own: the
        // second pass arrives with owner == PlayerIndex and the first test sends it straight to the
        // epilogue.
        const long SV_HOOK        = 0x7b90;   // the call dword [edx+0x8c] at the end of UpdateVisible
        const long SV_HOOK_LEGACY = 0x7b6c;   // where 1.1.x..1.5.0 hooked instead - see LegacyLayouts

        static byte[] SharedVisionCaveBody() => new CaveAsm(SV_CAVE)
            .Raw("ff928c000000")                     // relocated: the ally's OWN slot, exactly as vanilla
            .Raw("a1b82952003bf8741d5057").Call(0x4788f0)   // local? ally? -> IsAlly(owner, local)
            .Raw("83c40885c0740f8b3db82952000fb64624").Jmp(0x407b74)   // owner := local, rebuild arg4, go again
            .Raw("5f5ec3")                           // inlined epilogue of 0x407b96: pop edi / pop esi / ret
            .Bytes;

        public static List<PatchSite> SharedVisionSites()
        {
            var cave = SharedVisionCaveBody();
            return new List<PatchSite>
            {
                new PatchSite { Name = "shared_vision_hook", Offset = SV_HOOK,  Original = H("ff928c000000"), Patched = HookJmp(SV_HOOK, SV_CAVE, 6) },
                new PatchSite { Name = "shared_vision_cave", Offset = SV_CAVE,  Original = H(Zeros(cave.Length)), Patched = cave },
            };
        }

        // --- Combot part + superweapon unlock ------------------------------------------------
        // Found via Cheat Engine (see docs/plan-2.0.md): the assembly-bay build-list gate at
        // fileoff 0x4bade9 reads a part's availability mask at descriptor+0x4c and skips the part
        // if the local player's bit is absent. ORing the bit in makes it buildable WITH its icon
        // (merely bypassing the test leaves the icon logic seeing it unavailable).
        //
        // The 11-byte gate is hooked into a cave that walks a table of the chosen descriptor
        // addresses; on a match it ORs the player bit, then either way re-runs the original
        // mov/test/je. Loop-over-table (not inline cmp per part) keeps the cave small and the
        // jumps short regardless of how many parts are selected.
        // All FILE offsets (file_offset == RVA in this build). VAs are formed by adding IMAGE_BASE
        // where a jump/hook target is computed; do not bake IMAGE_BASE into these constants.
        const long GATE_HOOK = 0xbade9;     // 11 bytes overwritten: mov ecx,[esi+0x4c]; test eax,ecx; je
        const long UNLOCK_CAVE = 0xd1dd4;   // free space right after the shared-vision cave
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
        // Repacked in 1.5.0 to give the fog cave the room a per-player reveal needs. The zone
        // starts at 0xd1cd2, and the 14 bytes that used to sit unused in front of the fog cave
        // are part of its allocation now. Every body below is position-independent - CaveAsm
        // derives its rel32 from the cave offset and HookJmp from both - so moving a cave is
        // editing one constant.
        const long FOG_CAVE = 0xd1cd2;
        const long MJ_CAVE = 0xd1d38;
        const long RES_CAVE = 0xd1d5b;
        const long BT_CAVE = 0xd1d7e;
        const long SV_CAVE = 0xd1da5;
        const long UNLOCK_CAVE_END = 0xd1ed0;   // parts must not reach the crew-name cave
        const long CAVE_ZONE_END = 0xd2000;

        static uint CaveVA(long fileoff) => (uint)(IMAGE_BASE + fileoff);

        // --- Superseded layouts --------------------------------------------------------------------
        // An exe patched by an older release carries caves at offsets this build no longer writes, so
        // re-patching would leave that dead code behind. Patcher.Apply always rebuilds from the
        // pristine .bak, which wipes it — the case that needs saying is a patched exe with
        // no clean backup, where the old layout cannot be undone from here. Detecting it makes it possible to say so
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
            new LegacyLayout { Release = "1.1.x", HookOffset = SV_HOOK_LEGACY, CaveOffset = 0xd1de0 },
            new LegacyLayout { Release = "1.1.x", HookOffset = GATE_HOOK, CaveOffset = 0xd1e20 },

            // Shared vision moved its hook in 1.5.0, from the owner load to the render call. A file
            // carrying the old hook must not be updated in place: the new hook site is still
            // pristine there, so patching would leave the old jmp standing and both would run into
            // a cave that no longer means what the old hook expects.
            new LegacyLayout { Release = "1.2.x - 1.4.x", HookOffset = SV_HOOK_LEGACY, CaveOffset = 0xd1da0 },

            // 1.2.x - 1.4.x cave placement, retired by the 1.5.0 repack. One row per hook, so a
            // file from that generation is recognised whichever feature it happens to carry.
            new LegacyLayout { Release = "1.2.x - 1.4.x", HookOffset = FOG_HOOK,  CaveOffset = 0xd1ce0 },
            new LegacyLayout { Release = "1.2.x - 1.4.x", HookOffset = MJ_HOOK,   CaveOffset = 0xd1d10 },
            new LegacyLayout { Release = "1.2.x - 1.4.x", HookOffset = RES_HOOK,  CaveOffset = 0xd1d40 },
            new LegacyLayout { Release = "1.2.x - 1.4.x", HookOffset = BT_HOOK,   CaveOffset = 0xd1d70 },
            new LegacyLayout { Release = "1.2.x - 1.4.x", HookOffset = GATE_HOOK, CaveOffset = 0xd1dd0 },
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
        const long SPEED_CAVE_END = 0xd1fa0;   // bounded since 1.4.0: the crew-accounting caves follow
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
                    // The owner comes from the THGOBJECT at mover+0x40, whose HIGH WORD is the
                    // player number - the same thing CPlayerManager::AmLocalMachine (0x42e7e0)
                    // compares against ?PlayerIndex@@3HA at 0x5229b8.
                    //
                    // NOT [ecx+0x28]. That is CMover::m_player, and for these objects it is dead:
                    // ??0CMover zero-initialises it, ?SetPlayer@CMover has no call site in the
                    // image, and the only code that fills it is the inlined SetPlayer in the
                    // projectile path. CVehicle attaches through CMover::Attach and never writes
                    // it, so the gate was comparing a constant 0 against the local player index and
                    // was false for everyone, including the local player - which is exactly how it
                    // behaved: "my units only" did nothing at all while "all players" worked.
                    // (Wrong since 1.2.0, found 2026-07-27.)
                    //
                    // The manager-pointer idiom from the other player-scoped caves cannot be used
                    // here: it compares a CPlayerManager* against `this`, which only holds when
                    // `this` is a CBasicGobject. Here it is a CMover (sizeof 0x64) - reading the
                    // +0x150 owner field would run off the end of the object.
                    Emit("50");                        // push eax
                    Emit("0fb74142");                  // movzx eax, word [ecx+0x42]   ; owner player
                    Emit("3b05"); U32(LOCAL_PLAYER);   // cmp  eax,[?PlayerIndex@@3HA]
                    Emit("58");                        // pop  eax   (POP leaves EFLAGS alone)
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

            if (SPEED_CAVE + c.Count > SPEED_CAVE_END)
                throw new System.InvalidOperationException(
                    string.Format("Movement-speed cave overflows .text free space ({0} bytes, max {1}).",
                                  c.Count, SPEED_CAVE_END - SPEED_CAVE));

            sites.Add(new PatchSite { Name = "move_speed_cave", Offset = SPEED_CAVE, Original = H(Zeros(c.Count)), Patched = c.ToArray() });
            sites.Add(SpeedHook("move_speed_hook1", SPEED_HOOK1, stub1, "d941148b410c"));
            sites.Add(SpeedHook("move_speed_hook2", SPEED_HOOK2, stub2, "d905a47e4d00d84914"));
            sites.Add(SpeedHook("move_speed_hook3", SPEED_HOOK3, stub3, "d84914d981a0000000"));
            sites.AddRange(SpeedLookaheadClampSites());
            return sites;
        }

        // The path look-ahead at 0x4393c0 decides how many probe points to walk ahead of a moving
        // unit, and looks each result up in a FIVE-entry table at 0x50c400 (5, 6, 4, 4, 4). The
        // count is
        //     N = trunc((PI - |headingError|) * (1/MaxVelocity) * currentVelocity * ramp + 2.0)
        // where 1/MaxVelocity is baked into the mover at 0x43854d and currentVelocity converges on
        // whatever the integrator is told to aim for. The product of those two is meant to be a
        // 0..1 speed ratio, which is why five entries are exactly enough in vanilla: at full speed,
        // straight ahead, N tops out at 5 and index 4 is the last entry.
        //
        // This patch scales the target velocity but not the reciprocal, so that ratio becomes the
        // multiplier itself and N grows with it - 6 at 1.5x, 21 at 6x. The load at 0x439691 has no
        // bounds check (its sibling switch at 0x43948e does), so it reads past the table into
        // 0, 0xc, 0, 0 and the unit acts on response flags that were never meant for it.
        //
        // Clamping the count is better than lengthening the table: it needs no data space, and it
        // restores exactly the vanilla ceiling rather than inventing behaviour for indices the
        // engine never defined. The clamp goes in a trampoline in the 15 bytes of alignment padding
        // after CVehicleMover::Init, and the existing __ftol call is redirected through it - so
        // nothing moves and no cave byte is spent. Checked image-wide: no branch and no data word
        // reaches into 0x438551..0x43855f.
        const long SPEED_FTOL_REL = 0x39448;    // the rel32 of the call at 0x439447, opcode untouched
        const long SPEED_CLAMP    = 0x38551;    // alignment padding after Init's ret at 0x438550

        static List<PatchSite> SpeedLookaheadClampSites()
        {
            return new List<PatchSite>
            {
                // call 0x4c5230 -> call 0x438551
                new PatchSite { Name = "speed_lookahead_call",  Offset = SPEED_FTOL_REL, Original = H("e4bd0800"), Patched = H("05f1ffff") },
                //   call __ftol / cmp eax,5 / jle +3 / push 5 / pop eax / ret
                new PatchSite { Name = "speed_lookahead_clamp", Offset = SPEED_CLAMP,
                                Original = H("909090909090909090909090909090"),
                                Patched  = H("e8dacc080083f8057e036a0558c390") },
            };
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

        // --- High-tier crew quota: give it back when the crew dies ---------------------------------
        // A[level] at [player+lvl*4+0x5d0] is the per-tier elite quota. Proven by its only reader:
        // CanBuildCrew (0x47c220) is called from exactly one place, 0x460267, inside a loop that
        // steps 1..4 through the pointer table at 0x5112a0 (0x568b70/0x568b08/0x568aa0/0x568a38 =
        // tiers 2..5; tier 1's 0x568bd8 is deliberately absent) and enables or hides that tier's
        // build button through the mask setters at 0x4b79e0 / 0x4b7a90 / 0x4b7af0. The quota values
        // are the table at 0x512118: 5, 4, 3, 2.
        //
        // Vanilla decrements A[level] and B[level] when a crew of that tier is created and never
        // gives them back. Lose both tier-5 crews and the tier-5 button is gone for the rest of the
        // match — the crews are dead permanently, not merely unavailable. That is the bug.
        //
        // The fix reads the slot's level at the instruction that is about to zero it, then credits
        // A[level] and B[level]. Both, because CanBuildCrew tests A > 0 AND A - B > 0, and those two
        // are only decremented in lockstep; crediting one alone would drift the difference.
        //
        // The relocated store runs unconditionally, exactly as vanilla does, including vanilla's own
        // out-of-range write for ci = 99/100. Only the credit is guarded, on both ci and level.
        const long RELEASE_CAVE = 0xd1fa0;
        const long RELEASE_HOOK = 0x7ba2d;   // last store of the cc == 0 branch; 7 bytes relocated
        const uint RELEASE_RET  = 0x47ba34;  // pop edi / pop esi / ret 8 - also a je target, never overrun

        static byte[] ReleaseCaveBody() => new CaveAsm(RELEASE_CAVE)
            .Raw("8b948818e45600")       // mov edx, [eax+ecx*4+0x56e418]  ; level, before it is zeroed
            .Raw("89bc8818e45600")       // relocated: mov [eax+ecx*4+0x56e418], edi
            .Raw("3db0040000")           // cmp eax, 0x4b0                 ; eax = ci*0x18, so ci vs 50
            .Raw("7316")                 // jae back                       ; unsigned: catches ci = 99/100
            .Raw("83fa05")               // cmp edx, 5                     ; a level outside 0..4 is corrupt
            .Raw("7311")                 // jae back
            .Raw("8d0411")               // lea eax, [ecx+edx]             ; ecx = idx*387, so *4 gives
            .Raw("ff0485d4e95600")       // inc [eax*4+0x56e9d4]           ; idx*0x60c + level*4: A[level]++
            .Raw("ff0485fce95600")       // inc [eax*4+0x56e9fc]           ; B[level]++
            .Jmp(RELEASE_RET)
            .Bytes;

        /// <summary>Return a high-tier crew's build quota when it dies. Vanilla charges the quota at
        /// creation and never refunds it, so a lost elite crew can never be replaced.</summary>
        public static List<PatchSite> CrewQuotaReleaseSites()
        {
            var cave = ReleaseCaveBody();
            return new List<PatchSite>
            {
                new PatchSite { Name = "crew_quota_cave", Offset = RELEASE_CAVE, Original = H(Zeros(cave.Length)), Patched = cave },
                new PatchSite { Name = "crew_quota_hook", Offset = RELEASE_HOOK, Original = H("89bc8818e45600"),
                                Patched = HookJmp(RELEASE_HOOK, RELEASE_CAVE, 7) },
            };
        }

        // --- Keep the one-byte name reuse from draining the census --------------------------------
        // CrewNameFix turns the "is this name in use?" test into an unconditional jump, so a tier
        // reuses its own names instead of failing. The FOUND path behind it still charges the census
        // though: A[L]-- and B[L]-- run for a slot that was never free. A drains faster than in
        // vanilla, CanBuildCrew clamps it to 0 at 0x47c252, and the build button greys out silently.
        //
        // The decrements cannot simply be skipped: the store that finishes them (0x47b8b3) sits after
        // the cursor update and reuses ecx/edi from the block. So the cave pre-credits instead —
        // if the slot was already taken, A[L]++ / B[L]++ first and let vanilla's decrements cancel
        // them out. eax is dead here (last read at 0x47b880, overwritten at 0x47b889), so nothing
        // needs saving, and no flags survive past 0x47b8b0's own cmp.
        const long FLIPACC_CAVE = 0xd1fd0;
        const long FLIPACC_HOOK = 0x7b882;   // mov [ecx+0x10], 1 — the store that loses the old value
        const uint FLIPACC_RET  = 0x47b889;

        static byte[] FlipAccountingCaveBody() => new CaveAsm(FLIPACC_CAVE)
            .Raw("83791000")             // cmp dword [ecx+0x10], 0   ; was the slot free?
            .Raw("c7411001000000")       // relocated: mov [ecx+0x10], 1
            .Raw("7411")                 // je back                   ; free -> vanilla accounting
            .Raw("8b4114")               // mov eax, [ecx+0x14]       ; eax = slot.level
            .Raw("ff8486d0050000")       // inc [esi+eax*4+0x5d0]     ; A[L]++
            .Raw("ff8486f8050000")       // inc [esi+eax*4+0x5f8]     ; B[L]++
            .Jmp(FLIPACC_RET)
            .Bytes;

        /// <summary>Ships with <see cref="CrewNameFix"/> and only with it: without the flip the
        /// allocator never reaches this path with an occupied slot.</summary>
        public static List<PatchSite> CrewFlipAccountingSites()
        {
            var cave = FlipAccountingCaveBody();
            return new List<PatchSite>
            {
                new PatchSite { Name = "crew_flipacc_cave", Offset = FLIPACC_CAVE, Original = H(Zeros(cave.Length)), Patched = cave },
                new PatchSite { Name = "crew_flipacc_hook", Offset = FLIPACC_HOOK, Original = H("c7411001000000"),
                                Patched = HookJmp(FLIPACC_HOOK, FLIPACC_CAVE, 7) },
            };
        }

        /// <summary>
        /// Lift the crew-name limit: a tier reuses its own names instead of running out, so more
        /// than 50 crews can be alive at once. Two sites, never one without the other — the flip
        /// alone would let the FOUND path charge the free-slot census for a slot that was never
        /// free. Shipped as its own option since 1.4.0; before that it rode along with Maximum,
        /// where nobody could tell it apart from the unit budget.
        /// </summary>
        public static List<PatchSite> CrewLimitOffSites()
        {
            var l = new List<PatchSite> { CrewNameFix() };
            l.AddRange(CrewFlipAccountingSites());
            return l;
        }

        // --- Version stamp -------------------------------------------------------------------------
        // Eight bytes at the very end of the cave zone: "MFRT" plus major/minor/patch and the cave
        // layout revision. Written by every patch since 1.4.0, so any later release can tell at a
        // glance who wrote these bytes and whether they still mean what it expects.
        //
        // Before 1.4.0 nothing was stamped, and 1.2 and 1.3 are genuinely indistinguishable - their
        // patch sites are byte-identical, the only later addition being the music table, which is
        // only present when the player imported music. Such a file reads as "ours, but unstamped",
        // and there is nothing to do but restore the original first.
        const long STAMP_SITE = 0xd1ff8;   // last 8 bytes before CAVE_ZONE_END
        static readonly byte[] StampMagic = { 0x4D, 0x46, 0x52, 0x54 };   // "MFRT"

        // The last byte is what the lock actually turns on: not the release number, but whether the
        // cave layout still matches. Two releases that place the same bytes at the same offsets can
        // read each other's work, and sending everybody through Restore original for a bugfix that
        // moved nothing would be pure noise. 1.4.0 wrote a zero here and its layout is still the
        // current one. Bump this when a cave offset, a hook target or the meaning of a patched byte
        // moves - never just because a version shipped.
        // 1 since 1.5.0: shared vision's hook moved from 0x7b6c to 0x7b90. A file carrying the
        // old hook cannot be updated in place - the new site is still pristine there, so the old
        // jmp would survive alongside the new one and both would enter a cave that no longer
        // means what the old hook expects. Restore original first.
        const byte LAYOUT_REV = 1;

        public static List<PatchSite> VersionStampSites()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var b = new byte[8];
            System.Array.Copy(StampMagic, b, 4);
            b[4] = (byte)v.Major; b[5] = (byte)v.Minor; b[6] = (byte)v.Build; b[7] = LAYOUT_REV;
            return new List<PatchSite>
            {
                new PatchSite { Name = "version_stamp", Offset = STAMP_SITE, Original = H(Zeros(8)), Patched = b },
            };
        }

        /// <summary>The version that patched this file, or null when there is no stamp (pre-1.4.0,
        /// or a pristine exe).</summary>
        public static string ReadStamp(byte[] d)
        {
            if (d == null || d.LongLength < STAMP_SITE + 8) return null;
            for (int i = 0; i < 4; i++)
                if (d[STAMP_SITE + i] != StampMagic[i]) return null;
            return string.Format("{0}.{1}.{2}", d[STAMP_SITE + 4], d[STAMP_SITE + 5], d[STAMP_SITE + 6]);
        }

        /// <summary>The cave layout revision a file was patched with, or null when there is no
        /// stamp. Not the release number - see LAYOUT_REV.</summary>
        public static int? ReadLayoutRev(byte[] d)
        {
            if (d == null || d.LongLength < STAMP_SITE + 8) return null;
            for (int i = 0; i < 4; i++)
                if (d[STAMP_SITE + i] != StampMagic[i]) return null;
            return d[STAMP_SITE + 7];
        }

        /// <summary>The cave layout revision this build writes and can read back.</summary>
        public static int OwnLayoutRev { get { return LAYOUT_REV; } }

        /// <summary>The version of this patcher, in the same shape ReadStamp returns.</summary>
        public static string OwnVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return string.Format("{0}.{1}.{2}", v.Major, v.Minor, v.Build);
        }

        /// <summary>Assemble the patch sites for the chosen cheat features and scope.</summary>
        public static List<PatchSite> CheatFeatureSites(ICollection<string> features, bool freeBuildAll, bool turboAll)
        {
            var l = new List<PatchSite>();
            if (features == null || features.Count == 0) return l;

            // Fog of war off, for the local player and nobody else - so it needs no scope switch.
            // See FogCaveBody for why it walks the grid itself rather than calling MakeAllSeen.
            if (features.Contains(CheatFog))
            {
                var fog = FogCaveBody();
                if (FOG_CAVE + fog.Length > FOG_CAVE_END)
                    throw new System.InvalidOperationException(
                        string.Format("Fog cave overflows into the next cave ({0} bytes, max {1}).",
                                      fog.Length, FOG_CAVE_END - FOG_CAVE));
                l.Add(new PatchSite { Name = "fog_redirect", Offset = FOG_HOOK, Original = H("a100a05700"),      Patched = HookJmp(FOG_HOOK, FOG_CAVE, 5) });
                l.Add(new PatchSite { Name = "fog_cave",     Offset = FOG_CAVE, Original = H(Zeros(fog.Length)), Patched = fog });
            }

            // Free building: MetaJoules + Resources (+ ManPower, the third gate the old profiles
            // never touched). "All" stubs each gate to return 1; "player" uses the owner-checking caves.
            if (features.Contains(CheatFreeBuild))
            {
                if (freeBuildAll)
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
                if (turboAll)
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
        // --- Rimtech music: give it a track range of its own ---------------------------------------
        // Metal Fatigue shipped on two CDs — CD 1 carried Rimtech's music, CD 2 Mil-Agro's and
        // Neuropa's. The game never told them apart in code: it asked the drive for a track NUMBER,
        // and the disc in the tray decided whose music that was. The re-release ships only CD 2's
        // audio (MUSIC\Track02..23.ogg, played by the ogg-winmm shim in _inmm.dll), so Rimtech asks
        // for the same numbers as Mil-Agro and gets Mil-Agro's music.
        //
        // The per-faction ranges are a table of {firstTrack, lastTrack} pairs at 0x50bfa0, picked by
        // CSoundSystem at 0x433d0c:
        //     [0]  3..7   Rimtech  quiet     [1]  8..12  Rimtech  action
        //     [2]  3..7   Mil-Agro quiet     [3]  8..12  Mil-Agro action
        //     [4] 13..17  Neuropa  quiet     [5] 18..22  Neuropa  action
        // Entries 0/1 are byte-identical to 2/3 — that duplication IS the bug. Pointing Rimtech at a
        // fresh range and supplying CD 1's music as Track24..33 fixes it; confirmed in-game, the
        // game then requests 24..33 and plays Rimtech's own music.
        //
        // NEVER write this without the files in place: the picker folds any track above the number of
        // files present down by ten (sub edx,0xa at 0x433d4e), so a bare table patch would land
        // Rimtech on 14..23 and play Neuropa's music — different, but no less wrong.
        const long MUSIC_TABLE = 0x10bfa0;
        public const int MusicFirstSlot = 24;
        public const int MusicSlotCount = 10;   // 5 quiet + 5 action, in that order

        public static List<PatchSite> RimtechMusicSites() => new List<PatchSite>
        {
            new PatchSite
            {
                Name     = "rimtech_music_range",
                Offset   = MUSIC_TABLE,
                Original = H("0300000007000000" + "080000000c000000"),   // {3,7} {8,12}
                Patched  = H("180000001c000000" + "1d00000021000000"),   // {24,28} {29,33}
            },
        };

        /// <summary>True if the exe already points Rimtech at the separate track range.</summary>
        public static bool HasRimtechMusic(byte[] d) =>
            Has(d, MUSIC_TABLE, "180000001c0000001d00000021000000");


        // --- Alternative cheats: spawn units from the game's own hidden cheat menu ------------------
        //
        // Metal Fatigue ships a working cheat panel that Zono only made invisible: three widgets on
        // the ESC menu named InGameCheatEnable / InGameCheatMj / InGameCheatWin, with hotkeys 'C',
        // 'm' and 'l'. Three Shift+C presses inside two seconds set the shown-bit on the other two
        // (counter at menu+0x104, 2.0f window at 0x4d2bcc), after which 'm' grants 10000 MetaJoules
        // and 'l' wins the mission outright. Both branches are replaced here.
        //
        // The payload creates gobjects through ?Create@CGobject@@SAPAV1@KG@Z (0x42a4c0) and places
        // them at the camera focus from ?GetLookAtPos@CCamera@@QAEXAAVCLVector@@@Z (0x45def0). A
        // chassis built this way gets the engine's field-assembly brain rather than the factory one,
        // and the six-record arrangement it needs - chassis, legs, torso, two arms, crew - is the one
        // the campaign uses for the alien combot it places in X_08. The payload does the assembling
        // itself; see below for why. Owner 0 is the engine's own alien slot (its crew name at 0x512380[0] is
        // literally "Alien"), and IsAlly returns false whenever either index is 0, so those units are
        // hostile to everyone. A player-0 chassis also inherits the targetable bit automatically once
        // its crew is player 0 (0x48e32a), which is what makes it attackable rather than scenery.
        //
        // 937 bytes is far more than the 14 free bytes left in the .text cave zone, so this one lives
        // in .rdata's section slack (VA 0x508c57, verified all zero) and the section is marked
        // executable by a single header byte. The image has no relocations, no DEP opt-in and a zero
        // checksum, so nothing else has to change and the file size stays identical.
        //
        // The parts are attached explicitly - CRobot::AddPart (0x48e670) per part, then SetCrew
        // (0x48ea10) - rather than left lying on the ground for the engine's field-assembly brain to
        // collect. That brain's candidate filter (0x48f820) tests slot, unattached, layer and distance
        // and nothing else: no owner, no grouping. Two spawn groups inside its 400-unit radius left
        // twelve loose parts and two chassis rummaging through the same heap, which crashed the game a
        // moment later in a constructor reading a pointer that belonged to the other group. Attaching
        // as each part is created makes that unrepresentable. The assembly brain would then undo the
        // work - finding nothing to do it detaches slot 0 - so the payload finishes the robot the way
        // the engine does: BackToBrain leaves the ordinary vehicle brain pending and NewBrain promotes
        // it over the assembly one.
        //
        // It also refuses when the owner is out of handles. The allocator at 0x429e90 only ever
        // increments the per-owner cursor at [owner*4 + 0x525250] - no search for a free slot, no
        // bounds check - and a destroyed unit never gives its handle back, so spawning is a budget
        // spent per level rather than per living unit. Past the window from the seed table at
        // 0x50b7b8 (player 0 gets 1499, everyone else 8000) the cursor walks into the next player's
        // handles and corrupts them. The payload compares against seed[owner+1] and prints a refusal
        // through the same message call the vanilla MetaJoules cheat used.
        //
        // Each part is checked before it is created. CBasicGobject's ctor (0x4a6880) copies a
        // 67-dword per-player stats block into +0x38..+0x143, but only when GetExtraData(classId)
        // (0x42a5c0) and the pointer behind it are both there. +0x140 is the last dword of that
        // block and the shared arm ctor dereferences it two instructions later, so a class without
        // a stats block takes the process down inside Create - before the payload can inspect what
        // it got back. That is what the random crashes at 0x49f1fc were: raw heap, which read as a
        // valid pointer often enough to look intermittent.
        //
        // A random loadout has no reason to settle for an empty shoulder because the die landed
        // on a prototype arm this level never loaded, so the draw itself retries - up to forty
        // times, which makes a miss impossible in practice even when the level loaded a third of
        // the table. The fixed alien combot deliberately does NOT redraw: quietly swapping an
        // alien arm for a Rimtech one is not what that mode promises, so there the part is simply
        // left off. The part loop keeps the same check as the last line of defence for exactly
        // that case.
        //
        // Each part also gets the chassis handle written into +0x18. Create builds a synthetic
        // handle whose LOWORD is 0, and the part brain at 0x49f9a0 reads exactly that: a part with
        // no creator sitting at position (0,0) - which is what an attached part's relative position
        // is - gets donated to the owner's spare-parts inventory. That is why spawned torsos turned
        // up in an assembly bay nobody had used yet. Vanilla parts always carry a real creator, and
        // now these do too, which is also simply true: the robot did create them.
        //
        // The blob is emitted from the verified build, never retyped. It was disassembled instruction
        // by instruction and every branch target checked against an instruction boundary.
        const long ALIEN_CAVE   = 0x108c57;   // .rdata slack, 937 bytes available
        const long ALIEN_HOOK_L = 0x10fd0;    // the vanilla instant-win branch
        const long ALIEN_HOOK_M = 0x10f92;    // the vanilla MetaJoules branch, message and all
        const long RDATA_FLAGS  = 0x22f;      // high byte of .rdata's Characteristics dword

        const string AlienCaveHex =
            "609c6a00e81d0000009d61e97383f0ff609c0fb705b829520050e8070000009d" +
            "61e94483f0ff83ec348b44243889442430a1bc29520085c00f8432020000a170" +
            "a0570085c00f8425020000fc33c08bfcb90c000000f3ab8b0d70a057008bc450" +
            "e83452f5ff8b04240b4424040b44240885c00f84f8010000a1c029520085c074" +
            "0c83782800751b83782400751f33c98b048dff8e500089448c104183f90672ef" +
            "eb56c74424107210246beb4cc744241063803918be4b8f50006a105be8cb0100" +
            "00897c2414be178f50006a0d5be8ba010000897c2418be8b8f50006a185be8a9" +
            "010000897c241ce8a0010000897c2420c7442424116671dd8b4c243083f90877" +
            "138b048d5052520083c0103b048dbcb75000722ea12805520085c00f844f0100" +
            "008b0d142a520085c90f844101000068eb8f500051e83fd1faff83c408e92e01" +
            "00008b4424108b4c24305150e81817f2ff83c40885c00f84140100008944242c" +
            "8bc88b108bdc53ff5260837c2414000f84fb0000006a018b4c2430e86956f8ff" +
            "8b4c24288b7c8c1485ff7440e8e500000074398b4c24305157e8cb16f2ff83c4" +
            "0885c074278bf08bc88b108bdc53ff52608b44242c8b40148946188b4c24286a" +
            "0051568b4c2438e84d58f8ffff442428837c24280472a98b4424248b4c243051" +
            "50e88316f2ff83c40885c074168bf08bc88b108bdc53ff5260568b4c2430e8b6" +
            "5bf8ff8b74242c6a008bcee8d955f8ff8bcee8724ef8ff8b068dbe540100006a" +
            "00578bceff90400300008b46146a006a0057506a018bcee84dfef9ff8b8e4801" +
            "00008b06518bceff90880100008b068bceff90e80200008b068bceff904c0300" +
            "008b463485c07408508bcee84919f2ff83c434c2040057e8ed16f2ff5985c074" +
            "0a8b0085c074048b0085c0c36a285de82bd5fbff0fafc3c1e80f8b3c86e8d4ff" +
            "ffff75034d75e8c363803918788bc27df01a6ea67b46787f2d06488c116671dd" +
            "f01a6ea600f7b924aaf5a898de2af28fc8607a9607387a2c39cefd864eb2983d" +
            "f239387b3f287b6281321f41a13ac8538d28de25788bc27d2ebadba29f0a5ad5" +
            "7bc05f96013d3b3813584542df74d28c039fb4c33d65522e44afda370c9da063" +
            "a76231049848425d76fdc7b4ce8fb1ccbb8bbd8a8571d5e327379b288e51afb9" +
            "f5db7c494c254c77d22aa6dcf6082c4b59330b94177609762ef8c4bbd20fa37a" +
            "b59f6579878bbb3d1ee5ae7f22d9c76aa57da14d7b46787f25a3b23c85af7794" +
            "6ee2d2ee2d06488c3b36f8df3661ead0696b64ef537061776e206c696d697420" +
            "7265616368656400";

        public static List<PatchSite> AlienSpawnSites()
        {
            var cave = H(AlienCaveHex);
            return new List<PatchSite>
            {
                new PatchSite { Name = "alien_rdata_exec", Offset = RDATA_FLAGS,  Original = H("40"),                        Patched = H("60") },
                new PatchSite { Name = "alien_cave",       Offset = ALIEN_CAVE,   Original = H(Zeros(cave.Length)),          Patched = cave },
                new PatchSite { Name = "alien_hook_win",   Offset = ALIEN_HOOK_L, Original = H("c7054c2d520001000000"), Patched = H("e9827c0f009090909090") },
                new PatchSite { Name = "alien_hook_mj",    Offset = ALIEN_HOOK_M, Original = H("8b0d142a520068043c4d0051e82d4f0a00668b15b829520052e8a078060083c40c8bc86a016800401c46e84f960600"),
                                                                                   Patched  = H("e9d07c0f00909090909090909090909090909090909090909090909090909090909090909090909090909090909090") },
            };
        }

        /// <summary>True if the alternative cheats are installed (the win branch is redirected).</summary>
        public static bool HasAlienSpawn(byte[] d) => Has(d, ALIEN_HOOK_L, "e9827c0f009090909090");

        public sealed class Installed
        {
            public bool RimtechMusic;
            public bool Fog, FreeBuild, Turbo, Crews;
            public bool CrewLimitOff;                  // the crew-name limit lifted on its own (1.4.0+)
            public bool FreeBuildScopeAll, TurboScopeAll;   // each cheat carries its own scope
            public bool PartsUnlock, PartsScopeAll;
            public readonly List<uint> UnlockedAddrs = new List<uint>();
            public bool MoveSpeed, MoveSpeedScopeAll;  // experimental: unit movement speed
            public double MoveSpeedFactor;
            public bool AlienSpawn;                    // experimental: alternative cheats
        }

        static bool Has(byte[] d, long off, byte[] b)
        {
            if (off < 0 || off + b.Length > d.LongLength) return false;
            for (int i = 0; i < b.Length; i++) if (d[off + i] != b[i]) return false;
            return true;
        }

        static bool Has(byte[] d, long off, string hex) => Has(d, off, H(hex));

        static uint U32(byte[] d, long off) =>
            (uint)(d[off] | (d[off + 1] << 8) | (d[off + 2] << 16) | (d[off + 3] << 24));

        public static Installed DetectInstalled(byte[] d)
        {
            var r = new Installed();

            // The player-only variants are recognised by their hook, whose rel32 depends on where the
            // cave sits — so derive it with HookJmp rather than hard-coding the bytes. Hard-coded ones
            // silently stopped matching when the cave zone was repacked, which left the checkboxes
            // blank for an exe that was in fact patched.
            r.Fog = Has(d, FOG_HOOK, HookJmp(FOG_HOOK, FOG_CAVE, 5));      // fog redirect (both scopes)
            // free building: all = stub at the gate, player = hook there
            bool fbAll = Has(d, MJ_HOOK, "b801000000c20400");
            bool fbPlayer = Has(d, MJ_HOOK, HookJmp(MJ_HOOK, MJ_CAVE, 6));
            r.FreeBuild = fbAll || fbPlayer;
            // instant build: all = fld1 stub, player = hook
            bool tbAll = Has(d, BT_HOOK, "d9e8c20400");
            bool tbPlayer = Has(d, BT_HOOK, HookJmp(BT_HOOK, BT_CAVE, 6));
            r.Turbo = tbAll || tbPlayer;
            // Each scope is read from its own site, so a file with one cheat global and the other
            // local restores exactly as it was written.
            r.FreeBuildScopeAll = fbAll;
            r.TurboScopeAll = tbAll;

            r.Crews = Has(d, 0x7c220, "b863000000c20400");
            // The flip is two bytes anywhere; what identifies the option is its accounting hook.
            r.CrewLimitOff = Has(d, 0x7b81d, "eb57") && Has(d, FLIPACC_HOOK, HookJmp(FLIPACC_HOOK, FLIPACC_CAVE, 7));
            r.RimtechMusic = HasRimtechMusic(d);

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
                // The owner gate is what tells the two scopes apart. Look for the gate the cave
                // actually emits - movzx eax, word [ecx+0x42], the high word of the mover's
                // THGOBJECT. This used to search for mov eax,[ecx+0x28] and had gone stale: that
                // was the original gate, dropped when [ecx+0x28] turned out to be CMover::m_player
                // and permanently zero for these objects. Nothing emitted 8B 41 28 any more, so
                // every patched file read back as "all players" and reopening the patcher silently
                // widened the scope.
                r.MoveSpeedScopeAll = true;
                for (long i = SPEED_CAVE; i < SPEED_CAVE + 128 && i + 4 <= d.LongLength; i++)
                    if (d[i] == 0x0F && d[i + 1] == 0xB7 && d[i + 2] == 0x41 && d[i + 3] == 0x42)
                    { r.MoveSpeedScopeAll = false; break; }
            }
            r.AlienSpawn = HasAlienSpawn(d);

            return r;
        }

        /// <summary>
        /// The unit budget, as a multiple of vanilla. Three DWORDs and nothing else: the arena the
        /// game allocates once at startup, a sentinel it keeps 0xC below the end, and the threshold
        /// at which IsGameMemoryLow refuses further production. Vanilla is a 10 MB arena with an
        /// 8 MB threshold, so the ratio is 80% and every step keeps it.
        ///
        /// Generated rather than written out: nine steps by hand would be 27 hand-computed hex
        /// literals, and a typo in a sentinel is exactly the kind of thing that shows up as a
        /// crash hours into a match. Maximum is the one exception - it pushes the threshold to
        /// 94% because at that size the headroom matters more than the safety net.
        /// </summary>
        public static readonly double[] UnitFactors = { 1.5, 2, 2.5, 3, 3.5, 4, 6, 8, MaximumFactor };

        /// <summary>The rightmost step. Not on the 10 MB grid, so it carries its own numbers.</summary>
        public const double MaximumFactor = 12.8;

        static string Le32(uint v) =>
            string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", v & 0xff, (v >> 8) & 0xff, (v >> 16) & 0xff, v >> 24);

        /// <summary>Key for a factor. The four original keys are kept verbatim so an exe patched by
        /// an earlier release still identifies itself.</summary>
        public static string KeyFor(double f) =>
            f == MaximumFactor ? "unleashed" :
            f == 2 ? "balanced2x" :
            f == 4 ? "balanced4x" :
            f == 8 ? "balanced8x" :
            "units" + f.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_") + "x";

        static Profile UnitProfile(double f)
        {
            uint arena = (uint)(f * 10 * 1024 * 1024);
            uint thresh = f == MaximumFactor ? 120u * 1024 * 1024 : (uint)(arena * 0.8);
            return new Profile
            {
                Key = KeyFor(f),
                Factor = f,
                Sites = new List<PatchSite>
                {
                    Mem(0xd231, "0000A000", Le32(arena)),        // arena
                    Mem(0xd243, "F4FF9F00", Le32(arena - 0xC)),  // sentinel = arena - 0xC
                    Mem(0xd5b4, "00008000", Le32(thresh)),       // production threshold
                }
            };
        }

        public static readonly List<Profile> Profiles = BuildProfiles();

        static List<Profile> BuildProfiles()
        {
            var l = new List<Profile>();
            foreach (var f in UnitFactors) l.Add(UnitProfile(f));
            return l;
        }

        public static Profile ByKey(string key) => Profiles.Find(p => p.Key == key);
    }
}
