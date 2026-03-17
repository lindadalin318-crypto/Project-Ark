@echo off
setlocal

set "MODE=%~1"
if "%MODE%"=="" set "MODE=both"

set "GDRE=f:\UnityProjects\Project-Ark\SideProject\GDRE_tools\gdre_tools.exe"
set "GAME_DIR=E:\SteamLibrary\steamapps\common\Slay the Spire 2"
set "PCK=%GAME_DIR%\SlayTheSpire2.pck"
set "ASSEMBLY=%GAME_DIR%\data_sts2_windows_x86_64\sts2.dll"
set "OUT_ROOT=f:\UnityProjects\Project-Ark\SideProject\StS2mod\recovery"
set "OUT_DIR=%OUT_ROOT%\sts2_v0_98_3"
set "LIST_OUT=%OUT_DIR%\pck_filelist.txt"
set "LOG_OUT=%OUT_DIR%\recover.log"

if not exist "%GDRE%" (
  echo [ERROR] GDRETools not found: %GDRE%
  exit /b 1
)

if not exist "%PCK%" (
  echo [ERROR] PCK not found: %PCK%
  exit /b 1
)

if not exist "%ASSEMBLY%" (
  echo [ERROR] C# assembly not found: %ASSEMBLY%
  exit /b 1
)

if not exist "%OUT_ROOT%" mkdir "%OUT_ROOT%"
if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

if /I "%MODE%"=="list" goto LIST
if /I "%MODE%"=="recover" goto RECOVER
if /I "%MODE%"=="both" goto BOTH

echo [ERROR] Unknown mode: %MODE%
echo Usage: %~nx0 [list^|recover^|both]
exit /b 1

:BOTH
call :LIST_STEP || exit /b 1
call :RECOVER_STEP || exit /b 1
echo [DONE] List + recover completed.
exit /b 0

:LIST
call :LIST_STEP
exit /b %ERRORLEVEL%

:RECOVER
call :RECOVER_STEP
exit /b %ERRORLEVEL%

:LIST_STEP
echo [INFO] Listing files from %PCK%
"%GDRE%" --headless --list-files="%PCK%" > "%LIST_OUT%" 2>&1
if errorlevel 1 (
  echo [ERROR] list-files failed. See: %LIST_OUT%
  exit /b 1
)
echo [OK] File list saved to %LIST_OUT%
exit /b 0

:RECOVER_STEP
echo [INFO] Recovering project into %OUT_DIR%
"%GDRE%" --headless --recover="%PCK%" --csharp-assembly="%ASSEMBLY%" --output="%OUT_DIR%" > "%LOG_OUT%" 2>&1
if errorlevel 1 (
  echo [ERROR] recover failed. See: %LOG_OUT%
  exit /b 1
)
echo [OK] Recovery finished. Log: %LOG_OUT%
exit /b 0
