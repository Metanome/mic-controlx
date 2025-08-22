# MicControlX

**Advanced Microphone Control Utility for Windows with Lenovo Gaming Laptop Optimization**

[![Version](https://img.shields.io/badge/version-3.1.1--gamma-blue.svg)](https://github.com/Metanome/mic-controlx)
[![License](https://img.shields.io/badge/license-GPL--3.0-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/Metanome/mic-controlx)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-purple.svg)](https://github.com/Metanome/mic-controlx)

## Overview

MicControlX is a powerful Windows application designed to provide seamless microphone control with special optimizations for Lenovo gaming laptops (Legion, LOQ, IdeaPad series). It offers instant mute/unmute functionality through customizable hotkeys, system tray integration, and visual feedback that matches Lenovo's design language.

### Key Features

- **Lenovo Gaming Laptop Optimization**: Native integration with Legion Toolkit and Lenovo Vantage
- **Instant Hotkey Control**: Customizable function key hotkeys (F1-F24) for quick mute/unmute
- **Smart Audio Management**: Automatic detection and control of default microphone devices
- **Visual OSD Overlays**: Multiple styles including Lenovo Vantage and Legion Toolkit themes
- **System Tray Integration**: Persistent background operation with tray icon status
- **Audio Feedback**: Optional sound notifications for mute state changes
- **Modern UI**: WPF-based interface with dark/light theme support
- **Single Instance**: Prevents multiple instances from running simultaneously
- **Auto-startup**: Optional Windows startup integration
- **Real-time Monitoring**: Detects external microphone state changes

## System Requirements

- **Operating System**: Windows 10/11 (x64)
- **Framework**: .NET 8.0 Runtime
- **Hardware**: Any Windows-compatible microphone
- **Optimized for**: Lenovo Legion, LOQ, and IdeaPad gaming laptops

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

### OSD Styles
Choose from three visual overlay styles:
- **Windows Default**: Clean, universal design
- **Lenovo Style**: Matches Lenovo Vantage notifications
- **Legion Style**: Dark theme matching Legion Toolkit notifications

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
  "OSDStyle": 1,
  "Theme": 0,
  "AutoStart": false,
  "EnableSoundFeedback": false
}
```

### Available Options
- **HotKeyVirtualKey**: Function key codes (F1=112, F2=113, ..., F24=135)
- **HotKeyDisplayName**: Name of the hotkey for display in the UI
- **ShowOSD**: Enable/disable visual overlays
- **ShowNotifications**: Enable/disable system tray notifications
- **OSDStyle**: `WindowsDefault (0)`, `LenovoStyle (1)`, or `LegionStyle (2)`
- **Theme**: `System (0)`, `Dark (1)`, or `Light (2)`
- **AutoStart**: Launch with Windows
- **EnableSoundFeedback**: Play sounds on mute/unmute

### Features
- **Lenovo Vantage and LLT Software Compatibility**: Coexists with Lenovo's software microphone controls
- **Hardware Fn Key Support**: Works alongside Lenovo's hardware Fn+F4 mute/unmute functionality

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
- **System.Management**: Hardware detection

### Building
The project targets **.NET 8.0-windows** and produces a single-file executable for easy distribution.

## Contributing

Contributions are welcome! Please feel free to:
- Report bugs and request features via [Issues](https://github.com/Metanome/mic-controlx/issues)
- Submit pull requests for improvements
- Share feedback about Lenovo laptop compatibility

## Acknowledgments

- **NAudio**: Professional audio library for .NET
- **WPF-UI**: Modern Fluent Design components
- **Lenovo Discord Community**: Feedback and testing support
- **Legion Toolkit**: Inspiration for Lenovo-specific features

## Support

- **Issues**: [GitHub Issues](https://github.com/Metanome/mic-controlx/issues)
- **Documentation**: This README and inline code comments
- **Updates**: Automatic checking available in application

---

**Made with ❤️ for the Windows and Lenovo gaming community**