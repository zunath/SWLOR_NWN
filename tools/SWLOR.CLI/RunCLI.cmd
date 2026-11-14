@echo off
setlocal

rem Visual Studio may inherit a system TEMP folder whose NuGet locks are inaccessible.
if not defined NUGET_SCRATCH set "NUGET_SCRATCH=%LOCALAPPDATA%\Temp\NuGetScratch"

rem Reused build servers can retain NuGet's previous scratch-folder selection.
dotnet build "%~dp0..\..\SWLOR.CLI\SWLOR.CLI.csproj" -c Release -p:RunPostBuildEvent=Never --disable-build-servers
if errorlevel 1 exit /b 1

rem A standalone deploy (-o without --host-output) stages the Release SWLOR.Game.Host when the debug server boots the
rem host entry; building SWLOR.CLI builds the game but not the host, so build the host too rather than deploy a missing
rem or stale one. The game's post-build passes --host-output for the host the solution built.
set "SWLOR_CLI_DEPLOY="
set "SWLOR_CLI_HOST_OUTPUT="
for %%A in (%*) do (
    if /I "%%~A"=="-o" set "SWLOR_CLI_DEPLOY=1"
    if /I "%%~A"=="--outputDeploy" set "SWLOR_CLI_DEPLOY=1"
    if /I "%%~A"=="--host-output" set "SWLOR_CLI_HOST_OUTPUT=1"
)
if defined SWLOR_CLI_DEPLOY if not defined SWLOR_CLI_HOST_OUTPUT (
    dotnet build "%~dp0..\..\SWLOR.Game.Host\SWLOR.Game.Host.csproj" -c Release -p:RunPostBuildEvent=Never --disable-build-servers
    if errorlevel 1 exit /b 1
)

dotnet "%~dp0..\..\SWLOR.CLI\bin\Release\net10.0\SWLOR.CLI.dll" %*
exit /b %ERRORLEVEL%
