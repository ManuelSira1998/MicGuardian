using System;

namespace MicGuardian.CoreAudio
{
    /// <summary>
    /// Constantes nativas utilizadas por la API Windows CoreAudio.
    /// </summary>
    public static class AudioConstants
    {
        public const int CLSCTX_INPROC_SERVER = 0x1;
        public const int CLSCTX_INPROC_HANDLER = 0x2;
        public const int CLSCTX_LOCAL_SERVER = 0x4;
        public const int CLSCTX_REMOTE_SERVER = 0x10;
        public const int CLSCTX_ALL = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER; // 23 (0x17)

        // EDataFlow: 0 = eRender (reproducción), 1 = eCapture (micrófonos/grabación), 2 = eAll
        public const int eRender = 0;
        public const int eCapture = 1;
        public const int eAll = 2;

        // ERole: 0 = eConsole (juegos, sistema), 1 = eMultimedia, 2 = eCommunications (llamadas)
        public const int eConsole = 0;
        public const int eMultimedia = 1;
        public const int eCommunications = 2;

        // Estados de extremo de audio (DeviceState)
        public const int DEVICE_STATE_ACTIVE = 0x00000001;
        public const int DEVICE_STATE_DISABLED = 0x00000002;
        public const int DEVICE_STATE_NOTPRESENT = 0x00000004;
        public const int DEVICE_STATE_UNPLUGGED = 0x00000008;

        public static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        public static readonly Guid IID_IMMDeviceEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
        public static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
    }
}
