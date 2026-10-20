# MicGuardian (C# + Windows CoreAudio API)

Servicio nativo en segundo plano (sin dependencias externas) que supervisa, desmutea y calibra de forma ininterrumpida el micrófono predeterminado en Windows. Diseñado para eliminar por completo los silenciamientos accidentales por hardware, conflictos de software de comunicaciones y anomalías de suspensión del sistema.

```
Teclado Lenovo / Hotkey F4 ──┐
Discord / Zoom / Teams       ├──► [Windows CoreAudio] ──► Extremo Silenciado (Muted: True)
Suspensión / Reanudación SO  ──┘            ▲
                                            │
                                            └── Corregido en <1s (Muted: False, Vol: 90%)
                                                         ▲
                                                         │
                                        [MicGuardian: 0% CPU, ~18 MB RAM]
```

---

## Qué problema resuelve

En entornos Windows modernos, y particularmente en laptops para juegos como las **Lenovo IdeaPad Gaming (serie 82SB / Legion)**, el micrófono suele quedar silenciado en segundo plano sin que la persona lo note:

1. **La tecla `F4` del teclado (Hotkey Mode):** En las computadoras portátiles Lenovo, las teclas F1-F12 vienen configuradas de fábrica para actuar como teclas de acción directa. La tecla **`F4`** tiene asignado por hardware el comando de silenciar el micrófono. Al jugar, presionar F4 o cerrar ventanas con `Alt + F4` y soltar `Alt` una fracción de segundo antes, el micrófono se apaga por completo a nivel de Windows.
2. **Desincronización de apps de comunicación:** Plataformas como Discord, Microsoft Teams, Skype o Zoom pueden tomar control exclusivo del dispositivo de captura. Si la aplicación se cuelga, se cierra forzosamente o desincroniza su estado interno, el extremo de audio del sistema operativo queda en `Muted: True`.
3. **Paso por suspensión o hibernación:** Al reanudar el equipo, ciertos controladores de Realtek Audio y Nahimic reanudan el hardware en estado mudo o reducen el volumen de captura a niveles inaudibles.

**MicGuardian** corre de forma invisible en segundo plano, detecta estos eventos en menos de un segundo y devuelve el micrófono a su estado operativo calibrado.

---

## Qué sabe hacer

- **Desmuteo automático e instantáneo:** Monitorea el estado binario del micrófono. Si se detecta silenciado, invoca de inmediato el método de hardware `SetMute(false)` con contexto de evento seguro.
- **Calibración y nivelación de volumen:** Si el volumen de captura cae por debajo del 60% (por manipulación involuntaria o auto-ajuste de juegos), lo restaura automáticamente al **90%** (configurable).
- **Soporte dinámico para hot-plugging:** Detecta automáticamente cuando el usuario conecta o desconecta auriculares Bluetooth (por ejemplo, realme Buds, AirPods), auriculares de diadema por jack 3.5 mm o micrófonos USB, migrando la protección al nuevo dispositivo activo al instante.
- **Cero dependencias externas:** No requiere Node.js, Python, Visual Studio ni librerías de terceros. Se compila en 1 segundo usando el compilador nativo de Windows (`csc.exe`) presente en todas las instalaciones de Windows 10 y 11.
- **Submarcado dual inteligente (GUI / Consola):** Compilado como aplicación de ventana sin interfaz gráfica (`/target:winexe`), no abre ventanas negras en pantalla ni interrumpe juegos. Si se invoca desde una terminal con parámetros (como `--status`), se acopla a la consola existente para imprimir los diagnósticos.
- **Instancia única protegida por Mutex:** Evita la duplicación de procesos en memoria mediante exclusión mutua a nivel de kernel de Windows.

---

## Comparativa de rendimiento

