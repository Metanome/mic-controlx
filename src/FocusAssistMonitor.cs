using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicControlX
{
    /// <summary>
    /// Monitors Windows Focus Assist (Do Not Disturb) status using WNF API
    /// </summary>
    public class FocusAssistMonitor : IDisposable
    {
        // Windows Notification Facility (WNF) API for real-time Focus Assist detection
        // Based on Microsoft documentation: https://learn.microsoft.com/en-us/answers/questions/2182613/
        
        [StructLayout(LayoutKind.Sequential)]
        public struct WNF_STATE_NAME
        {
            public uint Data1;
            public uint Data2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_ASSIST_DATA
        {
            public int FocusAssist;
        }

        private delegate int WNF_USER_CALLBACK(
            WNF_STATE_NAME StateName,
            uint ChangeStamp,
            IntPtr TypeId,
            IntPtr CallbackContext,
            IntPtr Buffer,
            uint Length);

        // WNF state name for Focus Assist changes (from Microsoft documentation)
        private static readonly WNF_STATE_NAME WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED = new WNF_STATE_NAME
        {
            Data1 = 0xA3BF1C75,
            Data2 = 0xD83063E
        };

        [DllImport("ntdll.dll")]
        private static extern int RtlQueryWnfStateData(
            out uint ChangeStamp,
            WNF_STATE_NAME StateName,
            WNF_USER_CALLBACK Callback,
            IntPtr CallbackContext,
            IntPtr TypeId);

        [DllImport("ntdll.dll")]
        private static extern int RtlSubscribeWnfStateChangeNotification(
            out IntPtr SubscriptionHandle,
            WNF_STATE_NAME StateName,
            uint ChangeStamp,
            WNF_USER_CALLBACK Callback,
            IntPtr CallbackContext,
            IntPtr TypeId,
            uint SerializationGroup,
            uint Flags);

        [DllImport("ntdll.dll")]
        private static extern int RtlUnsubscribeWnfStateChangeNotification(
            IntPtr SubscriptionHandle);

        private bool _disposed = false;
        private bool _isMonitoring = false;
        private IntPtr _wnfSubscription = IntPtr.Zero;
        private WNF_USER_CALLBACK? _wnfCallback;
        
        /// <summary>
        /// Event fired when Focus Assist status changes
        /// </summary>
        public event EventHandler<FocusAssistStatusChangedEventArgs>? StatusChanged;
        
        /// <summary>
        /// Gets the current Focus Assist status
        /// </summary>
        public FocusAssistStatus CurrentStatus { get; private set; } = FocusAssistStatus.Off;
        
        /// <summary>
        /// Gets whether the monitor is currently active
        /// </summary>
        public bool IsMonitoring => _isMonitoring;
        
        /// <summary>
        /// Starts monitoring Focus Assist status changes
        /// </summary>
        public void StartMonitoring()
        {
            if (_disposed || _isMonitoring)
                return;
                
            try
            {
                _isMonitoring = true;
                
                // Initialize WNF subscription asynchronously without blocking
                _ = Task.Run(async () => await InitializeWNFSubscriptionAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FocusAssist] Failed to start monitoring: {ex.Message}");
                CurrentStatus = FocusAssistStatus.Off;
            }
        }
        
        /// <summary>
        /// Stops monitoring Focus Assist status changes
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;
                
            _isMonitoring = false;
        }
        
        /// <summary>
        /// Manually checks the current Focus Assist status
        /// </summary>
        /// <returns>Current Focus Assist status</returns>
        public FocusAssistStatus CheckCurrentStatus()
        {
            // Return the current cached status - real updates come via WNF callbacks
            return CurrentStatus;
        }
        
        /// <summary>
        /// Forces a refresh of Focus Assist status (non-blocking)
        /// </summary>
        public void RefreshStatusAsync()
        {
            if (!_isMonitoring || _disposed)
                return;
                
            _ = Task.Run(async () => 
            {
                try
                {
                    await CheckFocusAssistViaWNFAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FocusAssist] Async refresh failed: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Gets the current Focus Assist status using WNF API
        /// </summary>
        private FocusAssistStatus GetCurrentFocusAssistStatus()
        {
            try
            {
                // Return cached status to avoid blocking calls
                // The WNF subscription will update CurrentStatus in real-time
                return CurrentStatus;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FocusAssist] Error getting focus assist status: {ex.Message}");
                return FocusAssistStatus.Off;
            }
        }

        /// <summary>
        /// Initializes WNF subscription for Focus Assist monitoring
        /// </summary>
        private async Task InitializeWNFSubscriptionAsync()
        {
            try
            {
                await CheckFocusAssistViaWNFAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FocusAssist] WNF initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks Focus Assist status using Windows Notification Facility (WNF) API
        /// This is the most reliable method as it's the official Windows approach
        /// Based on Microsoft documentation: https://learn.microsoft.com/en-us/answers/questions/2182613/
        /// </summary>
        private async Task<FocusAssistStatus?> CheckFocusAssistViaWNFAsync()
        {
            try
            {
                // Create a callback that will receive the Focus Assist data
                _wnfCallback = (stateName, changeStamp, typeId, callbackContext, buffer, length) =>
                {
                    try
                    {
                        if (buffer != IntPtr.Zero && length >= Marshal.SizeOf<FOCUS_ASSIST_DATA>())
                        {
                            var focusAssistData = Marshal.PtrToStructure<FOCUS_ASSIST_DATA>(buffer);
                            
                            // Update current status based on WNF data
                            var newStatus = focusAssistData.FocusAssist switch
                            {
                                0 => FocusAssistStatus.Off,
                                1 => FocusAssistStatus.PriorityOnly,
                                2 => FocusAssistStatus.AlarmsOnly,
                                _ => FocusAssistStatus.Off
                            };
                            
                            if (newStatus != CurrentStatus)
                            {
                                var previousStatus = CurrentStatus;
                                CurrentStatus = newStatus;
                                StatusChanged?.Invoke(this, new FocusAssistStatusChangedEventArgs(previousStatus, newStatus));
                            }
                        }
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[FocusAssist] WNF callback error: {ex.Message}");
                        return -1;
                    }
                };
                
                // Query current state using WNF API
                int result = RtlQueryWnfStateData(
                    out uint changeStamp,
                    WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED,
                    _wnfCallback,
                    IntPtr.Zero,
                    IntPtr.Zero);
                
                if (result == 0)
                {
                    // Give the callback a minimal moment to execute (reduced from 100ms to 50ms)
                    await Task.Delay(50);
                    
                    // Subscribe to future changes if we don't have a subscription yet
                    if (_wnfSubscription == IntPtr.Zero)
                    {
                        int subscribeResult = RtlSubscribeWnfStateChangeNotification(
                            out _wnfSubscription,
                            WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED,
                            changeStamp,
                            _wnfCallback,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            0,
                            0);
                        
                        if (subscribeResult == 0)
                        {
                            // Successfully subscribed to WNF Focus Assist changes
                        }
                        else
                        {
                            Debug.WriteLine($"[FocusAssist] WNF subscription failed with result: 0x{subscribeResult:X}");
                        }
                    }
                    
                    return CurrentStatus;
                }
                else
                {
                    Debug.WriteLine($"[FocusAssist] WNF query failed with result: 0x{result:X}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FocusAssist] WNF detection failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Determines if OSD should be suppressed based on current Focus Assist status
        /// </summary>
        /// <param name="respectFocusAssist">Whether to respect Focus Assist settings</param>
        /// <returns>True if OSD should be suppressed, false otherwise</returns>
        public bool ShouldSuppressOSD(bool respectFocusAssist)
        {
            if (!respectFocusAssist)
                return false;
                
            // Suppress OSD when Focus Assist is in Priority Only or Alarms Only mode
            var shouldSuppress = CurrentStatus == FocusAssistStatus.PriorityOnly || CurrentStatus == FocusAssistStatus.AlarmsOnly;
            
            return shouldSuppress;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoring();
                
                // Cleanup WNF subscription if we have one
                if (_wnfSubscription != IntPtr.Zero)
                {
                    try
                    {
                        int result = RtlUnsubscribeWnfStateChangeNotification(_wnfSubscription);
                        if (result == 0)
                        {
                            Debug.WriteLine("[FocusAssist] Successfully unsubscribed from WNF notifications");
                        }
                        else
                        {
                            Debug.WriteLine($"[FocusAssist] WNF unsubscribe failed with result: 0x{result:X}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[FocusAssist] Error during WNF cleanup: {ex.Message}");
                    }
                    finally
                    {
                        _wnfSubscription = IntPtr.Zero;
                        _wnfCallback = null;
                    }
                }
                
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// Windows Focus Assist status enumeration
    /// </summary>
    public enum FocusAssistStatus
    {
        /// <summary>
        /// Focus Assist is turned off - all notifications are allowed
        /// </summary>
        Off = 0,
        
        /// <summary>
        /// Focus Assist is in Priority Only mode - only priority notifications are allowed
        /// </summary>
        PriorityOnly = 1,
        
        /// <summary>
        /// Focus Assist is in Alarms Only mode - only alarms are allowed
        /// </summary>
        AlarmsOnly = 2
    }
    
    /// <summary>
    /// Event arguments for Focus Assist status changes
    /// </summary>
    public class FocusAssistStatusChangedEventArgs : EventArgs
    {
        public FocusAssistStatus PreviousStatus { get; }
        public FocusAssistStatus NewStatus { get; }
        
        public FocusAssistStatusChangedEventArgs(FocusAssistStatus previousStatus, FocusAssistStatus newStatus)
        {
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
        }
    }
}