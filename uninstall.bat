@echo off
setlocal
REM ============================================================
REM  Remove EdgeSlide from this computer (for clean testing).
REM  Deletes: the running app, installed files, Start Menu shortcut,
REM  startup entry, and user settings/logs. Per-user; no admin needed.
REM ============================================================

echo.
echo ===== Uninstalling EdgeSlide =====
echo.

REM 1. Stop it if it's running (so files aren't locked).
taskkill /IM EdgeSlide.exe /F >nul 2>&1

REM 2. Installed app folder (created by build.bat).
if exist "%LOCALAPPDATA%\EdgeSlide" (
    rmdir /s /q "%LOCALAPPDATA%\EdgeSlide"
    echo Removed %LOCALAPPDATA%\EdgeSlide
)

REM 3. Per-user "Programs" install folder (created by the NSIS installer, if used).
if exist "%LOCALAPPDATA%\Programs\EdgeSlide" (
    rmdir /s /q "%LOCALAPPDATA%\Programs\EdgeSlide"
    echo Removed %LOCALAPPDATA%\Programs\EdgeSlide
)

REM 4. Start Menu shortcut(s).
del /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\EdgeSlide.lnk" >nul 2>&1
if exist "%APPDATA%\Microsoft\Windows\Start Menu\Programs\EdgeSlide" (
    rmdir /s /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\EdgeSlide"
)
echo Removed Start Menu shortcut(s)

REM 5. Launch-at-startup registry entry (if it was enabled).
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v EdgeSlide /f >nul 2>&1

REM 6. Add/Remove Programs entry (only exists if installed via NSIS).
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdgeSlide" /f >nul 2>&1
reg delete "HKCU\Software\EdgeSlide" /f >nul 2>&1

REM 7. Settings, log and diagnostics file.
if exist "%APPDATA%\EdgeSlide" (
    rmdir /s /q "%APPDATA%\EdgeSlide"
    echo Removed %APPDATA%\EdgeSlide (settings + log)
)

REM 8. Legacy "EdgeSlider"-named leftovers from earlier test builds (pre-rename).
taskkill /IM EdgeSlider.exe /F >nul 2>&1
if exist "%LOCALAPPDATA%\EdgeSlider"          rmdir /s /q "%LOCALAPPDATA%\EdgeSlider"
if exist "%LOCALAPPDATA%\Programs\EdgeSlider" rmdir /s /q "%LOCALAPPDATA%\Programs\EdgeSlider"
if exist "%APPDATA%\EdgeSlider"               rmdir /s /q "%APPDATA%\EdgeSlider"
del /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\EdgeSlider.lnk" >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v EdgeSlider /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdgeSlider" /f >nul 2>&1
reg delete "HKCU\Software\EdgeSlider" /f >nul 2>&1
echo Removed any legacy EdgeSlider-named leftovers

echo.
echo ===== EdgeSlide fully removed =====
echo (Your project/source folder is untouched.)
echo.
pause
endlocal
