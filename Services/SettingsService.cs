using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RadialMenu.Models;

namespace RadialMenu.Services
{
    public class SettingsService
    {
        private readonly string _configPath;
        private readonly string _backupDir;

        // Raised after a successful Save(Settings) operation. Handlers should catch exceptions.
        public event Action? SettingsSaved;

        public SettingsService(string? configPath = null)
        {
            if (configPath != null)
            {
                _configPath = configPath;
            }
            else
            {
                // Use user app data for persistent settings across rebuilds
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appDir = Path.Combine(appData, "RadialMenu");
                Directory.CreateDirectory(appDir);
                _configPath = Path.Combine(appDir, "config.json");
            }
            _backupDir = Path.Combine(Path.GetDirectoryName(_configPath) ?? ".", "backups");
            Directory.CreateDirectory(_backupDir);
        }

        public Settings Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    var def = GetDefaultSettings();
                    Save(def);
                    return def;
                }

                var json = File.ReadAllText(_configPath);
                Settings? settings = null;
                var jo = JObject.Parse(json);
                if (jo["Items"] != null)
                {
                    // Legacy format: copy Items to Menu
                    var items = jo["Items"].ToObject<System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>>();
                    settings = new Settings
                    {
                        Menu = items ?? new System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>()
                    };
                    // Copy other properties if present
                    if (jo["Version"] != null) settings.Version = jo["Version"].ToObject<int>();
                    if (jo["Meta"] != null) settings.Meta = jo["Meta"].ToObject<Models.Meta>() ?? new Models.Meta();
                    if (jo["Hotkeys"] != null) settings.Hotkeys = jo["Hotkeys"].ToObject<Models.Hotkeys>() ?? new Models.Hotkeys();
                    if (jo["Appearance"] != null) settings.Appearance = jo["Appearance"].ToObject<Models.Appearance>() ?? new Models.Appearance();
                }
                else
                {
                    settings = jo.ToObject<Settings>();
                }
                if (settings == null)
                {
                    var def = GetDefaultSettings();
                    Save(def);
                    return def;
                }

                // placeholder for migration logic
                if (settings.Version < GetCurrentVersion())
                {
                    BackupConfig("pre-migrate");
                    settings = Migrate(settings);
                    Save(settings);
                }

                return settings;
            }
            catch (Exception)
            {
                var def = GetDefaultSettings();
                Save(def);
                return def;
            }
        }

        public void Save(Settings settings)
        {
            settings.Meta.LastModified = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            var tmp = _configPath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, _configPath, true);
            File.Delete(tmp);
            BackupConfig("save");

            // Notify listeners that settings were saved. Swallow exceptions coming from handlers.
            try
            {
                SettingsSaved?.Invoke();
            }
            catch { }
        }

        private void BackupConfig(string tag)
        {
            try
            {
                if (!File.Exists(_configPath)) return;
                var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var dest = Path.Combine(_backupDir, $"config_{tag}_{stamp}.json");
                File.Copy(_configPath, dest, true);
            }
            catch { /* swallow backup errors */ }
        }

        public System.Collections.Generic.List<string> ListBackups()
        {
            try
            {
                if (!Directory.Exists(_backupDir)) return new System.Collections.Generic.List<string>();
                var files = Directory.GetFiles(_backupDir, "*.json");
                var list = new System.Collections.Generic.List<string>(files);
                list.Sort((a, b) => -string.CompareOrdinal(a, b));
                return list;
            }
            catch { return new System.Collections.Generic.List<string>(); }
        }

        public bool RestoreBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath)) return false;
                // create a current backup before overwriting
                BackupConfig("pre-restore");
                File.Copy(backupPath, _configPath, true);
                // trigger reload by saving nothing (or raising event)
                try { SettingsSaved?.Invoke(); } catch { }
                return true;
            }
            catch { return false; }
        }

        private Settings Migrate(Settings old)
        {
            var cur = old;
            // Example migration path - extend for future versions
            if (cur.Version == 1)
            {
                // bump to version 2 if needed (no breaking changes yet)
                cur.Version = 2;
            }
            // set to current
            cur.Version = GetCurrentVersion();
            return cur;
        }

        private int GetCurrentVersion() => 2;

        private Settings GetDefaultSettings()
        {
            return new Settings
            {
                Version = GetCurrentVersion(),
                Meta = new Models.Meta { ProfileName = "Default", LastModified = DateTime.UtcNow },
                Hotkeys = new Models.Hotkeys { Toggle = "Win+F12" },
                Appearance = new Models.Appearance { UiScale = 1.0, InnerRadius = 40, OuterRadius = 220, Theme = "dark", CenterText = "MENU" },
                Menu = new System.Collections.ObjectModel.ObservableCollection<Models.MenuItemConfig>()
                {
                    new Models.MenuItemConfig { Label = "Open Calculator", Icon = "🧮", Action = "launch", Path = "calc.exe" },
                    new Models.MenuItemConfig { Label = "Open Notepad", Icon = "📝", Action = "launch", Path = "notepad.exe" }
                }
            };
        }
    }
}
