@echo off
REM ============================================================
REM  EdgeSlide - portable build (playful CLI)
REM  Output: dist\EdgeSlide.exe  (self-contained, shareable)
REM  Best in Windows Terminal. The old plain script is build-portable.bat.bak.
REM ============================================================
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$root = '%~dp0'.TrimEnd('\'); $Mode = 'portable'; [Console]::OutputEncoding = [Text.Encoding]::UTF8; iex ([IO.File]::ReadAllText($root + '\build-fancy.ps1', [Text.Encoding]::UTF8))"
echo.
pause
endlocal
