using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using MicGuardian.Configuration;
using MicGuardian.Engine;
using MicGuardian.Logging;

namespace MicGuardian
{
    public class Program
    {
        private static Mutex _singleInstanceMutex;
        private const string MutexName = "Local\\MicGuardian_SingleInstance_Mutex_v1";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        private const int ATTACH_PARENT_PROCESS = -1;
        private const int STD_OUTPUT_HANDLE = -11;

        private static void InitConsoleOutput()
        {
            try
            {
                if (AttachConsole(ATTACH_PARENT_PROCESS))
                {
                    IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                    if (stdHandle != IntPtr.Zero && stdHandle != new IntPtr(-1))
                    {
                        var safeHandle = new SafeFileHandle(stdHandle, false);
                        var fileStream = new FileStream(safeHandle, FileAccess.Write);
                        var writer = new StreamWriter(fileStream, System.Text.Encoding.Default) { AutoFlush = true };
                        Console.SetOut(writer);
                        Console.SetError(writer);
                    }
                }
            }
            catch { }
        }

        public static int Main(string[] args)
        {
            GuardianConfig config = ConfigManager.Load();

            if (args != null && args.Length > 0)
            {
                InitConsoleOutput();

                string cmd = args[0].Trim().ToLowerInvariant();
                switch (cmd)
                {
                    case "--status":
                    case "-s":
                        return ShowStatus();

                    case "--fix":
                    case "-f":
                        return RunFixOnce(config);

                    case "--config":
                    case "-c":
                        ShowConfig(config);
                        return 0;

                    case "--help":
                    case "-h":
                    case "/?":
                        ShowHelp();
                        return 0;

                    default:
                        Console.WriteLine("\n[MicGuardian] Parámetro desconocido: " + args[0]);
                        Console.WriteLine("Usa 'MicGuardian.exe --help' para ver los comandos disponibles.\n");
                        return 1;
                }
            }

            return RunDaemon(config);
        }

        private static int RunDaemon(GuardianConfig config)
        {
            Logger.Initialize(null, echoToConsole: false);

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Logger.Error("Fallo crítico en AppDomain.", eventArgs.ExceptionObject as Exception);
            };

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                Logger.Warning("Se detectó una instancia de MicGuardian ya activa. Saliendo.");
                return 0;
            }

            try
            {
                var service = new GuardianService(config);
                service.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Excepción fatal en la ejecución del servicio.", ex);
                return 1;
            }
            finally
            {
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Close();
                }
            }
        }

        private static int ShowStatus()
        {
            Logger.Initialize(null, echoToConsole: false);
            using (var monitor = new AudioEndpointMonitor())
            {
                string devId;
                bool isMuted;
                float vol;
                int state;

                if (monitor.GetStatus(out devId, out isMuted, out vol, out state))
                {
                    Console.WriteLine("\n=================================================");
                    Console.WriteLine("        ESTADO ACTUAL DEL MICRÓFONO");
                    Console.WriteLine("=================================================");
                    Console.WriteLine("Dispositivo ID  : " + devId);
                    Console.WriteLine("Estado Endpoint : " + (state == 1 ? "Activo (1)" : state.ToString()));
                    Console.WriteLine("Silenciado      : " + (isMuted ? "SÍ (Muted: True)" : "NO (Activo)"));
                    Console.WriteLine("Volumen Maestro : " + Math.Round(vol * 100) + "%");
                    Console.WriteLine("=================================================\n");
                    return 0;
                }
                else
                {
                    Console.WriteLine("\n[ERROR] No se pudo conectar con el dispositivo de captura de Windows CoreAudio.\n");
                    return 1;
                }
            }
        }

        private static int RunFixOnce(GuardianConfig config)
        {
            Logger.Initialize(null, echoToConsole: false);
            Console.WriteLine("\n[*] Ejecutando corrección inmediata de micrófono...");

            using (var monitor = new AudioEndpointMonitor())
            {
                if (monitor.CheckAndEnforce(config))
                {
                    Console.WriteLine("[OK] Micrófono revisado, desmuteado y calibrado al " + config.TargetVolumePercent + "%.\n");
                    return 0;
                }
                else
                {
                    Console.WriteLine("[ERROR] No se pudo aplicar la corrección. Verifica que haya un micrófono conectado.\n");
                    return 1;
                }
            }
        }

        private static void ShowConfig(GuardianConfig config)
        {
            Console.WriteLine("\n=================================================");
            Console.WriteLine("       CONFIGURACIÓN ACTUAL DE MICGUARDIAN");
            Console.WriteLine("=================================================");
            Console.WriteLine("Volumen Objetivo      : " + config.TargetVolumePercent + "%");
            Console.WriteLine("Umbral Mínimo         : " + config.MinVolumeThresholdPercent + "%");
            Console.WriteLine("Intervalo de Polling  : " + config.PollIntervalMilliseconds + " ms");
            Console.WriteLine("Auto-Desmuteo         : " + (config.EnableAutoUnmute ? "Habilitado" : "Deshabilitado"));
            Console.WriteLine("Control de Volumen    : " + (config.EnableVolumeEnforcement ? "Habilitado" : "Deshabilitado"));
            Console.WriteLine("Registro de Logs      : " + (config.EnableLogging ? "Habilitado" : "Deshabilitado"));
            Console.WriteLine("Ruta de Logs          : " + Logger.LogFilePath);
            Console.WriteLine("=================================================\n");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("\nMicGuardian v1.1.0 - Guardián Automático del Micrófono en Windows");
            Console.WriteLine("Desarrollado por Orlando Barrientos\n");
            Console.WriteLine("Uso:");
            Console.WriteLine("  MicGuardian.exe               Inicia el servicio en segundo plano (modo demonio)");
            Console.WriteLine("  MicGuardian.exe --status      Muestra el estado en tiempo real del micrófono");
            Console.WriteLine("  MicGuardian.exe --fix         Aplica una corrección y desmuteo inmediato");
            Console.WriteLine("  MicGuardian.exe --config      Muestra los valores de configuración activos");
            Console.WriteLine("  MicGuardian.exe --help        Muestra esta información de ayuda\n");
        }
    }
}
