# Metal Fatigue Retrofit

A distributable patcher that fixes two long-standing bugs in **Metal Fatigue** (Zono/Psygnosis, 2000; rights now at Nightdive/Atari):

1. **Global memory-based unit limit** — the game caps in-use game-object allocations at a hard-coded **8 MB** inside a fixed **10 MB** pool, blocking unit production long before any real memory pressure. Independent of how much RAM you have.
2. **Crew-name limit (~50 per faction)** — combots draw pilot/crew names from a fixed 50-name pool per faction; once exhausted, combot production is refused ("Couldn't find a name for this crew!").

## Download

Two equivalent downloads — both do the exact same thing:

- **Standalone (recommended)** — `MetalFatigueRetrofitPatcher.exe`. Just run it. Nothing is installed,
  nothing is left behind. Undo any time with the built-in **Restore original** button.
- **Installer (optional)** — `…-Setup.exe`. A classic wizard that adds a Start-Menu entry and an
  Add/Remove-Programs uninstaller (which can also restore the original). Handy if you prefer that,
  but it patches your game exactly the same way.

Both are unsigned, so Windows shows the same *"unknown publisher"* prompt either way — see
[Is this safe?](#is-this-safe) for how to verify. The patcher never installs itself into the game
or runs in the background; it edits one file and exits.

## How it works

The patcher operates on **your own legally-owned copy** of `MFatigue.exe`. It never distributes game code — it makes a backup, then patches your binary in place. This is how community patches have worked for decades.

- Works with **both GOG and Steam** — they ship the identical executable (see below).
- The game EXE exports 985 mangled C++ symbols, so functions were located by name — no guesswork.

> ⚠️ The EXE must stay named `MFatigue.exe` — the game refuses to launch under any other name. The patcher therefore patches in place (with a `.bak` backup), never emitting a renamed file.

## Versions

| Version | Unit budget | Combots |
|---------|-------------|---------|
| **50 combots · 2× units** | 16 MB soft cap (arena 20 MB) | native ~50 |
| **50 combots · 4× units** ★ | 32 MB soft cap (arena 40 MB) | native ~50 |
| **50 combots · 8× units** | 64 MB soft cap (arena 80 MB) | native ~50 |
| **Maximum** | 120 MB soft cap (arena 128 MB) | unlimited (cyclic name reuse) |

★ recommended for 6+ players. The ~50 combot cap is not a design choice — it comes from the
game's crew-name list, which holds exactly 50 names per faction. Only *Maximum* lifts it.

The memory soft cap is kept as a safety valve (blocks gracefully near real exhaustion instead
of crashing) — just moved far above the artificial 8 MB wall.

**Optional add-on:** *share vision with allies* — allied units also lift your fog of war.
Combinable with any of the versions above.

## Is this safe?

Windows shows an **"unknown publisher"** warning. That is expected: the patcher is not
code-signed, because a code-signing certificate costs several hundred euros a year — hard to
justify for a free community patch. So instead of asking for trust, here is how to verify it:

1. **Read the source.** The whole patcher is in this repository under the GPL. The patch bytes
   and every offset live in [`patcher/PatchData.cs`](patcher/PatchData.cs) — nothing is hidden
   or obfuscated, deliberately.
2. **Check the checksum.** Every release ships `SHA256SUMS.txt`. Verify with
   `Get-FileHash MetalFatigueRetrofitPatcher.exe -Algorithm SHA256` and compare.
3. **Scan it.** Upload the exe to [VirusTotal](https://www.virustotal.com/) — unsigned tools
   that write to other programs' files sometimes trigger heuristic flags.
4. **Build it yourself.** `dotnet build -c Release` reproduces the binary from this source.

**What the patcher actually does:**

- Reads the registry to find your Steam/GOG install — **read-only**
- Reads and writes exactly **one** file: your `MFatigue.exe`, after creating `MFatigue.exe.bak`
- Writes one small text file (`%ProgramData%\MetalFatiguePatcher\lastgame.txt`) so the
  uninstaller can offer to restore the original
- Asks for administrator rights only because games often live under `Program Files`
- **Makes no network connections**, has no telemetry, installs no service and no auto-start.
  (The licence link simply opens your browser.)

It also refuses to touch anything it does not recognise: it verifies the file against the known
build before patching, always patches from the verified backup, checks every byte it is about to
overwrite, and re-reads the result afterwards to confirm.

## Layout

- `patcher/` — the GUI patcher (detect · choose version · backup · patch · verify · restore)
- `installer/` — Inno Setup script for the optional `Setup.exe`
- `scripts/` — build and release helpers

## Steam & GOG

The **GOG and Steam `MFatigue.exe` are byte-identical** (SHA256 `26d428f1…`, the Nightdive 2021 re-release, no SteamStub). The same patch offsets apply to both, and the patcher auto-detects either store.

> Steam note: **"Verify integrity of game files"** will detect the patched EXE and re-download the original. That's expected — just re-run the patcher afterwards (your `.bak` also lets you restore). Steam does not re-verify on normal launch, so the patch persists.

## License

Copyright (C) 2026 **Dantalion** (github.com/realDantalion)

This program is free software: you can redistribute it and/or modify it under the terms of the **GNU General Public License v3** (or, at your option, any later version) — see [`LICENSE`](LICENSE).

It is distributed in the hope that it will be useful, but **WITHOUT ANY WARRANTY**; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

> **If you build on this work:** the GPL requires you to keep the copyright notices intact, state that you changed it, publish your source under the same licence, and — because the patcher displays *Appropriate Legal Notices* in its UI (GPL §5d) — keep those notices visible in your version too.

## Credits

- Patcher, reverse engineering and documentation: **Dantalion**
- *Metal Fatigue* was developed by **Zono** (2000) and re-released on Steam and GOG by **Nightdive Studios**. This project is not affiliated with them.

## Legal

Game rights holder: Nightdive Studios / Atari. This project distributes **only the patcher**, never game code or assets — it modifies the copy you already own. Multiplayer note: all players must run the **same** build — differently-patched EXEs desync.

## Status

Reverse engineering complete and all patches validated in-game: no crash at ~60–70 combots plus
heavy vehicle and AI battles (framerate is the natural limit now), and shared vision confirmed
working with an allied player. The GUI patcher is feature-complete in 10 languages.

Not yet verified: behaviour in a real **multiplayer** match. Shared vision should be safe there
— it only affects local rendering — but it has not been tested, and as always every player must
run the identical build.