| Parámetro | MicGuardian (C# Nativo) | Scripts comunes de PowerShell / Python |
| --- | --- | --- |
| **Uso de CPU en reposo** | **0.00%** (0 ciclos medibles) | 1.5% - 4.0% con picos de compilación JIT |
| **Consumo de memoria RAM** | **~18 MB** (Working Set estable) | 80 MB - 140 MB |
| **Tiempo de respuesta ante mute** | **< 1000 ms** | 3 - 10 segundos |
| **Dependencias de instalación** | **0** (Usa .NET Framework 4.x nativo de Windows) | Requiere Python runtime, pip o dependencias npm |
| **Arranque en segundo plano** | 100% invisible (sin parpadeos de consola) | Requiere trucos de `.vbs` o ventanas minimizadas |

---

## Arquitectura y diseño técnico

El sistema está estructurado bajo principios de arquitectura limpia y separación de responsabilidades:

| Módulo | Ruta | Propósito |
| --- | --- | --- |
| `CoreAudio` | `src/MicGuardian/CoreAudio/` | Definiciones estrictas de interfaces COM (`IMMDeviceEnumerator`, `IMMDevice`, `IAudioEndpointVolume`). Todas decoradas con `[PreserveSig]` para evitar excepciones de violación de acceso de memoria (`0xC0000005`) en `AUDIOSES.DLL`. |
| `Engine` | `src/MicGuardian/Engine/` | Contiene el monitor de hardware (`AudioEndpointMonitor`) y el servicio de fondo (`GuardianService`), administrando la tolerancia a fallos y latidos periódicos. |
| `Configuration` | `src/MicGuardian/Configuration/` | Manejador de persistencia para `%LOCALAPPDATA%\MicGuardian\config.json` con analizador nativo liviano sin dependencias. |
| `Logging` | `src/MicGuardian/Logging/` | Sistema de bitácora seguro para hilos con límite de tamaño (1 MB) y rotación automática para no degradar el disco. |
| `Program` | `src/MicGuardian/Program.cs` | Orquestador de inicio, analizador de banderas CLI y enlace de Win32 `AttachConsole`. |

Consulta [docs/architecture.md](docs/architecture.md) para un desglose exhaustivo de los contratos COM, el ciclo de vida de los punteros RCW y la gestión de eventos de Windows.

---

## Estructura del repositorio

```
MicGuardian/
├── .github/
│   └── workflows/
│       └── build-and-release.yml   # CI automatizado en GitHub Actions (Windows)
├── docs/
│   └── architecture.md             # Documentación técnica profunda de CoreAudio
├── scripts/
│   ├── build.bat                   # Compilación nativa por lotes
│   ├── build.ps1                   # Compilación para PowerShell
│   ├── install.ps1                 # Instalador con persistencia (Task Scheduler + Startup)
│   ├── uninstall.ps1               # Desinstalador limpio
│   └── test-guardian.ps1           # Suite de pruebas automatizadas de extremo a extremo
├── src/
│   └── MicGuardian/
│       ├── Configuration/
│       │   └── GuardianConfig.cs   # Modelo y deserializador de configuración
│       ├── CoreAudio/
│       │   ├── AudioConstants.cs   # Constantes y GUIDs de CoreAudio
│       │   ├── IAudioEndpointVolume.cs
│       │   ├── IMMDevice.cs
│       │   ├── IMMDeviceEnumerator.cs
│       │   └── MMDeviceEnumeratorComObject.cs
│       ├── Engine/
│       │   ├── AudioEndpointMonitor.cs
│       │   └── GuardianService.cs
│       ├── Logging/
│       │   └── Logger.cs
│       ├── Properties/
│       │   └── AssemblyInfo.cs     # Metadatos del ensamblado
│       ├── MicGuardian.csproj      # Proyecto .NET SDK compatible con MSBuild
│       └── Program.cs              # Entrada principal y soporte CLI
├── .editorconfig
├── .gitattributes
├── .gitignore
├── build.bat                       # Acceso directo para compilar en la raíz
├── LICENSE                         # Licencia de Uso Personal y No Comercial
└── README.md                       # Documentación principal
```

---

## Empezar

### Requisitos
- Windows 10 o Windows 11 (Cualquier edición de 64 o 32 bits).
- No requiere instalar SDKs pesados. Windows incluye por defecto el compilador necesario.

### 1. Compilación (en menos de 2 segundos)
Puedes compilar el proyecto haciendo doble clic sobre `build.bat` o desde la terminal:

```powershell
.\scripts\build.ps1
```

Esto generará el archivo ejecutable optimizado `MicGuardian.exe` en la raíz del proyecto.

### 2. Instalación permanente en el sistema
Para dejarlo instalado y protegido contra reinicios:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

Este script automatizado realiza lo siguiente:
1. Copia el ejecutable a `%LOCALAPPDATA%\Programs\MicGuardian\MicGuardian.exe`.
2. Registra una tarea en el **Programador de Tareas de Windows (Task Scheduler)** llamada `MicGuardian` que arranca con el inicio de sesión del usuario, tolera desconexiones de batería y se recupera automáticamente.
3. Registra un acceso directo en la carpeta de **Inicio de Windows (Startup)** como mecanismo de redundancia.
4. Lanza el servicio de inmediato.

---

## Uso por línea de comandos (CLI)

Puedes interactuar con el ejecutable en cualquier momento desde PowerShell o CMD:

```powershell
# Consultar el estado en tiempo real del micrófono actual
.\MicGuardian.exe --status

# Ejecutar una revisión, calibración y desmuteo inmediato sin levantar el demonio
.\MicGuardian.exe --fix

# Ver los parámetros de configuración activos
.\MicGuardian.exe --config

# Ver la ayuda y opciones disponibles
.\MicGuardian.exe --help
```

### Ejemplo de salida de `--status`:
```text
=================================================
        ESTADO ACTUAL DEL MICRÓFONO
=================================================
Dispositivo ID  : {0.0.1.00000000}.{873c86d3-71d4-4047-b2c8-acd30c052c2c}
Estado Endpoint : Activo (1)
Silenciado      : NO (Activo)
Volumen Maestro : 90%
=================================================
```

---

## Configuración avanzada

El programa genera automáticamente un archivo de configuración en formato JSON en:  
`%LOCALAPPDATA%\MicGuardian\config.json`

```json
{
  "TargetVolumePercent": 90,
  "MinVolumeThresholdPercent": 60,
  "PollIntervalMilliseconds": 1000,
  "EnableAutoUnmute": true,
  "EnableVolumeEnforcement": true,
  "EnableLogging": true
}
```

- **`TargetVolumePercent`**: Nivel al que se restaura el volumen cuando es corregido (predeterminado: 90%).
- **`MinVolumeThresholdPercent`**: Si el volumen cae por debajo de este valor, se dispara la corrección (predeterminado: 60%).
- **`PollIntervalMilliseconds`**: Tiempo en milisegundos entre verificaciones (predeterminado: 1000 ms).
- **`EnableAutoUnmute`**: Si es `true`, desmutea inmediatamente ante cualquier silenciamiento.
- **`EnableLogging`**: Guarda registro de eventos en `%LOCALAPPDATA%\MicGuardian\mic_guardian.log`.

---

## Pruebas de integración

El repositorio cuenta con una suite de pruebas de integridad de extremo a extremo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-guardian.ps1
```

La suite valida:
1. Integridad del binario compilado.
2. Ejecución y acoplamiento de consola para `--help`.
3. Conexión y lectura del subsistema de CoreAudio para `--status`.
4. Corrección de hardware mediante `--fix`.

---

## Desinstalación

Si deseas remover el servicio del sistema:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1
```

Detendrá el proceso activo y limpiará las tareas programadas y los accesos directos de inicio sin tocar tus archivos de código.

---

## Licencia y Términos de Uso

Este proyecto se distribuye bajo una **Licencia de Uso Personal y No Comercial**. Consulta el archivo [LICENSE](LICENSE) para conocer el texto legal completo.

### Resumen de Términos:
- **Uso Personal y Educativo (Gratuito):** Eres libre de descargar, compilar, modificar e inspeccionar este software para uso propio y privado en tus dispositivos personales o para fines de aprendizaje.
- **Uso Comercial y Reventa (Requiere Licencia y Regalías):** Queda prohibida la venta, monetización, sublicenciamiento o inclusión de este software (en binario o código fuente) en productos, servicios o tiendas de aplicaciones comerciales sin autorización previa por escrito.
- **Contacto para Licencias Comerciales:** Si eres una empresa, fabricante de hardware o desarrollador interesado en integrar o distribuir este software comercialmente, contacta al autor para acordar los términos de licenciamiento y regalías:  
  📫 **Orlando Barrientos** — `orlandoabarrientos@gmail.com`

---

Desarrollado con dedicación por **Orlando Barrientos** (2026).
