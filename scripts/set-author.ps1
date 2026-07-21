<#
  Replaces the author placeholder across the whole repository.

  The copyright line currently reads:
      Copyright (C) 2026 Dantalion (github.com/TODO-GITHUB)

  Once your GitHub account exists, run this once to fill in the real name:
      powershell -ExecutionPolicy Bypass -File scripts\set-author.ps1 -GitHubName yourname

  It touches source headers, the .csproj metadata, the localized legal notice
  (all 10 languages) and the README. Run it BEFORE the first public commit -
  the GPL notice should be correct in the very first published version.
#>
param(
  [Parameter(Mandatory = $true)][string]$GitHubName,
  [string]$Placeholder = "TODO-GITHUB"
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot

$patterns = @("*.cs", "*.py", "*.csproj", "*.md", "*.json", "*.iss")
$files = Get-ChildItem $repo -Recurse -File -Include $patterns |
         Where-Object { $_.FullName -notmatch '\\(bin|obj|dist|\.git)\\' }

$changed = 0
foreach ($f in $files) {
  $text = Get-Content $f.FullName -Raw -Encoding UTF8
  if ($text -like "*$Placeholder*") {
    ($text -replace [regex]::Escape($Placeholder), $GitHubName) |
      Set-Content $f.FullName -NoNewline -Encoding UTF8
    Write-Host ("  ~ " + $f.FullName.Substring($repo.Length + 1)) -ForegroundColor DarkGray
    $changed++
  }
}

Write-Host ""
if ($changed -eq 0) {
  Write-Host "No '$Placeholder' left - nothing to do." -ForegroundColor Yellow
} else {
  Write-Host "Updated $changed file(s) -> github.com/$GitHubName" -ForegroundColor Green
  Write-Host "Now rebuild:  scripts\build-release.ps1" -ForegroundColor Cyan
}
