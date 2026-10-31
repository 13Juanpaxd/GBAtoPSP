$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $root 'tools'
New-Item -ItemType Directory -Force $tools | Out-Null

function Install-WingetPackage([string]$id) {
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw 'No se encontro winget. Instala App Installer desde Microsoft Store y vuelve a ejecutar este archivo.'
    }
    & winget install --id $id --exact --accept-package-agreements --accept-source-agreements --silent
    if ($LASTEXITCODE -ne 0) { throw "No se pudo instalar $id." }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Install-WingetPackage 'Git.Git' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Install-WingetPackage 'Microsoft.DotNet.SDK.9' }
if (-not (Test-Path 'C:\msys64\usr\bin\bash.exe')) { Install-WingetPackage 'MSYS2.MSYS2' }

$git = if (Get-Command git -ErrorAction SilentlyContinue) { 'git' } else { Join-Path $env:ProgramFiles 'Git\cmd\git.exe' }
$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { 'dotnet' } else { Join-Path $env:ProgramFiles 'dotnet\dotnet.exe' }
if (($git -ne 'git') -and -not (Test-Path $git)) { throw 'Git se instalo, pero aun no esta disponible. Cierra esta ventana y ejecuta el instalador otra vez.' }
if (($dotnet -ne 'dotnet') -and -not (Test-Path $dotnet)) { throw '.NET se instalo, pero aun no esta disponible. Cierra esta ventana y ejecuta el instalador otra vez.' }
if ($git -ne 'git') { $env:Path = (Split-Path $git) + ';' + $env:Path }

$pspdev = Join-Path $tools 'pspdev'
if (-not (Test-Path (Join-Path $pspdev 'bin\psp-gcc.exe'))) {
    $zip = Join-Path $env:TEMP 'pspdev-win-15.2.0-v2.zip'
    Write-Host 'Descargando toolchain PSP para Windows...'
    Invoke-WebRequest -Uri 'https://github.com/dmang-dev/pspdev-win/releases/download/v2/pspdev-win-15.2.0-v2.zip' -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath $pspdev -Force
    Remove-Item $zip -Force
}

$source = Join-Path $root 'vendor/gpsp-kai'
if (-not (Test-Path $source)) {
    Write-Host 'Descargando gpSP Kai...'
    & $git clone --depth 1 https://github.com/PSP-Archive/unofficial-gpSP-kai.git $source
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo descargar gpSP Kai.' }
}

Write-Host 'Compilando el nucleo de PSP...'
& powershell -ExecutionPolicy Bypass -File (Join-Path $root 'native/build-gpsp-windows.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Compilando la aplicacion de Windows...'
& $dotnet build (Join-Path $root 'src/GbaToPsp/GbaToPsp.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Instalacion terminada. Abre Abrir GBA a PSP.cmd.'

