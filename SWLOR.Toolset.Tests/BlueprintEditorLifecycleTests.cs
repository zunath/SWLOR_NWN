using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Appearance;
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
        public async Task GenericCreatureEditorPersistsBuilderEnteredValues()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(_ => throw new NotSupportedException(), log);
            var schema = new EditorSchema
            {
                ResourceType = ResourceType.Utc,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Identity",
                        Fields = new[]
                        {
                            new FieldDescriptor
                            {
                                Label = "Tag",
                                FieldName = "Tag",
                                Kind = EditorKind.Text,
                                FieldType = GffFieldType.CExoString
                            }
                        }
                    }
                }
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

            ((TextFieldViewModel)editor.Groups.Single().Fields.Single()).Text =
                "builder_entered_creature";

            (await editor.TrySaveAsync()).Should().BeTrue();
            JsonGffDocument.Load(_path).Root.GetStringOrNull("Tag")
                .Should().Be("builder_entered_creature");
            editor.OnClose().Should().BeTrue();
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

        [Test]
        public async Task ExternalOverwriteAdoptsTheAcceptedGenerationBeforeTheFinalRecheck()
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
                new OverwritePrompts());
            editor.VarTableSection!.NewName = "LOCAL_TEST";
            editor.VarTableSection.NewType = "int";
            editor.VarTableSection.NewValue = "1";
            editor.VarTableSection.SetVariableCommand.Execute(null);

            File.WriteAllBytes(
                _path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    "reload_var",
                    "External"));

            (await editor.TrySaveAsync()).Should().BeTrue(
                "the accepted disk generation becomes the baseline for the final race check");
            editor.IsDirty.Should().BeFalse();
            editor.OnClose().Should().BeTrue();
        }

        [Test]
        public void CreatureAppearanceGalleryFollowsUndoAndRedo()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(_ => throw new NotSupportedException(), log);
            var schema = new EditorSchema
            {
                ResourceType = ResourceType.Utc,
                Groups = Array.Empty<FieldGroup>()
            };
            var options = new[]
            {
                new AppearanceOption("6", "Appearance 6", "row 6", CreatureAppearanceId: 6),
                new AppearanceOption("7", "Appearance 7", "row 7", CreatureAppearanceId: 7)
            };
            var editor = new BlueprintEditorViewModel(
                _path,
                "reload_var",
                ResourceType.Utc,
                schema,
                new LookupOptionProvider(context),
                gameCodeIndex: null,
                log,
                new ReloadPrompts(),
                appearanceGallery: (fieldContext, runEdit) =>
                    new AppearanceGallerySectionViewModel(
                        options,
                        thumbnails: null,
                        currentKey: () => fieldContext.Document.Root
                            .GetOrNull("Appearance_Type")!.GetInteger().ToString(),
                        apply: option => runEdit(
                            "Change creature appearance",
                            () => fieldContext.Document.Root
                                .GetOrNull("Appearance_Type")!
                                .SetInteger(option.CreatureAppearanceId!.Value)),
                        noun: "appearance"));
            var gallery = editor.AppearanceGallery!;
            gallery.Tiles.Single(tile => tile.IsCurrent).Option.Key.Should().Be("6");

            gallery.Highlighted = gallery.Tiles.Single(tile => tile.Option.Key == "7");
            gallery.Tiles.Single(tile => tile.IsCurrent).Option.Key.Should().Be("7");

            editor.Undo();
            gallery.Tiles.Single(tile => tile.IsCurrent).Option.Key.Should().Be("6");

            editor.Redo();
            gallery.Tiles.Single(tile => tile.IsCurrent).Option.Key.Should().Be("7");

            editor.Undo();
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

        private sealed class OverwritePrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Overwrite);

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
