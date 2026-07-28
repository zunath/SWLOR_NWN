using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// The item editor shell: header, Basic rows, family-driven Variables visibility, and the one
    /// behavior no other editor has - a save that renames the blueprint file when its ResRef row
    /// changed.
    /// </summary>
    [TestFixture]
    public class ItemEditorShellTests
    {
        private static BaseItemRow? Rows(int baseItem) => baseItem switch
        {
            16 => new BaseItemRow(16, "armor", 3),
            29 => new BaseItemRow(29, "miscmedium", 0),
            512 => new BaseItemRow(512, "lightsaber", 2),
            _ => null
        };

        private static IReadOnlyList<BehaviorChoice> Choices(string key) =>
            key == ItemChoiceKeys.BaseItems
                ? new[]
                {
                    new BehaviorChoice(16, "Armor"),
                    new BehaviorChoice(29, "Misc Medium"),
                    new BehaviorChoice(512, "Lightsaber")
                }
                : Array.Empty<BehaviorChoice>();

        private static ItemEditorViewModel Open(string resRef)
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "uti", $"{resRef}.uti.json");
            var document = UtiDocument.Load(path);
            return new ItemEditorViewModel(
                document.Fields,
                resRef,
                (_, mutation) => { mutation(); return true; },
                resolveChoices: Choices,
                baseItemRows: Rows);
        }

        [Test]
        public void ArmorBlueprintReadsAsArmor()
        {
            using var editor = Open("adren_harness");

            Assert.That(editor.HeaderName, Is.EqualTo("Adrenal Harness"));
            Assert.That(editor.HeaderKind, Is.EqualTo("blueprint"));
            Assert.That(editor.Family, Is.EqualTo(ItemFamily.Armor));
            Assert.That(editor.TemplateResRef, Is.EqualTo("adren_harness"));
            Assert.That(editor.IsIncomplete, Is.False);
        }

        [Test]
        public void BasicRowsFollowTheLayout()
        {
            using var editor = Open("adren_harness");

            Assert.That(
                editor.BasicRows.Select(row => row.Definition.Name),
                Is.EqualTo(ItemEditorLayout.Basic.Select(definition => definition.Name)));

            var totalCost = editor.BasicRows.Single(row => row.Definition.Name == "Cost");
            Assert.That(totalCost.IsReadOnly, Is.True);
            Assert.That(totalCost.Number, Is.EqualTo(3000));
        }

        [Test]
        public void EquipmentHasNoVariablesTab()
        {
            using var editor = Open("adren_harness");

            Assert.That(editor.ShowsVariablesTab, Is.False);
            Assert.That(editor.Variables, Is.Null);
        }

        [Test]
        public void UnclassifiedMiscellaneousItemIsCustomWithVariables()
        {
            using var editor = Open("ark_dragon_troph");

            Assert.That(editor.Family, Is.EqualTo(ItemFamily.Miscellaneous));
            Assert.That(editor.Role.Id, Is.EqualTo(ItemRoleCatalog.CustomId));
            Assert.That(editor.ShowsVariablesTab, Is.True);
            Assert.That(editor.Variables, Is.Not.Null);
        }
    }

    /// <summary>Save-time resref validation and file renaming, against a scratch copy of the corpus.</summary>
    [TestFixture]
    public class ItemDocumentRenameTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void CreateScratchModule()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor-item-rename-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "uti"));
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json"),
                Scratch("adren_harness"));
        }

        [TearDown]
        public void DeleteScratchModule()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private string Scratch(string resRef) => Path.Combine(_root, "uti", $"{resRef}.uti.json");

        private ItemDocumentViewModel OpenScratch(string resRef) =>
            new(Scratch(resRef), resRef, null, new OutputLogService(), new StubPrompts());

        private static void SetResRef(ItemDocumentViewModel document, string value)
        {
            var row = document.Editor.BasicRows.Single(candidate =>
                candidate.Definition.Name == "TemplateResRef");
            row.Text = value;
        }

        [Test]
        public async Task SaveUnderChangedResRefRenamesTheFile()
        {
            var document = OpenScratch("adren_harness");
            var renames = new List<(string OldResRef, string OldPath)>();
            document.Renamed += (_, oldResRef, oldPath) => renames.Add((oldResRef, oldPath));

            SetResRef(document, "adren_mk2");

            Assert.That(await document.TrySaveAsync(), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk2")), Is.True);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.False);
            Assert.That(document.ResRef, Is.EqualTo("adren_mk2"));
            Assert.That(document.FilePath, Is.EqualTo(Scratch("adren_mk2")));
            Assert.That(document.IsDirty, Is.False);
            Assert.That(renames, Is.EqualTo(new[] { ("adren_harness", Scratch("adren_harness")) }));

            var saved = UtiDocument.Load(Scratch("adren_mk2"));
            Assert.That(saved.TemplateResRef, Is.EqualTo("adren_mk2"));
        }

        [Test]
        public async Task SaveNormalizesResRefCase()
        {
            var document = OpenScratch("adren_harness");
            SetResRef(document, "ADREN_MK3");

            Assert.That(await document.TrySaveAsync(), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk3")), Is.True);
            Assert.That(document.ResRef, Is.EqualTo("adren_mk3"));
            Assert.That(
                UtiDocument.Load(Scratch("adren_mk3")).TemplateResRef,
                Is.EqualTo("adren_mk3"));
        }

        [Test]
        public async Task IllegalResRefIsRefusedAtTheEditAndEmptyAtTheSave()
        {
            var document = OpenScratch("adren_harness");

            // Illegal characters never reach the document: the GFF layer refuses the write itself.
            SetResRef(document, "bad resref!");
            Assert.That(document.IsDirty, Is.False);
            Assert.That(document.Editor.TemplateResRef, Is.EqualTo("adren_harness"));

            // An emptied resref does reach the document, and the save is what refuses it.
            SetResRef(document, string.Empty);
            Assert.That(document.IsDirty, Is.True);
            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.True);
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));
        }

        [Test]
        public async Task SaveRefusesAResRefAnotherBlueprintOwns()
        {
            File.Copy(Scratch("adren_harness"), Scratch("adren_taken"));

            var document = OpenScratch("adren_harness");
            SetResRef(document, "adren_taken");

            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.True);
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));
        }

        [Test]
        public async Task SaveWithoutARenameStaysInPlace()
        {
            var document = OpenScratch("adren_harness");
            var tagRow = document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag");
            tagRow.Text = "adren_harness_b";

            Assert.That(await document.TrySaveAsync(), Is.True);
            Assert.That(document.FilePath, Is.EqualTo(Scratch("adren_harness")));
            Assert.That(UtiDocument.Load(Scratch("adren_harness")).Tag, Is.EqualTo("adren_harness_b"));
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

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
