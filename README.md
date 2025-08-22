# MicControlX

**Advanced Universal Microphone Control Utility for Windows**

[![Version](https://img.shields.io/badge/version-3.1.1--gamma-blue.svg)](https://github.com/Metanome/mic-controlx)
[![License](https://img.shields.io/badge/license-GPL--3.0-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/Metanome/mic-controlx)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-purple.svg)](https://github.com/Metanome/mic-controlx)

## Overview

MicControlX is a powerful universal Windows application designed to provide seamless microphone control for any Windows system. It offers instant mute/unmute functionality through customizable hotkeys, system tray integration, and visual feedback with multiple aesthetic styles inspired by popular software designs.

### Key Features

## Key Features

- **Universal Compatibility**: Works with any Windows-compatible microphone and audio hardware
- **Global Hotkeys**: System-wide microphone toggle with intelligent conflict detection
- **Visual Feedback**: Multiple OSD styles with aesthetic themes
- **System Integration**: Windows startup support and system tray functionality  
- **Modern UI**: Fluent Design with dark/light theme support
- **Audio Notifications**: Optional sound feedback for mute/unmute actions
- **Smart Error Handling**: Helpful guidance when settings conflicts occur
- **Single Instance**: Prevents multiple instances from running simultaneously
- **Auto-startup**: Optional Windows startup integration
- **Real-time Monitoring**: Detects external microphone state changes

## System Requirements

- **Operating System**: Windows 10/11 (x64)
- **Framework**: .NET 8.0 Runtime
- **Hardware**: Any Windows-compatible microphone
- **Compatibility**: Universal - works with all Windows systems and audio hardware

## Installation

### Quick Start
1. Download the latest stable release from the [Releases](https://github.com/Metanome/mic-controlx/releases) page
2. Run the application
3. Configure your preferred hotkey in Settings
4. Enjoy!

### Building from Source
```bash
# Clone the repository
git clone https://github.com/Metanome/mic-controlx.git
cd mic-controlx

# Build using the provided script
build-standalone.bat

# Or build manually with .NET CLI
dotnet publish src\MicControlX.csproj --configuration Release --runtime win-x64 --output publish
```

## Usage

### Basic Operations
- **Launch**: Run `MicControlX.exe` - the application will appear in the system tray and show the main window
- **Mute/Unmute**: Press your configured hotkey (default: F11)
- **Settings**: Right-click the tray icon or click the Settings button in the main window
- **Exit**: Right-click tray icon → Exit, or close the main window

### Hotkey Configuration
1. Open Settings window
2. Select your preferred function key (F1-F24)
3. Click OK to save the new configuration
4. The new hotkey is immediately active

**Note**: Some keys like F8 and F12 may conflict with system functions or other applications but that depends on your system's configuration. The app will notify you if a hotkey fails to register and suggest alternatives.

### OSD Styles
Choose from three visual overlay styles:
- **Windows Default**: Clean, universal design suitable for all systems
- **Lenovo Vantage Style**: Layered icon design inspired by Lenovo Vantage aesthetics
- **Lenovo Legion Toolkit Style**: Dark theme design inspired by Legion Toolkit aesthetics

## Architecture

### Core Components

- **`AudioController.cs`**: NAudio-based microphone control and monitoring
- **`MainWindow.xaml`**: Primary WPF interface with Fluent UI styling
- **`ApplicationConfig.cs`**: Configuration management and persistence
- **`OsdOverlay.xaml`**: Visual feedback overlay system
- **`ThemeManager.cs`**: Dark/light theme management
- **`SoundFeedback.cs`**: Audio notification system
- **`GitHubUpdateChecker.cs`**: Automatic update checking

### Key Technologies
- **WPF with Fluent UI**: Modern Windows interface design
- **NAudio**: Professional audio device management
- **System Tray Integration**: Persistent background operation
- **Global Hotkeys**: System-wide keyboard capture
- **JSON Configuration**: Lightweight settings persistence

## Configuration

Settings are automatically saved to `%AppData%\MicControlX\config.json`:

```json
{
  "HotKeyVirtualKey": 122,
  "HotKeyDisplayName": "F11",
  "ShowOSD": true,
  "ShowNotifications": false,
  "OSDStyle": 0,
  "Theme": 0,
  "AutoStart": false,
  "EnableSoundFeedback": false
}
```

### Available Options
- **HotKeyVirtualKey**: Function key codes (F1=0x70, F2=0x71, ..., F24=0x87)
- **HotKeyDisplayName**: Name of the hotkey for display in the UI
- **ShowOSD**: Enable/disable visual overlays
- **ShowNotifications**: Enable/disable system tray notifications
- **OSDStyle**: `WindowsDefault (0)`, `VantageStyle (1)`, or `LLTStyle (2)`
- **Theme**: `System (0)`, `Dark (1)`, or `Light (2)`
- **AutoStart**: Launch with Windows
- **EnableSoundFeedback**: Play sounds on mute/unmute

## Development

### Project Structure
```
mic-controlx/
├── src/                    # Source code
│   ├── *.xaml             # WPF user interfaces
│   ├── *.xaml.cs          # UI code-behind
│   ├── *.cs               # Core application logic
│   └── MicControlX.csproj # Project file
├── assets/                # Resources
│   ├── icons/             # Application icons
│   └── sounds/            # Audio feedback files
├── build-standalone.bat   # Build script
├── MicControlX.sln       # Visual Studio solution
└── LICENSE               # GPL v3 license
```

### Dependencies
- **NAudio**: Audio device management
- **WPF-UI**: Modern Fluent Design components
- **Hardcodet.NotifyIcon.Wpf**: System tray functionality
- **System.Management**: System information detection

### Building
The project targets **.NET 8.0-windows** and produces a single-file executable for easy distribution.

## Contributing

Contributions are welcome! Please feel free to:
- Report bugs and request features via [Issues](https://github.com/Metanome/mic-controlx/issues)
- Submit pull requests for improvements
- Share feedback about compatibility with different systems

## Acknowledgments

- **NAudio**: Professional audio library for .NET
- **WPF-UI**: Modern Fluent Design components
- **Community Contributors**: Feedback and testing support
- **Design Inspirations**: Lenovo Vantage and Legion Toolkit for visual style inspiration

## Support

- **Issues**: [GitHub Issues](https://github.com/Metanome/mic-controlx/issues)
- **Documentation**: This README and inline code comments
- **Updates**: Automatic checking available in application

---

**Made with ❤️ for the Windows community**