using System;
using System.Windows.Controls;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RadialMenu.Models;

namespace RadialMenu.Controls
{
    public partial class TreeMenuEditor : UserControl
    {
        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.log");

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.UtcNow:O}] TreeMenuEditor: {message}\r\n");
            }
            catch { }
        }

        private Point _dragStartPoint;
        private MenuItemConfig? _draggedItem;

        public TreeMenuEditor()
        {
            Log("TreeMenuEditor constructor");
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var vm = DataContext as ViewModels.SettingsViewModel;
            if (vm != null)
            {
                vm.SelectMenuItem(e.NewValue as MenuItemConfig);
            }
        }

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Find the TreeViewItem under the mouse
                    var treeView = sender as TreeView;
                    var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                    if (treeViewItem != null)
                    {
                        _draggedItem = treeViewItem.DataContext as MenuItemConfig;
                        if (_draggedItem != null)
                        {
                            DragDrop.DoDragDrop(treeViewItem, _draggedItem, DragDropEffects.Move);
                        }
                    }
                }
            }
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            if (_draggedItem == null) return;

            var treeView = sender as TreeView;
            var dropTarget = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            MenuItemConfig? targetItem = dropTarget?.DataContext as MenuItemConfig;

            // For now, only handle reordering at top level
            var vm = DataContext as ViewModels.SettingsViewModel;
            if (vm == null) return;

            vm.PushSnapshot();

            // Remove from old location
            RemoveFromCollection(_draggedItem, vm.Working.Menu);

            // Insert at new location
            if (targetItem != null && targetItem != _draggedItem)
            {
                // Nest under target item
                if (targetItem.Submenu == null)
                    targetItem.Submenu = new System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>();
                targetItem.Submenu.Add(_draggedItem);
            }
            else
            {
                // Add to top level
                vm.Working.Menu.Add(_draggedItem);
            }

            vm.NotifyMenuChanged();
            _draggedItem = null;
        }

        private void RemoveFromCollection(MenuItemConfig item, System.Collections.ObjectModel.ObservableCollection<MenuItemConfig> collection)
        {
            if (collection.Contains(item))
            {
                collection.Remove(item);
                return;
            }

            // Check submenus recursively
            foreach (var child in collection)
            {
                if (child.Submenu != null)
                {
                    RemoveFromCollection(item, child.Submenu);
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor) return ancestor;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return default;
        }

        private void MultiSelectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && DataContext is ViewModels.SettingsViewModel vm)
            {
                var item = GetMenuItemFromCheckBox(checkBox);
                if (item != null)
                {
                    vm.ToggleMenuItemSelection(item);
                }
            }
        }

        private void MultiSelectCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && DataContext is ViewModels.SettingsViewModel vm)
            {
                var item = GetMenuItemFromCheckBox(checkBox);
                if (item != null)
                {
                    vm.ToggleMenuItemSelection(item);
                }
            }
        }

        private void ClearMultiSelection_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.SettingsViewModel vm)
            {
                vm.ClearMultiSelection();
                
                // Uncheck all checkboxes in the tree
                UncheckAllCheckBoxes(MenuTreeView);
            }
        }

        private Models.MenuItemConfig? GetMenuItemFromCheckBox(CheckBox checkBox)
        {
            // Find the TreeViewItem that contains this checkbox
            var treeViewItem = FindAncestor<TreeViewItem>(checkBox);
            return treeViewItem?.DataContext as Models.MenuItemConfig;
        }

        private void UncheckAllCheckBoxes(ItemsControl itemsControl)
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                if (container != null)
                {
                    // Find checkbox in this item
                    var checkBox = FindChildByName<CheckBox>(container, "MultiSelectCheckBox");
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = false;
                    }
                    
                    // Recursively process child items
                    UncheckAllCheckBoxes(container);
                }
            }
        }

        private static T FindChildByName<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (child as FrameworkElement)?.Name == childName)
                {
                    return (T)child;
                }
                
                var result = FindChildByName<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        private void TreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Find the TreeViewItem that was double-clicked
            var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (treeViewItem != null)
            {
                // Toggle the expansion of the specific item
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                Log($"TreeViewItem double-clicked - toggling expansion for item: {(treeViewItem.DataContext as MenuItemConfig)?.Label}, now expanded: {treeViewItem.IsExpanded}");
                
                // Prevent the click from bubbling up to avoid triggering other handlers
                e.Handled = true;
            }
        }
    }
}
