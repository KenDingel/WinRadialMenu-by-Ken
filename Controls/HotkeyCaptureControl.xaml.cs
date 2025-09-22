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
            var parts = new System.Collections.Generic.List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Win");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key != Key.LeftCtrl && key != Key.RightCtrl && key != Key.LeftAlt && key != Key.RightAlt &&
                key != Key.LeftShift && key != Key.RightShift && key != Key.LWin && key != Key.RWin)
            {
                parts.Add(key.ToString());
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
