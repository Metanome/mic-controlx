# MicControlX

**Advanced Universal Microphone Control Utility for Windows**

[![Version](https://img.shields.io/badge/version-4.1.0-blue.svg)](https://github.com/Metanome/mic-controlx)
[![Downloads](https://img.shields.io/github/downloads/Metanome/mic-controlx/total?color=brightgreen)](https://github.com/Metanome/mic-controlx/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://github.com/Metanome/mic-controlx)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-purple.svg)](https://github.com/Metanome/mic-controlx)

## Overview

MicControlX is a universal Windows application for microphone control. It provides instant mute/unmute functionality through global hotkeys, system tray integration, and visual feedback.

## Key Features

- **Universal Compatibility**: Works with any Windows-compatible microphone and audio hardware
- **Multi-Language Support**: English and Turkish with automatic system language detection
- **Global Hotkeys**: System-wide microphone toggle with intelligent conflict detection
- **Push-to-Talk Mode**: Hold hotkey for temporary mute/unmute, release to restore original state
- **Visual Feedback**: Multiple OSD notification styles
- **System Integration**: Windows startup support and system tray functionality  
- **Modern UI**: Fluent Design with dark/light theme support
- **Audio Notifications**: Optional sound feedback for mute/unmute actions
- **Smart Error Handling**: Helpful guidance when settings conflicts occur
- **Single Instance**: Prevents multiple instances from running simultaneously
- **Real-time Monitoring**: Detects external microphone state changes

## System Requirements

- **Operating System**: Windows 10/11 (x64)
- **Framework**: .NET 8.0 Runtime
- **Hardware**: Any Windows-compatible microphone

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
- **Quick Toggle**: Press your configured hotkey (default: F11) to permanently toggle mute/unmute
- **Push-to-Talk**: Hold your configured hotkey for temporary mute/unmute - releases back to original state when you let go
- **Settings**: Right-click the tray icon or click the Settings button in the main window
- **Exit**: Right-click tray icon → Exit, or close the main window

### Push-to-Talk Feature
MicControlX includes an intelligent dual-mode hotkey system:

- **Quick Press** (under 400ms): Permanently toggles mute/unmute state
- **Hold** (400ms or longer): Temporarily changes mute state while held
  
This is perfect for:
- **Quick meetings**: Hold to unmute briefly, then auto-mute when done speaking
- **Gaming**: Hold to talk to team, release to return to muted state
- **Streaming**: Temporarily unmute for audience interaction without forgetting to mute again

### OSD Styles
Choose from three notification styles:
- **Default Style**: Standard MicControlX notification style
- **Lenovo Vantage Style**: Alternative style option
- **Lenovo Legion Toolkit Style**: Gaming-focused notification style

## Architecture

### Core Components

- **`HotkeyManager.cs`**: Global hotkey handling
- **`AudioController.cs`**: Microphone control and monitoring
- **`LocalizationManager.cs`**: Multi-language support
- **`MainWindow.xaml`**: Primary interface
- **`ApplicationConfig.cs`**: Configuration management and persistence
- **`OsdOverlay.xaml`**: Visual notifications
- **`ThemeManager.cs`**: Dark/light theme management
- **`SoundFeedback.cs`**: Audio notification system
- **`GitHubUpdateChecker.cs`**: Automatic update checking

### Key Technologies
- **WPF**: Modern Windows interface
- **NAudio**: Audio device management
- **System Tray Integration**: Background operation
- **Global Hotkeys**: System-wide keyboard shortcuts
- **JSON Configuration**: Settings persistence

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
  "EnableSoundFeedback": false,
  "Language": "auto"
}
```

### Available Options
- **HotKeyVirtualKey**: Function key codes (F1=0x70, F2=0x71, ..., F24=0x87)
- **HotKeyDisplayName**: Name of the hotkey for display in the UI
- **ShowOSD**: Enable/disable visual overlays
- **ShowNotifications**: Enable/disable system tray notifications
- **OSDStyle**: `DefaultStyle (0)`, `VantageStyle (1)`, or `LLTStyle (2)`
- **Theme**: `System (0)`, `Dark (1)`, or `Light (2)`
- **Language**: `"auto"` (system default), `"en"` (English), or `"tr"` (Turkish)
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