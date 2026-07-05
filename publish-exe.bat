@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0publish-exe.ps1"
pause
