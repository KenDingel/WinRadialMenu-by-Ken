using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using RadialMenu.Models;
using RadialMenu.Services;
using System.Windows.Threading;

namespace RadialMenu.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private Settings _working = new Settings();

        private readonly System.Collections.Generic.List<string> _undoStack = new();
        private readonly System.Collections.Generic.List<string> _redoStack = new();
        private const int MaxSnapshots = 30;

        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.log");

        private readonly DispatcherTimer _autoSaveTimer;

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.UtcNow:O}] SettingsViewModel: {message}\r\n");
            }
            catch { }
        }

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            Log("SettingsViewModel constructor started");

            // Initialize auto-save timer
            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoSaveTimer.Tick += (s, e) => 
            {
                _autoSaveTimer.Stop();
                if (IsDirty)
                {
                    Save();
                }
            };

            // Initialize commands before any setter side-effects
            SaveCommand = new RelayCommand(_ => Save(), _ => IsDirty);
            CancelCommand = new RelayCommand(_ => Cancel());
            UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);

            AddMenuItemCommand = new RelayCommand(_ => AddMenuItem());
            DuplicateMenuItemCommand = new RelayCommand(_ => DuplicateMenuItem(), _ => SelectedMenuItem != null);
            DeleteMenuItemCommand = new RelayCommand(_ => DeleteMenuItem(), _ => SelectedMenuItem != null);

            IndentCommand = new RelayCommand(_ => Indent(), _ => CanIndent());
            OutdentCommand = new RelayCommand(_ => Outdent(), _ => CanOutdent());

            NavigateCommand = new RelayCommand(Navigate);
            ImportCommand = new RelayCommand(_ => ImportRequested?.Invoke());
            ExportCommand = new RelayCommand(_ => ExportRequested?.Invoke());
            TestActionCommand = new RelayCommand(_ => TestAction(), _ => SelectedMenuItem != null);
            SelectIconCommand = new RelayCommand(SelectIcon);
            BulkChangeColorCommand = new RelayCommand(_ => BulkChangeColor(), _ => SelectedMenuItems.Count > 0);

            // Load settings safely
            try { _working = _settingsService.Load() ?? new Settings(); } catch { _working = new Settings(); }
            if (_working.Menu == null) _working.Menu = new ObservableCollection<MenuItemConfig>();

            // Subscribe to appearance and hotkeys property changes to enable IsDirty state
            _working.Appearance.PropertyChanged += OnAppearancePropertyChanged;
            _working.Hotkeys.PropertyChanged += OnHotkeysPropertyChanged;

            Log($"Loaded settings with {_working.Menu.Count} menu items");

            // Debugging log
            Console.WriteLine("Settings loaded: " + Newtonsoft.Json.JsonConvert.SerializeObject(_working));

            _isDirty = false;
            Log("SettingsViewModel constructor completed");
        }

        private Settings SafeLoad()
        {
            try { return _settingsService.Load() ?? new Settings(); } catch { return new Settings(); }
        }

        public Settings Working
        {
            get => _working;
            set
            {
                // Unsubscribe from old events
                if (_working?.Appearance != null)
                {
                    _working.Appearance.PropertyChanged -= OnAppearancePropertyChanged;
                }
                if (_working?.Hotkeys != null)
                {
                    _working.Hotkeys.PropertyChanged -= OnHotkeysPropertyChanged;
                }

                _working = value ?? new Settings();
                if (_working.Menu == null) _working.Menu = new ObservableCollection<MenuItemConfig>();
                
                // Subscribe to new events
                _working.Appearance.PropertyChanged += OnAppearancePropertyChanged;
                _working.Hotkeys.PropertyChanged += OnHotkeysPropertyChanged;
                
                OnPropertyChanged();
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value) return;
                _isDirty = value;

                // Auto-save logic
                if (_isDirty)
                {
                    _autoSaveTimer.Start();
                }
                else
                {
                    _autoSaveTimer.Stop();
                }

                // Debugging log
                Console.WriteLine($"IsDirty changed to: {_isDirty}");

                OnPropertyChanged();
                SaveCommand?.RaiseCanExecuteChanged();
            }
        }

        // Commands used by tests
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }

        public RelayCommand AddMenuItemCommand { get; }
        public RelayCommand DuplicateMenuItemCommand { get; }
        public RelayCommand DeleteMenuItemCommand { get; }

        public RelayCommand IndentCommand { get; }
        public RelayCommand OutdentCommand { get; }

        public RelayCommand NavigateCommand { get; }
        public RelayCommand ImportCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand TestActionCommand { get; }
        public RelayCommand SelectIconCommand { get; }
        public RelayCommand BulkChangeColorCommand { get; }

        public System.Collections.Generic.List<string> ActionTypes { get; } = new() { "None", "launch", "run", "discord" };
        
        // Color palette for menu item colors
        public System.Collections.Generic.List<ColorOption> AvailableColors { get; } = ColorPalette.PredefinedColors;

        // Icon palette for menu item icons
        public System.Collections.Generic.List<IconOption> AvailableIcons { get; } = IconPalette.PredefinedIcons;

        public event Action? MenuChanged;
        public event Action<string>? NavigateRequested;
        public event Action? ImportRequested;
        public event Action? ExportRequested;

        private MenuItemConfig? _selectedMenuItem;
        public MenuItemConfig? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (_selectedMenuItem != null)
                    _selectedMenuItem.PropertyChanged -= OnSelectedItemPropertyChanged;
                _selectedMenuItem = value;
                if (_selectedMenuItem != null)
                    _selectedMenuItem.PropertyChanged += OnSelectedItemPropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedColor));
                DuplicateMenuItemCommand?.RaiseCanExecuteChanged();
                DeleteMenuItemCommand?.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<MenuItemConfig> _selectedMenuItems = new ObservableCollection<MenuItemConfig>();
        public ObservableCollection<MenuItemConfig> SelectedMenuItems 
        { 
            get => _selectedMenuItems; 
            set 
            { 
                _selectedMenuItems = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMultipleSelected));
                BulkChangeColorCommand?.RaiseCanExecuteChanged();
            } 
        }

        public bool HasMultipleSelected => SelectedMenuItems.Count > 1;

        private ColorOption _bulkColor = ColorPalette.GetDefaultColor();
        public ColorOption BulkColor
        {
            get => _bulkColor;
            set
            {
                _bulkColor = value;
                OnPropertyChanged();
            }
        }

        public ColorOption SelectedColor
        {
            get
            {
                if (SelectedMenuItem?.Color == null)
                    return ColorPalette.GetDefaultColor();
                return ColorPalette.GetColorByHex(SelectedMenuItem.Color);
            }
            set
            {
                if (SelectedMenuItem != null && value != null)
                {
                    SelectedMenuItem.Color = value.HexValue;
                    OnPropertyChanged();
                }
            }
        }

        private void OnSelectedItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PushSnapshot();
        }

        private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
            PushSnapshot();
        }

        public void SelectMenuItem(MenuItemConfig? item) => SelectedMenuItem = item;
        
        public void ToggleMenuItemSelection(MenuItemConfig item)
        {
            if (SelectedMenuItems.Contains(item))
            {
                SelectedMenuItems.Remove(item);
            }
            else
            {
                SelectedMenuItems.Add(item);
            }
            OnPropertyChanged(nameof(HasMultipleSelected));
            BulkChangeColorCommand?.RaiseCanExecuteChanged();
        }

        public void ClearMultiSelection()
        {
            SelectedMenuItems.Clear();
            OnPropertyChanged(nameof(HasMultipleSelected));
            BulkChangeColorCommand?.RaiseCanExecuteChanged();
        }

        private void AddMenuItem()
        {
            PushSnapshot();
            var item = new MenuItemConfig { Id = Guid.NewGuid().ToString(), Label = "New Item", Action = "", Path = "" };
            Working.Menu.Add(item);
            SelectMenuItem(item);
            MenuChanged?.Invoke();
        }

        private void DuplicateMenuItem()
        {
            if (SelectedMenuItem == null) return;
            PushSnapshot();
            var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<MenuItemConfig>(Newtonsoft.Json.JsonConvert.SerializeObject(SelectedMenuItem));
            if (copy != null)
            {
                copy.Id = Guid.NewGuid().ToString();
                Working.Menu.Add(copy);
                SelectMenuItem(copy);
                MenuChanged?.Invoke();
            }
        }

        private void DeleteMenuItem()
        {
            if (SelectedMenuItem == null) return;
            PushSnapshot();
            var found = Working.Menu.FirstOrDefault(x => x.Id == SelectedMenuItem.Id);
            if (found != null) Working.Menu.Remove(found);
            SelectedMenuItem = null;
            MenuChanged?.Invoke();
        }

        private void BulkChangeColor()
        {
            if (SelectedMenuItems.Count == 0) return;
            PushSnapshot();
            
            foreach (var item in SelectedMenuItems)
            {
                item.Color = BulkColor.HexValue;
            }
            
            MenuChanged?.Invoke();
        }

        private void Indent()
        {
            if (SelectedMenuItem == null) return;
            PushSnapshot();
            var index = Working.Menu.IndexOf(SelectedMenuItem);
            if (index > 0)
            {
                Working.Menu.RemoveAt(index);
                var prev = Working.Menu[index - 1];
                if (prev.Submenu == null)
                    prev.Submenu = new ObservableCollection<MenuItemConfig>();
                prev.Submenu.Add(SelectedMenuItem);
                MenuChanged?.Invoke();
            }
        }

        private void Outdent()
        {
            if (SelectedMenuItem == null) return;
            PushSnapshot();
            var (parentCollection, parentItem) = FindParent(SelectedMenuItem, Working.Menu, null);
            if (parentCollection != null && parentItem != null)
            {
                parentCollection.Remove(SelectedMenuItem);
                var grandParentCollection = parentItem == null ? Working.Menu : (parentItem.Submenu ?? Working.Menu);
                var parentIndex = grandParentCollection.IndexOf(parentItem ?? SelectedMenuItem);
                grandParentCollection.Insert(parentIndex + 1, SelectedMenuItem);
                MenuChanged?.Invoke();
            }
        }

        private (ObservableCollection<MenuItemConfig>?, MenuItemConfig?) FindParent(MenuItemConfig item, ObservableCollection<MenuItemConfig> collection, MenuItemConfig? parent)
        {
            if (collection.Contains(item))
                return (collection, parent);
            foreach (var child in collection)
            {
                if (child.Submenu != null)
                {
                    var result = FindParent(item, child.Submenu, child);
                    if (result.Item1 != null) return result;
                }
            }
            return (null, null);
        }

        private void TestAction()
        {
            if (SelectedMenuItem == null) return;
            try
            {
                switch (SelectedMenuItem.Action?.ToLower())
                {
                    case "launch":
                        if (!string.IsNullOrEmpty(SelectedMenuItem.Path))
                            System.Diagnostics.Process.Start(SelectedMenuItem.Path);
                        break;

                    case "run":
                        if (!string.IsNullOrEmpty(SelectedMenuItem.Path))
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c {SelectedMenuItem.Path}",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                        break;

                    default:
                        // None or unknown
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"TestAction failed: {ex.Message}");
            }
        }

        private void SelectIcon(object? iconOption)
        {
            if (SelectedMenuItem != null && iconOption is IconOption icon)
            {
                SelectedMenuItem.Icon = icon.Emoji;
            }
        }

        private void Navigate(object? page)
        {
            if (page is string p)
                NavigateRequested?.Invoke(p);
        }

        private void Import()
        {
            ImportRequested?.Invoke();
        }

        private void Export()
        {
            ExportRequested?.Invoke();
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void PushSnapshot()
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_working);
                _undoStack.Add(json);
                if (_undoStack.Count > MaxSnapshots) _undoStack.RemoveAt(0);
                _redoStack.Clear();
                UndoCommand?.RaiseCanExecuteChanged();
                RedoCommand?.RaiseCanExecuteChanged();
                IsDirty = true;
            }
            catch { }
        }

        public void Undo()
        {
            if (!CanUndo) return;
            try
            {
                var last = _undoStack[^1];
                _undoStack.RemoveAt(_undoStack.Count - 1);
                _redoStack.Add(Newtonsoft.Json.JsonConvert.SerializeObject(_working));
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(last);
                if (obj != null) Working = obj;
            }
            catch { }
            UndoCommand?.RaiseCanExecuteChanged();
            RedoCommand?.RaiseCanExecuteChanged();
            IsDirty = true;
        }

        public void Redo()
        {
            if (!CanRedo) return;
            try
            {
                var last = _redoStack[^1];
                _redoStack.RemoveAt(_redoStack.Count - 1);
                _undoStack.Add(Newtonsoft.Json.JsonConvert.SerializeObject(_working));
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(last);
                if (obj != null) Working = obj;
            }
            catch { }
            UndoCommand?.RaiseCanExecuteChanged();
            RedoCommand?.RaiseCanExecuteChanged();
            IsDirty = true;
        }

        // Implemented the Save method to persist settings
        private void Save()
        {
            try
            {
                _settingsService.Save(_working);
                _isDirty = false;
                Log("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Log($"Error saving settings: {ex.Message}");
            }
        }

        private void OnHotkeysPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        private void Cancel()
        {
            Working = SafeLoad();
            IsDirty = false;
            SaveCommand?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void NotifyMenuChanged() => MenuChanged?.Invoke();

        private bool CanIndent() => SelectedMenuItem != null && Working.Menu.Contains(SelectedMenuItem) && Working.Menu.IndexOf(SelectedMenuItem) > 0;

        private bool CanOutdent() => SelectedMenuItem != null && FindParent(SelectedMenuItem, Working.Menu, null).Item2 != null;
    }
}
