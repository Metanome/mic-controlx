# MicControlX

Advanced microphone control application for Windows with Lenovo gaming laptop optimization.

## Features

- Global hotkey microphone toggle (F1-F24 keys)
- System tray integration with status icons
- Lenovo gaming laptop optimization (Legion, LOQ, IdeaPad)
- Multiple OSD overlay styles matching Lenovo software
- Auto-start with Windows support
- Sound feedback on toggle
- Real-time hotkey switching without restart
- Portable configuration support

## System Requirements

- Windows OS (32/64-bit)
- .NET 8.0 Runtime
- 15MB RAM, 8MB disk space

## Installation

1. Download MicControlX.exe from the latest release
2. Place the executable in your preferred folder
3. Run the application - it will appear in the system tray
4. Right-click the tray icon to access settings

## Usage

- Press your configured hotkey (default: F11) to toggle microphone
- Right-click system tray icon for settings
- Hover over tray icon to see current microphone status

## Lenovo Gaming Laptop Features

MicControlX automatically detects Lenovo systems and provides enhanced features:

- **Legion Series**: Matches Legion Toolkit OSD style when installed
- **LOQ/IdeaPad**: Uses Lenovo Vantage style when available
- **All Lenovo**: Enhanced system integration and compatibility

Compatible with Lenovo Vantage, Legion Toolkit, and hardware Fn keys.

## Configuration Options

### Hotkeys
- Select any F-key (F1 through F24)
- No modifier keys for maximum reliability
- Changes apply immediately

### Display
- Choose between Windows Default, Lenovo Vantage, or Legion Toolkit OSD styles
- OSD appears bottom-center above taskbar
- Option to use system notifications instead

### Application
- Auto-start with Windows
- Sound feedback on microphone toggle
- Dark/Light/System theme selection

## Technical Information

- Built with .NET 8.0 and WinForms
- Uses NAudio library for audio control
- Single-instance application with mutex enforcement
- No admin rights required for basic operation
- Portable configuration when possible

## Building from Source

Requirements:
- .NET 8.0 SDK
- Windows development environment

Commands:
```
git clone https://github.com/Metanome/mic-controlx.git
cd mic-controlx
dotnet build --configuration Release
```

## Troubleshooting

**Hotkey not working:**
- Try a different F-key in settings
- Check for conflicts with other applications
- Restart the application

**Microphone not responding:**
- Verify Windows audio drivers are installed
- Check Windows sound settings
- Ensure microphone permissions are enabled

**Lenovo features unavailable:**
- Confirm you have a Lenovo system
- Install Lenovo Vantage or Legion Toolkit if desired
- Restart MicControlX after installing Lenovo software
