$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = Join-Path $root 'vendor/gpsp-kai'
$msys = if (Test-Path (Join-Path $root 'tools/msys64/usr/bin/bash.exe')) { Join-Path $root 'tools/msys64' } else { 'C:\msys64' }
$pspdev = Join-Path $root 'tools/pspdev'
$bash = Join-Path $msys 'usr/bin/bash.exe'
$strip = Join-Path $pspdev 'bin/psp-strip.exe'

foreach ($path in @($source, $bash, $strip)) {
    if (-not (Test-Path $path)) { throw "Falta: $path. Ejecuta Instalar dependencias.bat." }
}

$patch = Join-Path $root 'native/gpsp-kai.patch'
$previousErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
git -C $source apply --check $patch 2>$null
$canApply = $LASTEXITCODE -eq 0
$ErrorActionPreference = $previousErrorActionPreference
if ($canApply) {
    git -C $source apply $patch
} else {
    git -C $source apply --reverse --check $patch
    if ($LASTEXITCODE -ne 0) { throw 'La copia de gpSP tiene cambios incompatibles con el parche.' }
}

Copy-Item (Join-Path $root 'native/bootstrap.c') (Join-Path $source 'src/bootstrap.c') -Force
$msysRoot = $root.Replace('\', '/').Replace('C:', '/c')
$build = ('export PSPDEV=/pspdev; export PATH=/usr/bin:/pspdev/bin:$PATH; make -C %ROOT%/vendor/gpsp-kai/gpsp clean PSPSDK=/pspdev/psp/sdk PREFIX=/pspdev/psp; make -C %ROOT%/vendor/gpsp-kai/gpsp UO_gpSP_kai.elf PSPSDK=/pspdev/psp/sdk PREFIX=/pspdev/psp -j2').Replace('%ROOT%', $msysRoot)
& $bash -lc $build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$destination = Join-Path $root 'native/build/gpsp_bundle.elf'
New-Item -ItemType Directory -Force (Split-Path $destination) | Out-Null
& $strip -o $destination (Join-Path $source 'gpsp/UO_gpSP_kai.elf')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not (Select-String -Path $destination -Pattern 'G2PS_BOOTSTRAP_V1' -SimpleMatch -Quiet)) {
    throw 'El ELF no contiene el arranque G2PS.'
}
Write-Host "ELF creado en $destination"

