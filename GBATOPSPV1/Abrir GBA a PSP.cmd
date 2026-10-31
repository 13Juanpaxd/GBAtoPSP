@echo off
setlocal
cd /d "%~dp0"
set "app=%~dp0src\GbaToPsp\bin\Release\net9.0-windows\GbaToPsp.exe"
set "elf=%~dp0native\build\gpsp_bundle.elf"
if not exist "%app%" goto setup
if not exist "%elf%" goto setup
start "" "%app%"
exit /b 0
:setup
echo Preparando GBA to PSP por primera vez...
call "%~dp0Instalar dependencias.bat"
if not exist "%app%" exit /b 1
if not exist "%elf%" exit /b 1
start "" "%app%"

