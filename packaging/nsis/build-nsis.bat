@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  Build EdgeSlide-Setup.exe with NSIS.
REM
REM  Requires:
REM    * .NET 10 SDK
REM    * NSIS (provides makensis.exe)  https://nsis.sourceforge.io
REM
REM  Output: EdgeSlide-Setup.exe in this folder.
REM ============================================================

cd /d "%~dp0"
set "PROJ=%~dp0..\..\EdgeSlide.csproj"
set "PUBDIR=%~dp0publish"

echo.
echo ===== Building EdgeSlide Setup.exe =====
echo.

where dotnet >nul 2>&1 || (echo .NET SDK not found. Install it and retry. & pause & exit /b 1)

REM --- 1. Publish self-contained so recipients need nothing installed ---
if exist "%PUBDIR%" rmdir /s /q "%PUBDIR%"
echo Publishing app (self-contained x64)...
dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=false -o "%PUBDIR%"
if not exist "%PUBDIR%\EdgeSlide.exe" (
    echo BUILD FAILED - EdgeSlide.exe not produced. See messages above.
    pause
    exit /b 1
)

REM --- 2. Find makensis.exe ---
set "MAKENSIS="
for /f "delims=" %%p in ('where makensis 2^>nul') do set "MAKENSIS=%%p"
if not defined MAKENSIS if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" set "MAKENSIS=%ProgramFiles(x86)%\NSIS\makensis.exe"
if not defined MAKENSIS if exist "%ProgramFiles%\NSIS\makensis.exe" set "MAKENSIS=%ProgramFiles%\NSIS\makensis.exe"
if not defined MAKENSIS (
    echo.
    echo Could not find makensis.exe. Install NSIS from https://nsis.sourceforge.io
    echo then run this script again.
    pause
    exit /b 1
)
echo Using makensis: !MAKENSIS!

REM --- 3. Compile the installer ---
"!MAKENSIS!" "%~dp0EdgeSlide.nsi"
if not exist "%~dp0EdgeSlide-Setup.exe" (
    echo Installer build failed. See messages above.
    pause
    exit /b 1
)

echo.
echo ===== Done: EdgeSlide-Setup.exe =====
echo Hand this single file to anyone - it installs per-user with no admin prompt.
echo (Unsigned installers show a Windows SmartScreen warning; see packaging\README.md.)
echo.
pause
endlocal
