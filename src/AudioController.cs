using System;
using System.Linq;
using System.Timers;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicControlX
{
    /// <summary>
    /// Information about a microphone device
    /// </summary>
    public class MicrophoneDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
    }

    public class AudioController : IDisposable, IMMNotificationClient
    {
        private readonly MMDeviceEnumerator audioEnumerator = new();
        private readonly System.Timers.Timer stateMonitorTimer;
        private readonly System.Timers.Timer deviceChangeThrottleTimer;
        private bool isMuted = false;
        private bool disposed = false;
        private bool internalChange = false; // Flag to distinguish internal vs external changes
        private bool deviceChangeQueued = false; // Flag to track pending device change

        public bool IsMuted => isMuted;

        public event Action<bool>? MuteStateChanged;
        public event Action<string>? ErrorOccurred;
        public event Action<bool>? ExternalChangeDetected; // New event for external changes
        public event Action? DeviceChanged; // New event for device changes

        public AudioController()
        {
            // Initialize the current state before starting monitoring to avoid false triggers
            InitializeCurrentState();
            
            // Log detected microphones for diagnostics
            LogDetectedMicrophones();
            
            // Initialize monitoring timer to check state every 200ms for better responsiveness
            stateMonitorTimer = new System.Timers.Timer(200);
            stateMonitorTimer.Elapsed += MonitorMicrophoneState;
            stateMonitorTimer.AutoReset = true;
            stateMonitorTimer.Start();

            // Initialize device change throttle timer (500ms delay)
            deviceChangeThrottleTimer = new System.Timers.Timer(500);
            deviceChangeThrottleTimer.Elapsed += OnDeviceChangeThrottleElapsed;
            deviceChangeThrottleTimer.AutoReset = false; // Only fire once per trigger

            // Register for device change notifications
            audioEnumerator.RegisterEndpointNotificationCallback(this);
        }
        
        private void LogDetectedMicrophones()
        {
            try
            {
                var captureDevices = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                Logger.Info($"Detected {captureDevices.Count} active microphone(s):");
                foreach (var device in captureDevices)
                {
                    var muteState = device.AudioEndpointVolume.Mute ? "Muted" : "Active";
                    Logger.Info($"  - {device.FriendlyName} [{muteState}]");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to enumerate microphones", ex);
            }
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
                Logger.Warn($"Initial microphone state detection failed: {ex.Message}");
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
                Logger.Error($"Microphone monitor error: {ex.Message}");
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
                int failedDevices = 0;
                foreach (var device in captureDevices)
                {
                    try
                    {
                        device.AudioEndpointVolume.Mute = newMute;
                        
                        // Verify the mute actually worked
                        if (device.AudioEndpointVolume.Mute != newMute)
                        {
                            failedDevices++;
                            Logger.Warn($"Mute verification failed for: {device.FriendlyName}");
                        }
                    }
                    catch (Exception deviceEx)
                    {
                        failedDevices++;
                        Logger.Error($"Failed to mute device {device.FriendlyName}", deviceEx);
                    }
                }
                
                // Warn if any devices failed to mute
                if (failedDevices > 0)
                {
                    Logger.Warn($"{failedDevices} device(s) failed to mute");
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
                int failedDevices = 0;
                foreach (var device in captureDevices)
                {
                    try
                    {
                        device.AudioEndpointVolume.Mute = mute;
                        
                        // Verify the mute actually worked
                        if (device.AudioEndpointVolume.Mute != mute)
                        {
                            failedDevices++;
                            Logger.Warn($"Mute verification failed for: {device.FriendlyName}");
                        }
                    }
                    catch (Exception deviceEx)
                    {
                        failedDevices++;
                        Logger.Error($"Failed to mute device {device.FriendlyName}", deviceEx);
                    }
                }
                
                // Warn if any devices failed to mute
                if (failedDevices > 0)
                {
                    Logger.Warn($"{failedDevices} device(s) failed to mute");
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

        /// <summary>
        /// Gets the current default microphone device name
        /// </summary>
        /// <returns>Device name or empty string if not available</returns>
        public string GetCurrentMicrophoneDeviceName()
        {
            try
            {
                // Get the actual default capture device, not just the first one
                var defaultDevice = audioEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                if (defaultDevice != null)
                {
                    var deviceName = CleanDeviceName(defaultDevice.FriendlyName);
                    System.Diagnostics.Debug.WriteLine($"[AudioController] Current default microphone: '{deviceName}' (ID: {defaultDevice.ID})");
                    return deviceName;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioController] Get device name failed: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets detailed information about the current microphone device
        /// </summary>
        /// <returns>Device info or null if not available</returns>
        public MicrophoneDeviceInfo? GetCurrentMicrophoneDeviceInfo()
        {
            try
            {
                // Get the actual default capture device, not just the first one
                var defaultDevice = audioEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                if (defaultDevice != null)
                {
                    return new MicrophoneDeviceInfo
                    {
                        Name = CleanDeviceName(defaultDevice.FriendlyName),
                        Id = defaultDevice.ID,
                        State = defaultDevice.State.ToString(),
                        IsMuted = defaultDevice.AudioEndpointVolume.Mute
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioController] Get device info failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cleans up device name by removing common prefixes and suffixes
        /// </summary>
        /// <param name="deviceName">Raw device name</param>
        /// <returns>Cleaned device name</returns>
        private string CleanDeviceName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return string.Empty;

            // Remove common microphone prefixes/suffixes for cleaner display
            var cleaned = deviceName.Trim();
            
            // Remove common patterns
            if (cleaned.StartsWith("Microphone (", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(12); // Remove "Microphone ("
                if (cleaned.EndsWith(")"))
                    cleaned = cleaned.Substring(0, cleaned.Length - 1); // Remove trailing ")"
            }
            else if (cleaned.EndsWith(" Microphone", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 11); // Remove " Microphone"
            }
            
            return cleaned.Trim();
        }

        #region IMMNotificationClient Implementation
        
        public void OnDeviceAdded(string deviceId)
        {
            // Not critical for our use case, but could trigger device update
            System.Diagnostics.Debug.WriteLine($"AudioController: Device added - {deviceId}");
        }

        public void OnDeviceRemoved(string deviceId)
        {
            // Device removed - might need to update display
            System.Diagnostics.Debug.WriteLine($"AudioController: Device removed - {deviceId}");
            TriggerThrottledDeviceChange();
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            // Device state changed (added/removed) - use throttling to prevent spam
            System.Diagnostics.Debug.WriteLine($"[AudioController] Device state changed: {deviceId}, State: {newState}");
            TriggerThrottledDeviceChange(); // Keep throttling for add/remove events
        }

        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role role, string deviceId)
        {
            // This is the key event - default microphone changed
            System.Diagnostics.Debug.WriteLine($"[AudioController] Default device changed: DataFlow={dataFlow}, Role={role}, DeviceId={deviceId}");
            if (dataFlow == DataFlow.Capture && (role == Role.Communications || role == Role.Console))
            {
                System.Diagnostics.Debug.WriteLine("[AudioController] Microphone default device changed - firing immediate update");
                DeviceChanged?.Invoke(); // Immediate update for default device changes
            }
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
        {
            // Property changed - might be device name or other info
            // Make property changes immediate for responsive UI updates
            try
            {
                var device = audioEnumerator.GetDevice(deviceId);
                if (device?.DataFlow == DataFlow.Capture)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioController] Capture device property changed immediately: {deviceId}");
                    
                    // Check if this is the current default device to avoid unnecessary updates
                    var defaultDevice = audioEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                    if (defaultDevice != null && defaultDevice.ID == deviceId)
                    {
                        System.Diagnostics.Debug.WriteLine("[AudioController] Default device property changed - firing immediate update");
                        DeviceChanged?.Invoke(); // Immediate update for property changes
                    }
                }
            }
            catch
            {
                // Device might no longer exist, ignore
            }
        }

        private void TriggerThrottledDeviceChange()
        {
            deviceChangeQueued = true;
            deviceChangeThrottleTimer.Stop();
            deviceChangeThrottleTimer.Start();
        }

        private void OnDeviceChangeThrottleElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (deviceChangeQueued)
            {
                deviceChangeQueued = false;
                System.Diagnostics.Debug.WriteLine("AudioController: Throttled device change - firing DeviceChanged event");
                DeviceChanged?.Invoke();
            }
        }

        #endregion

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                
                // Unregister from device notifications
                try
                {
                    audioEnumerator?.UnregisterEndpointNotificationCallback(this);
                }
                catch
                {
                    // Ignore errors during disposal
                }
                
                stateMonitorTimer?.Stop();
                stateMonitorTimer?.Dispose();
                deviceChangeThrottleTimer?.Stop();
                deviceChangeThrottleTimer?.Dispose();
                audioEnumerator?.Dispose();
            }
        }
    }
}
