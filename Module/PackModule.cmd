@echo off
setlocal
pushd "%~dp0"
call "..\tools\SWLOR.CLI\RunCLI.cmd" -p ".\Star Wars LOR v2.mod"
set "PACK_EXIT_CODE=%ERRORLEVEL%"
popd
exit /b %PACK_EXIT_CODE%
