using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RadialMenu.Models
{
    public class Settings
    {
        public int Version { get; set; } = 2;
        public Meta Meta { get; set; } = new Meta();
        public Hotkeys Hotkeys { get; set; } = new Hotkeys();
        public Appearance Appearance { get; set; } = new Appearance();
    public System.Collections.ObjectModel.ObservableCollection<MenuItemConfig> Menu { get; set; } = new System.Collections.ObjectModel.ObservableCollection<MenuItemConfig>();
    }

    public class Meta
    {
        public string ProfileName { get; set; } = "Default";
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class Hotkeys
    {
        public string Toggle { get; set; } = "Win+F12";
    }

    public class Appearance
    {
        public double UiScale { get; set; } = 1.0;
        public double InnerRadius { get; set; } = 40;
        public double OuterRadius { get; set; } = 220;
        public string Theme { get; set; } = "dark";
        public string CenterText { get; set; } = "MENU";
        public Dictionary<string, string> Colors { get; set; } = new Dictionary<string, string>();
    }

    public class MenuItemConfig : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _label = "";
        private string _icon = "📄";
        private string? _color;
        private string? _action;
        private string? _path;
        private System.Collections.ObjectModel.ObservableCollection<MenuItemConfig>? _submenu;

        public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Label { get => _label; set { _label = value; OnPropertyChanged(); } }
        public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
        public string? Color { get => _color; set { _color = value; OnPropertyChanged(); } }
        public string? Action { get => _action; set { _action = value; OnPropertyChanged(); } }
        public string? Path { get => _path; set { _path = value; OnPropertyChanged(); } }
        public System.Collections.ObjectModel.ObservableCollection<MenuItemConfig>? Submenu { get => _submenu; set { _submenu = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
