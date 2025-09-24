using System;
using System.IO;
using Xunit;
using RadialMenu.Services;
using RadialMenu.Models;

namespace RadialMenu.Tests
{
    public class SettingsServiceMoreTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _configPath;
        private readonly SettingsService _svc;

        public SettingsServiceMoreTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "RadialMenu.MoreTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _configPath = Path.Combine(_tempDir, "config.json");
            _svc = new SettingsService(_configPath);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void Load_InvalidJson_FallsBackToDefault()
        {
            File.WriteAllText(_configPath, "{ invalid json...");
            var s = _svc.Load();
            Assert.NotNull(s);
            Assert.Equal(2, s.Version);
            Assert.True(File.Exists(_configPath));
        }

        [Fact]
        public void Load_OlderVersion_PerformsMigration()
        {
            var old = new Settings { Version = 1 };
            old.Meta.ProfileName = "OldProfile";
            File.WriteAllText(_configPath, Newtonsoft.Json.JsonConvert.SerializeObject(old));

            var loaded = _svc.Load();
            Assert.Equal(2, loaded.Version);
            Assert.Equal("OldProfile", loaded.Meta.ProfileName);
        }

        [Fact]
        public void Save_WritesAtomically_NoTmpLeft()
        {
            var s = _svc.Load();
            _svc.Save(s);
            var tmp = _configPath + ".tmp";
            Assert.False(File.Exists(tmp));
        }

        [Fact]
        public void SettingsSaved_Event_IsRaised()
        {
            var called = false;
            _svc.SettingsSaved += () => called = true;
            var s = _svc.Load();
            _svc.Save(s);
            Assert.True(called);
        }
    }
}
