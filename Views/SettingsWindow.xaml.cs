using System;
using System.Windows;
using System.IO;
using RadialMenu.ViewModels;

namespace RadialMenu.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.log");

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.UtcNow:O}] Settings: {message}\r\n");
            }
            catch { }
        }

        private SettingsViewModel? _vm;

        public SettingsWindow()
        {
            try
            {
                Log("SettingsWindow constructor started");
                InitializeComponent();
                
                if (Application.Current is App app && app.SettingsService != null)
                {
                    Log("Using app SettingsService");
                    _vm = new SettingsViewModel(app.SettingsService);
                    DataContext = _vm;
                }
                else
                {
                    Log("Fallback: creating new SettingsService");
                    // fallback: create SettingsService directly
                    var svc = new Services.SettingsService();
                    _vm = new SettingsViewModel(svc);
                    DataContext = _vm;
                }

                // Load default page
                string defaultPage = "general";
                if (_vm != null)
                {
                    if (string.IsNullOrEmpty(_vm.Working.Meta.LastOpenedTab))
                    {
                        _vm.Working.Meta.LastOpenedTab = "menu";
                        defaultPage = "menu";
                    }
                    else
                    {
                        defaultPage = _vm.Working.Meta.LastOpenedTab;
                    }
                }
                ShowPage(defaultPage);

                // Wire ViewModel events
                if (_vm != null)
                {
                    _vm.NavigateRequested += (page) =>
                    {
                        try
                        {
                            Dispatcher.Invoke(() => ShowPage(page));
                        }
                        catch (Exception ex)
                        {
                            Log($"NavigateRequested error: {ex.Message}");
                        }
                    };
                    _vm.ImportRequested += () => 
                    {
                        try
                        {
                            Dispatcher.Invoke(() => DoImport());
                        }
                        catch (Exception ex)
                        {
                            Log($"ImportRequested error: {ex.Message}");
                        }
                    };
                    _vm.ExportRequested += () => 
                    {
                        try
                        {
                            Dispatcher.Invoke(() => DoExport());
                        }
                        catch (Exception ex)
                        {
                            Log($"ExportRequested error: {ex.Message}");
                        }
                    };
                }
                Log("SettingsWindow constructor completed");
            }
            catch (Exception ex)
            {
                Log($"SettingsWindow constructor failed: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show(
                    $"Failed to initialize settings window: {ex.Message}", 
                    "Settings Initialization Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                throw; // Re-throw to be caught by the outer ShowSettings method
            }
        }

        private void ShowPage(string? page)
        {
            var p = (page ?? "General").ToLowerInvariant();
            Log($"ShowPage called with page: {p}");
            try
            {
                switch (p)
                {
                    case "general": ContentHost.Content = CreateGeneralPage(); break;
                    case "hotkeys": ContentHost.Content = CreateHotkeysPage(); break;
                    case "appearance": ContentHost.Content = CreateAppearancePage(); break;
                    case "menu": 
                        ContentHost.Content = CreateMenuPlaceholder(); 
                        if (_vm != null && _vm.SelectedMenuItem == null && _vm.Working.Menu.Count > 0) 
                            _vm.SelectMenuItem(_vm.Working.Menu[0]);
                        break;
                    case "advanced": ContentHost.Content = CreateAdvancedPlaceholder(); break;
                    case "diagnostics": ContentHost.Content = CreateDiagnosticsPage(); break;
                    default: ContentHost.Content = CreateGeneralPage(); break;
                }
                Log($"ShowPage completed for {p}");
            }
            catch (Exception ex)
            {
                Log($"ShowPage failed for {p}: {ex.Message}\n{ex.StackTrace}");
                // Fallback to general page
                ContentHost.Content = CreateGeneralPage();
            }
        }

        private System.Windows.UIElement CreateGeneralPage()
        {
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(6) };
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "General", FontSize = 16, FontWeight = FontWeights.Bold });
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Startup and profile settings", Margin = new Thickness(0,6,0,12) });

            var profileLabel = new System.Windows.Controls.TextBlock { Text = "Profile Name" };
            var profileBox = new System.Windows.Controls.TextBox();
            profileBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("Working.Meta.ProfileName") { Mode = System.Windows.Data.BindingMode.TwoWay });
            sp.Children.Add(profileLabel);
            sp.Children.Add(profileBox);

            return sp;
        }

        private System.Windows.UIElement CreateHotkeysPage()
        {
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(6) };
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Hotkeys", FontSize = 16, FontWeight = FontWeights.Bold });
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Global toggle hotkey", Margin = new Thickness(0,6,0,12) });

            var hotkeyControl = new RadialMenu.Controls.HotkeyCaptureControl();
            var binding = new System.Windows.Data.Binding("Working.Hotkeys.Toggle") { Mode = System.Windows.Data.BindingMode.TwoWay };
            hotkeyControl.SetBinding(RadialMenu.Controls.HotkeyCaptureControl.HotkeyProperty, binding);
            sp.Children.Add(hotkeyControl);

            return sp;
        }

        private System.Windows.UIElement CreateAppearancePage()
        {
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(6) };
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Appearance", FontSize = 16, FontWeight = FontWeights.Bold });

            var scaleLabel = new System.Windows.Controls.TextBlock { Text = "UI Scale" };
            var scaleBox = new System.Windows.Controls.TextBox { Width = 80 };
            scaleBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("Working.Appearance.UiScale") { Mode = System.Windows.Data.BindingMode.TwoWay });
            sp.Children.Add(scaleLabel);
            sp.Children.Add(scaleBox);

            var centerLabel = new System.Windows.Controls.TextBlock { Text = "Center Text" };
            var centerBox = new System.Windows.Controls.TextBox { Width = 240 };
            centerBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("Working.Appearance.CenterText") { Mode = System.Windows.Data.BindingMode.TwoWay });
            sp.Children.Add(centerLabel);
            sp.Children.Add(centerBox);

            var innerLabel = new System.Windows.Controls.TextBlock { Text = "Inner Radius" };
            var innerBox = new System.Windows.Controls.TextBox { Width = 80 };
            innerBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("Working.Appearance.InnerRadius") { Mode = System.Windows.Data.BindingMode.TwoWay });
            sp.Children.Add(innerLabel);
            sp.Children.Add(innerBox);

            var outerLabel = new System.Windows.Controls.TextBlock { Text = "Outer Radius" };
            var outerBox = new System.Windows.Controls.TextBox { Width = 80 };
            outerBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("Working.Appearance.OuterRadius") { Mode = System.Windows.Data.BindingMode.TwoWay });
            sp.Children.Add(outerLabel);
            sp.Children.Add(outerBox);

            var themeLabel = new System.Windows.Controls.TextBlock { Text = "Theme (light/dark/auto)" };
            var themeBox = new System.Windows.Controls.TextBox { Width = 120 };
            themeBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("Working.Appearance.Theme") { Mode = System.Windows.Data.BindingMode.TwoWay });
            sp.Children.Add(themeLabel);
            sp.Children.Add(themeBox);

            return sp;
        }

        private System.Windows.UIElement CreateMenuPlaceholder()
        {
            Log("CreateMenuPlaceholder started");
            try
            {
                var grid = new System.Windows.Controls.Grid();
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, System.Windows.GridUnitType.Star) });

                Log("Creating TreeMenuEditor");
                var tree = new RadialMenu.Controls.TreeMenuEditor();
                Log("Creating CanvasMenuPreview");
                var canvas = new RadialMenu.Controls.CanvasMenuPreview();
                // Ensure controls have the same DataContext (the SettingsViewModel)
                tree.DataContext = _vm;
                canvas.DataContext = _vm;
                System.Windows.Controls.Grid.SetColumn(tree, 0);
                System.Windows.Controls.Grid.SetColumn(canvas, 1);
                grid.Children.Add(tree);
                grid.Children.Add(canvas);

                Log("CreateMenuPlaceholder completed");
                return grid;
            }
            catch (Exception ex)
            {
                Log($"CreateMenuPlaceholder failed: {ex.Message}\n{ex.StackTrace}");
                // Return a simple error message
                return new System.Windows.Controls.TextBlock { Text = $"Error loading menu builder: {ex.Message}", Foreground = System.Windows.Media.Brushes.Red };
            }
        }

        private System.Windows.UIElement CreateAdvancedPlaceholder()
        {
            return new System.Windows.Controls.TextBlock { Text = "Advanced - JSON editor and migration tools (coming soon)", Margin = new Thickness(8) };
        }

        private System.Windows.UIElement CreateDiagnosticsPage()
        {
            var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(6) };
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Diagnostics", FontSize = 16, FontWeight = FontWeights.Bold });
            sp.Children.Add(new System.Windows.Controls.TextBlock { Text = "Backups (most recent first):", Margin = new Thickness(0,6,0,6) });

            var list = new System.Windows.Controls.ListBox { Height = 200 };
            if (Application.Current is App app && app.SettingsService != null)
            {
                var backups = app.SettingsService.ListBackups();
                foreach (var b in backups)
                {
                    list.Items.Add(b);
                }
            }

            sp.Children.Add(list);

            var btn = new System.Windows.Controls.Button { Content = "Restore Selected Backup", Margin = new Thickness(0,8,0,0), Width = 200 };
            btn.Click += (s, e) =>
            {
                if (list.SelectedItem == null) { MessageBox.Show("Select a backup first.", "Restore", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                var path = list.SelectedItem.ToString();
                if (Application.Current is App a && a.SettingsService != null)
                {
                    if (a.SettingsService.RestoreBackup(path!))
                    {
                        MessageBox.Show("Backup restored. Settings reloaded.", "Restore", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to restore backup.", "Restore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };

                        // (DataContext assigned where the menu page is created)
            sp.Children.Add(btn);
            return sp;
        }

        private void DoImport()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(dlg.FileName);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<RadialMenu.Models.Settings>(json);
                    if (settings != null && _vm != null)
                    {
                        _vm.Working = settings;
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Failed to import settings: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DoExport()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*", FileName = "radialmenu-settings.json" };
            if (dlg.ShowDialog() == true && _vm != null)
            {
                try
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(_vm.Working, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(dlg.FileName, json);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Failed to export settings: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
