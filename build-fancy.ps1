# ============================================================
#  EdgeSlide — playful build engine (demoscene edition)
#  Invoked by build.bat (Mode=install) and build-portable.bat
#  (Mode=portable), which read this as UTF-8 and set $root/$Mode.
#  Best viewed in Windows Terminal.
# ============================================================

if (-not $root) { $root = (Get-Location).Path }
if (-not $Mode) { $Mode = 'portable' }
$ErrorActionPreference = 'Stop'
$esc = [char]27
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}

# ---- colours (truecolor ANSI) ----
function C([int]$r,[int]$g,[int]$b){ "$esc[38;2;$r;$g;${b}m" }
$reset  = "$esc[0m"
$amber  = C 255 179 71
$purple = C 172 101 243
$dim    = C 107 106 102
$text   = C 240 238 232
$green  = C 94 227 148
$redc   = C 232 95 95

# amber -> purple gradient, t in 0..1
function Grad([double]$t){
  $r = [int](255 + (172 - 255) * $t)
  $g = [int](179 + (101 - 179) * $t)
  $b = [int]( 71 + (243 -  71) * $t)
  C $r $g $b
}

# ---- wordmark (ANSI Shadow) ----
$banner = @(
'███████╗██████╗  ██████╗ ███████╗███████╗██╗     ██╗██████╗ ███████╗',
'██╔════╝██╔══██╗██╔════╝ ██╔════╝██╔════╝██║     ██║██╔══██╗██╔════╝',
'█████╗  ██║  ██║██║  ███╗█████╗  ███████╗██║     ██║██║  ██║█████╗  ',
'██╔══╝  ██║  ██║██║   ██║██╔══╝  ╚════██║██║     ██║██║  ██║██╔══╝  ',
'███████╗██████╔╝╚██████╔╝███████╗███████║███████╗██║██████╔╝███████╗',
'╚══════╝╚═════╝  ╚═════╝ ╚══════╝╚══════╝╚══════╝╚═╝╚═════╝ ╚══════╝'
)

$modeLabel = if ($Mode -eq 'install') { 'install build' } else { 'portable build' }

Clear-Host
Write-Host ""
for ($i = 0; $i -lt $banner.Count; $i++) {
  $t = $i / [math]::Max(1, $banner.Count - 1)
  Write-Host ("  " + (Grad $t) + $banner[$i] + $reset)
  Start-Sleep -Milliseconds 40
}
Write-Host ("        " + $dim + "touchpad edge sliders" + $reset + "   " + $amber + "·" + $reset + "   " + $dim + $modeLabel + $reset)
Write-Host ""

# ---- slider sweep intro ----
$wide = 36
for ($p = 0; $p -le $wide; $p++) {
  $bar = ""
  for ($k = 0; $k -lt $wide; $k++) {
    if     ($k -lt $p) { $bar += (Grad ($k / $wide)) + [char]0x25B0 }
    elseif ($k -eq $p) { $bar += $text + [char]0x25B0 }
    else               { $bar += $dim  + [char]0x25B1 }
  }
  $pct = [int](($p / $wide) * 100)
  Write-Host -NoNewline ("`r  " + $text + [char]0x2595 + $bar + $text + [char]0x258F + " " + $amber + ("{0,3}" -f $pct) + "%" + $reset)
  Start-Sleep -Milliseconds 14
}
Write-Host "`n"

# ---- helpers ----
function Step($m){ Write-Host ("  " + $amber + [char]0x25C6 + " " + $reset + $text + $m + $reset) }
function Ok($m)  { Write-Host ("  " + $green + [char]0x2713 + " " + $reset + $dim  + $m + $reset) }
function Bad($m) { Write-Host ("  " + $redc  + [char]0x2717 + " " + $m + $reset) }

# ---- shared: SDK + close running ----
Step "Checking for the .NET SDK"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  Bad ".NET SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet and retry."
  exit 1
}
Ok "dotnet is on PATH"

Step "Closing any running EdgeSlide"
Get-Process EdgeSlide -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600
Ok "pad's clear"

