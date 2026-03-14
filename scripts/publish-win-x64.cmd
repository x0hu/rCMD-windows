@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-win-x64.ps1"
if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

echo Publish succeeded.
exit /b 0
