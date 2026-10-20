# =========================================================================
# MicGuardian - Suite de Pruebas de Extremo a Extremo
# =========================================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Get-Item $scriptDir).Parent.FullName
$exe = Join-Path $rootDir "MicGuardian.exe"

Write-Host "=========================================================" -ForegroundColor Yellow
Write-Host "         EJECUTANDO SUITE DE PRUEBAS DE MICGUARDIAN       " -ForegroundColor Yellow
Write-Host "=========================================================" -ForegroundColor Yellow

# 1. Comprobar existencia del binario
if (-not (Test-Path $exe)) {
    Write-Host "[1/4] Compilando binario de prueba..." -ForegroundColor Cyan
    & (Join-Path $scriptDir "build.ps1")
} else {
    Write-Host "[1/4] Binario verificado: $exe" -ForegroundColor Green
}

# 2. Probar bandera --help
Write-Host "[2/4] Probando bandera CLI --help..." -ForegroundColor Cyan
$helpOut = & $exe --help | Out-String
if ($helpOut -match "MicGuardian") {
    Write-Host "  -> Prueba de ayuda superada." -ForegroundColor Green
} else {
    throw "Fallo al ejecutar --help."
}

# 3. Probar bandera --status
Write-Host "[3/4] Probando consulta de estado --status..." -ForegroundColor Cyan
$statusOut = & $exe --status | Out-String
Write-Host $statusOut -ForegroundColor Gray
if ($statusOut -match "ESTADO ACTUAL DEL") {
    Write-Host "  -> Prueba de lectura de CoreAudio superada." -ForegroundColor Green
} else {
    throw "Fallo al consultar el estado del microfono."
}

# 4. Probar auto-corrección con --fix
Write-Host "[4/4] Probando calibracion y desmuteo con --fix..." -ForegroundColor Cyan
$fixOut = & $exe --fix | Out-String
Write-Host $fixOut -ForegroundColor Gray
if ($fixOut -match "superada|calibrado|OK") {
    Write-Host "  -> Prueba de correccion inmediata superada." -ForegroundColor Green
} else {
    throw "Fallo en la prueba de correccion."
}

Write-Host "`n[EXITO TOTAL] Todas las pruebas de integridad pasaron satisfactoriamente." -ForegroundColor Green
