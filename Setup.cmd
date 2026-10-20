@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\SetupRepository.ps1" -Mode Setup %*
set "SETUP_EXIT_CODE=%ERRORLEVEL%"
if not "%SETUP_EXIT_CODE%"=="0" pause
exit /b %SETUP_EXIT_CODE%
