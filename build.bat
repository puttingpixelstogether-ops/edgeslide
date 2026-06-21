@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  EdgeSlide one-click build + install
REM  Double-click this file. It will:
REM    1. Make sure a .NET SDK is available (installs it if not)
REM    2. Close any running EdgeSlide so its files aren't locked
REM    3. Build EdgeSlide (framework-dependent; uses your installed .NET runtime)
REM    4. Copy it to %LOCALAPPDATA%\EdgeSlide
REM    5. Create a Start Menu shortcut
REM    6. Offer to launch it
REM ============================================================

cd /d "%~dp0"
echo.
echo ===== EdgeSlide installer =====
echo.

REM --- 1. Check for the .NET SDK -------------------------------------------
where dotnet >nul 2>&1
if %errorlevel%==0 (
    for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set DOTNETVER=%%v
)

if not defined DOTNETVER (
    echo No .NET SDK was found. It is needed once to build the app.
    echo.
    where winget >nul 2>&1
    if !errorlevel!==0 (
        echo Attempting to install the latest .NET SDK automatically with winget...
        winget install --id Microsoft.DotNet.SDK.10 -e --accept-source-agreements --accept-package-agreements
        echo.
        echo If the install succeeded, please CLOSE this window and run build.bat again
        echo so the new dotnet command is picked up.
        pause
        exit /b 0
    ) else (
        echo winget is not available on this PC.
        echo Please install the .NET SDK manually from:
        echo     https://dotnet.microsoft.com/download/dotnet
        echo then run build.bat again.
        pause
        exit /b 1
    )
)

echo Using .NET SDK version: %DOTNETVER%
echo.

REM --- 2. Close any running EdgeSlide so its files aren't locked ------------
echo Closing any running EdgeSlide instance...
taskkill /IM EdgeSlide.exe /F >nul 2>&1
REM give Windows a moment to release the file handles
ping -n 2 127.0.0.1 >nul

REM --- 3. Build (framework-dependent: uses your installed .NET runtime) -----
REM  This avoids downloading large self-contained runtime packs from NuGet.
if exist "%~dp0publish" rmdir /s /q "%~dp0publish"
echo Building EdgeSlide (this can take a minute the first time)...
echo.
dotnet publish "%~dp0EdgeSlide.csproj" -c Release -o "%~dp0publish"

if not exist "%~dp0publish\EdgeSlide.exe" (
    echo.
    echo BUILD FAILED - EdgeSlide.exe was not produced. See messages above.
    pause
    exit /b 1
)

REM --- 3. Install to %LOCALAPPDATA%\EdgeSlide ----------------------------
set "INSTALLDIR=%LOCALAPPDATA%\EdgeSlide"
echo.
echo Installing to "%INSTALLDIR%"...
if not exist "%INSTALLDIR%" mkdir "%INSTALLDIR%"
REM Framework-dependent build = exe + its .dll and dependencies, so copy them all.
xcopy /y /e /i "%~dp0publish\*" "%INSTALLDIR%\" >nul

REM --- 4. Start Menu shortcut ---------------------------------------------
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%STARTMENU%\EdgeSlide.lnk');" ^
  "$s.TargetPath='%INSTALLDIR%\EdgeSlide.exe';" ^
  "$s.WorkingDirectory='%INSTALLDIR%';" ^
  "$s.Description='Touchpad edge sliders for brightness and volume';" ^
  "$s.Save()"

echo.
echo ===== Done! =====
echo EdgeSlide is installed at: %INSTALLDIR%\EdgeSlide.exe
echo A shortcut was added to your Start Menu.
echo (Open it, then use the tray icon's Settings to enable "Launch at startup".)
echo.

REM --- 5. Offer to launch -------------------------------------------------
choice /c YN /m "Launch EdgeSlide now"
if errorlevel 2 goto :end
start "" "%INSTALLDIR%\EdgeSlide.exe"

:end
echo.
echo You can close this window.
pause
endlocal
