@echo off
REM ============================================================
REM  EdgeSlide - portable build for ARM64 (playful CLI)
REM  Output: dist-arm64\EdgeSlide-Portable-arm64.exe  (self-contained)
REM
REM  Cross-builds from an x64 machine - no ARM64 hardware needed to
REM  produce it (you just can't test-run it without an ARM64 device).
REM  Upload the result next to the x64 exe on the GitHub release.
REM ============================================================
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$root = '%~dp0'.TrimEnd('\'); $Mode = 'portable'; $Arch = 'arm64'; [Console]::OutputEncoding = [Text.Encoding]::UTF8; iex ([IO.File]::ReadAllText($root + '\build-fancy.ps1', [Text.Encoding]::UTF8))"
echo.
pause
endlocal
