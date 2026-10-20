using System;
using System.Threading;
using MicGuardian.Configuration;
using MicGuardian.Logging;

namespace MicGuardian.Engine
{
    /// <summary>
    /// Servicio de supervisión continua. Ejecuta el bucle de polling de bajo consumo,
    /// maneja latidos periódicos y tolerancia a fallos.
    /// </summary>
    public class GuardianService
    {
        private readonly GuardianConfig _config;
        private readonly AudioEndpointMonitor _monitor;
        private volatile bool _isRunning;
        private const int HeartbeatIntervalCycles = 600; // ~10 minutos a 1 ciclo/seg

        public GuardianService(GuardianConfig config)
        {
            _config = config;
            _monitor = new AudioEndpointMonitor();
        }

        public void Run()
        {
            _isRunning = true;
            Logger.Info("Iniciando bucle de supervisión continua de MicGuardian.");

            int cycle = 0;
            while (_isRunning)
            {
                try
                {
                    _monitor.CheckAndEnforce(_config);

                    cycle++;
                    if (cycle >= HeartbeatIntervalCycles)
                    {
                        cycle = 0;
                        Logger.Info(string.Format("Latido del sistema: guardián activo en extremo '{0}'.", _monitor.CurrentDeviceId));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Excepción no controlada en el bucle del guardián.", ex);
                }

                try
                {
                    Thread.Sleep(Math.Max(250, _config.PollIntervalMilliseconds));
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }

            _monitor.Dispose();
            Logger.Info("Servicio MicGuardian detenido.");
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
