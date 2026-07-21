# Patcher — C# WinForms GUI (.NET Framework 4.8)

The distributable tool. A small, dependency-free `MetalFatigueRetrofitPatcher.exe` (net48 is preinstalled on Win10/11) that patches the user's own `MFatigue.exe` in place.

## Features
- **Multi-language** (10: English, German, Spanish, Portuguese, Italian, French, Japanese,
  Korean, Chinese, Russian): auto-detected from the Windows UI culture, English as fallback,
  with a clickable flag grid in the banner to switch at runtime. Strings live in `Lang.cs` as
  code tables (not .resx satellites) so the app stays a single .exe — adding a language means
  adding one column per entry (plus a `flag_xx.png`).
- **Source selector**: Auto-detect · Steam · GOG (registry-based) + manual browse.
- **Profile selector**: Unleashed | Balanced (see root README).
- **Hidden cheat profiles** — click the banner **5×** to unlock two extra variants
  (banner swaps to CHEAT MODE styling), giving four options in total:
  1. **Balanced** — 50 combots, 4× units
  2. **Maximum** — no practical limits
  3. **Maximum + Cheats (nur ich)** — free building, turbo build, no fog — **local player only**,
     the AI keeps playing by normal rules
  4. **+ Cheats für ALLE** — same cheats for every player incl. AI (AI becomes brutal; chaos testing)

  Switching profiles just re-patches from the pristine backup, so flipping between any of them is one click.
- **Backup** (`MFatigue.exe.bak`) created automatically before the first patch.
- **Switchable profiles** — always re-patches from the pristine backup.
- **Verify** after writing; **Restore original** button.
- **Pristine check** — refuses to back up an already-modified / wrong build.
- Requests admin (game folders under Program Files need elevated write).

## Files
| file | role |
|------|------|
| `Program.cs`   | entry point |
| `MainForm.cs`  | UI + flow (code-built, no designer) |
| `PatchData.cs` | patch definitions (offsets, original/patched bytes) and the profiles |
| `Patcher.cs`   | backup · patch-from-pristine · verify · restore |
| `GameFinder.cs`| Steam/GOG/auto detection |
| `app.manifest` | admin elevation + DPI |

## Build
```
dotnet build -c Release
# or open MetalFatiguePatcher.csproj in Rider / Visual Studio
```
Output: `bin/Release/net48/MetalFatigueRetrofitPatcher.exe` (self-contained on any Win10/11).

The patch bytes in `PatchData.cs` are verified byte-identical to the in-game-tested shipping builds.

## TODO
- GOG and Steam ship the identical build, so fixed offsets cover both. Supporting a *different* build (e.g. the original 2000 CD release) would need signature scanning.
- Optional: icon, single-file publish, code signing.
