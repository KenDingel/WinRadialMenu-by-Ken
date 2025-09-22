using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Newtonsoft.Json;

namespace RadialMenu
{
    public partial class SettingsWindow : Window
    {
        private TextBox _configTextBox;
        private MenuConfiguration _config;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            Title = "RadialMenu Settings";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new TextBlock
            {
                Text = "RadialMenu Configuration",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Config editor
            var scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(10),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            _configTextBox = new TextBox
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.NoWrap
            };
            scrollViewer.Content = _configTextBox;
            
            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            saveButton.Click += SaveButton_Click;
            buttonPanel.Children.Add(saveButton);

            var resetButton = new Button
            {
                Content = "Reset to Default",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5)
            };
            resetButton.Click += ResetButton_Click;
            buttonPanel.Children.Add(resetButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            cancelButton.Click += (s, e) => Close();
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;

            // Add help text
            var helpText = new TextBlock
            {
                Text = "Edit the JSON configuration below. Each item can have: Label, Icon (emoji), Color (hex), Action (launch/url/folder/command), Path, and Submenu.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10, 5, 10, 0),
                FontStyle = FontStyles.Italic
            };
            Grid.SetRow(helpText, 0);
            grid.Children.Add(helpText);
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _config = JsonConvert.DeserializeObject<MenuConfiguration>(json) ?? new MenuConfiguration();
                    _configTextBox.Text = JsonConvert.SerializeObject(_config, Formatting.Indented);
                }
                else
                {
                    LoadDefaultConfiguration();
                }
            }
            catch
            {
                LoadDefaultConfiguration();
            }
        }

        private void LoadDefaultConfiguration()
        {
            _config = GetDefaultConfiguration();
            _configTextBox.Text = JsonConvert.SerializeObject(_config, Formatting.Indented);
        }

        private MenuConfiguration GetDefaultConfiguration()
        {
            // Return same default as in RadialMenuWindow
            return new MenuConfiguration
            {
                Items = new System.Collections.Generic.List<ConfigItem>
                {
                    new ConfigItem
                    {
                        Label = "Apps",
                        Icon = "📱",
                        Color = "#FF4CAF50",
                        Submenu = new System.Collections.Generic.List<ConfigItem>
                        {
                            new ConfigItem { Label = "VS Code", Icon = "📝", Action = "launch", Path = "code" },
                            new ConfigItem { Label = "Terminal", Icon = "⌨", Action = "launch", Path = "wt" }
                        }
                    },
                    new ConfigItem
                    {
                        Label = "Web",
                        Icon = "🌐",
                        Color = "#FF2196F3",
                        Submenu = new System.Collections.Generic.List<ConfigItem>
                        {
                            new ConfigItem { Label = "GitHub", Icon = "🐙", Action = "url", Path = "https://github.com" }
                        }
                    }
                }
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate JSON
                _config = JsonConvert.DeserializeObject<MenuConfiguration>(_configTextBox.Text) ?? new MenuConfiguration();
                
                // Save to file
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                File.WriteAllText(configPath, _configTextBox.Text);
                
                MessageBox.Show("Configuration saved successfully! The menu will use the new configuration next time it appears.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid JSON configuration:\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset to default configuration?", 
                "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LoadDefaultConfiguration();
            }
        }
    }
}
