@echo off
setlocal
pushd "%~dp0"
call "..\tools\SWLOR.CLI\RunCLI.cmd" -u ".\Star Wars LOR v2.mod"
set "UNPACK_EXIT_CODE=%ERRORLEVEL%"
popd
exit /b %UNPACK_EXIT_CODE%
