using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RadialMenu.Controls
{
    public partial class HotkeyCaptureControl : UserControl
    {
        public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
            "Hotkey", typeof(string), typeof(HotkeyCaptureControl), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyChanged));

        public string Hotkey
        {
            get => (string)GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }

        // Dictionary to map system key names to friendly display names
        private static readonly Dictionary<Key, string> KeyDisplayNames = new Dictionary<Key, string>
        {
            { Key.Oem3, "~" },           // Tilde
            { Key.Oem1, ";" },           // Semicolon
            { Key.OemPlus, "=" },        // Equals
            { Key.OemMinus, "-" },       // Minus
            { Key.OemOpenBrackets, "[" }, // Left bracket
            { Key.OemCloseBrackets, "]" }, // Right bracket
            { Key.OemPipe, "\\" },       // Backslash
            { Key.OemQuotes, "'" },      // Quote
            { Key.OemComma, "," },       // Comma
            { Key.OemPeriod, "." },      // Period
            { Key.OemQuestion, "/" },    // Forward slash
            { Key.Space, "Space" },
            { Key.Tab, "Tab" },
            { Key.Enter, "Enter" },
            { Key.Back, "Backspace" },
            { Key.Delete, "Delete" },
            { Key.Insert, "Insert" },
            { Key.Home, "Home" },
            { Key.End, "End" },
            { Key.PageUp, "PageUp" },
            { Key.PageDown, "PageDown" },
            { Key.Escape, "Esc" },
            { Key.PrintScreen, "PrtScn" },
            { Key.Pause, "Pause" },
            { Key.CapsLock, "CapsLock" },
            { Key.NumLock, "NumLock" },
            { Key.Scroll, "ScrollLock" }
        };

        public HotkeyCaptureControl()
        {
            InitializeComponent();
            HotkeyBox.PreviewKeyDown += HotkeyBox_PreviewKeyDown;
            HotkeyBox.GotFocus += (s, e) => HotkeyBox.Text = "Press keys...";
            HotkeyBox.LostFocus += (s, e) => HotkeyBox.Text = Hotkey;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var parts = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Win");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key != Key.LeftCtrl && key != Key.RightCtrl && key != Key.LeftAlt && key != Key.RightAlt &&
                key != Key.LeftShift && key != Key.RightShift && key != Key.LWin && key != Key.RWin)
            {
                // Use friendly name if available, otherwise use the default key name
                string keyName = KeyDisplayNames.ContainsKey(key) ? KeyDisplayNames[key] : key.ToString();
                parts.Add(keyName);
            }

            Hotkey = string.Join("+", parts);
            HotkeyBox.Text = Hotkey;
        }

        private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyCaptureControl ctrl)
            {
                var newVal = e.NewValue as string ?? string.Empty;
                if (ctrl.HotkeyBox != null && ctrl.HotkeyBox.Text != newVal)
                {
                    ctrl.HotkeyBox.Text = newVal;
                }
            }
        }
    }
}
