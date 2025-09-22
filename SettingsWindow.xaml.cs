using System.Windows;
using System.Windows.Controls;
using RadialMenu.ViewModels;
using RadialMenu.Services;

namespace RadialMenu
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;

        public SettingsWindow()
        {
            InitializeComponent();

            var app = Application.Current as App;
            var settingsService = app?.SettingsService ?? new SettingsService();
            _vm = new SettingsViewModel(settingsService);
            DataContext = _vm;

            // Wire navigation selection changes
            var nav = this.FindName("NavList") as ListBox;
            nav!.SelectionChanged += Nav_SelectionChanged;
            // initialize default content
            ShowGeneralPage();
        }

        private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;
            if (lb == null) return;
            switch (lb.SelectedIndex)
            {
                case 0: ShowGeneralPage(); break;
                case 1: ShowHotkeysPage(); break;
                case 2: ShowAppearancePage(); break;
                case 3: ShowMenuBuilderPage(); break;
                case 4: ShowAdvancedPage(); break;
            }
        }

        private void ShowGeneralPage()
        {
            ContentHost.Content = new TextBlock { Text = "General settings coming soon...", VerticalAlignment = VerticalAlignment.Center };
        }

        private void ShowHotkeysPage()
        {
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = "Global Toggle Hotkey", FontWeight = FontWeights.Bold });
            var hotkey = new Controls.HotkeyCaptureControl();
            hotkey.SetValue(Controls.HotkeyCaptureControl.HotkeyProperty, _vm.Working.Hotkeys.Toggle);
            panel.Children.Add(hotkey);
            ContentHost.Content = panel;
        }

        private void ShowAppearancePage()
        {
            ContentHost.Content = new Controls.AppearancePage { DataContext = _vm };
        }

        private void ShowMenuBuilderPage()
        {
            ContentHost.Content = new TextBlock { Text = "Menu Builder (visual) — TODO", VerticalAlignment = VerticalAlignment.Center };
        }

        private void ShowAdvancedPage()
        {
            ContentHost.Content = new TextBlock { Text = "Advanced settings and JSON editor — TODO", VerticalAlignment = VerticalAlignment.Center };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _vm.SaveCommand.Execute(null);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _vm.CancelCommand.Execute(null);
            Close();
        }
    }
}
