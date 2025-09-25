using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;

namespace RadialMenu
{
    public partial class App : Application
    {
        private TaskbarIcon? _notifyIcon;
        private RadialMenuWindow? _radialMenu;
        private GlobalHotKey? _hotKey;
        private GlobalHotKey? _settingsHotKey;
        private Mutex? _mutex;
        private Services.SettingsService? _settingsService;
        private Services.DesktopClickDetectionService? _desktopClickService;
        public Services.SettingsService? SettingsService => _settingsService;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure single instance
            _mutex = new Mutex(true, "RadialMenuApp", out bool isNewInstance);
            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("RadialMenu is already running!", "RadialMenu", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // Initialize settings service
            _settingsService = new Services.SettingsService();
            var settings = _settingsService.Load();
            _settingsService.SettingsSaved += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _radialMenu?.ReloadConfiguration();
                        RegisterHotkeyFromSettings(); // Re-register hotkey when settings change
                    }
                    catch { }
                });
            };

            // Create system tray icon
            CreateNotifyIcon();

            // Create radial menu window (hidden initially)
            _radialMenu = new RadialMenuWindow();

            // Initialize desktop click detection service
            _desktopClickService = new Services.DesktopClickDetectionService();
            _desktopClickService.DesktopHoldCompleted += OnDesktopHoldCompleted;

            // Register global hotkey from settings
            RegisterHotkeyFromSettings();

            // Register settings hotkey (Ctrl+Alt+S)
            _settingsHotKey = new GlobalHotKey(GlobalHotKey.MOD_CONTROL | GlobalHotKey.MOD_ALT, Keys.S, ShowSettings);
            if (!_settingsHotKey.Register())
            {
                // Optional: log or message
            }

            // Hide from taskbar
            MainWindow = _radialMenu;
        }

        private void CreateNotifyIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName),
                ToolTipText = "RadialMenu - Press Win+F12 to activate, Ctrl+Alt+S for settings"
            };

            _notifyIcon.TrayMouseDoubleClick += (s, e) => ShowSettings();
            
            // Context menu
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
            settingsItem.Click += (s, e) => ShowSettings();
            contextMenu.Items.Add(settingsItem);
            
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => Shutdown();
            contextMenu.Items.Add(exitItem);
            
            _notifyIcon.ContextMenu = contextMenu;
        }

        private void OnHotKeyPressed()
        {
            Dispatcher.Invoke(() =>
            {
                if (_radialMenu != null && !_radialMenu.IsVisible)
                {
                    // Get cursor position
                    var cursorPos = System.Windows.Forms.Cursor.Position;
                    _radialMenu.ShowAt(cursorPos.X, cursorPos.Y);
                }
                else if (_radialMenu != null && _radialMenu.IsVisible)
                {
                    _radialMenu.Hide();
                }
            });
        }

        private void OnDesktopHoldCompleted(int x, int y)
        {
            Dispatcher.Invoke(() =>
            {
                if (_radialMenu != null && !_radialMenu.IsVisible)
                {
                    _radialMenu.ShowAt(x, y);
                }
            });
        }

        private void ShowSettings()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var settingsWindow = new Views.SettingsWindow();
                    settingsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to open settings window: {ex.Message}\n\nDetails: {ex}", 
                        "Settings Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            });
        }

        private void RegisterHotkeyFromSettings()
        {
            try
            {
                // Unregister existing hotkey
                _hotKey?.Unregister();
                
                // Load current settings
                var settings = _settingsService?.Load();
                var hotkeyString = settings?.Hotkeys?.Toggle ?? "Win+F12";
                
                // Parse hotkey string
                if (ParseHotkey(hotkeyString, out uint modifiers, out System.Windows.Forms.Keys key))
                {
                    _hotKey = new GlobalHotKey(modifiers, key, OnHotKeyPressed);
                    if (!_hotKey.Register())
                    {
                        System.Windows.MessageBox.Show(
                            $"Failed to register global hotkey ({hotkeyString}). It may already be in use by another application.", 
                            "RadialMenu", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Fallback to default if parsing fails
                    _hotKey = new GlobalHotKey(GlobalHotKey.MOD_WIN, System.Windows.Forms.Keys.F12, OnHotKeyPressed);
                    _hotKey.Register();
                }
            }
            catch (Exception ex)
            {
                // Log error and use default hotkey
                System.Diagnostics.Debug.WriteLine($"Error registering hotkey: {ex.Message}");
                _hotKey = new GlobalHotKey(GlobalHotKey.MOD_WIN, System.Windows.Forms.Keys.F12, OnHotKeyPressed);
                _hotKey.Register();
            }
        }

        private bool ParseHotkey(string hotkeyString, out uint modifiers, out System.Windows.Forms.Keys key)
        {
            modifiers = 0;
            key = System.Windows.Forms.Keys.None;
            
            try
            {
                var parts = hotkeyString.Split('+');
                if (parts.Length == 0) return false;
                
                // Parse modifiers
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    switch (parts[i].Trim().ToLower())
                    {
                        case "ctrl":
                            modifiers |= GlobalHotKey.MOD_CONTROL;
                            break;
                        case "alt":
                            modifiers |= GlobalHotKey.MOD_ALT;
                            break;
                        case "shift":
                            modifiers |= GlobalHotKey.MOD_SHIFT;
                            break;
                        case "win":
                            modifiers |= GlobalHotKey.MOD_WIN;
                            break;
                    }
                }
                
                // Parse key (last part)
                var keyPart = parts[parts.Length - 1].Trim();
                
                // Handle special key mappings
                var keyMapping = new Dictionary<string, System.Windows.Forms.Keys>(StringComparer.OrdinalIgnoreCase)
                {
                    { "~", System.Windows.Forms.Keys.Oemtilde },
                    { "Space", System.Windows.Forms.Keys.Space },
                    { "Tab", System.Windows.Forms.Keys.Tab },
                    { "Enter", System.Windows.Forms.Keys.Enter },
                    { "Backspace", System.Windows.Forms.Keys.Back },
                    { "Delete", System.Windows.Forms.Keys.Delete },
                    { "Insert", System.Windows.Forms.Keys.Insert },
                    { "Home", System.Windows.Forms.Keys.Home },
                    { "End", System.Windows.Forms.Keys.End },
                    { "PageUp", System.Windows.Forms.Keys.PageUp },
                    { "PageDown", System.Windows.Forms.Keys.PageDown },
                    { "Esc", System.Windows.Forms.Keys.Escape },
                    { "PrtScn", System.Windows.Forms.Keys.PrintScreen },
                    { "Pause", System.Windows.Forms.Keys.Pause }
                };
                
                if (keyMapping.ContainsKey(keyPart))
                {
                    key = keyMapping[keyPart];
                    return true;
                }
                
                // Try to parse as enum
                if (Enum.TryParse<System.Windows.Forms.Keys>(keyPart, true, out key))
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _settingsHotKey?.Unregister();
            _hotKey?.Unregister();
            _desktopClickService?.Dispose();
            _notifyIcon?.Dispose();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }

    // Global hotkey handler
    public class GlobalHotKey
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private readonly Window _window = new Window();
        private readonly int _id;
        private readonly uint _modifier;
        private readonly uint _key;
        private readonly Action _action;

        public GlobalHotKey(uint modifier, Keys key, Action action)
        {
            _id = GetHashCode();
            _modifier = modifier;
            _key = (uint)key;
            _action = action;
            
            var helper = new System.Windows.Interop.WindowInteropHelper(_window);
            helper.EnsureHandle();
            
            var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
            source?.AddHook(HwndHook);
        }

        public bool Register()
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(_window);
            return RegisterHotKey(helper.Handle, _id, _modifier, _key);
        }

        public void Unregister()
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(_window);
            UnregisterHotKey(helper.Handle, _id);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
            {
                _action();
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
