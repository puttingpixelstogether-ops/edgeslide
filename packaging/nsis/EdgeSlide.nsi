; ============================================================
;  EdgeSlide NSIS installer script
;  Produces a per-user Setup.exe (no admin rights required).
;  Build with:  makensis EdgeSlide.nsi
;  Expects the published app in the ".\publish" subfolder
;  (build-nsis.bat creates it for you).
; ============================================================

Unicode true
!include "MUI2.nsh"

!define APPNAME      "EdgeSlide"
!define COMPANY      "EdgeSlide"
!define APPVERSION   "1.0.0"
!define APPEXE       "EdgeSlide.exe"

Name "${APPNAME}"
OutFile "EdgeSlide-Setup.exe"
RequestExecutionLevel user                       ; per-user, no UAC prompt
InstallDir "$LOCALAPPDATA\Programs\${APPNAME}"
InstallDirRegKey HKCU "Software\${APPNAME}" "InstallDir"
SetCompressor /SOLID lzma

!define MUI_ICON   "..\EdgeSlide.ico"
!define MUI_UNICON "..\EdgeSlide.ico"
!define MUI_ABORTWARNING

; Offer to launch on finish
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPEXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Launch ${APPNAME} now"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; ----- Uninstall registry display -----
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"

Section "Install"
  SetOutPath "$INSTDIR"

  ; Close a running instance so files aren't locked.
  ; (Best-effort; ignores errors if not running.)
  nsExec::Exec 'taskkill /IM "${APPEXE}" /F'

  ; All published files (self-contained build = exe + runtime + deps).
  File /r "publish\*.*"

  ; Start Menu shortcut
  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortCut  "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${APPEXE}" "" "$INSTDIR\${APPEXE}" 0

  ; Remember install dir + write uninstaller
  WriteRegStr HKCU "Software\${APPNAME}" "InstallDir" "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Add/Remove Programs entry (per-user)
  WriteRegStr   HKCU "${UNINSTKEY}" "DisplayName"     "${APPNAME}"
  WriteRegStr   HKCU "${UNINSTKEY}" "DisplayVersion"  "${APPVERSION}"
  WriteRegStr   HKCU "${UNINSTKEY}" "Publisher"       "${COMPANY}"
  WriteRegStr   HKCU "${UNINSTKEY}" "DisplayIcon"     "$INSTDIR\${APPEXE}"
  WriteRegStr   HKCU "${UNINSTKEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegDWORD HKCU "${UNINSTKEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTKEY}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  nsExec::Exec 'taskkill /IM "${APPEXE}" /F'

  Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
  RMDir  "$SMPROGRAMS\${APPNAME}"

  ; Remove startup entry if the app added one
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "EdgeSlide"

  RMDir /r "$INSTDIR"

  DeleteRegKey HKCU "${UNINSTKEY}"
  DeleteRegKey HKCU "Software\${APPNAME}"

  ; Note: user settings/logs in %APPDATA%\EdgeSlide are left in place on purpose.
SectionEnd
