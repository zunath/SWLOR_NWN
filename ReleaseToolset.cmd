@echo off
rem Builds the Windows and Linux toolset packages on this machine and (with --publish) releases them with gh.
rem Dry run by default. See "Publish a release" in SWLOR.Toolset\README.md.
setlocal
where python >nul 2>nul
if errorlevel 1 (
    py -3 -B "%~dp0tools\ToolsetDistribution.py" release-local %*
) else (
    python -B "%~dp0tools\ToolsetDistribution.py" release-local %*
)
exit /b %ERRORLEVEL%
