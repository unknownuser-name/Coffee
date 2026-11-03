@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\open_coffee_project.ps1" %*
if errorlevel 1 pause
