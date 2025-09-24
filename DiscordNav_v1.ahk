; Discord Channel Navigator (AutoHotkey v1)
; Usage: DiscordNav_v1.ahk "channel_name"
; This script activates Discord, opens the quick switcher (Ctrl+K), 
; types the channel name, and presses Enter

#NoEnv
#SingleInstance Force
SendMode Input
SetWorkingDir %A_ScriptDir%

; Get the channel name from command line argument
if 0 < 1
{
    MsgBox, Usage: DiscordNav_v1.ahk "channel_name"
    ExitApp
}

ChannelName := %1%

; Activate Discord window
; Try common Discord window titles
WinActivate, Discord
if !WinActive("Discord")
{
    ; Try alternative window title patterns
    WinActivate, ahk_exe Discord.exe
}

; Wait a moment for Discord to become active
Sleep, 500

; Send Ctrl+K to open Quick Switcher
Send, ^k

; Wait for the switcher to open
Sleep, 250

; Type the channel name
Send, %ChannelName%

; Wait a moment
Sleep, 500

; Press Enter to navigate to the channel
Send, {Enter}

ExitApp