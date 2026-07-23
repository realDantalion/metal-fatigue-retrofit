<#
  Builds the Metal Fatigue Retrofit release artifacts into dist\:
    - Standalone:  MetalFatigueRetrofitPatcher-<ver>.exe, and a versioned .zip bundling a plainly
                   named copy with README.txt + LICENSE.txt (the licence-complete single download)
    - Installer:   MetalFatigueRetrofitPatcher-Setup-<ver>.exe   (needs Inno Setup 6)
    - SHA256SUMS.txt covering everything above

  Both downloads are always produced together, so every release can offer both. The version
  is read from the .csproj so there is a single source of truth (bump it in one place).

    powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
    powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -RequireInstaller

  -RequireInstaller makes a missing Inno Setup a hard error instead of a warning. Use it for
  official releases so you can never accidentally ship standalone-only. Without it, the script
  still builds the standalone (handy on a machine without Inno) and just skips the installer.
#>
param(
  [switch]$RequireInstaller
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot

# --- Single source of truth for the version: the .csproj ---
$csproj = Join-Path $repo "patcher\MetalFatiguePatcher.csproj"
$version = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version
if (-not $version) { throw "Could not read <Version> from $csproj" }
$version = "$version".Trim()
Write-Host "== Metal Fatigue Retrofit v$version ==" -ForegroundColor Cyan

Write-Host "== Building patcher (Release) ==" -ForegroundColor Cyan
dotnet build (Join-Path $repo "MetalFatiguePatcher.sln") -c Release -v minimal
$exe = Join-Path $repo "patcher\bin\Release\net48\MetalFatigueRetrofitPatcher.exe"
if (-not (Test-Path $exe)) { throw "Build output not found: $exe" }

# --- Fresh dist\ so stale artifacts never leak into a release ---
$dist = Join-Path $repo "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dist | Out-Null

# --- Standalone: the loose exe, plus a zip that carries the licence with it ---
# The zip is the GPL-complete download (program + LICENSE + README in one file); the loose exe
# is just the frictionless path. The docs are staged in a temp folder instead of dist\, so they
# end up inside the zip WITHOUT also showing up as separate release assets nobody downloads.
# The loose exe carries the version in its filename (like the Setup.exe); the copy INSIDE the zip
# stays plainly named so it extracts to a clean MetalFatigueRetrofitPatcher.exe.
Copy-Item $exe (Join-Path $dist "MetalFatigueRetrofitPatcher-$version.exe") -Force
$stage = Join-Path $dist "_bundle"
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item $exe $stage -Force
Copy-Item (Join-Path $repo "README.md")  (Join-Path $stage "README.txt")  -Force
Copy-Item (Join-Path $repo "LICENSE")    (Join-Path $stage "LICENSE.txt") -Force
$zip = Join-Path $dist "MetalFatigueRetrofitPatcher-$version.zip"
# Wildcard, not the folder itself - otherwise the zip would nest everything under _bundle\.
Compress-Archive -DestinationPath $zip -Path (Join-Path $stage "*")
Remove-Item $stage -Recurse -Force
Write-Host "Standalone -> $zip" -ForegroundColor Green

# --- Installer (Inno Setup). Version is passed in via /D so the .iss never drifts. ---
$iscc = $null
foreach ($p in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "${env:ProgramFiles}\Inno Setup 6\ISCC.exe")) {
  if (Test-Path $p) { $iscc = $p; break }
}
if ($iscc) {
  Write-Host "== Building installer (Inno Setup) ==" -ForegroundColor Cyan
  & $iscc "/DMyAppVersion=$version" (Join-Path $repo "installer\MetalFatiguePatcher.iss")
  if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE" }
  $setup = Join-Path $repo "installer\Output\MetalFatigueRetrofitPatcher-Setup-$version.exe"
  if (-not (Test-Path $setup)) { throw "Installer expected but not found: $setup" }
  Copy-Item $setup $dist -Force
  Write-Host "Installer  -> $(Join-Path $dist (Split-Path $setup -Leaf))" -ForegroundColor Green
}
elseif ($RequireInstaller) {
  throw "Inno Setup 6 not found (ISCC.exe), but -RequireInstaller was set. Install it from jrsoftware.org."
}
else {
  Write-Host "Inno Setup 6 not found - skipping installer (standalone only)." -ForegroundColor Yellow
  Write-Host "  Install from jrsoftware.org, or pass -RequireInstaller to make this fatal." -ForegroundColor Yellow
}

# --- Checksums LAST, so they cover every artifact (installer included) ---
$sumFile = Join-Path $dist "SHA256SUMS.txt"
Get-ChildItem $dist -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
  "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name
} | Set-Content $sumFile -Encoding ASCII
Write-Host "Checksums  -> $sumFile" -ForegroundColor Green

Write-Host ""
Write-Host "Release artifacts in $dist :" -ForegroundColor Cyan
Get-ChildItem $dist -File | ForEach-Object { Write-Host ("  " + $_.Name) }
Write-Host "Done." -ForegroundColor Cyan
