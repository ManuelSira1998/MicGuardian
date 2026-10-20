# =========================================================================
# MicGuardian - Script de Compilación para PowerShell
# =========================================================================

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Get-Item $scriptDir).Parent.FullName
$outputExe = Join-Path $rootDir "MicGuardian.exe"

Write-Host "Iniciando compilacion de MicGuardian ($Configuration)..." -ForegroundColor Cyan

# Intentar compilar con csc.exe nativo
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $csc)) {
    throw "No se encontro csc.exe en el sistema."
}

$srcFilter = Join-Path $rootDir "src\MicGuardian\*.cs"
$args = @(
    "/target:winexe",
    "/optimize+",
    "/out:`"$outputExe`"",
    "/recurse:`"$srcFilter`""
)

& $csc $args

if ($LASTEXITCODE -eq 0 -and (Test-Path $outputExe)) {
    $info = Get-Item $outputExe
    Write-Host "[OK] Compilacion exitosa: $($info.FullName) ($([math]::Round($info.Length / 1KB, 1)) KB)" -ForegroundColor Green
} else {
    throw "La compilacion fallo con codigo de salida $LASTEXITCODE."
}
