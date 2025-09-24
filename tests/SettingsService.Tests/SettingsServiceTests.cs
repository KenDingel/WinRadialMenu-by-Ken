using System;
using System.IO;
using Xunit;
using RadialMenu.Services;
using RadialMenu.Models;

namespace RadialMenu.Tests
{
    public class SettingsServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _configPath;
        private readonly SettingsService _svc;

        public SettingsServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "RadialMenu.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _configPath = Path.Combine(_tempDir, "config.json");
            _svc = new SettingsService(_configPath);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void Load_WhenMissing_CreatesDefault()
        {
            if (File.Exists(_configPath)) File.Delete(_configPath);
            var s = _svc.Load();
            Assert.NotNull(s);
            Assert.Equal(2, s.Version);
            Assert.NotNull(s.Menu);
            Assert.NotEmpty(s.Menu);
            Assert.True(File.Exists(_configPath));
        }

        [Fact]
        public void Save_ThenLoad_PreservesData()
        {
            var s = _svc.Load();
            s.Meta.ProfileName = "TestProfile";
            s.Hotkeys.Toggle = "Ctrl+Shift+T";
            _svc.Save(s);

            var reload = _svc.Load();
            Assert.Equal("TestProfile", reload.Meta.ProfileName);
            Assert.Equal("Ctrl+Shift+T", reload.Hotkeys.Toggle);
        }

        [Fact]
        public void Save_CreatesBackup()
        {
            var s = _svc.Load();
            _svc.Save(s);
            var backups = _svc.ListBackups();
            Assert.NotNull(backups);
            Assert.NotEmpty(backups);
        }

        [Fact]
        public void RestoreBackup_Works()
        {
            var s = _svc.Load();
            s.Meta.ProfileName = "BeforeRestore";
            _svc.Save(s);

            // create a fake backup file
            var bak = Path.Combine(Path.GetDirectoryName(_configPath)!, "backups", "test_backup.json");
            Directory.CreateDirectory(Path.GetDirectoryName(bak)!);
            var other = _svc.Load();
            other.Meta.ProfileName = "RestoredProfile";
            File.WriteAllText(bak, Newtonsoft.Json.JsonConvert.SerializeObject(other));

            var ok = _svc.RestoreBackup(bak);
            Assert.True(ok);
            var now = _svc.Load();
            Assert.Equal("RestoredProfile", now.Meta.ProfileName);
        }
    }
}
