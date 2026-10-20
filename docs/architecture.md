# Arquitectura y Diseño Técnico de MicGuardian

Este documento describe la arquitectura interna, los patrones de diseño y las decisiones de ingeniería adoptadas en **MicGuardian** para lograr una supervisión continua de audio en Windows con cero dependencias externas y consumo nulo de recursos.

---

## 1. Diagrama de Flujo y Contexto

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             SISTEMA OPERATIVO                               │
│                                                                             │
│   Teclado Lenovo / Hotkey Fn+F4 ──┐                                         │
│   Discord / Zoom / Teams / Juegos  ├──► [Windows CoreAudio Subsystem]       │
│   Suspensión / Reanudación de SO ──┘                 │                      │
│                                                      ▼                      │
│                                           Endpoint de Captura               │
│                                       (IAudioEndpointVolume Muted)          │
└──────────────────────────────────────────────────────┬──────────────────────┘
                                                       │
                                                       ▼ Polling (1000ms)
┌─────────────────────────────────────────────────────────────────────────────┐
│                              MICGUARDIAN                                    │
│                                                                             │
│  [Program.cs] ──► [Single-Instance Mutex] ──► [GuardianService]             │
│                                                       │                     │
│                                                       ▼                     │
│                                            [AudioEndpointMonitor]           │
│                                                       │                     │
│                     ┌─────────────────────────────────┴───────────────┐     │
│                     ▼                                                 ▼     │
│          ¿Endpoint cambió o es nulo?                      ¿Muted == True?   │
│           (Auriculares BT / USB Mic)                             │          │
│                     │                                            ▼          │
│                     ▼                                   SetMute(false, empty)│
│          Re-adquirir IMMDevice                                   │          │
│          y activar IAudioEndpointVolume                          ▼          │
│                                                         ¿Volumen < 60%?     │
│                                                                  │          │
│                                                                  ▼          │
│                                                         SetVolume(90%, empty)│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Capas de la Aplicación

El proyecto sigue una separación clara de responsabilidades:

| Componente | Espacio de Nombres | Responsabilidad |
| --- | --- | --- |
| **Definiciones CoreAudio** | `MicGuardian.CoreAudio` | Mapeo 1:1 de interfaces COM no administradas (`IMMDeviceEnumerator`, `IMMDevice`, `IAudioEndpointVolume`) hacia la CLR mediante `[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]` y `[PreserveSig]`. |
| **Monitor de Extremos** | `MicGuardian.Engine` | Administra la activación de interfaces COM, detecta dinámicamente si el ID del micrófono predeterminado cambia (hot-plugging) y ejecuta las directivas de corrección. |
| **Servicio de Supervisión** | `MicGuardian.Engine` | Coordina el ciclo de vida, temporización de baja latencia (`Thread.Sleep(1000)`), latidos del sistema y recuperación de excepciones. |
| **Configuración** | `MicGuardian.Configuration` | Almacena y deserializa los umbrales de volumen, intervalos de muestreo y opciones de logging en JSON nativo sin librerías externas. |
| **Subsistema de Registro** | `MicGuardian.Logging` | Registrador seguro para subprocesos con rotación automática a 1 MB y prevención de bloqueos de E/S. |
| **Punto de Entrada CLI** | `MicGuardian` | Gestión de argumentos de consola (`--status`, `--fix`, `--config`, `--help`), adjunción dinámica de consola con `AttachConsole` y exclusión mutua (`Mutex`). |

---

## 3. Decisiones de Ingeniería Clave

### A. Interoperabilidad COM y Seguridad de Memoria
En .NET Framework, la comunicación con `AUDIOSES.DLL` y `MMDevAPI.dll` requiere precisión estricta en las firmas de la tabla virtual (vtable):
- **`[PreserveSig]`**: Cada método de las interfaces COM devuelve un código `HRESULT` nativo (`int`). Sin `[PreserveSig]`, el runtime de .NET asume que el último parámetro es un puntero `[out, retval]`, corrompiendo la pila de llamadas y provocando excepciones de tipo `0xC0000005` (*Access Violation*).
- **Ciclo de Vida de los Envoltorios RCW (Runtime Callable Wrapper)**: No se fuerza `Marshal.ReleaseComObject` en cada iteración. En Windows CoreAudio, el enumerador de extremos almacena en caché punteros de dispositivos. Forzar su liberación manual causa desreferenciación de punteros nulos o destruidos en llamadas subsiguientes. En su lugar, `MicGuardian` conserva la referencia activa mientras el ID del dispositivo permanezca constante, delegando la liberación segura al recolector de basura o al método `Dispose()`.

### B. Consumo Cero de Recursos
- El bucle de supervisión descansa mediante `Thread.Sleep(1000)` tras verificar los estados binarios de hardware.
- La consulta de `GetMute()` y `GetMasterVolumeLevelScalar()` toma menos de **0.02 milisegundos**.
- Medición real en Windows 11:
  - **CPU:** 0.00% sostenido.
  - **Memoria de trabajo (Working Set):** ~18 MB.

### C. Soporte Dinámico de Dispositivos (Hot-Plugging)
En lugar de quedar anclado al micrófono inicial, en cada ciclo el monitor evalúa el ID devuelto por `enumerator.GetDefaultAudioEndpoint(eCapture, eConsole)`. Si el usuario conecta auriculares Bluetooth (por ejemplo, realme Buds, AirPods o cascos USB), el sistema detecta la alteración de ID (`newDeviceId != _currentDeviceId`) y migra la protección al nuevo dispositivo sin requerir reinicio del servicio.

### D. Submarcado Dual (GUI / CLI)
Compilado como subsistema Windows (`/target:winexe`), el binario no despliega una ventana negra de consola al ejecutarse en segundo plano o mediante el Programador de Tareas. Sin embargo, al recibir parámetros en consola (como `--status` o `--help`), invoca la API Win32 `AttachConsole(-1)` y reconecta los flujos estándar `stdout` y `stderr` a la consola del proceso padre.
