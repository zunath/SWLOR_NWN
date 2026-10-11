@echo off
setlocal

rem Visual Studio may inherit a system TEMP folder whose NuGet locks are inaccessible.
if not defined NUGET_SCRATCH set "NUGET_SCRATCH=%LOCALAPPDATA%\Temp\NuGetScratch"

rem Reused build servers can retain NuGet's previous scratch-folder selection.
dotnet build "%~dp0..\..\SWLOR.CLI\SWLOR.CLI.csproj" -c Release -p:RunPostBuildEvent=Never --disable-build-servers
if errorlevel 1 exit /b 1

dotnet "%~dp0..\..\SWLOR.CLI\bin\Release\net10.0\SWLOR.CLI.dll" %*
exit /b %ERRORLEVEL%
