using System;
using System.Runtime.InteropServices;
using MicGuardian.Configuration;
using MicGuardian.CoreAudio;
using MicGuardian.Logging;

namespace MicGuardian.Engine
{
    /// <summary>
    /// Administrador de extremo de audio. Conecta con la API nativa de Windows CoreAudio,
    /// detecta cambios dinámicos en el micrófono predeterminado y aplica políticas de volumen y mute.
    /// </summary>
    public class AudioEndpointMonitor : IDisposable
    {
        private IMMDeviceEnumerator _enumerator;
        private IMMDevice _currentDevice;
        private IAudioEndpointVolume _currentVolume;
        private string _currentDeviceId;
        private Guid _emptyContext = Guid.Empty;

        public string CurrentDeviceId
        {
            get { return _currentDeviceId; }
        }

        public bool EnsureActiveEndpoint()
        {
            try
            {
                if (_enumerator == null)
                {
                    _enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
                }

                IMMDevice defaultDevice = null;
                // eCapture = 1, eConsole = 0
                int hr = _enumerator.GetDefaultAudioEndpoint(AudioConstants.eCapture, AudioConstants.eConsole, out defaultDevice);
                if (hr != 0 || defaultDevice == null)
                {
                    Reset();
                    return false;
                }

                string newDeviceId = null;
                defaultDevice.GetId(out newDeviceId);

                if (newDeviceId != _currentDeviceId || _currentVolume == null)
                {
                    _currentDevice = defaultDevice;
                    _currentDeviceId = newDeviceId;

                    object epvObj = null;
                    Guid iid = AudioConstants.IID_IAudioEndpointVolume;
                    hr = _currentDevice.Activate(ref iid, AudioConstants.CLSCTX_ALL, IntPtr.Zero, out epvObj);

                    if (hr == 0 && epvObj != null)
                    {
                        _currentVolume = (IAudioEndpointVolume)epvObj;
                        Logger.Info(string.Format("Extremo de captura enlazado: {0}", _currentDeviceId));
                    }
                    else
                    {
                        Reset();
                        return false;
                    }
                }

                return _currentVolume != null;
            }
            catch (Exception ex)
            {
                Logger.Error("Fallo al enlazar el extremo de audio.", ex);
                Reset();
                return false;
            }
        }

        public bool CheckAndEnforce(GuardianConfig config)
        {
            if (!EnsureActiveEndpoint() || _currentVolume == null) return false;

            try
            {
                bool isMuted = false;
                int hr = _currentVolume.GetMute(out isMuted);
                if (hr != 0)
                {
                    Reset();
                    return false;
                }

                // 1. Restaurar silenciamiento accidental
                if (isMuted && config.EnableAutoUnmute)
                {
                    _currentVolume.SetMute(false, ref _emptyContext);
                    Logger.Warning("¡Silenciamiento detectado! Micrófono desmuteado automáticamente.");
                }

                // 2. Comprobar nivel de volumen
                float currentVol = 0;
                hr = _currentVolume.GetMasterVolumeLevelScalar(out currentVol);
                if (hr == 0 && config.EnableVolumeEnforcement)
                {
                    if (currentVol < config.MinVolumeThresholdScalar)
                    {
                        _currentVolume.SetMasterVolumeLevelScalar(config.TargetVolumeScalar, ref _emptyContext);
                        Logger.Warning(string.Format("¡Volumen bajo ({0:P0}) detectado! Calibrado al {1}%.", currentVol, config.TargetVolumePercent));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Fallo durante la evaluación de políticas de audio.", ex);
                Reset();
                return false;
            }
        }

        public bool GetStatus(out string deviceId, out bool isMuted, out float volumeScalar, out int deviceState)
        {
            deviceId = null;
            isMuted = false;
            volumeScalar = 0f;
            deviceState = 0;

            if (!EnsureActiveEndpoint() || _currentVolume == null) return false;

            try
            {
                deviceId = _currentDeviceId;
                _currentDevice.GetState(out deviceState);
                _currentVolume.GetMute(out isMuted);
                _currentVolume.GetMasterVolumeLevelScalar(out volumeScalar);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Reset()
        {
            _currentVolume = null;
            _currentDevice = null;
            _currentDeviceId = null;
            _enumerator = null;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