$proj = Join-Path $root 'EdgeSlide.csproj'

if ($Mode -eq 'portable') {
  # ---------------- PORTABLE: self-contained single-file ----------------
  Step "Cleaning previous build"
  $dist = Join-Path $root 'dist'
  if (Test-Path $dist) { Remove-Item -Recurse -Force $dist }
  Ok "dist wiped"

  Step "Bundling self-contained single-file exe (grab a coffee)"
  Write-Host $dim
  & dotnet publish $proj -c Release -r win-x64 --self-contained true `
      -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
      -o $dist
  $code = $LASTEXITCODE
  Write-Host -NoNewline $reset

  $exe = Join-Path $dist 'EdgeSlide.exe'
  if ($code -ne 0 -or -not (Test-Path $exe)) { Write-Host ""; Bad "Build failed — scroll up for the real error."; exit 1 }
  Get-ChildItem $dist -Filter *.pdb -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

  $size = "{0:N1} MB" -f ((Get-Item $exe).Length / 1MB)
  $done = "$exe   $dim($size)$reset"
  $hint = "Double-click to run, or share it. Lives in the tray, no install."
}
else {
  # ---------------- INSTALL: framework-dependent, into LOCALAPPDATA -------
  Step "Cleaning previous build"
  $pub = Join-Path $root 'publish'
  if (Test-Path $pub) { Remove-Item -Recurse -Force $pub }
  Ok "publish wiped"

  Step "Building (framework-dependent; uses your installed .NET runtime)"
  Write-Host $dim
  & dotnet publish $proj -c Release -o $pub
  $code = $LASTEXITCODE
  Write-Host -NoNewline $reset
  if ($code -ne 0 -or -not (Test-Path (Join-Path $pub 'EdgeSlide.exe'))) { Write-Host ""; Bad "Build failed — scroll up for the real error."; exit 1 }

  Step "Installing to %LOCALAPPDATA%\EdgeSlide"
  $installDir = Join-Path $env:LOCALAPPDATA 'EdgeSlide'
  if (-not (Test-Path $installDir)) { New-Item -ItemType Directory -Path $installDir | Out-Null }
  Copy-Item (Join-Path $pub '*') $installDir -Recurse -Force
  $exe = Join-Path $installDir 'EdgeSlide.exe'
  Ok "copied"

  Step "Adding Start Menu shortcut"
  $startMenu = [Environment]::GetFolderPath('Programs')
  $wsh = New-Object -ComObject WScript.Shell
  $lnk = $wsh.CreateShortcut((Join-Path $startMenu 'EdgeSlide.lnk'))
  $lnk.TargetPath = $exe
  $lnk.WorkingDirectory = $installDir
  $lnk.Description = 'Touchpad edge sliders for brightness and volume'
  $lnk.Save()
  Ok "shortcut added"

  $done = $exe
  $hint = "Installed. Find it in the Start Menu or the tray."
}

# ---- success ----
$ver = ""
$vm = Select-String -Path $proj -Pattern '<Version>(.*?)</Version>' -ErrorAction SilentlyContinue
if ($vm) { $ver = "v" + $vm.Matches[0].Groups[1].Value }

Write-Host ""
$full = ""
for ($k = 0; $k -lt $wide; $k++) { $full += (Grad ($k / $wide)) + [char]0x25B0 }
Write-Host ("  " + $text + [char]0x2595 + $full + $text + [char]0x258F + " " + $green + "100%" + $reset)
Write-Host ""
Write-Host ("  " + $green + [char]0x2713 + " BUILD COMPLETE" + $reset + "   " + $dim + $ver + $reset)
Write-Host ("  " + $dim + [char]0x2192 + " " + $reset + $text + $done + $reset)
Write-Host ("  " + $dim + $hint + $reset)
Write-Host ""

if ($Mode -eq 'install') {
  $ans = Read-Host ("  " + $amber + "Launch EdgeSlide now? [Y/n]" + $reset)
  if ($ans -notmatch '^[Nn]') { Start-Process $exe }
}

exit 0
