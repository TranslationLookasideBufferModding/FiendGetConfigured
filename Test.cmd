@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build.ps1" -Task Test
if errorlevel 1 pause
