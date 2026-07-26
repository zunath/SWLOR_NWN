using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class BlueprintEditorLifecycleTests
    {
        private string _directory = null!;
        private string _path = null!;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(), "swlor-blueprint-editor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "reload_var.utc.json");
            File.WriteAllBytes(
                _path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc, "reload_var", "Original"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public async Task ExternalReloadRebindsTheVariablesTabToTheReloadedDocument()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(_ => throw new NotSupportedException(), log);
            var schema = new EditorSchema
            {
                ResourceType = ResourceType.Utc,
                Groups = Array.Empty<FieldGroup>(),
                HasVarTable = true
            };
            var editor = new BlueprintEditorViewModel(
                _path,
                "reload_var",
                ResourceType.Utc,
                schema,
                new LookupOptionProvider(context),
                gameCodeIndex: null,
                log,
                new ReloadPrompts());
            editor.IsPlaceableEditor.Should().BeFalse();
            var oldSection = editor.VarTableSection!;
            oldSection.NewName = "LOCAL_TEST";
            oldSection.NewType = "int";
            oldSection.NewValue = "1";
            oldSection.SetVariableCommand.Execute(null);
            editor.IsDirty.Should().BeTrue();

            File.WriteAllBytes(
                _path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc, "reload_var", "External"));

            (await editor.TrySaveAsync()).Should().BeTrue();

            editor.VarTableSection.Should().NotBeSameAs(oldSection);
            editor.Tabs.Where(tab => tab.Title == "Variables").Should().ContainSingle()
                .Which.Content.Should().BeSameAs(editor.VarTableSection);
            editor.OnClose().Should().BeTrue();
        }

        private sealed class ReloadPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Reload);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
