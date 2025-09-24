# Discord Integration Setup Guide

The RadialMenu now supports direct Discord channel navigation using AutoHotkey. This feature allows you to quickly jump to Discord voice channels, text channels, or direct messages directly from your radial menu.

## Prerequisites

1. **AutoHotkey Installation**: Download and install AutoHotkey from https://www.autohotkey.com/
2. **Discord Application**: Make sure Discord is installed and running on your system
3. **DiscordNav.ahk Script**: The script is automatically included when you build the application

## AutoHotkey Path Configuration

The RadialMenu will automatically try to find your AutoHotkey installation in these locations:
- `C:\Program Files\AutoHotkey\AutoHotkey.exe`
- `C:\Program Files (x86)\AutoHotkey\AutoHotkey.exe`
- `C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe`
- `%LOCALAPPDATA%\Programs\AutoHotkey\AutoHotkey.exe`
- In your system PATH

### Custom AutoHotkey Path

If AutoHotkey is installed in a different location, you can configure it by:

1. **Manual Configuration**: Edit your `radialmenu-settings-latest.json` file and add:
   ```json
   {
     "ExternalTools": {
       "AutoHotkeyPath": "C:\\Your\\Custom\\Path\\To\\AutoHotkey.exe"
     }
   }
   ```

2. **Future Settings UI**: A settings interface for configuring external tools will be added in future updates.

## How It Works

The Discord integration uses an AutoHotkey script (`DiscordNav.ahk`) that:
1. Activates the Discord window
2. Sends `Ctrl+K` to open Discord's Quick Switcher
3. Types the channel/user name you specified
4. Presses Enter to navigate to the channel

## Setting Up Discord Menu Items

1. Open RadialMenu Settings
2. Create a new menu item or edit an existing one
3. Set the **Action Type** to `discord`
4. In the **Path/Command** field, enter the channel name, user name, or search term exactly as you would type it in Discord's Quick Switcher

### Examples:

- **Voice Channel**: Enter `General` (for a voice channel named "General")
- **Text Channel**: Enter `general-chat` (for a text channel named "general-chat") 
- **Direct Message**: Enter `@username` or just `username` (to message a specific user)
- **Server Jump**: Enter `ServerName` (to jump to a specific server)

## Sample Configuration

Here's an example of Discord menu items in your settings JSON:

```json
{
  "Id": "discord-menu",
  "Label": "Discord Channels",
  "Icon": "💬",
  "Color": "#FF7289DA",
  "Action": "",
  "Path": "",
  "Submenu": [
    {
      "Id": "general-voice",
      "Label": "General Voice",
      "Icon": "🎤",
      "Color": "#FF7289DA",
      "Action": "discord",
      "Path": "General",
      "Submenu": null
    },
    {
      "Id": "music-channel",
      "Label": "Music Production",
      "Icon": "🎵",
      "Color": "#FFFF5722",
      "Action": "discord",
      "Path": "music-production",
      "Submenu": null
    },
    {
      "Id": "dm-friend",
      "Label": "Message Friend",
      "Icon": "💭",
      "Color": "#FF9C27B0",
      "Action": "discord",
      "Path": "@friendusername",
      "Submenu": null
    }
  ]
}
```

## Troubleshooting

### Discord Not Found
If you get an error that Discord cannot be found:
- Make sure Discord is running
- Try using the full Discord window title if needed
- Check that Discord is not minimized to system tray

### AutoHotkey Not Found
If you get an error about AutoHotkey not found:
- Download and install AutoHotkey from the official website
- Make sure `AutoHotkey.exe` is in your system PATH
- Restart your computer after installation
- Run `setup-autohotkey.bat` for automated setup assistance

### AutoHotkey Version Compatibility
The RadialMenu automatically detects your AutoHotkey version and uses the appropriate script:
- **AutoHotkey v2**: Uses `DiscordNav.ahk` (modern syntax)
- **AutoHotkey v1**: Uses `DiscordNav_v1.ahk` (legacy syntax)

Both scripts are included in the application and work identically.

### Script Not Found
If you get an error about Discord navigation scripts not found:
- Rebuild the project using `dotnet build` - both scripts should be automatically copied to the output directory
- Make sure both `DiscordNav.ahk` and `DiscordNav_v1.ahk` files are in the same directory as your RadialMenu executable
- Check file permissions
- The error message will show you the exact expected path for troubleshooting

### Channel Not Found
If Discord opens but doesn't navigate to the right channel:
- Make sure the channel name in the Path field matches exactly (case-sensitive)
- Try using the channel name as it appears in Discord's Quick Switcher
- For voice channels, use just the name (e.g., "General")
- For text channels, use the full name with hyphens (e.g., "general-chat")

## Tips

- The Quick Switcher in Discord is very flexible - you can use partial names
- For better reliability, use exact channel names
- You can create multiple Discord menu items for frequently used channels
- Consider organizing Discord channels in submenus by server or category
- Test your configuration by manually using `Ctrl+K` in Discord first

## Advanced Usage

You can also use this for other Discord Quick Switcher functions:
- Jump to specific messages by using message content
- Navigate to different servers by server name
- Access Discord settings by using keywords like "settings"

The AutoHotkey script can be customized if you need different behavior for your specific Discord setup.