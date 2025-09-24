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
            if (sender is SettingsViewModel vm && (e.PropertyName == "Working" || e.PropertyName == "Working.Appearance" || e.PropertyName == "SelectedMenuItem"))
            {
                Log($"PropertyChanged: {e.PropertyName}");
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
                var rootItems = settings?.Menu ?? new System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>();
                
                // Determine which items to show based on selection
                var itemsToRender = rootItems;
                var selectedItem = vm.SelectedMenuItem;
                var parentItem = (Models.MenuItemConfig?)null;
                
                // If an item is selected, show the collection it belongs to (its siblings)
                if (selectedItem != null)
                {
                    var (parentCollection, parentItemFound) = FindParentCollection(selectedItem, rootItems, null);
                    if (parentCollection != null)
                    {
                        itemsToRender = parentCollection;
                        parentItem = parentItemFound;
                        Log($"Rendering {itemsToRender.Count} items from collection containing: {selectedItem.Label}");
                    }
                    else
                    {
                        Log($"Could not find parent collection for: {selectedItem.Label}, showing root");
                    }
                }
                else
                {
                    Log($"No selection, rendering {itemsToRender.Count} top-level menu items");
                }

                var w = ActualWidth > 0 ? ActualWidth : 300;
                var h = ActualHeight > 0 ? ActualHeight : 300;
                var center = new Point(w / 2, h / 2);

                var inner = settings?.Appearance?.InnerRadius ?? 40;
                var outer = settings?.Appearance?.OuterRadius ?? 140;
                var spread = outer * (settings?.Appearance?.UiScale ?? 1.0);

                var count = Math.Max(1, itemsToRender.Count);
                var angleStep = 360.0 / count;

                // Render center indicator if we're showing a submenu (parent item exists)
                if (parentItem != null)
                {
                    var centerSize = 32 * (settings?.Appearance?.UiScale ?? 1.0);
                    var centerCircle = new Ellipse
                    {
                        Width = centerSize,
                        Height = centerSize,
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(parentItem.Color ?? "#FF2D7DD2")),
                        Stroke = Brushes.Gold,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(centerCircle, center.X - centerSize / 2);
                    Canvas.SetTop(centerCircle, center.Y - centerSize / 2);
                    PreviewCanvas.Children.Add(centerCircle);

                    // Center label
                    var centerLabel = new TextBlock
                    {
                        Text = parentItem.Icon ?? "",
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold
                    };
                    centerLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(centerLabel, center.X - centerLabel.DesiredSize.Width / 2);
                    Canvas.SetTop(centerLabel, center.Y - centerLabel.DesiredSize.Height / 2);
                    PreviewCanvas.Children.Add(centerLabel);
                }

                for (int i = 0; i < itemsToRender.Count; i++)
                {
                    var mi = itemsToRender[i];
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

                    // Add special styling for items with submenus
                    if (mi.Submenu != null && mi.Submenu.Count > 0)
                    {
                        circle.Stroke = Brushes.Yellow;
                        circle.StrokeThickness = 2;
                    }

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

                    // Add submenu indicator
                    if (mi.Submenu != null && mi.Submenu.Count > 0)
                    {
                        var indicator = new Ellipse
                        {
                            Width = 12,
                            Height = 12,
                            Fill = Brushes.Yellow,
                            Stroke = Brushes.White,
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(indicator, tx + nodeSize / 2 - 6);
                        Canvas.SetTop(indicator, ty - nodeSize / 2);
                        PreviewCanvas.Children.Add(indicator);

                        // Add submenu count text
                        var countText = new TextBlock
                        {
                            Text = mi.Submenu.Count.ToString(),
                            Foreground = Brushes.Black,
                            FontSize = 8,
                            FontWeight = FontWeights.Bold,
                            TextAlignment = TextAlignment.Center
                        };
                        countText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        Canvas.SetLeft(countText, tx + nodeSize / 2 - 6 - countText.DesiredSize.Width / 2);
                        Canvas.SetTop(countText, ty - nodeSize / 2 - countText.DesiredSize.Height / 2);
                        PreviewCanvas.Children.Add(countText);
                    }

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

                // Add navigation hint text
                var navigationHint = "";
                if (parentItem != null)
                {
                    navigationHint = $"Showing submenu of: {parentItem.Label} ({itemsToRender.Count} items)";
                }
                else
                {
                    navigationHint = $"Top level menu ({itemsToRender.Count} items)";
                }
                
                if (selectedItem != null)
                {
                    navigationHint += $" | Selected: {selectedItem.Label}";
                }
                
                var hintText = new TextBlock
                {
                    Text = navigationHint,
                    Foreground = Brushes.LightGray,
                    FontSize = 10,
                    Margin = new Thickness(10, 10, 0, 0)
                };
                Canvas.SetLeft(hintText, 10);
                Canvas.SetTop(hintText, 10);
                PreviewCanvas.Children.Add(hintText);

                Log("RenderPreview completed");
            }
            catch (Exception ex)
            {
                Log($"RenderPreview failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private (System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>?, Models.MenuItemConfig?) FindParentCollection(Models.MenuItemConfig item, System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig> collection, Models.MenuItemConfig? parent)
        {
            if (collection.Contains(item))
                return (collection, parent);
            foreach (var child in collection)
            {
                if (child.Submenu != null)
                {
                    var result = FindParentCollection(item, child.Submenu, child);
                    if (result.Item1 != null) return result;
                }
            }
            return (null, null);
        }
    }
}

