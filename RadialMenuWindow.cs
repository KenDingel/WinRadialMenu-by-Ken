using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace RadialMenu
{
    public partial class RadialMenuWindow : Window
    {
    private Canvas _canvas = null!;
        private Point _centerPoint;
        private readonly List<RadialMenuItem> _menuItems = new();
        private RadialMenuItem? _hoveredItem;
        private readonly double _innerRadius = 40;
        private readonly double _outerRadius = 150;
    private MenuConfiguration _config = null!;
        private Stack<MenuLevel> _menuStack = new();
    private Ellipse _centerCircle = null!;
    private TextBlock _centerText = null!;
        private bool _isAnimating = false;

        public RadialMenuWindow()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            // Window settings
            Title = "RadialMenu";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 400;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.Manual;
            
            // Main canvas
            _canvas = new Canvas
            {
                Width = 400,
                Height = 400,
                Background = Brushes.Transparent
            };

            // Add blur effect to background
            var blurEffect = new BlurEffect { Radius = 10 };
            
            // Center circle (dead zone)
            _centerCircle = new Ellipse
            {
                Width = _innerRadius * 2,
                Height = _innerRadius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20)),
                Stroke = new SolidColorBrush(Color.FromArgb(255, 45, 125, 210)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(_centerCircle, 200 - _innerRadius);
            Canvas.SetTop(_centerCircle, 200 - _innerRadius);
            _canvas.Children.Add(_centerCircle);

            // Center text (shows current level or "MENU")
            _centerText = new TextBlock
            {
                Text = "MENU",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            // initial placement; will be repositioned when showing the menu
            Canvas.SetLeft(_centerText, 200 - 20);
            Canvas.SetTop(_centerText, 200 - 8);
            _canvas.Children.Add(_centerText);

            Content = _canvas;

            // Event handlers
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            KeyDown += OnKeyDown;
            Deactivated += OnDeactivated;
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _config = JsonConvert.DeserializeObject<MenuConfiguration>(json) ?? GetDefaultConfiguration();
                }
                else
                {
                    _config = GetDefaultConfiguration();
                    SaveConfiguration();
                }
            }
            catch
            {
                _config = GetDefaultConfiguration();
            }
        }

        private MenuConfiguration GetDefaultConfiguration()
        {
            return new MenuConfiguration
            {
                Items = new List<ConfigItem>
                {
                    new ConfigItem
                    {
                        Label = "Apps",
                        Icon = "📱",
                        Color = "#FF4CAF50",
                        Submenu = new List<ConfigItem>
                        {
                            new ConfigItem { Label = "VS Code", Icon = "📝", Action = "launch", Path = "code" },
                            new ConfigItem { Label = "Terminal", Icon = "⌨", Action = "launch", Path = "wt" },
                            new ConfigItem { Label = "Calculator", Icon = "🔢", Action = "launch", Path = "calc" },
                            new ConfigItem { Label = "Notepad", Icon = "📄", Action = "launch", Path = "notepad" }
                        }
                    },
                    new ConfigItem
                    {
                        Label = "Web",
                        Icon = "🌐",
                        Color = "#FF2196F3",
                        Submenu = new List<ConfigItem>
                        {
                            new ConfigItem { Label = "GitHub", Icon = "🐙", Action = "url", Path = "https://github.com" },
                            new ConfigItem { Label = "Google", Icon = "🔍", Action = "url", Path = "https://google.com" },
                            new ConfigItem { Label = "YouTube", Icon = "📺", Action = "url", Path = "https://youtube.com" },
                            new ConfigItem { Label = "Gmail", Icon = "📧", Action = "url", Path = "https://gmail.com" }
                        }
                    },
                    new ConfigItem
                    {
                        Label = "Folders",
                        Icon = "📁",
                        Color = "#FFFFC107",
                        Submenu = new List<ConfigItem>
                        {
                            new ConfigItem { Label = "Documents", Icon = "📑", Action = "folder", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                            new ConfigItem { Label = "Downloads", Icon = "⬇", Action = "folder", Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") },
                            new ConfigItem { Label = "Desktop", Icon = "🖥", Action = "folder", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                            new ConfigItem { Label = "C: Drive", Icon = "💾", Action = "folder", Path = "C:\\" }
                        }
                    },
                    new ConfigItem
                    {
                        Label = "System",
                        Icon = "⚙",
                        Color = "#FF9C27B0",
                        Submenu = new List<ConfigItem>
                        {
                            new ConfigItem { Label = "Task Manager", Icon = "📊", Action = "launch", Path = "taskmgr" },
                            new ConfigItem { Label = "Control Panel", Icon = "🎛", Action = "launch", Path = "control" },
                            new ConfigItem { Label = "Settings", Icon = "⚙", Action = "launch", Path = "ms-settings:" },
                            new ConfigItem { Label = "Device Manager", Icon = "🔌", Action = "launch", Path = "devmgmt.msc" }
                        }
                    },
                    new ConfigItem
                    {
                        Label = "Power",
                        Icon = "⚡",
                        Color = "#FFF44336",
                        Submenu = new List<ConfigItem>
                        {
                            new ConfigItem { Label = "Lock", Icon = "🔒", Action = "command", Path = "rundll32.exe user32.dll,LockWorkStation" },
                            new ConfigItem { Label = "Sleep", Icon = "😴", Action = "command", Path = "rundll32.exe powrprof.dll,SetSuspendState 0,1,0" },
                            new ConfigItem { Label = "Restart", Icon = "🔄", Action = "command", Path = "shutdown /r /t 0" },
                            new ConfigItem { Label = "Shutdown", Icon = "⏻", Action = "command", Path = "shutdown /s /t 0" }
                        }
                    },
                    new ConfigItem
                    {
                        Label = "Tools",
                        Icon = "🔧",
                        Color = "#FF00BCD4",
                        Action = "folder",
                        Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                    }
                }
            };
        }

        private void SaveConfiguration()
        {
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

    public void ShowAt(int x, int y)
        {
            if (_isAnimating) return;

            Left = x - Width / 2;
            Top = y - Height / 2;
            _centerPoint = new Point(Width / 2, Height / 2);

            // Reposition center circle and center text to be perfectly centered
            PositionCenterElements();

            // Load root menu
            LoadMenuItems(_config.Items, null);
            _menuStack.Clear();
            _menuStack.Push(new MenuLevel { Items = _config.Items, Name = "MENU" });
            _centerText.Text = "MENU";

            // Re-center center text after text change
            PositionCenterElements();

            Show();
            Activate();
            AnimateIn();
        }

        private void LoadMenuItems(List<ConfigItem> configItems, Point? origin = null)
        {
            // Clear existing items
            foreach (var item in _menuItems)
            {
                _canvas.Children.Remove(item.Visual);
                _canvas.Children.Remove(item.LabelText);
            }
            _menuItems.Clear();
            // Create new node items (circular)
            var itemCount = configItems.Count;
            var angleStep = 360.0 / Math.Max(1, itemCount);

            var originPoint = origin ?? _centerPoint;
            var spreadRadius = _outerRadius - 30; // how far nodes sit from center
            var nodeSize = 56.0;

            for (int i = 0; i < itemCount; i++)
            {
                var config = configItems[i];
                var angle = (i * angleStep) - 90; // start from top
                var angleRad = angle * Math.PI / 180;

                // Target position around center (or submenu parent)
                var targetX = _centerPoint.X + Math.Cos(angleRad) * spreadRadius;
                var targetY = _centerPoint.Y + Math.Sin(angleRad) * spreadRadius;

                // Parse color
                Color itemColor;
                try
                {
                    itemColor = (Color)ColorConverter.ConvertFromString(config.Color ?? "#FF2D7DD2");
                }
                catch
                {
                    itemColor = Color.FromRgb(45, 125, 210);
                }

                // Create circular visual
                var ellipse = new Ellipse
                {
                    Width = nodeSize,
                    Height = nodeSize,
                    Fill = new SolidColorBrush(Color.FromArgb(220, itemColor.R, itemColor.G, itemColor.B)),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, itemColor.R, itemColor.G, itemColor.B)),
                    StrokeThickness = 2
                };

                // Place ellipse at target but animate from origin
                Canvas.SetLeft(ellipse, targetX - nodeSize / 2);
                Canvas.SetTop(ellipse, targetY - nodeSize / 2);
                var translate = new TranslateTransform(originPoint.X - targetX, originPoint.Y - targetY);
                var scale = new ScaleTransform(0.3, 0.3);
                var tg = new TransformGroup();
                tg.Children.Add(scale);
                tg.Children.Add(translate);
                ellipse.RenderTransform = tg;
                ellipse.RenderTransformOrigin = new Point(0.5, 0.5);

                // Add subtle glow
                ellipse.Effect = new DropShadowEffect { Color = itemColor, BlurRadius = 12, ShadowDepth = 0, Opacity = 0 };

                _canvas.Children.Add(ellipse);

                // Create label centered inside the circle
                var label = new TextBlock
                {
                    Text = config.Icon + "\n" + config.Label,
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = nodeSize - 8,
                    IsHitTestVisible = false
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var labelSize = label.DesiredSize;
                Canvas.SetLeft(label, targetX - labelSize.Width / 2);
                Canvas.SetTop(label, targetY - labelSize.Height / 2);
                _canvas.Children.Add(label);

                // Animate translate and scale to origin -> target
                var animX = new DoubleAnimation(originPoint.X - targetX, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                var animY = new DoubleAnimation(originPoint.Y - targetY, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                var animScale = new DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(320)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                translate.BeginAnimation(TranslateTransform.XProperty, animX);
                translate.BeginAnimation(TranslateTransform.YProperty, animY);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);

                var menuItem = new RadialMenuItem
                {
                    Config = config,
                    Visual = ellipse,
                    LabelText = label,
                    Angle = angle,
                    BaseColor = itemColor,
                    Center = new Point(targetX, targetY),
                    Radius = nodeSize / 2
                };

                _menuItems.Add(menuItem);
            }
        }

        private System.Windows.Shapes.Path CreatePieSlice(double angleStep, double startAngle)
        {
            var endAngle = startAngle + angleStep;
            var startRad = startAngle * Math.PI / 180;
            var endRad = endAngle * Math.PI / 180;

            var innerStartX = _centerPoint.X + Math.Cos(startRad) * _innerRadius;
            var innerStartY = _centerPoint.Y + Math.Sin(startRad) * _innerRadius;
            var innerEndX = _centerPoint.X + Math.Cos(endRad) * _innerRadius;
            var innerEndY = _centerPoint.Y + Math.Sin(endRad) * _innerRadius;

            var outerStartX = _centerPoint.X + Math.Cos(startRad) * _outerRadius;
            var outerStartY = _centerPoint.Y + Math.Sin(startRad) * _outerRadius;
            var outerEndX = _centerPoint.X + Math.Cos(endRad) * _outerRadius;
            var outerEndY = _centerPoint.Y + Math.Sin(endRad) * _outerRadius;

            var largeArc = angleStep > 180 ? 1 : 0;

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(innerStartX, innerStartY) };

            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(innerEndX, innerEndY),
                Size = new Size(_innerRadius, _innerRadius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = largeArc == 1
            });

            figure.Segments.Add(new LineSegment { Point = new Point(outerEndX, outerEndY) });

            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(outerStartX, outerStartY),
                Size = new Size(_outerRadius, _outerRadius),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = largeArc == 1
            });

            figure.IsClosed = true;
            geometry.Figures.Add(figure);

            return new System.Windows.Shapes.Path { Data = geometry };
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(_canvas);
            var dx = mousePos.X - _centerPoint.X;
            var dy = mousePos.Y - _centerPoint.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            // Check if in dead zone
            if (distance < _innerRadius)
            {
                UnhoverAll();
                return;
            }

            // Check if outside outer radius
            if (distance > _outerRadius)
            {
                UnhoverAll();
                return;
            }

            // Use proximity-based detection for node items
            RadialMenuItem? hoveredItem = null;
            foreach (var item in _menuItems)
            {
                var dx2 = mousePos.X - item.Center.X;
                var dy2 = mousePos.Y - item.Center.Y;
                var dist = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                if (dist <= item.Radius + 8) // small padding for easier hover
                {
                    hoveredItem = item;
                    break;
                }
            }
            if (hoveredItem != _hoveredItem)
            {
                UnhoverAll();
                if (hoveredItem != null)
                {
                    HoverItem(hoveredItem);
                }
                _hoveredItem = hoveredItem;
            }
        }

        private RadialMenuItem? GetItemAtAngle(double angle)
        {
            // Not used in node-mode, but keep for compatibility
            return null;
        }

        // Normalize angles to 0..360 range
        private static double NormalizeAngle(double angle)
        {
            var a = angle % 360.0;
            if (a < 0) a += 360.0;
            return a;
        }

        private void HoverItem(RadialMenuItem item)
        {
            // Scale up the node (preserving existing RenderTransform group)
            if (item.Visual.RenderTransform is TransformGroup tg)
            {
                // find or add scale transform
                var scale = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
                if (scale == null)
                {
                    scale = new ScaleTransform(1, 1);
                    tg.Children.Insert(0, scale);
                }
                var scaleX = new DoubleAnimation(scale.ScaleX, 1.15, TimeSpan.FromMilliseconds(150)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                var scaleY = new DoubleAnimation(scale.ScaleY, 1.15, TimeSpan.FromMilliseconds(150)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }

            // Brighten color
            var brightColor = Color.FromArgb(240, item.BaseColor.R, item.BaseColor.G, item.BaseColor.B);
            item.Visual.Fill = new SolidColorBrush(brightColor);

            // Increase glow
            if (item.Visual.Effect is DropShadowEffect glow)
            {
                var opa = new DoubleAnimation(glow.Opacity, 0.9, TimeSpan.FromMilliseconds(150));
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, opa);
                glow.BlurRadius = 20;
            }

            // Bold text
            item.LabelText.FontWeight = FontWeights.Bold;
        }

        private void UnhoverAll()
        {
            foreach (var item in _menuItems)
            {
                // shrink back scale if present
                if (item.Visual.RenderTransform is TransformGroup tg)
                {
                    var scale = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
                    if (scale != null)
                    {
                        var scaleX = new DoubleAnimation(scale.ScaleX, 1.0, TimeSpan.FromMilliseconds(150));
                        var scaleY = new DoubleAnimation(scale.ScaleY, 1.0, TimeSpan.FromMilliseconds(150));
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
                    }
                }

                item.Visual.Fill = new SolidColorBrush(Color.FromArgb(220, item.BaseColor.R, item.BaseColor.G, item.BaseColor.B));
                
                if (item.Visual.Effect is DropShadowEffect glow)
                {
                    var opa = new DoubleAnimation(glow.Opacity, 0.0, TimeSpan.FromMilliseconds(150));
                    glow.BeginAnimation(DropShadowEffect.OpacityProperty, opa);
                    glow.BlurRadius = 12;
                }

                item.LabelText.FontWeight = FontWeights.SemiBold;
            }
            _hoveredItem = null;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_hoveredItem != null)
            {
                ExecuteItem(_hoveredItem);
            }
        }

        private void ExecuteItem(RadialMenuItem item)
        {
            if (item.Config.Submenu != null && item.Config.Submenu.Count > 0)
            {
                // Navigate to submenu
                _menuStack.Push(new MenuLevel { Items = item.Config.Submenu, Name = item.Config.Label, Origin = item.Center });
                _centerText.Text = item.Config.Label;
                AnimateTransition(() => LoadMenuItems(item.Config.Submenu, item.Center));
            }
            else
            {
                // Execute action
                try
                {
                    switch (item.Config.Action?.ToLower())
                    {
                        case "launch":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = item.Config.Path,
                                UseShellExecute = true
                            });
                            break;

                        case "url":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = item.Config.Path,
                                UseShellExecute = true
                            });
                            break;

                        case "folder":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = item.Config.Path,
                                UseShellExecute = true
                            });
                            break;

                        case "command":
                            if (string.IsNullOrWhiteSpace(item.Config.Path)) break;
                            var parts = item.Config.Path.Split(new[] { ' ' }, 2);
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = parts[0],
                                Arguments = parts.Length > 1 ? parts[1] : "",
                                UseShellExecute = true
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to execute: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                HideMenu();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_menuStack.Count > 1)
                {
                    // Go back to previous menu
                    _menuStack.Pop();
                    var previousLevel = _menuStack.Peek();
                    _centerText.Text = previousLevel.Name;
                    AnimateTransition(() => LoadMenuItems(previousLevel.Items, previousLevel.Origin));
                }
                else
                {
                    HideMenu();
                }
            }
        }

        private void OnDeactivated(object? sender, EventArgs e)
        {
            HideMenu();
        }

        private void HideMenu()
        {
            AnimateOut(() => Hide());
        }

        private void AnimateIn()
        {
            _isAnimating = true;
            Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.Completed += (s, e) => _isAnimating = false;
            BeginAnimation(OpacityProperty, fadeIn);

            // Animate menu items
            foreach (var item in _menuItems)
            {
                var transform = new ScaleTransform(0.5, 0.5, _centerPoint.X, _centerPoint.Y);
                item.Visual.RenderTransform = transform;
                
                var scaleAnim = new DoubleAnimation(0.5, 1, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }

        private void AnimateOut(Action onComplete)
        {
            _isAnimating = true;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                _isAnimating = false;
                onComplete();
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateTransition(Action onComplete)
        {
            _isAnimating = true;
            
            // Fade out current items
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
            fadeOut.Completed += (s, e) =>
            {
                onComplete();
                
                // Fade in new items
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                fadeIn.Completed += (s2, e2) => _isAnimating = false;
                _canvas.BeginAnimation(OpacityProperty, fadeIn);
            };
            _canvas.BeginAnimation(OpacityProperty, fadeOut);
        }

        // Ensure center circle and text are perfectly centered around _centerPoint
        private void PositionCenterElements()
        {
            if (_centerCircle != null)
            {
                Canvas.SetLeft(_centerCircle, _centerPoint.X - _centerCircle.Width / 2);
                Canvas.SetTop(_centerCircle, _centerPoint.Y - _centerCircle.Height / 2);
            }

            if (_centerText != null)
            {
                _centerText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = _centerText.DesiredSize;
                Canvas.SetLeft(_centerText, _centerPoint.X - size.Width / 2);
                Canvas.SetTop(_centerText, _centerPoint.Y - size.Height / 2);
            }
        }
    }

    // Data classes
    public class RadialMenuItem
    {
        public ConfigItem Config { get; set; } = new();
        public System.Windows.Shapes.Shape Visual { get; set; } = new System.Windows.Shapes.Path();
        public TextBlock LabelText { get; set; } = new();
        public double Angle { get; set; }
        public Color BaseColor { get; set; }
        public Point Center { get; set; }
        public double Radius { get; set; }
    }

    public class MenuLevel
    {
        public List<ConfigItem> Items { get; set; } = new();
        public string Name { get; set; } = "MENU";
        public Point? Origin { get; set; }
    }

    public class MenuConfiguration
    {
        public List<ConfigItem> Items { get; set; } = new();
    }

    public class ConfigItem
    {
        public string Label { get; set; } = "";
        public string Icon { get; set; } = "📄";
        public string? Color { get; set; }
        public string? Action { get; set; }
        public string? Path { get; set; }
        public List<ConfigItem>? Submenu { get; set; }
    }
}
