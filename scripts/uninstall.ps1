# =========================================================================
# MicGuardian - Desinstalador Limpio
# =========================================================================

Write-Host "Deteniendo procesos activos de MicGuardian..." -ForegroundColor Cyan
Get-Process MicGuardian -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Removiendo tarea de Task Scheduler..." -ForegroundColor Cyan
Unregister-ScheduledTask -TaskName "MicGuardian" -Confirm:$false -ErrorAction SilentlyContinue

Write-Host "Removiendo acceso directo de Inicio..." -ForegroundColor Cyan
$shortcutPath = Join-Path ([Environment]::GetFolderPath("Startup")) "MicGuardian.lnk"
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
}

Write-Host "`n[OK] MicGuardian ha sido desinstalado por completo." -ForegroundColor Green
Write-Host "Nota: Los archivos fuente en este repositorio no han sido modificados." -ForegroundColor Gray
