; Metal Fatigue Retrofit — Inno Setup script
; Build with Inno Setup 6 (free): compile this file with the IDE or ISCC.exe.
; Produces Setup that installs the patcher, and whose UNINSTALLER offers to
; restore the original MFatigue.exe (from the .bak the patcher created).

#define MyAppName "Metal Fatigue Retrofit"
; Version comes from build-release.ps1 via  ISCC /DMyAppVersion=<ver>  (single source of
; truth = the .csproj). This fallback only applies when compiling the .iss directly in the IDE.
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Metal Fatigue Community"
#define MyAppExeName "MetalFatigueRetrofitPatcher.exe"

[Setup]
AppId={{961E0AA8-5237-4555-A0B8-93E7658E2BC7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\MetalFatigueRetrofit
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename=MetalFatigueRetrofitPatcher-Setup-{#MyAppVersion}
OutputDir=Output
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; GPL-3.0 must be conveyed with the program - show it during setup and install a copy.
LicenseFile=..\LICENSE
PrivilegesRequired=admin
; No ArchitecturesInstallIn64BitMode: the patcher is a 32-bit binary (it drives a 32-bit game and,
; since 1.3.0, decodes OGG through the game's own 32-bit libvorbisfile.dll), so it belongs in
; Program Files (x86). Up to 1.2.0 setup ran in 64-bit mode and installed to Program Files — an
; upgrade would therefore leave a second copy behind, which the [Code] section removes explicitly.
; Pick the language from the user's Windows settings; only ask if there's no match.
ShowLanguageDialog=auto

[Languages]
; Installer-chrome languages are limited to the .isl files Inno Setup bundles.
; The patcher app itself is localized into 10 languages regardless of this list;
; Korean/Chinese/Russian .isl are unofficial (not shipped with Inno), so adding them
; here would break the ISCC compile — those users get the English installer chrome
; and the fully-localized app.
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"

[CustomMessages]
en.RestorePrompt=Restore the original MFatigue.exe (undo the patch)?
en.RestoreOk=Original restored.
en.RestoreFail=Could not restore the original (is the game running?). Please close the game and use the patcher's restore button.
en.RestoreBadBackup=The backup file looks damaged, so the original was NOT restored. Your backup has been left untouched next to the game.
de.RestoreBadBackup=Die Backup-Datei sieht beschädigt aus, das Original wurde daher NICHT wiederhergestellt. Dein Backup bleibt unangetastet neben dem Spiel liegen.
es.RestoreBadBackup=El archivo de copia de seguridad parece dañado, por lo que NO se restauró el original. Tu copia se ha dejado intacta junto al juego.
fr.RestoreBadBackup=Le fichier de sauvegarde semble endommagé, l'original n'a donc PAS été restauré. Votre sauvegarde reste intacte à côté du jeu.
ja.RestoreBadBackup=バックアップファイルが破損しているため、オリジナルは復元されませんでした。バックアップはゲームの隣にそのまま残されています。
de.RestorePrompt=Die ursprüngliche MFatigue.exe wiederherstellen (Patch rückgängig machen)?
de.RestoreOk=Original wiederhergestellt.
de.RestoreFail=Konnte das Original nicht wiederherstellen (läuft das Spiel noch?). Bitte das Spiel schließen und den Patcher zum Wiederherstellen nutzen.
es.RestorePrompt=¿Restaurar la MFatigue.exe original (deshacer el parche)?
es.RestoreOk=Original restaurado.
es.RestoreFail=No se pudo restaurar el original (¿el juego está abierto?). Cierra el juego y usa el botón de restaurar del parcheador.
fr.RestorePrompt=Restaurer la MFatigue.exe d'origine (annuler le patch) ?
fr.RestoreOk=Original restauré.
fr.RestoreFail=Impossible de restaurer l'original (le jeu est-il ouvert ?). Fermez le jeu et utilisez le bouton de restauration du patcheur.
ja.RestorePrompt=元の MFatigue.exe を復元しますか（パッチを取り消す）？
ja.RestoreOk=オリジナルに戻しました。
ja.RestoreFail=オリジナルを復元できませんでした（ゲームが起動中ですか？）。ゲームを終了し、パッチャーの復元ボタンをご利用ください。
it.RestorePrompt=Ripristinare la MFatigue.exe originale (annullare la patch)?
it.RestoreOk=Originale ripristinato.
it.RestoreFail=Impossibile ripristinare l'originale (il gioco è in esecuzione?). Chiudi il gioco e usa il pulsante di ripristino del patcher.
it.RestoreBadBackup=Il file di backup sembra danneggiato, quindi l'originale NON è stato ripristinato. Il tuo backup è rimasto intatto accanto al gioco.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
; Build the patcher first: dotnet build -c Release
Source: "..\patcher\bin\Release\net48\MetalFatigueRetrofitPatcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; DestName: "README.txt"; Flags: ignoreversion
Source: "..\LICENSE";   DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[UninstallDelete]
; Error logs the patcher wrote next to its own exe. They are created after install, so Setup
; does not know about them and would otherwise leave {app} behind with a stray .txt in it.
Type: files; Name: "{app}\MetalFatigueRetrofitPatcher-ErrorLog-*.txt"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; runascurrentuser is REQUIRED here, not cosmetic: postinstall entries default to
; runasoriginaluser (non-elevated), and nowait launches via CreateProcess, which cannot
; raise a UAC prompt. The patcher's manifest demands administrator, so that combination
; fails with ERROR_ELEVATION_REQUIRED (740) - the checkbox errored out and started nothing.
; Setup is already elevated (PrivilegesRequired=admin), so inheriting its token starts the
; patcher with no extra prompt. shellexec would also work, but costs a second UAC dialog.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait runascurrentuser postinstall skipifsilent

[Code]
// Byte size of the supported MFatigue.exe build — used to sanity-check the backup.
const
  ExpectedSize = 1191989;
  UninstallKey = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{961E0AA8-5237-4555-A0B8-93E7658E2BC7}_is1';

function MarkerFile(): string;
begin
  Result := ExpandConstant('{commonappdata}') + '\MetalFatiguePatcher\lastgame.txt';
end;

// Locate an earlier install's uninstaller. Up to 1.2.0 setup ran in 64-bit mode, so its entry sits
// in the 64-bit registry view; this setup runs 32-bit and would never see it by default. Check both.
function PreviousUninstaller(): string;
var
  s: string;
begin
  Result := '';
  // Nested rather than "IsWin64 and RegQuery...": HKLM64 must not be touched at all on 32-bit
  // Windows, and relying on short-circuit evaluation here is not worth the risk.
  if IsWin64 then
  begin
    if RegQueryStringValue(HKLM64, UninstallKey, 'UninstallString', s) then
    begin
      Result := RemoveQuotes(s);
      Exit;
    end;
  end;
  if RegQueryStringValue(HKLM32, UninstallKey, 'UninstallString', s) then
    Result := RemoveQuotes(s);
end;

// Remove an older install before laying down this one, so the move from Program Files to
// Program Files (x86) does not leave a second copy and a stale Start-menu entry behind.
procedure CurStepChanged(CurStep: TSetupStep);
var
  un, marker, parked: string;
  code: Integer;
begin
  if CurStep <> ssInstall then Exit;

  un := PreviousUninstaller();
  if un = '' then Exit;

  // The old uninstaller OFFERS TO RESTORE the game's original exe. Run silently it takes the default
  // answer and would quietly undo the user's patch — during what is merely an upgrade. That offer is
  // keyed on the marker file, so park the marker for the duration and put it back afterwards. This
  // needs no cooperation from the old uninstaller, which is already shipped and cannot be changed.
  marker := MarkerFile();
  parked := marker + '.upgrade';
  if FileExists(marker) then
    RenameFile(marker, parked);

  Exec(un, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-', '', SW_HIDE, ewWaitUntilTerminated, code);

  if FileExists(parked) then
    RenameFile(parked, marker);
end;

// On uninstall, offer to restore the patched game to its original state.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  marker, gameExe, bak: string;
  lines: TArrayOfString;
  bakSize: Integer;   // FileSize() takes "var Size: Integer" — Int64 needs FileSize64 (Inno 6.1+)
begin
  if CurUninstallStep = usUninstall then
  begin
    marker := MarkerFile();
    if FileExists(marker) and LoadStringsFromFile(marker, lines) and (GetArrayLength(lines) > 0) then
    begin
      gameExe := Trim(lines[0]);
      bak := gameExe + '.bak';
      if FileExists(gameExe) and FileExists(bak) then
      begin
        // Sanity-check the backup before overwriting the game with it. A truncated or
        // corrupt .bak must never replace the user's executable.
        if not (FileSize(bak, bakSize) and (bakSize = ExpectedSize)) then
        begin
          MsgBox(ExpandConstant('{cm:RestoreBadBackup}'), mbError, MB_OK);
          Exit;
        end;

        if MsgBox(ExpandConstant('{cm:RestorePrompt}') + #13#10 + #13#10 + gameExe,
                  mbConfirmation, MB_YESNO) = IDYES then
        begin
          if FileCopy(bak, gameExe, False) then
          begin
            // Deliberately keep the .bak — it is the user's only clean original.
            DeleteFile(marker);
            MsgBox(ExpandConstant('{cm:RestoreOk}'), mbInformation, MB_OK);
          end
          else
            MsgBox(ExpandConstant('{cm:RestoreFail}'), mbError, MB_OK);
        end;
      end;
    end;
  end;
end;
