using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using RadialMenu.ViewModels;

namespace RadialMenu.Controls
{
    public partial class CanvasMenuPreview : UserControl
    {
        private readonly string _logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.log");

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.UtcNow:O}] CanvasMenuPreview: {message}\r\n");
            }
            catch { }
        }

        public CanvasMenuPreview()
        {
            Log("CanvasMenuPreview constructor");
            InitializeComponent();
            Loaded += CanvasMenuPreview_Loaded;
        }

        private void CanvasMenuPreview_Loaded(object? sender, RoutedEventArgs e)
        {
            Log("CanvasMenuPreview_Loaded started");
            try
            {
                if (DataContext is SettingsViewModel vm)
                {
                    Log("Subscribing to events and rendering initial preview");
                    vm.MenuChanged += Vm_MenuChanged;
                    vm.PropertyChanged += Vm_PropertyChanged;
                    RenderPreview(vm);
                    Log("CanvasMenuPreview_Loaded completed");
                }
                else
                {
                    Log("DataContext is not SettingsViewModel");
                }
            }
            catch (Exception ex)
            {
                Log($"CanvasMenuPreview_Loaded failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is SettingsViewModel vm && (e.PropertyName == "Working" || e.PropertyName == "Working.Appearance"))
            {
                Dispatcher.Invoke(() => RenderPreview(vm));
            }
        }

        private void Vm_MenuChanged()
        {
            if (DataContext is SettingsViewModel vm)
            {
                Dispatcher.Invoke(() => RenderPreview(vm));
            }
        }

        private void RenderPreview(SettingsViewModel vm)
        {
            Log("RenderPreview started");
            try
            {
                PreviewCanvas.Children.Clear();
                var settings = vm.Working;
                var items = settings?.Menu ?? new System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>();
                Log($"Rendering {items.Count} menu items");
                var w = ActualWidth > 0 ? ActualWidth : 300;
                var h = ActualHeight > 0 ? ActualHeight : 300;
                var center = new Point(w / 2, h / 2);

                var inner = settings?.Appearance?.InnerRadius ?? 40;
                var outer = settings?.Appearance?.OuterRadius ?? 140;
                var spread = outer * (settings?.Appearance?.UiScale ?? 1.0);

                var count = Math.Max(1, items.Count);
                var angleStep = 360.0 / count;

                for (int i = 0; i < items.Count; i++)
                {
                    var mi = items[i];
                    var angle = (i * angleStep) - 90;
                    var rad = angle * Math.PI / 180.0;
                    var tx = center.X + Math.Cos(rad) * spread * 0.6;
                    var ty = center.Y + Math.Sin(rad) * spread * 0.6;

                    // draw node
                    var nodeSize = 44 * (settings?.Appearance?.UiScale ?? 1.0);
                    var circle = new Ellipse
                    {
                        Width = nodeSize,
                        Height = nodeSize,
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(mi.Color ?? "#FF2D7DD2")),
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(circle, tx - nodeSize / 2);
                    Canvas.SetTop(circle, ty - nodeSize / 2);

                    // highlight if selected
                    if (vm.SelectedMenuItem != null && vm.SelectedMenuItem == mi)
                    {
                        var ring = new Ellipse
                        {
                            Width = nodeSize + 16,
                            Height = nodeSize + 16,
                            Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                            StrokeThickness = 3,
                            Fill = new SolidColorBrush(Color.FromArgb(24, 255, 255, 255))
                        };
                        Canvas.SetLeft(ring, tx - (nodeSize + 16) / 2);
                        Canvas.SetTop(ring, ty - (nodeSize + 16) / 2);
                        PreviewCanvas.Children.Add(ring);
                    }

                    PreviewCanvas.Children.Add(circle);

                    // add label
                    var label = new TextBlock
                    {
                        Text = (mi.Icon ?? "") + "\n" + (mi.Label ?? ""),
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 10,
                        Width = 80
                    };
                    label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(label, tx - 40);
                    Canvas.SetTop(label, ty + 24);
                    PreviewCanvas.Children.Add(label);
                }
                Log("RenderPreview completed");
            }
            catch (Exception ex)
            {
                Log($"RenderPreview failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}

