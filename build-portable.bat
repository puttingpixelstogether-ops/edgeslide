@echo off
setlocal
REM ============================================================
REM  Build a single, standalone EdgeSlide.exe you can share.
REM
REM  The result bundles the .NET runtime, so whoever you send it to
REM  just double-clicks it - no install, no .NET required.
REM
REM  Output: dist\EdgeSlide.exe
REM ============================================================

cd /d "%~dp0"
echo.
echo ===== Building portable EdgeSlide.exe =====
echo.

where dotnet >nul 2>&1 || (echo .NET SDK not found. Install it and retry. & pause & exit /b 1)

if exist "%~dp0dist" rmdir /s /q "%~dp0dist"

dotnet publish "%~dp0EdgeSlide.csproj" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o "%~dp0dist"

if not exist "%~dp0dist\EdgeSlide.exe" (
    echo.
    echo BUILD FAILED - EdgeSlide.exe was not produced. See messages above.
    pause
    exit /b 1
)

REM Keep only the exe in dist (publish may drop a couple of debug files).
del /q "%~dp0dist\*.pdb" 2>nul

echo.
echo ===== Done =====
echo Your shareable file is:
echo     %~dp0dist\EdgeSlide.exe
echo.
echo Send that single file to anyone. They double-click it and it runs in the tray.
echo (First launch may show a SmartScreen "unknown publisher" prompt: More info -^> Run anyway.)
echo.
pause
endlocal
