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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RadialMenu.Controls;
using RadialMenu.Utilities;

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
    private double _innerRadius = 40;
    private double _outerRadius = 220;
    private MenuConfiguration _config = null!;
        private Stack<MenuLevel> _menuStack = new();
    private Ellipse _centerCircle = null!;
    private TextBlock _centerText = null!;
        private bool _isAnimating = false;
        private readonly DispatcherTimer _hoverExecuteTimer;
        
        // Spinning animation elements
        private Ellipse _spinningRing = null!;
        private Canvas _glowingTipContainer = null!;
        private Ellipse _glowingTip = null!;
        private Storyboard _spinningAnimation = null!;
        
        // Energy particle system
        private EnergyParticleSystem _particleSystem = null!;
        
        // Cursor position tracking for window positioning
        private Point _activationCursorPos;
        
        // Windows API for window positioning
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, 
            int X, int Y, int cx, int cy, uint uFlags);
            
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        // Multi-monitor support APIs
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
        
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
        
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        public RadialMenuWindow()
        {
            // Initialize hover execute timer
            _hoverExecuteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _hoverExecuteTimer.Tick += (s, e) =>
            {
                _hoverExecuteTimer.Stop();
                if (_hoveredItem != null)
                {
                    ExecuteItem(_hoveredItem);
                }
            };

            // Load appearance settings before creating UI elements so sizes/positions are consistent
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    if (settings?.Appearance != null)
                    {
                        if (settings.Appearance.UiScale > 0)
                            _uiScale = settings.Appearance.UiScale;
                        if (settings.Appearance.InnerRadius > 0)
                            _innerRadius = settings.Appearance.InnerRadius;
                        if (settings.Appearance.OuterRadius > 0)
                            _outerRadius = settings.Appearance.OuterRadius;
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
            Width = 1800 * _uiScale; // Increased by 200% (3x original size)
            Height = 1800 * _uiScale; // Increased by 200% (3x original size)
            WindowStartupLocation = WindowStartupLocation.Manual;
            
            // Main canvas
            _canvas = new Canvas
            {
                Width = 1800 * _uiScale, // Increased by 200% (3x original size)
                Height = 1800 * _uiScale, // Increased by 200% (3x original size)
                Background = Brushes.Transparent
            };

            // Add blur effect to background
            var blurEffect = new BlurEffect { Radius = 10 };
            
            // Modern flat center circle with sophisticated gradient
            _centerCircle = new Ellipse
            {
                Width = (_innerRadius * 2) * _uiScale,
                Height = (_innerRadius * 2) * _uiScale,
                Fill = CreateModernCenterGradient(),
                Stroke = new SolidColorBrush(Color.FromArgb(60, 45, 125, 210)),
                StrokeThickness = 1 * _uiScale,
                Effect = new DropShadowEffect 
                { 
                    Color = Colors.Black, 
                    BlurRadius = 12, 
                    Direction = 270, 
                    ShadowDepth = 3, 
                    Opacity = 0.25 
                }
            };
            Canvas.SetLeft(_centerCircle, (Width / 2) - (_centerCircle.Width / 2));
            Canvas.SetTop(_centerCircle, (Height / 2) - (_centerCircle.Height / 2));
            _canvas.Children.Add(_centerCircle);
            
            // Add hover event to center circle for navigation back
            _centerCircle.MouseEnter += OnCenterCircleMouseEnter;

            // Center text (shows current level or from settings)
            var centerText = "MENU";
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    if (!string.IsNullOrEmpty(settings?.Appearance?.CenterText))
                    {
                        centerText = settings.Appearance.CenterText;
                    }
                }
            }
            catch { }
            
            _centerText = new TextBlock
            {
                Text = centerText,
                Foreground = Brushes.White,
                FontSize = 16 * _uiScale,
                FontWeight = FontWeights.Medium,
                FontFamily = new FontFamily("Segoe UI, Arial, sans-serif"),
                TextAlignment = TextAlignment.Center,
                Effect = new DropShadowEffect 
                { 
                    Color = Colors.Black, 
                    BlurRadius = 3, 
                    Direction = 270, 
                    ShadowDepth = 1, 
                    Opacity = 0.7 
                }
            };
            // initial placement; will be repositioned when showing the menu
            Canvas.SetLeft(_centerText, (Width / 2) - 20 * _uiScale);
            Canvas.SetTop(_centerText, (Height / 2) - 8 * _uiScale);
            _canvas.Children.Add(_centerText);

            // Create spinning animation ring
            CreateSpinningAnimationElements();

            Content = _canvas;

            // Event handlers
            MouseMove += OnMouseMove;
            KeyDown += OnKeyDown;
            Deactivated += OnDeactivated;
        }

        private void CreateSpinningAnimationElements()
        {
            double centerX = Width / 2;
            double centerY = Height / 2;
            double ringRadius = (_outerRadius + 15) * _uiScale; // Slightly outside the outer radius

            // Create spinning ring - a thin ellipse at the outer radius
            _spinningRing = new Ellipse
            {
                Width = ringRadius * 2,
                Height = ringRadius * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(60, 45, 125, 210)), // More subtle semi-transparent blue
                StrokeThickness = 1.5 * _uiScale,
                Fill = Brushes.Transparent,
                RenderTransformOrigin = new Point(0.5, 0.5), // Rotate around center
                RenderTransform = new RotateTransform(),
                Visibility = Visibility.Collapsed // Initially hidden
            };
            
            // Position the ring at center
            Canvas.SetLeft(_spinningRing, centerX - ringRadius);
            Canvas.SetTop(_spinningRing, centerY - ringRadius);
            
            // Create a container for the glowing tip that will rotate around the center
            _glowingTipContainer = new Canvas
            {
                Width = ringRadius * 2,
                Height = ringRadius * 2,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(),
                Visibility = Visibility.Collapsed // Initially hidden
            };
            
            // Create glowing tip - a small bright ellipse
            _glowingTip = new Ellipse
            {
                Width = 8 * _uiScale,
                Height = 8 * _uiScale,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(255, 120, 200, 255), 0.0), // Bright cyan center
                        new GradientStop(Color.FromArgb(150, 45, 125, 210), 0.5),  // Blue middle
                        new GradientStop(Colors.Transparent, 1.0) // Transparent edge
                    }
                },
                Effect = new BlurEffect { Radius = 5 }
            };
            
            // Position the glowing tip on the right edge of the container (will rotate around)
            Canvas.SetLeft(_glowingTip, ringRadius * 2 - (_glowingTip.Width / 2));
            Canvas.SetTop(_glowingTip, ringRadius - (_glowingTip.Height / 2));
            _glowingTipContainer.Children.Add(_glowingTip);
            
            // Position the container at center
            Canvas.SetLeft(_glowingTipContainer, centerX - ringRadius);
            Canvas.SetTop(_glowingTipContainer, centerY - ringRadius);
            
            // Add to canvas (behind menu items)
            _canvas.Children.Add(_spinningRing);
            _canvas.Children.Add(_glowingTipContainer);
            
            // Create the rotation animation
            CreateSpinningAnimation();
        }

        private void CreateSpinningAnimation()
        {
            // Create the storyboard for continuous rotation
            _spinningAnimation = new Storyboard();

            // Animation for the ring rotation
            var ringAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(4), // Slower, more relaxed rotation
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(ringAnimation, _spinningRing);
            Storyboard.SetTargetProperty(ringAnimation, new PropertyPath("RenderTransform.Angle"));

            // Animation for the glowing tip container - same rotation
            var tipAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(4), // Same duration as ring
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            Storyboard.SetTarget(tipAnimation, _glowingTipContainer);
            Storyboard.SetTargetProperty(tipAnimation, new PropertyPath("RenderTransform.Angle"));

            // Animation for the glowing tip pulsing - adds extra visual interest
            var pulseAnimation = new DoubleAnimation
            {
                From = 0.6,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(1), // Fast pulse
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            Storyboard.SetTarget(pulseAnimation, _glowingTip);
            Storyboard.SetTargetProperty(pulseAnimation, new PropertyPath("Opacity"));

            // Add animations to storyboard
            _spinningAnimation.Children.Add(ringAnimation);
            _spinningAnimation.Children.Add(tipAnimation);
            _spinningAnimation.Children.Add(pulseAnimation);
        }

        private void CreateEnergyParticleSystem()
        {
            // Use the same dimensions as the canvas to ensure proper rendering
            var canvasWidth = _canvas?.Width ?? 0;
            var canvasHeight = _canvas?.Height ?? 0;
            
            // Ensure we have valid dimensions
            if (canvasWidth <= 0 || canvasHeight <= 0 || double.IsNaN(canvasWidth) || double.IsNaN(canvasHeight))
            {
                canvasWidth = 1800 * _uiScale;
                canvasHeight = 1800 * _uiScale;
            }
            
            // Create the energy particle system with enhanced fantasy colors
            _particleSystem = new EnergyParticleSystem
            {
                Width = canvasWidth, // Match canvas width
                Height = canvasHeight, // Match canvas height
                UIScale = _uiScale,
                ParticleCount = 36, // Old school optimization: fewer particles, bigger impact
                SpiralTurns = 2.5, // Reduced for performance
                AnimationDuration = TimeSpan.FromMilliseconds(2200), // Shorter for snappier feel
                PrimaryColor = Color.FromArgb(255, 100, 220, 255), // Brighter cyan fantasy energy
                SecondaryColor = Color.FromArgb(255, 200, 120, 255), // Golden-purple magic
                ParticleSize = 24.0, // Larger base size to compensate for fewer particles
                TestMode = false // Disable test mode - use real particles
            };
            
            // Position it behind menu items but above background elements
            Canvas.SetLeft(_particleSystem, 0);
            Canvas.SetTop(_particleSystem, 0);
            
            // Set z-index to make particles visible above background but behind menu items
            Canvas.SetZIndex(_particleSystem, 1);
            
            // Add to canvas (should be behind menu items)
            _canvas?.Children.Add(_particleSystem);
            
            System.Diagnostics.Debug.WriteLine($"[RadialMenuWindow] Created particle system - Canvas size: {canvasWidth}x{canvasHeight}, UIScale: {_uiScale}, Children count: {_canvas?.Children.Count ?? -1}");
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

        /// <summary>
        /// Creates a modern gradient brush for menu nodes with sophisticated visual effects
        /// </summary>
        private Brush CreateModernNodeGradient(Color baseColor)
        {
            var gradient = new RadialGradientBrush();
            gradient.GradientOrigin = new Point(0.3, 0.3);
            gradient.Center = new Point(0.5, 0.5);
            gradient.RadiusX = 1.0;
            gradient.RadiusY = 1.0;

            // Create a sophisticated multi-stop gradient
            var lightColor = LightenColor(baseColor, 0.4f);
            var midColor = baseColor;
            var darkColor = DarkenColor(baseColor, 0.3f);

            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(240, lightColor.R, lightColor.G, lightColor.B), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(210, midColor.R, midColor.G, midColor.B), 0.6));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(180, darkColor.R, darkColor.G, darkColor.B), 1.0));

            return gradient;
        }

        /// <summary>
        /// Lightens a color by the specified factor
        /// </summary>
        private Color LightenColor(Color color, float factor)
        {
            var r = Math.Min(255, color.R + (int)((255 - color.R) * factor));
            var g = Math.Min(255, color.G + (int)((255 - color.G) * factor));
            var b = Math.Min(255, color.B + (int)((255 - color.B) * factor));
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Darkens a color by the specified factor
        /// </summary>
        private Color DarkenColor(Color color, float factor)
        {
            var r = Math.Max(0, color.R - (int)(color.R * factor));
            var g = Math.Max(0, color.G - (int)(color.G * factor));
            var b = Math.Max(0, color.B - (int)(color.B * factor));
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Creates a vibrant hover gradient with enhanced brightness and modern flat design
        /// </summary>
        private Brush CreateModernHoverGradient(Color baseColor)
        {
            var gradient = new RadialGradientBrush();
            gradient.GradientOrigin = new Point(0.2, 0.2);
            gradient.Center = new Point(0.5, 0.5);
            gradient.RadiusX = 1.2;
            gradient.RadiusY = 1.2;

            // Create an enhanced hover gradient with more vibrant colors
            var brightColor = LightenColor(baseColor, 0.6f);
            var midColor = LightenColor(baseColor, 0.2f);
            var edgeColor = baseColor;

            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, brightColor.R, brightColor.G, brightColor.B), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(240, midColor.R, midColor.G, midColor.B), 0.4));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(220, edgeColor.R, edgeColor.G, edgeColor.B), 1.0));

            return gradient;
        }

        /// <summary>
        /// Creates a sophisticated gradient for the center circle with modern flat design principles
        /// </summary>
        private Brush CreateModernCenterGradient()
        {
            var gradient = new RadialGradientBrush();
            gradient.GradientOrigin = new Point(0.3, 0.3);
            gradient.Center = new Point(0.5, 0.5);
            gradient.RadiusX = 1.0;
            gradient.RadiusY = 1.0;

            // Create a sophisticated center gradient with subtle depth
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(240, 50, 50, 50), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(220, 30, 30, 30), 0.6));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(200, 20, 20, 20), 1.0));

            return gradient;
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
            
            // Load and apply appearance settings
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    if (settings?.Appearance != null)
                    {
                        UpdateUIScale(settings.Appearance.UiScale > 0 ? settings.Appearance.UiScale : 1.0);
                        UpdateRadii(
                            settings.Appearance.InnerRadius > 0 ? settings.Appearance.InnerRadius : 40,
                            settings.Appearance.OuterRadius > 0 ? settings.Appearance.OuterRadius : 220
                        );
                        UpdateCenterText(settings.Appearance.CenterText);
                        UpdateTheme(settings.Appearance.Theme);
                        UpdateParticleSettings(settings.Appearance.ParticlesEnabled);
                    }
                }
            }
            catch { }
            
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
                    _menuStack.Push(new MenuLevel { Items = itemsToLoad, Name = "MENU", CreatedNodes = created, Origin = null });
                    
                    // Load center text from settings or use default
                    var centerText = "MENU";
                    try
                    {
                        if (System.Windows.Application.Current is App app && app.SettingsService != null)
                        {
                            var settings = app.SettingsService.Load();
                            if (!string.IsNullOrEmpty(settings?.Appearance?.CenterText))
                            {
                                centerText = settings.Appearance.CenterText;
                            }
                        }
                    }
                    catch { }
                    
                    _centerText.Text = centerText;
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

            // Store the cursor position for potential window positioning
            _activationCursorPos = new Point(x, y);

            Left = x - Width / 2;
            Top = y - Height / 2;
            _centerPoint = new Point(_canvas.Width / 2, _canvas.Height / 2);

            Log($"ShowAt called at cursor position ({x}, {y}). Config items: {_config?.Items?.Count ?? 0}. Canvas size: {_canvas.Width}x{_canvas.Height}. Center: {_centerPoint}");

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
            // Load center text from settings
            var centerText = "MENU";
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    if (!string.IsNullOrEmpty(settings?.Appearance?.CenterText))
                    {
                        centerText = settings.Appearance.CenterText;
                    }
                }
            }
            catch { }
            
            _menuStack.Push(new MenuLevel { Items = itemsToLoad, Name = centerText, CreatedNodes = created, Origin = null });
            _centerText.Text = centerText;

            // Re-center center text after text change
            PositionCenterElements();

            // Always recreate particle system to ensure fresh state (prevents repeated activation issues)
            if (_particleSystem != null)
            {
                // Stop and remove existing particle system
                _particleSystem.Stop();
                _canvas.Children.Remove(_particleSystem);
            }
            
            // Create fresh particle system every time
            CreateEnergyParticleSystem();
            
            // Set opacity to 0 before showing to prevent flash
            Opacity = 0;
            Show();
            Activate();
            AnimateIn();
        }

        private List<RadialMenuItem> LoadMenuItems(List<ConfigItem> configItems, Point? origin = null)
        {
            // Clear existing items
            foreach (var item in _menuItems)
            {
                _canvas.Children.Remove(item.Visual);
                _canvas.Children.Remove(item.LabelText);
                // Clean up progress ring if it exists
                if (item.ProgressRing != null)
                {
                    _canvas.Children.Remove(item.ProgressRing);
                }
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

                // Create modern flat circular visual with gradient
                var ellipse = new Ellipse
                {
                    Width = nodeSize,
                    Height = nodeSize,
                    Fill = CreateModernNodeGradient(itemColor),
                    Stroke = new SolidColorBrush(Color.FromArgb(80, itemColor.R, itemColor.G, itemColor.B)),
                    StrokeThickness = 0.5,
                    Effect = new DropShadowEffect 
                    { 
                        Color = Colors.Black, 
                        BlurRadius = 8, 
                        Direction = 270, 
                        ShadowDepth = 2, 
                        Opacity = 0.15 
                    }
                };

                // Place ellipse at target position
                Canvas.SetLeft(ellipse, targetX - nodeSize / 2);
                Canvas.SetTop(ellipse, targetY - nodeSize / 2);

                // Enhanced animations for both root and submenu items
                if (origin == null)
                {
                    // Modern entrance animation for root menu items
                    var scale = new ScaleTransform(0.1, 0.1);
                    var opacity = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300 + i * 50));
                    opacity.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    
                    var scaleAnim = new DoubleAnimation(0.1, 1.0, TimeSpan.FromMilliseconds(400 + i * 50));
                    scaleAnim.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
                    
                    ellipse.RenderTransform = scale;
                    ellipse.RenderTransformOrigin = new Point(0.5, 0.5);
                    ellipse.Opacity = 0;
                    
                    // Staggered animation based on index for cascade effect
                    ellipse.BeginAnimation(UIElement.OpacityProperty, opacity);
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }
                else
                {
                    // Enhanced submenu animation with sophisticated motion
                    var initialTranslateX = originPoint.X - targetX;
                    var initialTranslateY = originPoint.Y - targetY;
                    var translate = new TranslateTransform(initialTranslateX, initialTranslateY);
                    var scale = new ScaleTransform(0.1, 0.1);
                    var rotate = new RotateTransform(0);
                    
                    var tg = new TransformGroup();
                    tg.Children.Add(scale);
                    tg.Children.Add(rotate);
                    tg.Children.Add(translate);
                    
                    ellipse.RenderTransform = tg;
                    ellipse.RenderTransformOrigin = new Point(0.5, 0.5);
                    ellipse.Opacity = 0;

                    // Staggered timing for cascade effect
                    var delay = i * 80;
                    var duration = 450;
                    
                    // Smooth easing functions
                    var backEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
                    var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };
                    
                    // Animate translate with smooth motion
                    var animX = new DoubleAnimation(initialTranslateX, 0, TimeSpan.FromMilliseconds(duration)) 
                    { 
                        EasingFunction = backEase,
                        BeginTime = TimeSpan.FromMilliseconds(delay)
                    };
                    var animY = new DoubleAnimation(initialTranslateY, 0, TimeSpan.FromMilliseconds(duration)) 
                    { 
                        EasingFunction = backEase,
                        BeginTime = TimeSpan.FromMilliseconds(delay)
                    };
                    
                    // Animate scale with bounce
                    var animScale = new DoubleAnimation(0.1, 1.0, TimeSpan.FromMilliseconds(duration)) 
                    { 
                        EasingFunction = backEase,
                        BeginTime = TimeSpan.FromMilliseconds(delay)
                    };
                    
                    // Subtle rotation for dynamic effect
                    var animRotate = new DoubleAnimation(180, 0, TimeSpan.FromMilliseconds(duration)) 
                    { 
                        EasingFunction = cubicEase,
                        BeginTime = TimeSpan.FromMilliseconds(delay)
                    };
                    
                    // Fade in
                    var animOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(duration - 100)) 
                    { 
                        EasingFunction = cubicEase,
                        BeginTime = TimeSpan.FromMilliseconds(delay + 50)
                    };
                    
                    translate.BeginAnimation(TranslateTransform.XProperty, animX);
                    translate.BeginAnimation(TranslateTransform.YProperty, animY);
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);
                    rotate.BeginAnimation(RotateTransform.AngleProperty, animRotate);
                    ellipse.BeginAnimation(UIElement.OpacityProperty, animOpacity);
                }

                // Add subtle glow
                ellipse.Effect = new DropShadowEffect { Color = itemColor, BlurRadius = 12, ShadowDepth = 0, Opacity = 0 };

                _canvas.Children.Add(ellipse);
                // Ensure nodes render above center circle
                Panel.SetZIndex(ellipse, 2);

                // Create modern flat label centered inside the circle
                var label = new TextBlock
                {
                    Text = config.Icon + "\n" + config.Label,
                    Foreground = Brushes.White,
                    FontSize = 11 * _uiScale,
                    FontWeight = FontWeights.Medium,
                    FontFamily = new FontFamily("Segoe UI, Arial, sans-serif"),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = nodeSize - 12,
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect 
                    { 
                        Color = Colors.Black, 
                        BlurRadius = 2, 
                        Direction = 270, 
                        ShadowDepth = 1, 
                        Opacity = 0.5 
                    }
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var labelSize = label.DesiredSize;
                Canvas.SetLeft(label, targetX - labelSize.Width / 2);
                Canvas.SetTop(label, targetY - labelSize.Height / 2);
                _canvas.Children.Add(label);
                Panel.SetZIndex(label, 3);
                
                // Animate label with same timing as the ellipse
                if (origin == null)
                {
                    // Root menu label animation
                    var labelOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350 + i * 50));
                    labelOpacity.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    labelOpacity.BeginTime = TimeSpan.FromMilliseconds(150); // Slight delay after ellipse starts
                    label.Opacity = 0;
                    label.BeginAnimation(UIElement.OpacityProperty, labelOpacity);
                }
                else
                {
                    // Submenu label animation
                    var delay = i * 80 + 100; // Slight delay after ellipse
                    var labelOpacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    labelOpacity.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    labelOpacity.BeginTime = TimeSpan.FromMilliseconds(delay);
                    label.Opacity = 0;
                    label.BeginAnimation(UIElement.OpacityProperty, labelOpacity);
                }

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
                // make visual feel interactive
                ellipse.Cursor = Cursors.Hand;
                // attach direct mouse events to ensure hover/click works even if proximity math misses
                ellipse.MouseEnter += (s, ev) =>
                {
                    if (_hoveredItem != menuItem)
                    {
                        UnhoverAll();
                        _hoverExecuteTimer.Stop(); // Stop any pending execution
                        HoverItem(menuItem);
                        _hoveredItem = menuItem;
                        _hoverExecuteTimer.Start(); // Start timer for new hover
                    }
                };
                ellipse.MouseLeave += (s, ev) =>
                {
                    // only unhover if this item is currently hovered
                    if (_hoveredItem == menuItem)
                    {
                        UnhoverAll();
                        _hoverExecuteTimer.Stop(); // Stop pending execution
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
            var baseChildDistance = 140.0 * _uiScale; // base scaled distance from parent

            var connectors = new List<UIElement>();

            // Capture parent center BEFORE any potential canvas shift
            var originalParentCenter = new Point(parent.Center.X, parent.Center.Y);

            // Calculate initial target positions
            var targets = new List<Point>();
            for (int i = 0; i < count; i++)
            {
                var angle = startAngle + i * step;
                var rad = angle * Math.PI / 180;
                var tx = parent.Center.X + Math.Cos(rad) * baseChildDistance;
                var ty = parent.Center.Y + Math.Sin(rad) * baseChildDistance;
                targets.Add(new Point(tx, ty));
            }

            // Calculate scale factor to fit all targets within canvas bounds
            double scaleFactor = 1.0;
            try
            {
                var half = ((48.0 * 1.5) / 2.0) * _uiScale; // scaled half node size
                scaleFactor = CalculateScaleToFitTargets(targets, half);
            }
            catch { }

            // Apply scale factor to child distance and recalculate positions
            var adjustedChildDistance = baseChildDistance * scaleFactor;
            for (int i = 0; i < count; i++)
            {
                var angle = startAngle + i * step;
                var rad = angle * Math.PI / 180;
                var tx = parent.Center.X + Math.Cos(rad) * adjustedChildDistance;
                var ty = parent.Center.Y + Math.Sin(rad) * adjustedChildDistance;
                targets[i] = new Point(tx, ty);
            }

            for (int i = 0; i < count; i++)
            {
                var cfg = configItems[i];
                var angle = startAngle + i * step;
                var rad = angle * Math.PI / 180;
                var targetX = targets[i].X;
                var targetY = targets[i].Y;

                Color itemColor;
                try { itemColor = (Color)ColorConverter.ConvertFromString(cfg.Color ?? "#FF2D7DD2"); }
                catch { itemColor = Color.FromRgb(45, 125, 210); }

                var nodeSize = (48.0 * 1.5) * _uiScale; // scaled node size
                var ellipse = new Ellipse
                {
                    Width = nodeSize,
                    Height = nodeSize,
                    Fill = new SolidColorBrush(Color.FromArgb(220, itemColor.R, itemColor.G, itemColor.B)),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, itemColor.R, itemColor.G, itemColor.B)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(ellipse, targetX - nodeSize / 2);
                Canvas.SetTop(ellipse, targetY - nodeSize / 2);

                // connector line (behind nodes)
                var line = new System.Windows.Shapes.Line
                {
                    X1 = originalParentCenter.X,
                    Y1 = originalParentCenter.Y,
                    X2 = originalParentCenter.X,
                    Y2 = originalParentCenter.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    StrokeThickness = 2,
                    Opacity = 0.8
                };
                // add connector behind
                _canvas.Children.Add(line);
                connectors.Add(line);

                // initial transform from parent center -> will animate into place with spring
                var translate = new TranslateTransform(originalParentCenter.X - targetX, originalParentCenter.Y - targetY);
                var scale = new ScaleTransform(0.3, 0.3);
                var tg = new TransformGroup();
                tg.Children.Add(scale);
                tg.Children.Add(translate);
                ellipse.RenderTransform = tg;
                ellipse.RenderTransformOrigin = new Point(0.5, 0.5);
                ellipse.Effect = new DropShadowEffect { Color = itemColor, BlurRadius = 12, ShadowDepth = 0, Opacity = 0 };

                _canvas.Children.Add(ellipse);

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
                Canvas.SetLeft(label, targetX - labelSize.Width / 2);
                Canvas.SetTop(label, targetY - labelSize.Height / 2);
                _canvas.Children.Add(label);

                // Animate line end to target point
                var x2Anim = new DoubleAnimation(originalParentCenter.X, targetX, TimeSpan.FromMilliseconds(480)) { EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 6, EasingMode = EasingMode.EaseOut } };
                var y2Anim = new DoubleAnimation(originalParentCenter.Y, targetY, TimeSpan.FromMilliseconds(480)) { EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 6, EasingMode = EasingMode.EaseOut } };
                line.BeginAnimation(System.Windows.Shapes.Line.X2Property, x2Anim);
                line.BeginAnimation(System.Windows.Shapes.Line.Y2Property, y2Anim);

                // animate translate and scale with springy elastic ease
                var animX = new DoubleAnimation(originalParentCenter.X - targetX, 0, TimeSpan.FromMilliseconds(520)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 } };
                var animY = new DoubleAnimation(originalParentCenter.Y - targetY, 0, TimeSpan.FromMilliseconds(520)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 } };
                var animScale = new DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(520)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 } };
                translate.BeginAnimation(TranslateTransform.XProperty, animX);
                translate.BeginAnimation(TranslateTransform.YProperty, animY);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScale);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScale);

                var menuItem = new RadialMenuItem
                {
                    Config = cfg,
                    Visual = ellipse,
                    LabelText = label,
                    Angle = angle,
                    BaseColor = itemColor,
                    Center = new Point(targetX, targetY),
                    Radius = nodeSize / 2
                };

                _menuItems.Add(menuItem);
                // make interactive
                ellipse.Cursor = Cursors.Hand;
                ellipse.MouseEnter += (s, ev) =>
                {
                    if (_hoveredItem != menuItem)
                    {
                        UnhoverAll();
                        _hoverExecuteTimer.Stop(); // Stop any pending execution
                        HoverItem(menuItem);
                        _hoveredItem = menuItem;
                        _hoverExecuteTimer.Start(); // Start timer for new hover
                    }
                };
                ellipse.MouseLeave += (s, ev) =>
                {
                    if (_hoveredItem == menuItem) 
                    {
                        UnhoverAll();
                        _hoverExecuteTimer.Stop(); // Stop pending execution
                    }
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

        private System.Windows.Shapes.Path CreateProgressRing(RadialMenuItem item, double progress)
        {
            var center = item.Center;
            var radius = item.Radius + 4; // Slightly larger than the node
            var strokeWidth = 3.0;
            
            var geometry = new PathGeometry();
            var figure = new PathFigure();
            
            // Calculate arc based on progress (0.0 to 1.0)
            var angle = progress * 360.0;
            var angleRad = (angle - 90) * Math.PI / 180.0; // Start from top
            
            // Start point (top of circle)
            var startX = center.X;
            var startY = center.Y - radius;
            figure.StartPoint = new Point(startX, startY);
            
            if (progress > 0)
            {
                // End point
                var endX = center.X + Math.Cos(angleRad) * radius;
                var endY = center.Y + Math.Sin(angleRad) * radius;
                
                var isLargeArc = angle > 180;
                
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(endX, endY),
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = isLargeArc
                });
            }
            
            geometry.Figures.Add(figure);
            
            var path = new System.Windows.Shapes.Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = strokeWidth,
                Fill = null,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            
            // Position and add to canvas
            _canvas.Children.Add(path);
            Panel.SetZIndex(path, 4); // Above everything else
            
            return path;
        }

        private void AnimateProgressRing(System.Windows.Shapes.Path progressRing, double durationMs)
        {
            // Create a custom animation to update the progress ring geometry
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 FPS
            var startTime = DateTime.Now;
            var duration = TimeSpan.FromMilliseconds(durationMs);
            
            timer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / durationMs);
                
                if (progress >= 1.0)
                {
                    timer.Stop();
                    return;
                }
                
                // Update the progress ring geometry
                UpdateProgressRingGeometry(progressRing, progress);
            };
            
            timer.Start();
        }

        private void UpdateProgressRingGeometry(System.Windows.Shapes.Path progressRing, double progress)
        {
            // Find the RadialMenuItem this progress ring belongs to
            RadialMenuItem? item = null;
            foreach (var menuItem in _menuItems)
            {
                if (menuItem.ProgressRing == progressRing)
                {
                    item = menuItem;
                    break;
                }
            }
            
            if (item == null) return;
            
            var center = item.Center;
            var radius = item.Radius + 4; // Slightly larger than the node
            
            var geometry = new PathGeometry();
            var figure = new PathFigure();
            
            // Calculate arc based on progress (0.0 to 1.0)
            var angle = progress * 360.0;
            var angleRad = (angle - 90) * Math.PI / 180.0; // Start from top
            
            // Start point (top of circle)
            var startX = center.X;
            var startY = center.Y - radius;
            figure.StartPoint = new Point(startX, startY);
            
            if (progress > 0)
            {
                // End point
                var endX = center.X + Math.Cos(angleRad) * radius;
                var endY = center.Y + Math.Sin(angleRad) * radius;
                
                var isLargeArc = angle > 180;
                
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(endX, endY),
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = isLargeArc
                });
            }
            
            geometry.Figures.Add(figure);
            progressRing.Data = geometry;
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
                var scaleX = new DoubleAnimation(scale.ScaleX, 1.1, TimeSpan.FromMilliseconds(150)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                var scaleY = new DoubleAnimation(scale.ScaleY, 1.1, TimeSpan.FromMilliseconds(150)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }

            // Create enhanced hover gradient with modern flat design
            var hoverGradient = CreateModernHoverGradient(item.BaseColor);
            item.Visual.Fill = hoverGradient;

            // Modern flat stroke with subtle glow
            item.Visual.Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
            item.Visual.StrokeThickness = 2;

            // Create and animate progress ring
            item.ProgressRing = CreateProgressRing(item, 0.0);
            AnimateProgressRing(item.ProgressRing, 500); // 500ms to complete the circle

            // Increase glow with brighter color
            if (item.Visual.Effect is DropShadowEffect glow)
            {
                glow.Color = Colors.White; // Use white for better glow effect
                var opa = new DoubleAnimation(glow.Opacity, 1.0, TimeSpan.FromMilliseconds(150));
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, opa);
                glow.BlurRadius = 25; // Increased blur for more prominent glow
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

                // Restore modern flat gradient
                item.Visual.Fill = CreateModernNodeGradient(item.BaseColor);
                
                // Reset stroke with subtle modern flat styling
                item.Visual.Stroke = new SolidColorBrush(Color.FromArgb(80, item.BaseColor.R, item.BaseColor.G, item.BaseColor.B));
                item.Visual.StrokeThickness = 0.5;
                
                // Remove progress ring if it exists
                if (item.ProgressRing != null)
                {
                    _canvas.Children.Remove(item.ProgressRing);
                    item.ProgressRing = null;
                }
                
                if (item.Visual.Effect is DropShadowEffect glow)
                {
                    glow.Color = item.BaseColor; // Reset to item color
                    var opa = new DoubleAnimation(glow.Opacity, 0.0, TimeSpan.FromMilliseconds(150));
                    glow.BeginAnimation(DropShadowEffect.OpacityProperty, opa);
                    glow.BlurRadius = 12;
                }

                item.LabelText.FontWeight = FontWeights.SemiBold;
            }
            _hoveredItem = null;
        }

        private RECT GetMonitorBounds(Point cursorPosition)
        {
            try
            {
                var point = new POINT { X = (int)cursorPosition.X, Y = (int)cursorPosition.Y };
                var hMonitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
                
                if (hMonitor != IntPtr.Zero)
                {
                    var monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
                    
                    if (GetMonitorInfo(hMonitor, ref monitorInfo))
                    {
                        return monitorInfo.rcWork; // Use work area (excludes taskbar)
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error getting monitor bounds: {ex.Message}");
            }
            
            // Fallback to primary screen
            return new RECT
            {
                Left = 0,
                Top = 0,
                Right = (int)SystemParameters.PrimaryScreenWidth,
                Bottom = (int)SystemParameters.PrimaryScreenHeight
            };
        }

        private async Task PositionWindowNearCursor(Process process, string executableName)
        {
            try
            {
                // Wait for the process to start and create its main window
                await Task.Delay(500); // Initial delay to let the process start
                
                // Try to find and position the window for up to 5 seconds
                for (int attempts = 0; attempts < 10; attempts++)
                {
                    if (process.HasExited) return;
                    
                    // Wait for the main window handle to be available
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        var hwnd = process.MainWindowHandle;
                        
                        // Get current window size
                        if (GetWindowRect(hwnd, out RECT rect))
                        {
                            int windowWidth = rect.Right - rect.Left;
                            int windowHeight = rect.Bottom - rect.Top;
                            
                            // Calculate position near cursor, but ensure window is fully visible
                            int newX = (int)(_activationCursorPos.X - windowWidth / 2);
                            int newY = (int)(_activationCursorPos.Y - windowHeight / 2);
                            
                            // Get the monitor bounds where the cursor was when menu was activated
                            var monitorBounds = GetMonitorBounds(_activationCursorPos);
                            
                            // Ensure window stays within monitor bounds
                            newX = Math.Max(monitorBounds.Left, Math.Min(newX, monitorBounds.Right - windowWidth));
                            newY = Math.Max(monitorBounds.Top, Math.Min(newY, monitorBounds.Bottom - windowHeight));
                            
                            // Position the window
                            SetWindowPos(hwnd, IntPtr.Zero, newX, newY, 0, 0, 
                                SWP_NOZORDER | SWP_NOSIZE);
                            
                            Log($"Positioned window '{executableName}' at ({newX}, {newY}) near cursor ({_activationCursorPos.X}, {_activationCursorPos.Y}) on monitor bounds ({monitorBounds.Left}, {monitorBounds.Top}, {monitorBounds.Right}, {monitorBounds.Bottom})");
                            return;
                        }
                    }
                    
                    await Task.Delay(500); // Wait before next attempt
                }
                
                Log($"Could not position window for '{executableName}' - main window handle not found");
            }
            catch (Exception ex)
            {
                Log($"Error positioning window for '{executableName}': {ex.Message}");
            }
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
                    
                    // Trigger particle effect for submenu opening
                    StartSubmenuParticleEffect(item.Center);
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
                            var launchStartInfo = new ProcessStartInfo
                            {
                                FileName = item.Config.Path,
                                UseShellExecute = true
                            };
                            
                            // Set working directory to the executable's directory if it's a file path
                            if (!string.IsNullOrWhiteSpace(item.Config.Path) && System.IO.File.Exists(item.Config.Path))
                            {
                                launchStartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(item.Config.Path);
                            }
                            
                            var launchProcess = Process.Start(launchStartInfo);
                            if (launchProcess != null)
                            {
                                _ = PositionWindowNearCursor(launchProcess, item.Config.Path ?? "unknown");
                            }
                            break;

                        case "url":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = item.Config.Path,
                                UseShellExecute = true
                            });
                            break;

                        case "folder":
                            var folderProcess = Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = item.Config.Path,
                                UseShellExecute = true
                            });
                            if (folderProcess != null)
                            {
                                _ = PositionWindowNearCursor(folderProcess, "explorer.exe");
                            }
                            break;

                        case "command":
                            if (string.IsNullOrWhiteSpace(item.Config.Path)) break;
                            var parts = item.Config.Path.Split(new[] { ' ' }, 2);
                            var commandStartInfo = new ProcessStartInfo
                            {
                                FileName = parts[0],
                                Arguments = parts.Length > 1 ? parts[1] : "",
                                UseShellExecute = true
                            };
                            
                            // Set working directory to the executable's directory if it's a file path
                            if (System.IO.File.Exists(parts[0]))
                            {
                                commandStartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(parts[0]);
                            }
                            
                            var commandProcess = Process.Start(commandStartInfo);
                            if (commandProcess != null)
                            {
                                _ = PositionWindowNearCursor(commandProcess, parts[0]);
                            }
                            break;

                        case "discord":
                            if (string.IsNullOrWhiteSpace(item.Config.Path)) break;
                            
                            try
                            {
                                // Get AutoHotkey executable and appropriate script for the version
                                var (ahkExecutable, scriptPath) = GetAutoHotkeyInfo(item.Config.Path);
                                
                                // Check if the script exists
                                if (!System.IO.File.Exists(scriptPath))
                                {
                                    MessageBox.Show($"Discord navigation script not found in application directory.\n\nExpected location: {scriptPath}\n\nPlease ensure the script is in the same folder as the application executable.", "Discord Script Missing", 
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                    break;
                                }

                                var discordProcess = Process.Start(new ProcessStartInfo
                                {
                                    FileName = ahkExecutable,
                                    Arguments = $"\"{scriptPath}\" \"{item.Config.Path}\"",
                                    UseShellExecute = true,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                });
                            }
                            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
                            {
                                // AutoHotkey not found in PATH
                                var result = MessageBox.Show("AutoHotkey is not installed or not found in PATH.\n\nWould you like to download AutoHotkey from the official website?\n\nClick Yes to open the download page, or No to cancel.", 
                                    "AutoHotkey Required", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                
                                if (result == MessageBoxResult.Yes)
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "https://www.autohotkey.com/",
                                        UseShellExecute = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to run Discord navigation script: {ex.Message}", "Discord Navigation Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;

                        case "clipboard":
                            if (!string.IsNullOrWhiteSpace(item.Config.Path))
                            {
                                // Silently attempt clipboard operation - no error messages or logging
                                ClipboardHelper.SetText(item.Config.Path);
                            }
                            break;

                        default:
                            // Default fallback: try to launch the file if a path is specified
                            if (!string.IsNullOrWhiteSpace(item.Config.Path))
                            {
                                var defaultStartInfo = new ProcessStartInfo
                                {
                                    FileName = item.Config.Path,
                                    UseShellExecute = true
                                };
                                
                                // Set working directory to the executable's directory if it's a file path
                                if (System.IO.File.Exists(item.Config.Path))
                                {
                                    defaultStartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(item.Config.Path);
                                }
                                
                                var defaultProcess = Process.Start(defaultStartInfo);
                                if (defaultProcess != null)
                                {
                                    _ = PositionWindowNearCursor(defaultProcess, item.Config.Path ?? "unknown");
                                }
                            }
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
                NavigateBack();
            }
        }

        /// <summary>
        /// Handles mouse enter event for the center circle to navigate back when hovering
        /// </summary>
        private void OnCenterCircleMouseEnter(object sender, MouseEventArgs e)
        {
            // Only navigate back if there are nested menu levels open
            if (_menuStack.Count > 1)
            {
                NavigateBack();
            }
        }

        /// <summary>
        /// Navigates back to the previous menu level or hides the menu if at root level
        /// </summary>
        private void NavigateBack()
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

        private void OnDeactivated(object? sender, EventArgs e)
        {
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
            // Opacity should already be set to 0 before Show() is called
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.Completed += (s, e) => _isAnimating = false;
            BeginAnimation(OpacityProperty, fadeIn);

            // Start the spinning animation
            StartSpinningAnimation();
            
            // Start the energy particle spiral effect
            StartEnergyParticleEffect();

            // Skip individual item animations for root menu (items are already positioned correctly)
            // Only animate submenu items that already have transforms from LoadMenuItems
            foreach (var item in _menuItems)
            {
                // Check if item already has animation transforms (submenu items)
                if (item.Visual.RenderTransform is TransformGroup tg && 
                    tg.Children.OfType<TranslateTransform>().Any() && 
                    tg.Children.OfType<ScaleTransform>().Any())
                {
                    // Item already has animation setup from LoadMenuItems - let it continue
                    continue;
                }
                
                // For root menu items that don't have existing animations, just ensure they're visible
                // No additional scaling animation needed since they should appear in final position immediately
            }
        }

        private void AnimateOut(Action onComplete)
        {
            _isAnimating = true;
            
            // Stop the spinning animation
            StopSpinningAnimation();
            
            // Stop the particle effect
            StopEnergyParticleEffect();
            
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

        private void StartSpinningAnimation()
        {
            if (_spinningAnimation != null && _spinningRing != null && _glowingTipContainer != null)
            {
                // Make the animation elements visible
                _spinningRing.Visibility = Visibility.Visible;
                _glowingTipContainer.Visibility = Visibility.Visible;
                
                // Start the animation
                _spinningAnimation.Begin();
            }
        }

        private void StopSpinningAnimation()
        {
            if (_spinningAnimation != null)
            {
                _spinningAnimation.Stop();
                
                // Hide the animation elements
                if (_spinningRing != null)
                    _spinningRing.Visibility = Visibility.Collapsed;
                if (_glowingTipContainer != null)
                    _glowingTipContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void StartEnergyParticleEffect()
        {
            // Check if particles are enabled in settings
            bool particlesEnabled = false;
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    particlesEnabled = settings?.Appearance?.ParticlesEnabled ?? false;
                }
            }
            catch { }

            if (_particleSystem != null && particlesEnabled)
            {
                // Calculate the maximum radius for particles (should extend beyond menu items)
                var maxRadius = (_outerRadius + 50) * _uiScale;
                _particleSystem.StartEnergySpiral(_centerPoint, maxRadius);
            }
        }

        private void StopEnergyParticleEffect()
        {
            _particleSystem?.Stop();
        }

        private string FindAutoHotkeyExecutable()
        {
            // Check if user has configured a custom path
            string? customPath = null;
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    customPath = settings?.ExternalTools?.AutoHotkeyPath;
                }
            }
            catch
            {
                // Ignore errors loading settings
            }

            if (!string.IsNullOrWhiteSpace(customPath) && System.IO.File.Exists(customPath))
            {
                return customPath;
            }

            // Common AutoHotkey installation paths
            var commonPaths = new[]
            {
                @"C:\Program Files\AutoHotkey\AutoHotkey.exe",
                @"C:\Program Files (x86)\AutoHotkey\AutoHotkey.exe",
                @"C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe",
                @"C:\Program Files\AutoHotkey\v2\AutoHotkey32.exe",
                Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\AutoHotkey\AutoHotkey.exe")
            };

            // Check each common path
            foreach (var path in commonPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }

            // Try to find AutoHotkey in PATH
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "AutoHotkey.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            var firstPath = output.Split('\n')[0].Trim();
                            if (System.IO.File.Exists(firstPath))
                            {
                                return firstPath;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors from 'where' command
            }

            // If still not found, try just "AutoHotkey.exe" (will work if in PATH)
            return "AutoHotkey.exe";
        }

        private (string executable, string script) GetAutoHotkeyInfo(string channelName)
        {
            var ahkExecutable = FindAutoHotkeyExecutable();
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Try to determine AutoHotkey version by testing a simple script
            var isV2 = TestAutoHotkeyVersion(ahkExecutable);
            
            if (isV2)
            {
                var v2ScriptPath = System.IO.Path.Combine(baseDirectory, "DiscordNav.ahk");
                return (ahkExecutable, v2ScriptPath);
            }
            else
            {
                var v1ScriptPath = System.IO.Path.Combine(baseDirectory, "DiscordNav_v1.ahk");
                return (ahkExecutable, v1ScriptPath);
            }
        }

        private bool TestAutoHotkeyVersion(string ahkExecutable)
        {
            try
            {
                // Create a simple test script to determine version
                var tempScript = System.IO.Path.GetTempFileName();
                var testScriptV2 = "ExitApp()";
                System.IO.File.WriteAllText(tempScript, testScriptV2);

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ahkExecutable,
                    Arguments = $"\"{tempScript}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                });

                if (process != null)
                {
                    process.WaitForExit(2000); // Wait up to 2 seconds
                    var error = process.StandardError.ReadToEnd();
                    
                    // Clean up temp file
                    try { System.IO.File.Delete(tempScript); } catch { }
                    
                    // If no syntax errors, it's likely v2
                    // If there are syntax errors mentioning v1 syntax, it's v1
                    return process.ExitCode == 0 || !error.Contains("recognized action");
                }
            }
            catch
            {
                // If version detection fails, assume v1 for compatibility
            }
            
            return false; // Default to v1
        }

        private void StartSubmenuParticleEffect(Point originPoint)
        {
            // Check if particles are enabled in settings
            bool particlesEnabled = false;
            try
            {
                if (System.Windows.Application.Current is App app && app.SettingsService != null)
                {
                    var settings = app.SettingsService.Load();
                    particlesEnabled = settings?.Appearance?.ParticlesEnabled ?? false;
                }
            }
            catch { }

            if (_particleSystem != null && particlesEnabled)
            {
                // Stop any existing particle effect
                _particleSystem.Stop();
                
                // Start a smaller, more focused particle effect for submenus
                var maxRadius = 120 * _uiScale; // Smaller radius for submenu effects
                _particleSystem.ParticleCount = 18; // Fewer particles for submenu
                _particleSystem.AnimationDuration = TimeSpan.FromMilliseconds(1000); // Shorter duration
                _particleSystem.StartEnergySpiral(originPoint, maxRadius);
            }
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
        private double CalculateScaleToFitTargets(List<Point> targets, double half)
        {
            if (_canvas == null || targets.Count == 0) return 1.0;

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in targets)
            {
                minX = Math.Min(minX, p.X - half);
                minY = Math.Min(minY, p.Y - half);
                maxX = Math.Max(maxX, p.X + half);
                maxY = Math.Max(maxY, p.Y + half);
            }

            // Add generous padding (15% of canvas size) to ensure nodes always fit
            double paddingX = _canvas.Width * 0.15;
            double paddingY = _canvas.Height * 0.15;
            double availableWidth = _canvas.Width - 2 * paddingX;
            double availableHeight = _canvas.Height - 2 * paddingY;

            // Calculate required bounds
            double requiredWidth = maxX - minX;
            double requiredHeight = maxY - minY;

            // Calculate scale factor needed to fit content
            double scaleX = requiredWidth > availableWidth ? availableWidth / requiredWidth : 1.0;
            double scaleY = requiredHeight > availableHeight ? availableHeight / requiredHeight : 1.0;
            
            // Use the smaller scale to ensure both dimensions fit
            double scale = Math.Min(scaleX, scaleY);
            
            // Ensure minimum scale of 0.3 to maintain visibility and usability
            scale = Math.Max(scale, 0.3);
            
            // Don't scale up, only scale down
            return Math.Min(scale, 1.0);
        }

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

        // Dynamic appearance update methods
        public void UpdateUIScale(double newScale)
        {
            if (newScale <= 0) return;
            
            _uiScale = newScale;
            
            // If window is visible, we need to hide it first to resize (due to AllowsTransparency)
            bool wasVisible = IsVisible;
            if (wasVisible)
            {
                Hide();
            }
            
            // Update window and canvas sizes
            Width = 1800 * _uiScale;
            Height = 1800 * _uiScale;
            _canvas.Width = Width;
            _canvas.Height = Height;
            
            // Recalculate center point
            _centerPoint = new Point(Width / 2, Height / 2);
            
            // Update center circle size and position
            if (_centerCircle != null)
            {
                _centerCircle.Width = (_innerRadius * 2) * _uiScale;
                _centerCircle.Height = (_innerRadius * 2) * _uiScale;
            }
            
            // Update center text positioning and size
            if (_centerText != null)
            {
                _centerText.FontSize = 16 * _uiScale;
                Canvas.SetLeft(_centerText, (Width / 2) - 20 * _uiScale);
                Canvas.SetTop(_centerText, (Height / 2) - 8 * _uiScale);
            }
            
            // Reposition center elements and recreate menu items if menu is loaded
            if (_menuStack.Count > 0)
            {
                PositionCenterElements();
                // Recreate menu items with new scaling
                var currentLevel = _menuStack.Peek();
                var created = LoadMenuItems(currentLevel.Items, null);
                currentLevel.CreatedNodes = created;
            }
            
            // Show window again if it was visible
            if (wasVisible)
            {
                Show();
            }
        }

        public void UpdateRadii(double innerRadius, double outerRadius)
        {
            if (innerRadius <= 0 || outerRadius <= innerRadius) return;
            
            _innerRadius = innerRadius;
            _outerRadius = outerRadius;
            
            // Update center circle size
            if (_centerCircle != null)
            {
                _centerCircle.Width = (_innerRadius * 2) * _uiScale;
                _centerCircle.Height = (_innerRadius * 2) * _uiScale;
            }
            
            // Recreate menu items with new radii if menu is loaded
            if (_menuStack.Count > 0)
            {
                PositionCenterElements();
                var currentLevel = _menuStack.Peek();
                var created = LoadMenuItems(currentLevel.Items, null);
                currentLevel.CreatedNodes = created;
            }
        }

        public void UpdateCenterText(string text)
        {
            // Update center text if we're at root level
            if (_centerText != null && _menuStack.Count <= 1)
            {
                _centerText.Text = text ?? "MENU";
            }
        }

        public void UpdateTheme(string theme)
        {
            // TODO: Apply theme to the application resources
            // var app = Application.Current as RadialMenu.App;
            // if (app != null)
            // {
            //     app.ApplyTheme(theme);
            // }

            // Recreate menu items to pick up any color changes
            if (_menuStack.Count > 0)
            {
                var currentLevel = _menuStack.Peek();
                var created = LoadMenuItems(currentLevel.Items, null);
                currentLevel.CreatedNodes = created;
            }
        }

        public void UpdateParticleSettings(bool particlesEnabled)
        {
            // If particles are disabled and currently running, stop them
            if (!particlesEnabled && _particleSystem != null)
            {
                _particleSystem.Stop();
            }
            // If particles are enabled and the menu is currently visible, 
            // we don't automatically start them - they will start on the next menu activation
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
        public System.Windows.Shapes.Path? ProgressRing { get; set; }
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
}
