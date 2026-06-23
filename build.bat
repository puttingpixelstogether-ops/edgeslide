@echo off
REM ============================================================
REM  EdgeSlide - build + install (playful CLI)
REM  Builds framework-dependent and installs to %LOCALAPPDATA%\EdgeSlide
REM  with a Start Menu shortcut. Best in Windows Terminal.
REM  The old plain script is build.bat.bak.
REM ============================================================
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$root = '%~dp0'.TrimEnd('\'); $Mode = 'install'; [Console]::OutputEncoding = [Text.Encoding]::UTF8; iex ([IO.File]::ReadAllText($root + '\build-fancy.ps1', [Text.Encoding]::UTF8))"
echo.
pause
endlocal
