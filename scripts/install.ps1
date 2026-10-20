# =========================================================================
# MicGuardian - Instalador de Servicio y Persistencia
# =========================================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Get-Item $scriptDir).Parent.FullName
$outputExe = Join-Path $rootDir "MicGuardian.exe"

# 1. Compilar si no existe el binario en la raíz
if (-not (Test-Path $outputExe)) {
    Write-Host "Compilando binario inicial..." -ForegroundColor Cyan
    & (Join-Path $scriptDir "build.ps1")
}

# 2. Directorio de instalación en AppData local del usuario
$installDir = "$env:LOCALAPPDATA\Programs\MicGuardian"
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

$targetExe = Join-Path $installDir "MicGuardian.exe"

# 3. Detener proceso en ejecución previo si existe
Write-Host "Verificando instancias activas..." -ForegroundColor Cyan
Get-Process MicGuardian -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 600

# 4. Copiar binario a la carpeta de instalación
Copy-Item $outputExe $targetExe -Force
Write-Host "[OK] Binario instalado en: $targetExe" -ForegroundColor Green

# 5. Registrar tarea en el Programador de Tareas (Task Scheduler)
$taskName = "MicGuardian"
$action = New-ScheduledTaskAction -Execute $targetExe
$triggerLogon = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit 0 -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

try {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger @($triggerLogon) -Settings $settings -Description "Microphone auto-unmute guardian for Lenovo/Windows" -Force | Out-Null
    Write-Host "[OK] Tarea programada '$taskName' registrada (inicio con sesion)." -ForegroundColor Green
} catch {
    Write-Warning "No se pudo registrar la tarea en Task Scheduler: $_"
}

# 6. Registrar acceso directo en la carpeta de Inicio de Windows (doble redundancia)
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupDir "MicGuardian.lnk"
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Microphone Guardian Auto-Unmute Service"
$shortcut.Save()
Write-Host "[OK] Acceso directo redundante registrado en: $shortcutPath" -ForegroundColor Green

# 7. Iniciar el servicio inmediatamente
Write-Host "Iniciando servicio de fondo..." -ForegroundColor Cyan
Start-ScheduledTask -TaskName $taskName
Start-Sleep -Seconds 1

$p = Get-Process MicGuardian -ErrorAction SilentlyContinue
if ($p) {
    Write-Host "`n[EXITO] MicGuardian esta activo y protegiendo tu microfono (PID: $($p.Id))." -ForegroundColor Green
} else {
    Write-Warning "El proceso no aparecio de inmediato; comprueba el visor de eventos o la tarea programada."
}
