using System;
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
                    }
                    catch { }
                });
            };

            // Create system tray icon
            CreateNotifyIcon();

            // Create radial menu window (hidden initially)
            _radialMenu = new RadialMenuWindow();

            // Register global hotkey (Win+F12)
            _hotKey = new GlobalHotKey(GlobalHotKey.MOD_WIN, Keys.F12, OnHotKeyPressed);
            if (!_hotKey.Register())
            {
                System.Windows.MessageBox.Show("Failed to register global hotkey (Win+F12). It may already be in use by another application.", "RadialMenu", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

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

        private void ShowSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            settingsWindow.ShowDialog();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _settingsHotKey?.Unregister();
            _hotKey?.Unregister();
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
