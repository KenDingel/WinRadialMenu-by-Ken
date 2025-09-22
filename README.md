# RadialMenu - Professional Windows Radial Navigation System

A futuristic, gesture-based radial menu for Windows that provides instant access to applications, folders, websites, and system functions with a simple hotkey press.

![RadialMenu Demo](demo.gif)

## Features

✨ **Professional Quality Interface**
- Smooth, hardware-accelerated animations
- Modern glass/acrylic effects
- Customizable colors and icons
- Clean, minimalist design

🎯 **Gesture-Based Navigation**
- No clicking required - just drag in a direction
- Dead zone in center for easy cancellation
- Nested submenus with smooth transitions
- ESC key or click outside to dismiss

⚡ **Lightning Fast**
- Native Windows performance
- Instant activation with Win+`
- Minimal resource usage
- Single portable executable

🔧 **Fully Customizable**
- JSON-based configuration
- Easy-to-use settings editor
- Support for emojis as icons
- Custom colors for each category

## Installation

### Option 1: Build from Source

1. **Prerequisites**
   - .NET 8 SDK or later
   - Visual Studio 2022 or VS Code with C# extension

2. **Build Steps**
   ```powershell
   # Clone or download the project
   cd RadialMenu
   
   # Build release version
   dotnet build -c Release
   
   # Or create single portable executable
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```

3. **Run**
   - Navigate to `bin\Release\net8.0-windows\` or `bin\Release\net8.0-windows\win-x64\publish\`
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

### Using the Settings Editor
1. Double-click the system tray icon
2. Or right-click tray icon → Settings
3. Edit the JSON configuration
4. Click Save to apply changes

### Manual Configuration
Edit `config.json` in the application directory:

```json
{
  "Items": [
    {
      "Label": "Apps",
      "Icon": "📱",
      "Color": "#FF4CAF50",
      "Submenu": [
        {
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
- `launch` - Launch an application
- `url` - Open a website
- `folder` - Open a folder in Explorer
- `command` - Execute a system command

**Item Properties:**
- `Label` - Display text
- `Icon` - Emoji or text icon
- `Color` - Hex color code (e.g., "#FF4CAF50")
- `Action` - Action type (see above)
- `Path` - Path or URL to execute
- `Submenu` - Array of child items

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

## Keyboard Shortcuts

- **Win+`** - Activate radial menu
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

- Windows 10/11
- .NET 8 Runtime (included in self-contained build)
- 10MB disk space
- 50MB RAM

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

## Tips & Tricks

1. **Organize by Frequency**: Put most-used items in easy directions (up, right, down, left)
2. **Use Color Coding**: Assign similar colors to related categories
3. **Emoji Icons**: Use Windows+Period to insert emojis in config
4. **Nested Organization**: Group related items in submenus to reduce clutter
5. **Quick Actions**: Put system commands like Lock/Sleep at root level for quick access

## Building & Distribution

### Create Portable EXE
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

### Create Installer (optional)
Use WiX Toolset or Inno Setup with the published files

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
- .NET 8 & WPF
- Hardcodet.NotifyIcon.Wpf for system tray
- Newtonsoft.Json for configuration

---

Enjoy your new futuristic radial menu! Press Win+` to get started. 🚀

For a full Settings redesign roadmap and implementation plan, see `SETTINGS-ROADMAP.md`.
