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
using Newtonsoft.Json.Linq;
using System.Windows.Threading;

namespace RadialMenu
{
    public partial class RadialMenuWindow : Window
    {
    private readonly string _logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "radialmenu.log");
    private void Log(string message)
    {
        try
        {
            File.AppendAllText(_logPath, $"[{DateTime.UtcNow:O}] {message}\r\n");
        }
        catch { }
    }

    private Canvas _canvas = null!;
        private Point _centerPoint;
            // UI scale multiplier (default 1.6). Increase to scale up the entire menu.
            private double _uiScale = 1.6;
        private readonly List<RadialMenuItem> _menuItems = new();
        private RadialMenuItem? _hoveredItem;
    private readonly double _innerRadius = 40;
    private readonly double _outerRadius = 220;
    private MenuConfiguration _config = null!;
        private Stack<MenuLevel> _menuStack = new();
    private Ellipse _centerCircle = null!;
    private TextBlock _centerText = null!;
        private bool _isAnimating = false;
        private bool _justShown = false;
        private readonly DispatcherTimer _hoverExecuteTimer;

        public RadialMenuWindow()
        {
            // Initialize hover execute timer
            _hoverExecuteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _hoverExecuteTimer.Tick += (s, e) =>
            {
                _hoverExecuteTimer.Stop();
                if (_hoveredItem != null)
                {
                    ExecuteItem(_hoveredItem);
                }
            };

            // Attempt to read UI scale from settings before creating UI elements so sizes/positions are consistent
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    if (settings?.Appearance != null && settings.Appearance.UiScale > 0)
                    {
                        _uiScale = settings.Appearance.UiScale;
                    }
                }
            }
            catch { }

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
            Width = 2000 * _uiScale;
            Height = 2000 * _uiScale;
            WindowStartupLocation = WindowStartupLocation.Manual;
            
            // Main canvas
            _canvas = new Canvas
            {
                Width = 2000 * _uiScale,
                Height = 2000 * _uiScale,
                Background = Brushes.Transparent
            };

            // Create spinning vortex background
            CreateVortexBackground();

            // Add blur effect to background
            var blurEffect = new BlurEffect { Radius = 10 };
            
            // Center circle (dead zone) - dark matter portal style
            var centerGradient = new RadialGradientBrush
            {
                Center = new Point(0.3, 0.3),
                GradientOrigin = new Point(0.3, 0.3),
                RadiusX = 0.8,
                RadiusY = 0.8,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(220, 25, 25, 112), 0),     // Deep midnight blue center
                    new GradientStop(Color.FromArgb(180, 138, 43, 226), 0.3),  // Purple mid
                    new GradientStop(Color.FromArgb(150, 75, 0, 130), 0.7),    // Dark purple
                    new GradientStop(Color.FromArgb(100, 0, 0, 0), 1)          // Black edge
                }
            };

            _centerCircle = new Ellipse
            {
                Width = (_innerRadius * 2) * _uiScale,
                Height = (_innerRadius * 2) * _uiScale,
                Fill = centerGradient,
                Stroke = new SolidColorBrush(Color.FromArgb(200, 138, 43, 226)), // Purple stroke
                StrokeThickness = 2 * _uiScale
            };

            // Create deep shadow effect for inset appearance and portal depth
            var portalEffect = new DropShadowEffect
            {
                Color = Color.FromRgb(138, 43, 226),
                BlurRadius = 25,
                ShadowDepth = 0,
                Opacity = 0.8
            };
            
            _centerCircle.Effect = portalEffect;

            Canvas.SetLeft(_centerCircle, (Width / 2) - (_centerCircle.Width / 2));
            Canvas.SetTop(_centerCircle, (Height / 2) - (_centerCircle.Height / 2));
            _canvas.Children.Add(_centerCircle);

            // Add portal depth layers
            CreatePortalDepthLayers();

            // Center text (shows current level or "MENU") - dark matter style
            _centerText = new TextBlock
            {
                Text = "PORTAL",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 208, 255)), // Light purple text
                FontSize = 12 * _uiScale,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"), // Modern font
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            // Add enhanced glow effect to center text
            _centerText.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(138, 43, 226),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.9
            };

            // Add a rotating portal ring around the center for visual depth
            var centerRing = new Ellipse
            {
                Width = (_innerRadius * 2) * _uiScale + 20,
                Height = (_innerRadius * 2) * _uiScale + 20,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(Color.FromArgb(120, 138, 43, 226)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 8, 4 }
            };

            Canvas.SetLeft(centerRing, _centerPoint.X - centerRing.Width / 2);
            Canvas.SetTop(centerRing, _centerPoint.Y - centerRing.Height / 2);

            centerRing.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(138, 43, 226),
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.6
            };

            _canvas.Children.Add(centerRing);
            Panel.SetZIndex(centerRing, 0);

            // Add subtle rotation animation to center ring
            var rotateTransform = new RotateTransform(0, centerRing.Width / 2, centerRing.Height / 2);
            centerRing.RenderTransform = rotateTransform;
            centerRing.RenderTransformOrigin = new Point(0.5, 0.5);

            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(20),
                RepeatBehavior = RepeatBehavior.Forever
            };
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            // initial placement; will be repositioned when showing the menu
            Canvas.SetLeft(_centerText, (Width / 2) - 20 * _uiScale);
            Canvas.SetTop(_centerText, (Height / 2) - 8 * _uiScale);
            _canvas.Children.Add(_centerText);

            Content = _canvas;

            // Event handlers
            MouseMove += OnMouseMove;
            KeyDown += OnKeyDown;
            Deactivated += OnDeactivated;
        }

        private List<ConfigItem> ConvertMenuItems(System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig> menuItems)
        {
            var result = new List<ConfigItem>();
            foreach (var item in menuItems)
            {
                result.Add(new ConfigItem
                {
                    Label = item.Label,
                    Icon = item.Icon,
                    Color = item.Color,
                    Action = item.Action,
                    Path = item.Path,
                    Submenu = item.Submenu != null ? ConvertMenuItems(item.Submenu) : null
                });
            }
            return result;
        }

        private void LoadConfiguration()
        {
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    if (settings?.Menu != null)
                    {
                        _config = new MenuConfiguration { Items = ConvertMenuItems(settings.Menu) };
                    }
                    else
                    {
                        _config = GetDefaultConfiguration();
                    }
                }
                else
                {
                    // Fallback: load from file next to exe (for standalone settings editor)
                    var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                    if (File.Exists(configPath))
                    {
                        var json = File.ReadAllText(configPath);
                        try
                        {
                            // Try legacy format first (top-level Items)
                            var jo = Newtonsoft.Json.Linq.JObject.Parse(json);
                            if (jo["Items"] != null)
                            {
                                _config = jo.ToObject<MenuConfiguration>() ?? GetDefaultConfiguration();
                            }
                            else if (jo["Menu"] != null)
                            {
                                // New Settings schema (written by SettingsService) - map Menu -> Items
                                var menuToken = jo["Menu"];
                                if (menuToken != null)
                                {
                                    var items = menuToken.ToObject<List<ConfigItem>>() ?? new List<ConfigItem>();
                                    _config = new MenuConfiguration { Items = items };
                                }
                                else
                                {
                                    _config = GetDefaultConfiguration();
                                }
                            }
                            else
                            {
                                // unknown shape - fall back to legacy deserialization
                                _config = JsonConvert.DeserializeObject<MenuConfiguration>(json) ?? GetDefaultConfiguration();
                            }
                        }
                        catch
                        {
                            _config = GetDefaultConfiguration();
                        }
                    }
                    else
                    {
                        _config = GetDefaultConfiguration();
                        SaveConfiguration();
                    }
                }
            }
            catch
            {
                _config = GetDefaultConfiguration();
            }
        }

        // Public method to reload configuration at runtime and refresh the currently shown menu
        public void ReloadConfiguration()
        {
            LoadConfiguration();
            // If the menu is visible, reload the root menu items so changes appear immediately
            if (IsVisible)
            {
                try
                {
                    var current = _menuStack.Count > 0 ? _menuStack.Peek() : null;
                    // Load root items and replace visuals
                    var itemsToLoad = _config?.Items ?? new List<ConfigItem>();
                    var created = LoadMenuItems(itemsToLoad, null);
                    _menuStack.Clear();
                    _menuStack.Push(new MenuLevel { Items = itemsToLoad, Name = "PORTAL", CreatedNodes = created, Origin = null });
                    _centerText.Text = "PORTAL";
                    PositionCenterElements();
                }
                catch { }
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
            _centerPoint = new Point(_canvas.Width / 2, _canvas.Height / 2);

            Log($"ShowAt called. Config items: {_config?.Items?.Count ?? 0}. Canvas size: {_canvas.Width}x{_canvas.Height}. Center: {_centerPoint}");

            // Clear all canvas children except center elements
            UIElement[] centerElements = { _centerCircle, _centerText };
            _canvas.Children.Clear();
            foreach (var elem in centerElements)
            {
                _canvas.Children.Add(elem);
            }

            // Reposition center circle and center text to be perfectly centered
            PositionCenterElements();

            // Load root menu
            _menuStack.Clear();
            var itemsToLoad = _config?.Items ?? new List<ConfigItem>();
            var created = LoadMenuItems(itemsToLoad, null);
            Log($"LoadMenuItems returned {created?.Count ?? 0} created nodes.");
            _menuStack.Push(new MenuLevel { Items = itemsToLoad, Name = "PORTAL", CreatedNodes = created, Origin = null });
            _centerText.Text = "PORTAL";

            // Re-center center text after text change
            PositionCenterElements();

            Show();
            Activate();
            _justShown = true;
            var justShownTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            justShownTimer.Tick += (s, e) => { _justShown = false; justShownTimer.Stop(); };
            justShownTimer.Start();
            AnimateIn();
        }

        private List<RadialMenuItem> LoadMenuItems(List<ConfigItem> configItems, Point? origin = null)
        {
            // Clear existing items
            foreach (var item in _menuItems)
            {
                _canvas.Children.Remove(item.Visual);
                _canvas.Children.Remove(item.LabelText);
            }
            _menuItems.Clear();
            Log($"LoadMenuItems start. Requested count: {configItems?.Count ?? 0}");
            var createdNodes = new List<RadialMenuItem>();
            // Create new node items (circular)
            var itemCount = configItems?.Count ?? 0;
            var angleStep = 360.0 / Math.Max(1, itemCount);

            var originPoint = origin ?? _centerPoint;
            var spreadRadius = (_outerRadius - 30) * _uiScale; // scaled spread radius
            var nodeSize = (56.0 * 1.5) * _uiScale; // scaled node size

            if (itemCount == 0) return createdNodes;
            for (int i = 0; i < itemCount; i++)
            {
                var config = configItems![i];
                var angle = (i * angleStep) - 90; // start from top
                var angleRad = angle * Math.PI / 180;

                // Target position around center (or submenu parent)
                var targetX = _centerPoint.X + Math.Cos(angleRad) * spreadRadius;
                var targetY = _centerPoint.Y + Math.Sin(angleRad) * spreadRadius;

                // Parse color and create dark matter variant
                Color baseColor;
                try
                {
                    baseColor = (Color)ColorConverter.ConvertFromString(config.Color ?? "#FF483D8B");
                }
                catch
                {
                    baseColor = Color.FromRgb(72, 61, 139); // Dark slate blue
                }

                // Create dark matter neon color variants
                var cosmicColor = CreateCosmicVariant(baseColor);
                var voidColor = CreateVoidVariant(baseColor);

                // Create dark matter gradient background
                var gradientBrush = new RadialGradientBrush
                {
                    Center = new Point(0.3, 0.3),
                    GradientOrigin = new Point(0.3, 0.3),
                    RadiusX = 0.9,
                    RadiusY = 0.9,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(200, cosmicColor.R, cosmicColor.G, cosmicColor.B), 0),
                        new GradientStop(Color.FromArgb(120, voidColor.R, voidColor.G, voidColor.B), 0.6),
                        new GradientStop(Color.FromArgb(180, 25, 25, 112), 1) // Dark void edge
                    }
                };

                // Create circular visual with dark matter effects and animated border glow
                var ellipse = new Ellipse
                {
                    Width = nodeSize,
                    Height = nodeSize,
                    Fill = gradientBrush,
                    StrokeThickness = config.Submenu != null && config.Submenu.Count > 0 ? 3 : 2 // Thicker stroke for submenu items
                };

                // Create animated gradient stroke
                var gradientStroke = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(255, 138, 43, 226), 0),
                        new GradientStop(Color.FromArgb(200, cosmicColor.R, cosmicColor.G, cosmicColor.B), 0.5),
                        new GradientStop(Color.FromArgb(255, 75, 0, 130), 1)
                    }
                };

                ellipse.Stroke = gradientStroke;

                // Add animated gradient rotation
                var gradientRotateTransform = new RotateTransform(0, 0.5, 0.5);
                gradientStroke.RelativeTransform = gradientRotateTransform;

                var gradientRotationAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(8),
                    RepeatBehavior = RepeatBehavior.Forever
                };

                gradientRotateTransform.BeginAnimation(RotateTransform.AngleProperty, gradientRotationAnimation);

                // Add submenu indicator ring for items with children
                Ellipse? submenuRing = null;
                if (config.Submenu != null && config.Submenu.Count > 0)
                {
                    submenuRing = new Ellipse
                    {
                        Width = nodeSize + 8,
                        Height = nodeSize + 8,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromArgb(150, cosmicColor.R, cosmicColor.G, cosmicColor.B)),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 3, 3 } // Cosmic dashed line
                    };

                    Canvas.SetLeft(submenuRing, targetX - (nodeSize + 8) / 2);
                    Canvas.SetTop(submenuRing, targetY - (nodeSize + 8) / 2);

                    submenuRing.Effect = new DropShadowEffect
                    {
                        Color = cosmicColor,
                        BlurRadius = 4,
                        ShadowDepth = 0,
                        Opacity = 0.5
                    };

                    _canvas.Children.Add(submenuRing);
                    Panel.SetZIndex(submenuRing, 1);
                }

                // Place ellipse at target but animate from origin
                Canvas.SetLeft(ellipse, originPoint.X - nodeSize / 2);
                Canvas.SetTop(ellipse, originPoint.Y - nodeSize / 2);
                var translate = new TranslateTransform(0, 0);
                var scale = new ScaleTransform(0.3, 0.3);
                var tg = new TransformGroup();
                tg.Children.Add(scale);
                tg.Children.Add(translate);
                ellipse.RenderTransform = tg;
                ellipse.RenderTransformOrigin = new Point(0.5, 0.5);

                // Add subtle floating and pulsing animation to indicate interactivity
                var pulseAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.08,
                    Duration = TimeSpan.FromSeconds(3.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };

                // Create floating animation
                var floatAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 6,
                    Duration = TimeSpan.FromSeconds(4 + (i * 0.3)), // Stagger timing
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };

                // Create a subtle scale transform for pulsing (separate from main transform)
                var pulseScale = new ScaleTransform(1.0, 1.0);
                var floatTranslate = new TranslateTransform(0, 0);
                var floatGroup = new TransformGroup();
                floatGroup.Children.Add(tg);
                floatGroup.Children.Add(pulseScale);
                floatGroup.Children.Add(floatTranslate);
                ellipse.RenderTransform = floatGroup;

                // Start floating and pulsing after initial animation completes
                var animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                animationTimer.Tick += (s, e) => {
                    pulseScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                    pulseScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
                    floatTranslate.BeginAnimation(TranslateTransform.YProperty, floatAnimation);
                    animationTimer.Stop();
                };
                animationTimer.Start();

                // Add enhanced portal glow effect with pulsating animation
                var portalGlow = new DropShadowEffect 
                { 
                    Color = cosmicColor, 
                    BlurRadius = 18, 
                    ShadowDepth = 0, 
                    Opacity = 0.6 
                };

                ellipse.Effect = portalGlow;

                // Add pulsating glow animation
                var glowPulseAnimation = new DoubleAnimation
                {
                    From = 0.4,
                    To = 0.9,
                    Duration = TimeSpan.FromSeconds(2.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };

                portalGlow.BeginAnimation(DropShadowEffect.OpacityProperty, glowPulseAnimation);

                _canvas.Children.Add(ellipse);
                // Ensure nodes render above center circle
                Panel.SetZIndex(ellipse, 2);

                // Add connector line from center to node with directional arrow
                var connectorLine = new System.Windows.Shapes.Line
                {
                    X1 = _centerPoint.X,
                    Y1 = _centerPoint.Y,
                    X2 = _centerPoint.X, // Start at center, will animate to target
                    Y2 = _centerPoint.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, cosmicColor.R, cosmicColor.G, cosmicColor.B)),
                    StrokeThickness = 2 * _uiScale,
                    Opacity = 0.8
                };

                // Add cosmic glow to connector lines
                connectorLine.Effect = new DropShadowEffect
                {
                    Color = cosmicColor,
                    BlurRadius = 6,
                    ShadowDepth = 0,
                    Opacity = 0.5
                };

                _canvas.Children.Add(connectorLine);
                Panel.SetZIndex(connectorLine, 1); // Below nodes but above center circle

                // Create directional arrowhead at the end of the connector line
                var arrowSize = 8 * _uiScale;
                var arrow = new System.Windows.Shapes.Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(-arrowSize/2, -arrowSize/2),
                        new Point(arrowSize/2, 0),
                        new Point(-arrowSize/2, arrowSize/2)
                    },
                    Fill = new SolidColorBrush(Color.FromArgb(200, cosmicColor.R, cosmicColor.G, cosmicColor.B)),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 138, 43, 226)),
                    StrokeThickness = 1,
                    Opacity = 0.9
                };

                // Position arrow at target initially (will be animated)
                Canvas.SetLeft(arrow, targetX - arrowSize/2);
                Canvas.SetTop(arrow, targetY - arrowSize/2);

                // Rotate arrow to point outward from center
                var angleToCenter = Math.Atan2(targetY - _centerPoint.Y, targetX - _centerPoint.X) * 180 / Math.PI;
                arrow.RenderTransform = new RotateTransform(angleToCenter + 90, arrowSize/2, arrowSize/2);

                // Add cosmic glow to arrow
                arrow.Effect = new DropShadowEffect
                {
                    Color = cosmicColor,
                    BlurRadius = 4,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };

                _canvas.Children.Add(arrow);
                Panel.SetZIndex(arrow, 1);

                // Animate connector line to target position
                var lineAnimX = new DoubleAnimation(_centerPoint.X, targetX, TimeSpan.FromMilliseconds(400)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var lineAnimY = new DoubleAnimation(_centerPoint.Y, targetY, TimeSpan.FromMilliseconds(400)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                connectorLine.BeginAnimation(System.Windows.Shapes.Line.X2Property, lineAnimX);
                connectorLine.BeginAnimation(System.Windows.Shapes.Line.Y2Property, lineAnimY);

                // Animate arrow position
                var arrowAnimX = new DoubleAnimation(targetX - arrowSize/2, targetX - arrowSize/2, TimeSpan.FromMilliseconds(400));
                var arrowAnimY = new DoubleAnimation(targetY - arrowSize/2, targetY - arrowSize/2, TimeSpan.FromMilliseconds(400));
                arrow.BeginAnimation(Canvas.LeftProperty, arrowAnimX);
                arrow.BeginAnimation(Canvas.TopProperty, arrowAnimY);

                // Create label centered inside the circle
                var label = new TextBlock
                {
                    Text = config.Icon + "\n" + config.Label,
                    Foreground = Brushes.White,
                    FontSize = 11 * _uiScale,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = nodeSize - 8,
                    IsHitTestVisible = false
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var labelSize = label.DesiredSize;
                Canvas.SetLeft(label, originPoint.X - labelSize.Width / 2);
                Canvas.SetTop(label, originPoint.Y - labelSize.Height / 2);
                var labelTranslate = new TranslateTransform(0, 0);
                var labelScale = new ScaleTransform(0.3, 0.3);
                var labelTg = new TransformGroup();
                labelTg.Children.Add(labelScale);
                labelTg.Children.Add(labelTranslate);
                label.RenderTransform = labelTg;
                label.RenderTransformOrigin = new Point(0.5, 0.5);
                _canvas.Children.Add(label);
                Panel.SetZIndex(label, 3);

                var menuItem = new RadialMenuItem
                {
                    Config = config,
                    Visual = ellipse,
                    LabelText = label,
                    Angle = angle,
                    BaseColor = baseColor,
                    Center = _centerPoint,
                    Radius = spreadRadius
                };

                _menuItems.Add(menuItem);
                // make visual feel interactive
                ellipse.Cursor = Cursors.Hand;
                // attach direct mouse events to ensure hover/click works even if proximity math misses
                ellipse.MouseEnter += (s, ev) =>
                {
                    HoverItem(menuItem);
                    _hoveredItem = menuItem;
                };
                ellipse.MouseLeave += (s, ev) =>
                {
                    // only unhover if this item is currently hovered
                    if (_hoveredItem == menuItem)
                    {
                        UnhoverAll();
                    }
                };
                ellipse.MouseLeftButtonUp += (s, ev) => ExecuteItem(menuItem);
                createdNodes.Add(menuItem);
                Log($"Created node '{config.Label}' at ({targetX:0.##},{targetY:0.##}) angle {angle:0.##}");
            }
            Log($"LoadMenuItems end. Created {createdNodes.Count} nodes.");
            return createdNodes;
        }

        // Add submenu nodes branching out from a parent node; returns created nodes and connectors are stored in the menu level.
        private List<RadialMenuItem> AddSubmenuNodes(List<ConfigItem> configItems, RadialMenuItem parent, out List<UIElement> outConnectors)
        {
            var created = new List<RadialMenuItem>();
            var count = configItems.Count;
            var span = 120.0; // degrees of spread for children
            var startAngle = parent.Angle - span / 2;
            var step = count > 1 ? span / (count - 1) : 0;
            var childDistance = 140.0 * _uiScale; // scaled distance from parent

            var connectors = new List<UIElement>();

            // Precompute target positions so we can check canvas bounds and expand/shift canvas if needed
            var targets = new List<Point>();
            for (int i = 0; i < count; i++)
            {
                var angle = startAngle + i * step;
                var rad = angle * Math.PI / 180;
                var tx = parent.Center.X + Math.Cos(rad) * childDistance;
                var ty = parent.Center.Y + Math.Sin(rad) * childDistance;
                targets.Add(new Point(tx, ty));
            }

            // Ensure canvas will fit all targets (prevent cutoff by expanding/and/or shifting canvas if necessary)
            try
            {
                var half = ((48.0 * 1.5) / 2.0) * _uiScale; // scaled half node size
                var shift = EnsureCanvasFitsTargets(targets, half);
                if (shift.X != 0 || shift.Y != 0)
                {
                    for (int t = 0; t < targets.Count; t++)
                    {
                        targets[t] = new Point(targets[t].X + shift.X, targets[t].Y + shift.Y);
                    }
                }
            }
            catch { }

            for (int i = 0; i < count; i++)
            {
                var cfg = configItems[i];
                var angle = startAngle + i * step;
                var rad = angle * Math.PI / 180;
                var targetX = targets[i].X;
                var targetY = targets[i].Y;

                // Parse color and create dark matter variant
                Color baseColor;
                try { baseColor = (Color)ColorConverter.ConvertFromString(cfg.Color ?? "#FF483D8B"); }
                catch { baseColor = Color.FromRgb(72, 61, 139); }

                // Create dark matter color variants
                var cosmicColor = CreateCosmicVariant(baseColor);
                var voidColor = CreateVoidVariant(baseColor);

                // Create dark matter gradient background
                var gradientBrush = new RadialGradientBrush
                {
                    Center = new Point(0.3, 0.3),
                    GradientOrigin = new Point(0.3, 0.3),
                    RadiusX = 0.9,
                    RadiusY = 0.9,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(180, cosmicColor.R, cosmicColor.G, cosmicColor.B), 0),
                        new GradientStop(Color.FromArgb(120, voidColor.R, voidColor.G, voidColor.B), 0.6),
                        new GradientStop(Color.FromArgb(200, 25, 25, 112), 1)
                    }
                };

                var nodeSize = (48.0 * 1.5) * _uiScale; // scaled node size
                var ellipse = new Ellipse
                {
                    Width = nodeSize,
                    Height = nodeSize,
                    Fill = gradientBrush,
                    Stroke = new SolidColorBrush(Color.FromArgb(200, 138, 43, 226)), // Purple void stroke
                    StrokeThickness = 2
                };
                Canvas.SetLeft(ellipse, parent.Center.X - nodeSize / 2);
                Canvas.SetTop(ellipse, parent.Center.Y - nodeSize / 2);

                // Add portal glow effect for submenu items
                var glowEffect = new DropShadowEffect 
                { 
                    Color = cosmicColor, 
                    BlurRadius = 15, 
                    ShadowDepth = 0, 
                    Opacity = 0.5 
                };
                
                ellipse.Effect = glowEffect;

                _canvas.Children.Add(ellipse);
                // Ensure nodes render above center circle but below lines
                Panel.SetZIndex(ellipse, 2);

                // connector line (above nodes for visibility)
                var line = new System.Windows.Shapes.Line
                {
                    X1 = parent.Center.X,
                    Y1 = parent.Center.Y,
                    X2 = parent.Center.X,
                    Y2 = parent.Center.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(200, cosmicColor.R, cosmicColor.G, cosmicColor.B)),
                    StrokeThickness = 2.5 * _uiScale,
                    Opacity = 0.9
                };
                
                // Add cosmic glow to connector lines
                line.Effect = new DropShadowEffect
                {
                    Color = cosmicColor,
                    BlurRadius = 6,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };
                
                // add connector above nodes
                _canvas.Children.Add(line);
                Panel.SetZIndex(line, 3);
                connectors.Add(line);

                // initial transform from parent center -> will animate into place with spring
                var translate = new TranslateTransform(0, 0);
                var scale = new ScaleTransform(0.3, 0.3);
                var tg = new TransformGroup();
                tg.Children.Add(scale);
                tg.Children.Add(translate);
                ellipse.RenderTransform = tg;
                ellipse.RenderTransformOrigin = new Point(0.5, 0.5);

                var label = new TextBlock
                {
                    Text = cfg.Icon + "\n" + cfg.Label,
                    Foreground = Brushes.White,
                    FontSize = 11 * _uiScale,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = nodeSize - 6,
                    IsHitTestVisible = false
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var labelSize = label.DesiredSize;
                Canvas.SetLeft(label, parent.Center.X - labelSize.Width / 2);
                Canvas.SetTop(label, parent.Center.Y - labelSize.Height / 2);
                var labelTranslate = new TranslateTransform(0, 0);
                var labelScale = new ScaleTransform(0.3, 0.3);
                var labelTg = new TransformGroup();
                labelTg.Children.Add(labelScale);
                labelTg.Children.Add(labelTranslate);
                label.RenderTransform = labelTg;
                label.RenderTransformOrigin = new Point(0.5, 0.5);
                _canvas.Children.Add(label);

                // Animate line end to target point
                var x2Anim = new DoubleAnimation(parent.Center.X, targetX, TimeSpan.FromMilliseconds(480)) { EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 6, EasingMode = EasingMode.EaseOut } };
                var y2Anim = new DoubleAnimation(parent.Center.Y, targetY, TimeSpan.FromMilliseconds(480)) { EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 6, EasingMode = EasingMode.EaseOut } };
                line.BeginAnimation(System.Windows.Shapes.Line.X2Property, x2Anim);
                line.BeginAnimation(System.Windows.Shapes.Line.Y2Property, y2Anim);

                var menuItem = new RadialMenuItem
                {
                    Config = cfg,
                    Visual = ellipse,
                    LabelText = label,
                    Angle = angle,
                    BaseColor = baseColor,
                    Center = parent.Center,
                    Radius = childDistance
                };

                _menuItems.Add(menuItem);
                // make interactive
                ellipse.Cursor = Cursors.Hand;
                ellipse.MouseEnter += (s, ev) =>
                {
                    HoverItem(menuItem);
                    _hoveredItem = menuItem;
                };
                ellipse.MouseLeave += (s, ev) =>
                {
                    if (_hoveredItem == menuItem) UnhoverAll();
                };
                ellipse.MouseLeftButtonUp += (s, ev) => ExecuteItem(menuItem);

                created.Add(menuItem);
            }

            // attach connectors list to the top menu level later by caller
            // We'll return the created RadialMenuItem list and let caller store connectors if needed.
            outConnectors = connectors;
            return created;
        }

        private System.Windows.Shapes.Path CreatePieSlice(double angleStep, double startAngle)
        {
            var endAngle = startAngle + angleStep;
            var startRad = startAngle * Math.PI / 180;
            var endRad = endAngle * Math.PI / 180;
            var effectiveInner = _innerRadius * _uiScale;
            var effectiveOuter = _outerRadius * _uiScale;

            var innerStartX = _centerPoint.X + Math.Cos(startRad) * effectiveInner;
            var innerStartY = _centerPoint.Y + Math.Sin(startRad) * effectiveInner;
            var innerEndX = _centerPoint.X + Math.Cos(endRad) * effectiveInner;
            var innerEndY = _centerPoint.Y + Math.Sin(endRad) * effectiveInner;

            var outerStartX = _centerPoint.X + Math.Cos(startRad) * effectiveOuter;
            var outerStartY = _centerPoint.Y + Math.Sin(startRad) * effectiveOuter;
            var outerEndX = _centerPoint.X + Math.Cos(endRad) * effectiveOuter;
            var outerEndY = _centerPoint.Y + Math.Sin(endRad) * effectiveOuter;

            var largeArc = angleStep > 180 ? 1 : 0;

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(innerStartX, innerStartY) };

            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(innerEndX, innerEndY),
                Size = new Size(effectiveInner, effectiveInner),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = largeArc == 1
            });

            figure.Segments.Add(new LineSegment { Point = new Point(outerEndX, outerEndY) });

            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(outerStartX, outerStartY),
                Size = new Size(effectiveOuter, effectiveOuter),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = largeArc == 1
            });

            figure.IsClosed = true;
            geometry.Figures.Add(figure);

            return new System.Windows.Shapes.Path { Data = geometry };
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isAnimating) return;

            var mousePos = e.GetPosition(_canvas);
            var dx = mousePos.X - _centerPoint.X;
            var dy = mousePos.Y - _centerPoint.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            // Effective radii account for UI scale
            var effectiveInner = _innerRadius * _uiScale;
            var effectiveOuter = _outerRadius * _uiScale;

            // Check if in dead zone
            if (distance < effectiveInner)
            {
                UnhoverAll();
                _hoverExecuteTimer.Stop();
                return;
            }

            // Do not gate hover by a strict outer radius so submenu children
            // placed beyond the visual ring can still receive hover events.

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
                _hoverExecuteTimer.Stop(); // Stop any pending execution
                if (hoveredItem != null)
                {
                    HoverItem(hoveredItem);
                    _hoverExecuteTimer.Start(); // Start timer for new hover
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
                var scaleX = new DoubleAnimation(scale.ScaleX, 1.2, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var scaleY = new DoubleAnimation(scale.ScaleY, 1.2, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }

            // Enhance gradient brightness
            if (item.Visual.Fill is LinearGradientBrush gradient)
            {
                foreach (var stop in gradient.GradientStops)
                {
                    var brightColor = Color.FromArgb(
                        (byte)Math.Min(255, stop.Color.A + 40),
                        (byte)Math.Min(255, stop.Color.R + 30),
                        (byte)Math.Min(255, stop.Color.G + 30),
                        (byte)Math.Min(255, stop.Color.B + 30)
                    );
                    stop.Color = brightColor;
                }
            }

            // Enhance stroke
            item.Visual.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            item.Visual.StrokeThickness = 3;

            // Intensify multi-layer neon glow
            if (item.Visual.Effect is DropShadowEffect glow)
            {
                glow.Color = Colors.Cyan; // Cyberpunk cyan glow
                var opa = new DoubleAnimation(glow.Opacity, 1.0, TimeSpan.FromMilliseconds(200));
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, opa);
                glow.BlurRadius = 30; // More intense glow
                
                // Add drop shadow enhancement
                var dropShadowAnim = new DoubleAnimation(0.5, 0.8, TimeSpan.FromMilliseconds(200));
                // Note: We can't animate drop shadow properties easily, but the glow increase gives depth
            }

            // Bold text with glow
            item.LabelText.FontWeight = FontWeights.Bold;
            item.LabelText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            if (item.LabelText.Effect is null)
            {
                item.LabelText.Effect = new DropShadowEffect { Color = Colors.Cyan, BlurRadius = 8, Opacity = 0.8 };
            }
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
                        var scaleX = new DoubleAnimation(scale.ScaleX, 1.0, TimeSpan.FromMilliseconds(200));
                        var scaleY = new DoubleAnimation(scale.ScaleY, 1.0, TimeSpan.FromMilliseconds(200));
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
                    }
                }

                // Reset gradient to original cyberpunk colors
                var neonColor = CreateNeonVariant(item.BaseColor);
                var accentColor = CreateAccentVariant(item.BaseColor);
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(180, neonColor.R, neonColor.G, neonColor.B), 0),
                        new GradientStop(Color.FromArgb(120, accentColor.R, accentColor.G, accentColor.B), 0.5),
                        new GradientStop(Color.FromArgb(200, neonColor.R, neonColor.G, neonColor.B), 1)
                    }
                };
                item.Visual.Fill = gradientBrush;

                // Reset stroke
                item.Visual.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                item.Visual.StrokeThickness = 1.5;

                // Reset glow
                if (item.Visual.Effect is DropShadowEffect glow)
                {
                    glow.Color = neonColor;
                    var opa = new DoubleAnimation(glow.Opacity, 0.3, TimeSpan.FromMilliseconds(200));
                    glow.BeginAnimation(DropShadowEffect.OpacityProperty, opa);
                    glow.BlurRadius = 15;
                }

                // Reset text
                item.LabelText.FontWeight = FontWeights.SemiBold;
                item.LabelText.Foreground = Brushes.White;
                item.LabelText.Effect = null;
            }
            _hoveredItem = null;
        }

        private void ExecuteItem(RadialMenuItem item)
        {
            if (item.Config.Submenu != null && item.Config.Submenu.Count > 0)
            {
                // Avoid spawning duplicate children
                if (!item.Expanded)
                {
                    var created = AddSubmenuNodes(item.Config.Submenu, item, out var connectors);
                    _menuStack.Push(new MenuLevel { Items = item.Config.Submenu, Name = item.Config.Label, Origin = item.Center, CreatedNodes = created, Connectors = connectors });
                    _centerText.Text = item.Config.Label;
                    item.Expanded = true;
                }
                // keep existing nodes visible; no full reload
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
                    var popped = _menuStack.Pop();
                    // remove nodes/connectors created by the popped level
                    if (popped.CreatedNodes != null)
                    {
                        // animate nodes back to origin before removing
                        foreach (var n in popped.CreatedNodes)
                        {
                            // animate scale down and translate toward popped.Origin if available
                            var target = popped.Origin ?? _centerPoint;
                            if (n.Visual.RenderTransform is TransformGroup tg)
                            {
                                var translate = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                                var scale = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
                                if (translate != null)
                                {
                                    var animX = new DoubleAnimation(0, target.X - n.Center.X, TimeSpan.FromMilliseconds(360)) { EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 6, EasingMode = EasingMode.EaseIn } };
                                    var animY = new DoubleAnimation(0, target.Y - n.Center.Y, TimeSpan.FromMilliseconds(360)) { EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 6, EasingMode = EasingMode.EaseIn } };
                                    translate.BeginAnimation(TranslateTransform.XProperty, animX);
                                    translate.BeginAnimation(TranslateTransform.YProperty, animY);
                                }
                                if (scale != null)
                                {
                                    var sX = new DoubleAnimation(scale.ScaleX, 0.2, TimeSpan.FromMilliseconds(360));
                                    var sY = new DoubleAnimation(scale.ScaleY, 0.2, TimeSpan.FromMilliseconds(360));
                                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, sX);
                                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, sY);
                                }
                            }
                            // fade out and remove after animation completes
                            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(380));
                            fade.Completed += (s, e2) =>
                            {
                                _canvas.Children.Remove(n.Visual);
                                _canvas.Children.Remove(n.LabelText);
                                _menuItems.Remove(n);
                            };
                            n.Visual.BeginAnimation(UIElement.OpacityProperty, fade);
                            n.LabelText.BeginAnimation(UIElement.OpacityProperty, fade);
                        }
                    }
                    if (popped.Connectors != null)
                    {
                        foreach (var c in popped.Connectors)
                        {
                            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                            fade.Completed += (s, e2) => _canvas.Children.Remove(c);
                            ((UIElement)c).BeginAnimation(UIElement.OpacityProperty, fade);
                        }
                    }
                    // Clear Expanded on the parent item so it can be re-opened
                    if (popped.Origin != null)
                    {
                        var parent = _menuItems.FirstOrDefault(m => m.Center == popped.Origin.Value);
                        if (parent != null)
                        {
                            parent.Expanded = false;
                        }
                    }
                    var previousLevel = _menuStack.Peek();
                    _centerText.Text = previousLevel.Name;
                    // no reload; remaining nodes are still visible
                }
                else
                {
                    HideMenu();
                }
            }
        }

        private void OnDeactivated(object? sender, EventArgs e)
        {
            if (_justShown) return;
            HideMenu();
        }

        private void HideMenu()
        {
            _hoverExecuteTimer.Stop();
            AnimateOut(() => Hide());
        }

        private void AnimateIn()
        {
            _isAnimating = true;

            // Animate menu items
            foreach (var item in _menuItems)
            {
                // Calculate target position
                var targetX = item.Center.X + Math.Cos(item.Angle * Math.PI / 180) * item.Radius;
                var targetY = item.Center.Y + Math.Sin(item.Angle * Math.PI / 180) * item.Radius;
                var offsetX = targetX - item.Center.X;
                var offsetY = targetY - item.Center.Y;

                // Animate ellipse
                var floatGroup = item.Visual.RenderTransform as TransformGroup;
                if (floatGroup != null && floatGroup.Children.Count >= 1)
                {
                    var tg = floatGroup.Children[0] as TransformGroup;
                    if (tg != null && tg.Children.Count >= 2)
                    {
                        var scale = tg.Children[0] as ScaleTransform;
                        var translate = tg.Children[1] as TranslateTransform;
                        if (scale != null && translate != null)
                        {
                            var scaleAnim = new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(300))
                            {
                                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
                            };
                            scaleAnim.Completed += (s, e) => _isAnimating = false;
                            var translateAnimX = new DoubleAnimation(0, offsetX, TimeSpan.FromMilliseconds(300))
                            {
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            };
                            var translateAnimY = new DoubleAnimation(0, offsetY, TimeSpan.FromMilliseconds(300))
                            {
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            };
                            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                            translate.BeginAnimation(TranslateTransform.XProperty, translateAnimX);
                            translate.BeginAnimation(TranslateTransform.YProperty, translateAnimY);
                        }
                    }
                }

                // Animate label
                var labelTg = item.LabelText.RenderTransform as TransformGroup;
                if (labelTg != null && labelTg.Children.Count >= 2)
                {
                    var labelScale = labelTg.Children[0] as ScaleTransform;
                    var labelTranslate = labelTg.Children[1] as TranslateTransform;
                    if (labelScale != null && labelTranslate != null)
                    {
                        var labelScaleAnim = new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(300))
                        {
                            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
                        };
                        var labelTranslateAnimX = new DoubleAnimation(0, offsetX, TimeSpan.FromMilliseconds(300))
                        {
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        var labelTranslateAnimY = new DoubleAnimation(0, offsetY, TimeSpan.FromMilliseconds(300))
                        {
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        labelScale.BeginAnimation(ScaleTransform.ScaleXProperty, labelScaleAnim);
                        labelScale.BeginAnimation(ScaleTransform.ScaleYProperty, labelScaleAnim);
                        labelTranslate.BeginAnimation(TranslateTransform.XProperty, labelTranslateAnimX);
                        labelTranslate.BeginAnimation(TranslateTransform.YProperty, labelTranslateAnimY);
                    }
                }
            }
        }

        private void AnimateOut(Action onComplete)
        {
            _isAnimating = true;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(50));
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

        // Ensure the canvas is large enough (or shifted) so that given target points (in canvas coordinates)
        // are fully visible inside the canvas. This method will increase the canvas size if needed and shift
        // existing children so that logical coordinates remain consistent. `half` is half the node size.
        private Point EnsureCanvasFitsTargets(List<Point> targets, double half)
        {
            if (_canvas == null) return new Point(0, 0);

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in targets)
            {
                minX = Math.Min(minX, p.X - half);
                minY = Math.Min(minY, p.Y - half);
                maxX = Math.Max(maxX, p.X + half);
                maxY = Math.Max(maxY, p.Y + half);
            }

            // current canvas bounds
            double canvasLeft = 0;
            double canvasTop = 0;
            double canvasRight = _canvas.Width;
            double canvasBottom = _canvas.Height;

            double extraLeft = Math.Max(0, canvasLeft - minX);
            double extraTop = Math.Max(0, canvasTop - minY);
            double extraRight = Math.Max(0, maxX - canvasRight);
            double extraBottom = Math.Max(0, maxY - canvasBottom);

            // If nothing to do, return zero shift
            if (extraLeft == 0 && extraTop == 0 && extraRight == 0 && extraBottom == 0) return new Point(0, 0);

            // Expand canvas size as needed
            double newWidth = _canvas.Width + extraLeft + extraRight;
            double newHeight = _canvas.Height + extraTop + extraBottom;

            // Shift all existing children by extraLeft, extraTop so their logical positions are preserved
            foreach (UIElement child in _canvas.Children)
            {
                // For canvas-based children we adjust their Canvas.Left/Top
                var left = Canvas.GetLeft(child);
                var top = Canvas.GetTop(child);
                if (!double.IsNaN(left)) Canvas.SetLeft(child, left + extraLeft);
                if (!double.IsNaN(top)) Canvas.SetTop(child, top + extraTop);
            }

            // Update stored centers for existing menu items
            for (int i = 0; i < _menuItems.Count; i++)
            {
                var mi = _menuItems[i];
                mi.Center = new Point(mi.Center.X + extraLeft, mi.Center.Y + extraTop);
            }

            // Move center point as well
            _centerPoint = new Point(_centerPoint.X + extraLeft, _centerPoint.Y + extraTop);

            // Update center visual elements
            PositionCenterElements();

            // Apply new size
            _canvas.Width = newWidth;
            _canvas.Height = newHeight;

            // Also adjust window size so the whole canvas is shown
            this.Width = Math.Max(this.Width, newWidth);
            this.Height = Math.Max(this.Height, newHeight);

            return new Point(extraLeft, extraTop);
        }

        // Create portal depth layers for dimensional effect
        private void CreatePortalDepthLayers()
        {
            var centerX = _canvas.Width / 2;
            var centerY = _canvas.Height / 2;

            // Create multiple concentric circles for depth illusion
            for (int layer = 0; layer < 5; layer++)
            {
                var radius = (_innerRadius + (layer * 8)) * _uiScale;
                var depthCircle = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromArgb(
                        (byte)(40 - layer * 6), // Decreasing opacity
                        (byte)(138 - layer * 20),
                        (byte)(43 - layer * 8),
                        (byte)(226 - layer * 30)
                    )),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 1, 3 }
                };

                Canvas.SetLeft(depthCircle, centerX - radius);
                Canvas.SetTop(depthCircle, centerY - radius);

                // Add subtle glow
                depthCircle.Effect = new DropShadowEffect
                {
                    Color = Color.FromArgb(80, 138, 43, 226),
                    BlurRadius = 2,
                    ShadowDepth = 0,
                    Opacity = 0.3
                };

                _canvas.Children.Add(depthCircle);
                Panel.SetZIndex(depthCircle, -5 + layer);

                // Add slow counter-rotation for hypnotic effect
                var rotateTransform = new RotateTransform(0, radius, radius);
                depthCircle.RenderTransform = rotateTransform;

                var rotationAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = layer % 2 == 0 ? 360 : -360, // Alternate directions
                    Duration = TimeSpan.FromSeconds(25 + (layer * 8)),
                    RepeatBehavior = RepeatBehavior.Forever
                };

                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
            }

            // Add central void effect
            var voidCore = new Ellipse
            {
                Width = (_innerRadius * 0.6) * _uiScale,
                Height = (_innerRadius * 0.6) * _uiScale,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(255, 0, 0, 0), 0),
                        new GradientStop(Color.FromArgb(180, 25, 25, 112), 0.8),
                        new GradientStop(Color.FromArgb(100, 138, 43, 226), 1)
                    }
                }
            };

            Canvas.SetLeft(voidCore, centerX - (voidCore.Width / 2));
            Canvas.SetTop(voidCore, centerY - (voidCore.Height / 2));

            voidCore.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.9
            };

            _canvas.Children.Add(voidCore);
            Panel.SetZIndex(voidCore, 1);
        }

        // Create animated vortex background
        private void CreateVortexBackground()
        {
            var centerX = _canvas.Width / 2;
            var centerY = _canvas.Height / 2;

            // Create multiple spiral layers for depth
            for (int layer = 0; layer < 3; layer++)
            {
                var spiralPath = new System.Windows.Shapes.Path();
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();
                
                var radius = 300 + (layer * 100);
                var spiralTurns = 4;
                var pointsPerTurn = 50;
                var totalPoints = spiralTurns * pointsPerTurn;
                
                pathFigure.StartPoint = new Point(centerX, centerY);
                
                for (int i = 0; i <= totalPoints; i++)
                {
                    var angle = (double)i / pointsPerTurn * 2 * Math.PI;
                    var currentRadius = (double)i / totalPoints * radius;
                    var x = centerX + Math.Cos(angle) * currentRadius;
                    var y = centerY + Math.Sin(angle) * currentRadius;
                    
                    var lineSegment = new LineSegment(new Point(x, y), true);
                    pathFigure.Segments.Add(lineSegment);
                }
                
                pathGeometry.Figures.Add(pathFigure);
                spiralPath.Data = pathGeometry;
                
                // Create gradient stroke for the spiral
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(120, 138, 43, 226), 0),    // Deep purple
                        new GradientStop(Color.FromArgb(80, 75, 0, 130), 0.5),     // Dark purple
                        new GradientStop(Color.FromArgb(150, 25, 25, 112), 1)      // Midnight blue
                    }
                };
                
                spiralPath.Stroke = gradientBrush;
                spiralPath.StrokeThickness = 2 - (layer * 0.5);
                spiralPath.Opacity = 0.6 - (layer * 0.15);
                
                // Add glow effect
                spiralPath.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(138, 43, 226),
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.4
                };
                
                _canvas.Children.Add(spiralPath);
                Panel.SetZIndex(spiralPath, -10 + layer);
                
                // Add rotation animation
                var rotateTransform = new RotateTransform(0, centerX, centerY);
                spiralPath.RenderTransform = rotateTransform;
                
                var rotationAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(30 + (layer * 10)), // Different speeds for layers
                    RepeatBehavior = RepeatBehavior.Forever
                };
                
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
            }
            
            // Add particle effect background
            CreateParticleField();
        }

        // Create floating particle field for cosmic effect
        private void CreateParticleField()
        {
            var random = new Random();
            var centerX = _canvas.Width / 2;
            var centerY = _canvas.Height / 2;
            
            for (int i = 0; i < 30; i++)
            {
                var particle = new Ellipse
                {
                    Width = 2 + random.NextDouble() * 4,
                    Height = 2 + random.NextDouble() * 4,
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)(50 + random.Next(100)),
                        (byte)(100 + random.Next(155)),
                        (byte)(50 + random.Next(100)),
                        (byte)(150 + random.Next(105))
                    ))
                };
                
                // Position randomly around the canvas
                var angle = random.NextDouble() * 2 * Math.PI;
                var distance = random.NextDouble() * 400;
                var x = centerX + Math.Cos(angle) * distance;
                var y = centerY + Math.Sin(angle) * distance;
                
                Canvas.SetLeft(particle, x);
                Canvas.SetTop(particle, y);
                
                // Add subtle glow
                particle.Effect = new DropShadowEffect
                {
                    Color = Color.FromArgb(100, 200, 150, 255),
                    BlurRadius = 3,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };
                
                _canvas.Children.Add(particle);
                Panel.SetZIndex(particle, -15);
                
                // Add floating animation
                var floatTransform = new TranslateTransform();
                particle.RenderTransform = floatTransform;
                
                var floatAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = random.Next(-20, 20),
                    Duration = TimeSpan.FromSeconds(4 + random.NextDouble() * 6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                
                var floatAnimationY = new DoubleAnimation
                {
                    From = 0,
                    To = random.Next(-15, 15),
                    Duration = TimeSpan.FromSeconds(3 + random.NextDouble() * 4),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                
                floatTransform.BeginAnimation(TranslateTransform.XProperty, floatAnimation);
                floatTransform.BeginAnimation(TranslateTransform.YProperty, floatAnimationY);
            }
        }

        // Dark matter styling helper methods
        private static Color CreateCosmicVariant(Color baseColor)
        {
            // Create cosmic/stellar color variant
            var hsl = RgbToHsl(baseColor);
            hsl.H = (hsl.H + 60) % 360; // Shift hue for cosmic effect
            hsl.S = Math.Min(1.0, hsl.S * 1.4); // Increase saturation
            hsl.L = Math.Min(0.8, hsl.L * 1.3); // Increase lightness but keep cosmic
            return HslToRgb(hsl);
        }

        private static Color CreateVoidVariant(Color baseColor)
        {
            // Create dark void color variant
            var hsl = RgbToHsl(baseColor);
            hsl.S = Math.Max(0.2, hsl.S * 0.6); // Reduce saturation
            hsl.L = Math.Max(0.1, hsl.L * 0.4); // Make much darker
            return HslToRgb(hsl);
        }

        // Legacy color methods for backward compatibility
        private static Color CreateNeonVariant(Color baseColor)
        {
            return CreateCosmicVariant(baseColor);
        }

        private static Color CreateAccentVariant(Color baseColor)
        {
            return CreateVoidVariant(baseColor);
        }

        private struct HslColor
        {
            public double H, S, L;
        }

        private static HslColor RgbToHsl(Color rgb)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;

            double max = Math.Max(Math.Max(r, g), b);
            double min = Math.Min(Math.Min(r, g), b);
            double diff = max - min;

            double h = 0, s = 0, l = (max + min) / 2;

            if (diff != 0)
            {
                s = l > 0.5 ? diff / (2 - max - min) : diff / (max + min);

                if (max == r) h = (g - b) / diff + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / diff + 2;
                else h = (r - g) / diff + 4;

                h /= 6;
            }

            return new HslColor { H = h * 360, S = s, L = l };
        }

        private static Color HslToRgb(HslColor hsl)
        {
            double h = hsl.H / 360;
            double s = hsl.S;
            double l = hsl.L;

            double r, g, b;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;

                r = HueToRgb(p, q, h + 1.0/3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0/3.0);
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0/6.0) return p + (q - p) * 6 * t;
            if (t < 1.0/2.0) return q;
            if (t < 2.0/3.0) return p + (q - p) * (2.0/3.0 - t) * 6;
            return p;
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
    public bool Expanded { get; set; } = false;
}

public class MenuLevel
{
    public List<ConfigItem> Items { get; set; } = new();
    public string Name { get; set; } = "MENU";
    public Point? Origin { get; set; }
    public List<RadialMenuItem>? CreatedNodes { get; set; }
    public List<UIElement>? Connectors { get; set; }
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

