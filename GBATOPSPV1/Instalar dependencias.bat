@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\instalar.ps1"
if errorlevel 1 (
  echo.
  echo La instalacion no termino. Revisa el mensaje anterior.
  pause
  exit /b 1
)
echo.
pause

