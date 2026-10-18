@echo off
rem Compile staged ASCII MDLs with CleanModels; pass the resource and fresh output folders.
python "%~dp0..\tools\CompileBlasterModels.py" %*
exit /b %errorlevel%
