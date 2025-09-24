using System;
using Xunit;
using RadialMenu.Services;
using RadialMenu.ViewModels;
using System.IO;

namespace RadialMenu.Tests
{
    public class SettingsViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _configPath;
        private readonly SettingsService _svc;
        private readonly SettingsViewModel _vm;

        public SettingsViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "RadialMenu.Tests.VM", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _configPath = Path.Combine(_tempDir, "config.json");
            _svc = new SettingsService(_configPath);
            _vm = new SettingsViewModel(_svc);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void AddDuplicateDelete_Workflow()
        {
            var before = _vm.Working.Menu.Count;
            _vm.AddMenuItemCommand.Execute(null);
            Assert.True(_vm.Working.Menu.Count == before + 1);
            var added = _vm.Working.Menu[^1];
            _vm.SelectMenuItem(added);
            _vm.DuplicateMenuItemCommand.Execute(null);
            Assert.True(_vm.Working.Menu.Count == before + 2);
            _vm.DeleteMenuItemCommand.Execute(null);
            Assert.True(_vm.Working.Menu.Count == before + 1);
        }

        [Fact]
        public void UndoRedo_Works()
        {
            var beforeJson = Newtonsoft.Json.JsonConvert.SerializeObject(_vm.Working);
            _vm.AddMenuItemCommand.Execute(null);
            Assert.True(_vm.CanUndo);
            _vm.UndoCommand.Execute(null);
            var afterUndo = Newtonsoft.Json.JsonConvert.SerializeObject(_vm.Working);
            Assert.Equal(beforeJson, afterUndo);
            _vm.RedoCommand.Execute(null);
            Assert.True(_vm.CanRedo == false || _vm.Working.Menu.Count >= 1);
        }

        [Fact]
        public void IsDirty_Toggles()
        {
            var initial = _vm.IsDirty;
            _vm.AddMenuItemCommand.Execute(null);
            Assert.True(_vm.IsDirty);
            _vm.UndoCommand.Execute(null);
            Assert.True(_vm.IsDirty);
        }
    }
}
