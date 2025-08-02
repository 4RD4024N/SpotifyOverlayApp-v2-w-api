using NAudio.CoreAudioApi;

namespace SpotifyOverlay
{
    public static class VolumeManager
    {
        public static int GetMasterVolume()
        {
            try
            {
                using var deviceEnumerator = new MMDeviceEnumerator();
                var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            catch
            {
                return -1;
            }
        }
    }
}
