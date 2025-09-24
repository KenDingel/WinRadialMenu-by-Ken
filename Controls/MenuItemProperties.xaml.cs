using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace RadialMenu.Controls
{
    public partial class MenuItemProperties : UserControl
    {
        public MenuItemProperties()
        {
            InitializeComponent();
        }

        private void PathTextBox_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the dragged data contains files
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                // Check if any of the files is an executable
                bool hasExecutable = files.Any(file => 
                    Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file).Equals(".lnk", StringComparison.OrdinalIgnoreCase));
                
                if (hasExecutable)
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void PathTextBox_DragOver(object sender, DragEventArgs e)
        {
            // Same logic as DragEnter
            PathTextBox_DragEnter(sender, e);
        }

        private void PathTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && sender is TextBox textBox)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var viewModel = DataContext as ViewModels.SettingsViewModel;
                var currentAction = viewModel?.SelectedMenuItem?.Action;

                // Handle folders for folder action type
                if (currentAction?.Equals("folder", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var folder = files.FirstOrDefault(path => Directory.Exists(path));
                    if (!string.IsNullOrEmpty(folder))
                    {
                        textBox.Text = folder;
                        
                        // Trigger the binding update
                        var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
                        bindingExpression?.UpdateSource();
                    }
                }
                else
                {
                    // Find the first executable file for other action types
                    var executableFile = files.FirstOrDefault(file => 
                        Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(file).Equals(".lnk", StringComparison.OrdinalIgnoreCase));
                    
                    if (!string.IsNullOrEmpty(executableFile))
                    {
                        // Set the path in the textbox
                        textBox.Text = executableFile;
                        
                        // Trigger the binding update
                        var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
                        bindingExpression?.UpdateSource();
                        
                        // Auto-set action type to "launch" for EXE files
                        if (viewModel != null && viewModel.SelectedMenuItem != null)
                        {
                            if (string.IsNullOrEmpty(viewModel.SelectedMenuItem.Action) || 
                                viewModel.SelectedMenuItem.Action == "None")
                            {
                                viewModel.SelectedMenuItem.Action = "launch";
                            }
                        }
                    }
                }
            }
            
            e.Handled = true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Find the PathTextBox in the current control
            var pathTextBox = this.FindName("PathTextBox") as TextBox;
            if (pathTextBox == null) return;

            // Check if we're dealing with a folder action type
            var viewModel = DataContext as ViewModels.SettingsViewModel;
            var currentAction = viewModel?.SelectedMenuItem?.Action;

            if (currentAction?.Equals("folder", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Use folder browser for folder action type
                var folderDialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Select Folder"
                };

                if (folderDialog.ShowDialog() == true)
                {
                    pathTextBox.Text = folderDialog.FolderName;
                    
                    // Trigger the binding update
                    var bindingExpression = pathTextBox.GetBindingExpression(TextBox.TextProperty);
                    bindingExpression?.UpdateSource();
                }
            }
            else
            {
                // Use file browser for other action types
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Executable File",
                    Filter = "Executable files (*.exe)|*.exe|Shortcut files (*.lnk)|*.lnk|All files (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    pathTextBox.Text = openFileDialog.FileName;
                    
                    // Trigger the binding update
                    var bindingExpression = pathTextBox.GetBindingExpression(TextBox.TextProperty);
                    bindingExpression?.UpdateSource();
                    
                    // Auto-set action type to "launch" for EXE files
                    var extension = Path.GetExtension(openFileDialog.FileName);
                    if ((extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) || 
                         extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)) &&
                        viewModel != null && viewModel.SelectedMenuItem != null)
                    {
                        if (string.IsNullOrEmpty(viewModel.SelectedMenuItem.Action) || 
                            viewModel.SelectedMenuItem.Action == "None")
                        {
                            viewModel.SelectedMenuItem.Action = "launch";
                        }
                    }
                }
            }
        }
    }
}