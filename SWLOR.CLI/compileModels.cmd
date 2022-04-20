::---###############-############-#########-######-###
@echo off
setlocal enabledelayedexpansion
::---###############-############-#########-######-###

call :void_main >"%~n0.log" 2>&1

                goto :end
::---###############-############-#########-######-###

:void_main

rem set "CWD=C:\Users\[user]\Beamdog Library\00785\bin\win32\"
set "CWD=C:\Program Files (x86)\Steam\steamapps\common\Neverwinter Nights\bin\win32\" 

for %%F in ("development\*.mdl") do (
    START "" /D "%CWD%" /WAIT /MIN /HIGH nwmain.exe compilemodel "%%~nF"
)

               goto :eof
::---###############-############-#########-######-###

:end