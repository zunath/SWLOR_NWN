@echo off
setlocal

dotnet build "%~dp0..\..\SWLOR.CLI\SWLOR.CLI.csproj" -c Release -p:RunPostBuildEvent=Never
if errorlevel 1 exit /b 1

dotnet "%~dp0..\..\SWLOR.CLI\bin\Release\net10.0\SWLOR.CLI.dll" %*
exit /b %ERRORLEVEL%
