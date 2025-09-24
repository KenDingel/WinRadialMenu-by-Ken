# RadialMenu - Professional Windows Radial Navigation System

A futuristic, gesture-based radial menu for Windows that provides instant access to applications, folders, websites, and system functions with a simple hotkey press.

![RadialMenu Demo](demo.gif)

## Features

✨ **Professional Quality Interface**
- Smooth, hardware-accelerated animations with optional energy particle effects
- Modern glass/acrylic effects with customizable themes
- Customizable colors, icons, and visual styling
- Clean, minimalist design with dark/light theme support

🎯 **Gesture-Based Navigation**
- No clicking required - just drag in a direction
- Dead zone in center for easy cancellation
- Nested submenus with smooth transitions
- ESC key or click outside to dismiss

⚡ **Lightning Fast**
- Native Windows performance with .NET 8
- Instant activation with customizable hotkeys (default: Win+`)
- Minimal resource usage with self-contained builds
- Single portable executable option

🔧 **Fully Customizable**
- Advanced settings editor with live preview
- JSON-based configuration with versioned schema
- Support for emojis as icons and custom colors
- Visual menu builder with drag-and-drop editing
- Backup and restore functionality for settings

💬 **Discord Integration**
- Direct navigation to Discord channels and users
- AutoHotkey-powered automation for seamless integration
- Support for voice channels, text channels, and direct messages
- Automatic AutoHotkey version detection and script selection

## Installation

### Option 1: Build from Source

1. **Prerequisites**
   - .NET 8 SDK or later (download from https://dotnet.microsoft.com/download/dotnet/8.0)
   - Visual Studio 2022 or VS Code with C# extension
   - Optional: AutoHotkey for Discord integration (https://www.autohotkey.com/)

2. **Build Steps**
   ```powershell
   # Clone or download the project
   cd RadialMenu
   
   # Build release version
   dotnet build -c Release
   
   # Or create single portable executable (recommended)
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```

3. **Run**
   - Navigate to `bin\Release\net8.0-windows\` for standard build
   - Or `bin\Release\net8.0-windows\win-x64\publish\` for portable build
   - Run `RadialMenu.exe`

### Option 2: Use Pre-built Release
1. Download the latest release from Releases page
2. Extract to any folder
3. Run `RadialMenu.exe`

## Usage

### Basic Operation
1. **Launch**: Run `RadialMenu.exe` (appears in system tray)
2. **Activate**: Press `Win+`` anywhere
3. **Navigate**: Move mouse in direction of desired item
4. **Select**: Release mouse button to execute
5. **Cancel**: Press ESC or move outside the menu

### Navigation Tips
- **Center Dead Zone**: Return to center to deselect
- **Submenus**: Select items with ► to enter submenus
- **Back Navigation**: Press ESC to go back one level
- **Quick Dismiss**: Click anywhere outside the menu

## Configuration

### Using the Advanced Settings Editor
1. Double-click the system tray icon or right-click → Settings
2. Navigate through the comprehensive settings interface:
   - **General**: Startup behavior, tray icon, language, backups
   - **Hotkeys**: Global activation hotkey with conflict detection
   - **Appearance**: UI scale, radii, theme, center text, particle effects
   - **Menu Builder**: Visual drag-and-drop editor with live preview
   - **Advanced**: JSON editor, import/export, diagnostics
3. Changes are applied with live preview before saving
4. Settings are automatically backed up on save

### Manual Configuration
Edit `radialmenu-settings-latest.json` in your settings directory (default: Desktop/RadialMenu/):

```json
{
  "Version": 2,
  "Meta": {
    "ProfileName": "Default",
    "LastModified": "2025-09-24T05:50:15.9778362Z"
  },
  "Hotkeys": {
    "Toggle": "Win+F12"
  },
  "ExternalTools": {
    "AutoHotkeyPath": ""
  },
  "Appearance": {
    "UiScale": 1.5,
    "InnerRadius": 50.0,
    "OuterRadius": 150.0,
    "Theme": "dark",
    "CenterText": "MENU",
    "ParticlesEnabled": true,
    "Colors": {}
  },
  "Menu": [
    {
      "Id": "unique-id-here",
      "Label": "Apps",
      "Icon": "📱",
      "Color": "#FF4CAF50",
      "Action": "",
      "Path": "",
      "Submenu": [
        {
          "Id": "vscode-id",
          "Label": "VS Code",
          "Icon": "📝",
          "Action": "launch",
          "Path": "code"
        }
      ]
    }
  ]
}
```

### Configuration Options

**Action Types:**
- `launch` - Launch an application or open a file/folder
- `url` - Open a website in default browser
- `folder` - Open a folder in Windows Explorer
- `command` - Execute a system command or batch file
- `discord` - Navigate to Discord channel/user (requires AutoHotkey)

**Menu Item Properties:**
- `Id` - Unique identifier (auto-generated)
- `Label` - Display text shown in menu
- `Icon` - Emoji or Unicode symbol
- `Color` - Hex color code (e.g., "#FF4CAF50")
- `Action` - Action type (see above)
- `Path` - Path, URL, or command to execute
- `Submenu` - Array of nested menu items

**Appearance Settings:**
- `UiScale` - Overall interface scaling (0.5-3.0)
- `InnerRadius` - Center dead zone radius
- `OuterRadius` - Menu ring radius
- `Theme` - "dark" or "light"
- `CenterText` - Text displayed in menu center
- `ParticlesEnabled` - Enable energy particle effects
- `Colors` - Custom color overrides

**Hotkey Settings:**
- `Toggle` - Global activation hotkey (e.g., "Win+F12")

**External Tools:**
- `AutoHotkeyPath` - Custom path to AutoHotkey executable

## Examples

### Add a Custom App
```json
{
  "Label": "Photoshop",
  "Icon": "🎨",
  "Action": "launch",
  "Path": "C:\\Program Files\\Adobe\\Photoshop\\Photoshop.exe"
}
```

### Add a Website
```json
{
  "Label": "Reddit",
  "Icon": "📰",
  "Action": "url",
  "Path": "https://reddit.com"
}
```

### Add a Folder Shortcut
```json
{
  "Label": "Projects",
  "Icon": "💼",
  "Action": "folder",
  "Path": "D:\\MyProjects"
}
```

### Add a System Command
```json
{
  "Label": "IP Config",
  "Icon": "🌐",
  "Action": "command",
  "Path": "cmd /k ipconfig"
}
```

### Add Discord Navigation
```json
{
  "Label": "Discord General",
  "Icon": "💬",
  "Action": "discord",
  "Path": "General"
}
```

## Keyboard Shortcuts

- **Customizable Hotkey** - Activate radial menu (default: Win+`)
- **ESC** - Go back / Close menu
- **Mouse Drag** - Select items
- **Mouse Release** - Execute selection

## Troubleshooting

### Menu doesn't appear
- Check if RadialMenu is running (system tray)
- Try running as administrator
- Check if Win+` is used by another app

### Items don't launch
- Verify paths in config.json
- Use full paths for executables not in PATH
- Check if running with sufficient permissions

### Configuration not loading
- Ensure config.json is valid JSON
- Check for syntax errors in Settings editor
- Reset to default if needed

## System Requirements

- Windows 10/11 (64-bit)
- .NET 8 Runtime (included in self-contained builds)
- 10MB disk space for standard build, 50MB for self-contained
- 50MB RAM minimum, 100MB recommended
- Optional: AutoHotkey for Discord integration

## Customization Ideas

### Developer Setup
```json
{
  "Label": "Dev Tools",
  "Icon": "💻",
  "Color": "#FF00BCD4",
  "Submenu": [
    {"Label": "VS Code", "Icon": "📝", "Action": "launch", "Path": "code"},
    {"Label": "Terminal", "Icon": "⌨", "Action": "launch", "Path": "wt"},
    {"Label": "Git Bash", "Icon": "🔀", "Action": "launch", "Path": "git-bash"},
    {"Label": "Postman", "Icon": "📬", "Action": "launch", "Path": "postman"}
  ]
}
```

### Quick Links
```json
{
  "Label": "Quick Access",
  "Icon": "⚡",
  "Color": "#FFFF9800",
  "Submenu": [
    {"Label": "Downloads", "Icon": "⬇", "Action": "folder", "Path": "%USERPROFILE%\\Downloads"},
    {"Label": "Screenshots", "Icon": "📸", "Action": "folder", "Path": "%USERPROFILE%\\Pictures\\Screenshots"},
    {"Label": "Temp", "Icon": "🗑", "Action": "folder", "Path": "%TEMP%"}
  ]
}
```

## Discord Integration

RadialMenu supports direct navigation to Discord channels, users, and servers using AutoHotkey automation.

### Setup
1. Install AutoHotkey from https://www.autohotkey.com/
2. Run `setup-autohotkey.bat` (included) to configure paths
3. Add Discord menu items with `Action: "discord"`

### Usage Examples
```json
{
  "Label": "Discord Channels",
  "Icon": "💬",
  "Color": "#FF7289DA",
  "Submenu": [
    {
      "Label": "General Voice",
      "Icon": "🎤",
      "Action": "discord",
      "Path": "General"
    },
    {
      "Label": "Music Channel",
      "Icon": "🎵",
      "Action": "discord",
      "Path": "music-production"
    },
    {
      "Label": "DM Friend",
      "Icon": "💭",
      "Action": "discord",
      "Path": "@username"
    }
  ]
}
```

### Features
- Automatic AutoHotkey version detection (v1/v2)
- Smart path detection with fallback to system PATH
- Custom AutoHotkey executable path support
- Error handling with helpful user messages

## Building & Distribution

### Standard Build
```powershell
dotnet build -c Release
```
Output: `bin\Release\net8.0-windows\RadialMenu.exe`

### Portable Single Executable (Recommended)
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
Output: `bin\Release\net8.0-windows\win-x64\publish\RadialMenu.exe`

### Automated Build Scripts
- `build.bat` - Standard release build
- `build-portable.bat` - Portable executable with process management

### Distribution Notes
- Self-contained builds include .NET 8 runtime (no separate installation needed)
- Portable builds automatically handle running process termination
- All required files (AutoHotkey scripts, icons) are automatically included

## Contributing

Feel free to customize and enhance the RadialMenu for your needs. Some ideas:
- Add sound effects
- Create custom themes
- Add search functionality
- Implement keyboard navigation
- Add animation preferences

## License

This is a personal project provided as-is for your use and modification.

## Credits

Built with:
- .NET 8 & WPF for modern Windows development
- Hardcodet.NotifyIcon.Wpf for system tray integration
- Newtonsoft.Json for configuration management
- AutoHotkey for Discord automation integration
- Energy particle system for visual effects

Special thanks to the open-source community for the amazing libraries and tools that make this project possible.

---

Enjoy your new futuristic radial menu! Press your custom hotkey to get started. 🚀

For detailed setup instructions, see `QUICK-START.md`.
For Discord integration setup, see `DISCORD-SETUP.md`.
For implementation progress and roadmap, see `IMPLEMENTATION-PROGRESS.md` and `SETTINGS-ROADMAP.md`.
