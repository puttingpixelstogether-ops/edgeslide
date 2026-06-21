@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  Build an MSIX package for EdgeSlide (for the Microsoft Store
REM  or for sideloading).
REM
REM  Requires:
REM    * .NET 10 SDK
REM    * Windows SDK (provides makeappx.exe and signtool.exe)
REM
REM  Output: EdgeSlide.msix in this folder.
REM ============================================================

cd /d "%~dp0"
set "PROJ=%~dp0..\..\EdgeSlide.csproj"
set "STAGING=%~dp0staging"

echo.
echo ===== Building EdgeSlide MSIX =====
echo.

where dotnet >nul 2>&1 || (echo .NET SDK not found. Install it and retry. & pause & exit /b 1)

REM --- 1. Publish self-contained so the package needs no .NET on the target ---
if exist "%STAGING%" rmdir /s /q "%STAGING%"
echo Publishing app (self-contained x64)...
dotnet publish "%PROJ%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=false -o "%STAGING%"
if not exist "%STAGING%\EdgeSlide.exe" (
    echo BUILD FAILED - EdgeSlide.exe not produced. See messages above.
    pause
    exit /b 1
)

REM --- 2. Lay the package on top of the publish output ---
echo Copying manifest and assets...
copy /y "%~dp0Package.appxmanifest" "%STAGING%\AppxManifest.xml" >nul
if not exist "%STAGING%\Assets" mkdir "%STAGING%\Assets"
xcopy /y /e /i "%~dp0Assets\*" "%STAGING%\Assets\" >nul

REM --- 3. Find makeappx.exe from the Windows SDK ---
set "MAKEAPPX="
for /f "delims=" %%p in ('where makeappx 2^>nul') do set "MAKEAPPX=%%p"
if not defined MAKEAPPX (
    for /f "delims=" %%p in ('dir /b /s "%ProgramFiles(x86)%\Windows Kits\10\bin\*\x64\makeappx.exe" 2^>nul') do set "MAKEAPPX=%%p"
)
if not defined MAKEAPPX (
    echo.
    echo Could not find makeappx.exe. Install the Windows SDK, or run this from a
    echo "Developer Command Prompt for Visual Studio" which puts it on PATH.
    pause
    exit /b 1
)
echo Using makeappx: !MAKEAPPX!

REM --- 4. Pack ---
"!MAKEAPPX!" pack /o /d "%STAGING%" /p "%~dp0EdgeSlide.msix"
if not exist "%~dp0EdgeSlide.msix" (
    echo MSIX packaging failed. See messages above.
    pause
    exit /b 1
)

echo.
echo ===== Done: EdgeSlide.msix =====
echo.
echo NEXT STEPS:
echo   * Microsoft Store: upload EdgeSlide.msix in Partner Center. The Store signs it
echo     for you - first update the Identity (Name/Publisher) in Package.appxmanifest
echo     to the values Partner Center assigns, then rebuild.
echo   * Local testing (sideload): sign it with a self-signed cert whose subject matches
echo     the Publisher in the manifest (CN=EdgeSlideDev), trust that cert, then double-
echo     click the .msix. See packaging\README.md for the exact commands.
echo.
pause
endlocal
