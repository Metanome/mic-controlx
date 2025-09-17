using System;
using System.Linq;
using System.Timers;
using NAudio.CoreAudioApi;

namespace MicControlX
{
    public class AudioController : IDisposable
    {
        private readonly MMDeviceEnumerator audioEnumerator = new();
        private readonly System.Timers.Timer stateMonitorTimer;
        private bool isMuted = false;
        private bool disposed = false;
        private bool internalChange = false; // Flag to distinguish internal vs external changes

        public bool IsMuted => isMuted;

        public event Action<bool>? MuteStateChanged;
        public event Action<string>? ErrorOccurred;
        public event Action<bool>? ExternalChangeDetected; // New event for external changes

        public AudioController()
        {
            // Initialize the current state before starting monitoring to avoid false triggers
            InitializeCurrentState();
            
            // Initialize monitoring timer to check state every 200ms for better responsiveness
            stateMonitorTimer = new System.Timers.Timer(200);
            stateMonitorTimer.Elapsed += MonitorMicrophoneState;
            stateMonitorTimer.AutoReset = true;
            stateMonitorTimer.Start();
        }

        private void InitializeCurrentState()
        {
            try
            {
                // Get the actual current state without triggering events
                var captureDevices = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                
                if (captureDevices.Any())
                {
                    var defaultDevice = captureDevices.First();
                    isMuted = defaultDevice.AudioEndpointVolume.Mute;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial state detection failed: {ex.Message}");
                // Default to false if we can't detect
                isMuted = false;
            }
        }

        private void MonitorMicrophoneState(object? sender, ElapsedEventArgs e)
        {
            if (disposed) return;

            try
            {
                // Get all active capture devices (microphones)
                var captureDevices = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                
                if (!captureDevices.Any())
                    return;

                // Check the mute state of the first device (usually the default)
                var defaultDevice = captureDevices.First();
                bool actualMuteState = defaultDevice.AudioEndpointVolume.Mute;
                
                // If the state has changed externally, update our internal state and notify
                if (actualMuteState != isMuted)
                {
                    isMuted = actualMuteState;
                    
                    // Check if this was an external change (not triggered by our ToggleMute)
                    if (!internalChange)
                    {
                        ExternalChangeDetected?.Invoke(isMuted);
                    }
                    
                    MuteStateChanged?.Invoke(isMuted);
                    
                    // Reset the internal change flag
                    internalChange = false;
                }
            }
            catch (Exception ex)
            {
                // Don't spam error messages during monitoring
                System.Diagnostics.Debug.WriteLine($"Monitor Error: {ex.Message}");
            }
        }

        public bool ToggleMute()
        {
            try
            {
                // Mark this as an internal change
                internalChange = true;
                
                // Get all active capture devices (microphones)
                var captureDevices = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                
                if (!captureDevices.Any())
                {
                    internalChange = false;
                    ErrorOccurred?.Invoke(Strings.NoActiveMicrophones);
                    return false;
                }

                // Get current mute state from the first device (usually the default)
                var defaultDevice = captureDevices.First();
                bool currentMute = defaultDevice.AudioEndpointVolume.Mute;
                
                // Toggle mute state for all capture devices
                bool newMute = !currentMute;
                foreach (var device in captureDevices)
                {
                    device.AudioEndpointVolume.Mute = newMute;
                }
                
                // Update our state
                isMuted = newMute;
                MuteStateChanged?.Invoke(isMuted);
                
                return true;
            }
            catch (Exception ex)
            {
                internalChange = false;
                ErrorOccurred?.Invoke($"{Strings.NAudioError}: {ex.Message}");
                return false;
            }
        }

        public bool SetMuted(bool mute)
        {
            try
            {
                // Mark this as an internal change
                internalChange = true;
                
                // Get all active capture devices (microphones)
                var captureDevices = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                
                if (!captureDevices.Any())
                {
                    internalChange = false;
                    ErrorOccurred?.Invoke(Strings.NoActiveMicrophones);
                    return false;
                }

                // Set mute state for all capture devices
                foreach (var device in captureDevices)
                {
                    device.AudioEndpointVolume.Mute = mute;
                }
                
                // Update our state
                isMuted = mute;
                MuteStateChanged?.Invoke(isMuted);
                
                return true;
            }
            catch (Exception ex)
            {
                internalChange = false;
                ErrorOccurred?.Invoke($"{Strings.NAudioError}: {ex.Message}");
                return false;
            }
        }

        public bool GetCurrentMuteState()
        {
            try
            {
                // Get all active capture devices (microphones)
                var captureDevices = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                
                if (!captureDevices.Any())
                    return false;

                // Check the mute state of the first device (usually the default)
                var defaultDevice = captureDevices.First();
                isMuted = defaultDevice.AudioEndpointVolume.Mute;
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"{Strings.StateCheckError}: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                stateMonitorTimer?.Stop();
                stateMonitorTimer?.Dispose();
                audioEnumerator?.Dispose();
            }
        }
    }
}
