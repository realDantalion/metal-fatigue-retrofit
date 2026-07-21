# Installer (Inno Setup)

`MetalFatiguePatcher.iss` builds a classic `Setup.exe` wizard that installs the patcher, adds a Start-menu (and optional desktop) shortcut, and — crucially — whose **uninstaller offers to restore the original `MFatigue.exe`**.

## How the uninstall-restore works
1. When the patcher successfully patches a game, it writes the game's exe path to
   `%ProgramData%\MetalFatiguePatcher\lastgame.txt`.
2. On uninstall, the Inno script reads that marker; if `<game>\MFatigue.exe.bak` exists,
   it asks the user "Restore the original MFatigue.exe?" and copies the backup back.

So uninstalling can cleanly undo both the app **and** the game patch. (The patcher's own
"Restore Original" button does the same at any time, independent of the installer.)

## Build
1. Build the patcher: `dotnet build -c Release` (or run `scripts\build-release.ps1`).
2. Install **Inno Setup 6** (free — https://jrsoftware.org/isdl.php).
3. Compile: open `MetalFatiguePatcher.iss` in the Inno IDE and press Build, or
   `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" MetalFatiguePatcher.iss`.
4. Output: `installer\Output\MetalFatigueRetrofitPatcher-Setup-0.1.0.exe`.

`scripts\build-release.ps1` does steps 1 + 3 automatically if Inno Setup is installed,
and always produces the standalone `dist\MetalFatigueRetrofitPatcher-0.1.0.zip`.
