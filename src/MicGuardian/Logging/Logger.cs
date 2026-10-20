using System;
using System.IO;

namespace MicGuardian.Logging
{
    /// <summary>
    /// Subsistema de registro seguro para subprocesos con rotación de archivos.
    /// Diseñado para no lanzar excepciones que puedan interrumpir el bucle principal.
    /// </summary>
    public static class Logger
    {
        private static readonly object _syncRoot = new object();
        private static string _logFilePath;
        private static bool _echoToConsole = false;
        private const long MaxFileSizeBytes = 1024 * 1024; // 1 MB

        public static void Initialize(string customLogDir = null, bool echoToConsole = false)
        {
            _echoToConsole = echoToConsole;
            try
            {
                string baseDir = customLogDir;
                if (string.IsNullOrEmpty(baseDir))
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    baseDir = Path.Combine(appData, "MicGuardian");
                }

                if (!Directory.Exists(baseDir))
                {
                    Directory.CreateDirectory(baseDir);
                }

                _logFilePath = Path.Combine(baseDir, "mic_guardian.log");
            }
            catch
            {
                // Fallback silencioso para garantizar resiliencia
                _logFilePath = null;
            }
        }

        public static string LogFilePath
        {
            get { return _logFilePath; }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            string fullMessage = ex == null ? message : string.Format("{0} | Excepción: {1}", message, ex.Message);
            Write("FAIL", fullMessage);
        }

        private static void Write(string level, string message)
        {
            string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", DateTime.Now, level, message);

            if (_echoToConsole)
            {
                try
                {
                    Console.WriteLine(line);
                }
                catch { }
            }

            if (string.IsNullOrEmpty(_logFilePath)) return;

            lock (_syncRoot)
            {
                try
                {
                    // Rotación simple si supera 1 MB
                    if (File.Exists(_logFilePath))
                    {
                        var info = new FileInfo(_logFilePath);
                        if (info.Length > MaxFileSizeBytes)
                        {
                            string backup = _logFilePath + ".old";
                            if (File.Exists(backup)) File.Delete(backup);
                            File.Move(_logFilePath, backup);
                        }
                    }

                    File.AppendAllText(_logFilePath, line + "\r\n");
                }
                catch
                {
                    // No bloquear la ejecución del hilo principal por fallos de E/S
                }
            }
        }
    }
}
