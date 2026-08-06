using FluentAssertions;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
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

            // The Check-kind rows (Plot, Stolen, Cursed, Identified, No Economy) split off into
            // FlagRows for the Basic tab's Flags card; BasicRows+FlagRows back together still cover
            // ItemEditorLayout.Basic in its original order, flags at the end.
            Assert.That(
                editor.BasicRows.Select(row => row.Definition.Name)
                    .Concat(editor.FlagRows.Select(row => row.Definition.Name)),
                Is.EqualTo(ItemEditorLayout.Basic.Select(definition => definition.Name)));

            Assert.That(editor.FlagRows.Select(row => row.Definition.Name), Is.EqualTo(new[]
            {
                "Plot", "Stolen", "Cursed", "Identified", "NO_ECONOMY"
            }));
            Assert.That(editor.BasicRows.Any(row => row.Definition.Kind == BehaviorFieldKind.Check), Is.False);

            var totalCost = editor.BasicRows.Single(row => row.Definition.Name == "Cost");
            Assert.That(totalCost.IsReadOnly, Is.True);
            Assert.That(totalCost.Number, Is.EqualTo(3000));
        }

        [Test]
        public void IntegerRowRejectsFractionalInputInsteadOfTruncating()
        {
            using var editor = Open("adren_harness");
            var stack = editor.BasicRows.Single(row => row.Definition.Name == "StackSize");

            stack.Number = 5;
            Assert.That(stack.Number, Is.EqualTo(5));

            // "5.9" must not silently store 5 while the box keeps showing 5.9 - the edit is
            // rejected and the box snaps back to what the document holds.
            stack.Number = 5.9m;
            Assert.That(stack.Number, Is.EqualTo(5));
        }

        [Test]
        public void EquipmentExposesVariablesForItsLocals()
        {
            using var editor = Open("adren_harness");

            // Equipment has no Behavior tab (no roles), so without this there would be nowhere in
            // the toolset to edit its locals - the new-item template's NO_ECONOMY opt-out included.
            Assert.That(editor.ShowsVariablesTab, Is.True);
            Assert.That(editor.Variables, Is.Not.Null);
        }

        [Test]
        public void EditingDescriptionMirrorsAnEmptyEngineCompanion()
        {
            var path = Path.Combine(
                CorpusLocator.ModuleDirectory,
                "uti",
                "adren_harness.uti.json");
            var document = UtiDocument.Load(path);
            var store = new ItemValueStore(document.Fields);
            store.SetLocalizedText("Description", string.Empty);
            store.SetLocalizedText("DescIdentified", string.Empty);
            using var editor = new ItemEditorViewModel(
                document.Fields,
                "adren_harness",
                (_, mutation) => { mutation(); return true; });

            editor.BasicRows.Single(row => row.Definition.Name == "DescIdentified").Text =
                "A mirrored description.";

            store.GetLocalizedText("DescIdentified").Should().Be("A mirrored description.");
            store.GetLocalizedText("Description").Should().Be("A mirrored description.");
        }

        [Test]
        public void EditingDescriptionReplacesDivergentHistoricalCompanionText()
        {
            var path = Path.Combine(
                CorpusLocator.ModuleDirectory,
                "uti",
                "adren_harness.uti.json");
            var document = UtiDocument.Load(path);
            var store = new ItemValueStore(document.Fields);
            store.SetLocalizedText("Description", "Historical unidentified text");
            store.SetLocalizedText("DescIdentified", "Old player-facing text");
            using var editor = new ItemEditorViewModel(
                document.Fields,
                "adren_harness",
                (_, mutation) => { mutation(); return true; });

            editor.BasicRows.Single(row => row.Definition.Name == "DescIdentified").Text =
                "New player-facing text";

            store.GetLocalizedText("DescIdentified").Should().Be("New player-facing text");
            store.GetLocalizedText("Description").Should().Be("New player-facing text");
        }

        [Test]
        public void SourceLessItemCannotClearNoEconomy()
        {
            var path = Path.Combine(
                CorpusLocator.ModuleDirectory,
                "uti",
                "adren_harness.uti.json");
            var document = UtiDocument.Load(path);
            var locals = new VarTable(document.Fields);
            locals.SetInt(ItemEditorLayout.NoEconomyLocal, 1);
            using var editor = new ItemEditorViewModel(
                document.Fields,
                "adren_harness",
                (_, mutation) => { mutation(); return true; },
                sourceLookup: _ => Array.Empty<ItemSourceEntry>(),
                itemSourcesReady: () => true);
            var noEconomy = editor.FlagRows.Single(row =>
                row.Definition.Name == ItemEditorLayout.NoEconomyLocal);

            noEconomy.IsChecked = false;

            noEconomy.IsChecked.Should().BeTrue();
            locals.GetInt(ItemEditorLayout.NoEconomyLocal).Should().Be(1);
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

        [Test]
        public void SwitchingBaseTypeUpdatesTheStatsTabInBothDirections()
        {
            // The tab's visibility was announced while the OUTGOING family's stats were still
            // loaded, so switching to a base type with no stats left the tab on screen with
            // nothing in it.
            using var editor = Open("ark_dragon_troph");
            Assert.That(editor.ShowsStatsTab, Is.False, "a trophy has no stats to show");

            var baseType = editor.BasicRows.Single(row => row.Definition.Name == "BaseItem");
            baseType.Choice = baseType.Choices.Single(choice => choice.Value == 16);

            Assert.That(editor.Family, Is.EqualTo(ItemFamily.Armor));
            Assert.That(editor.ShowsStatsTab, Is.True, "armor has defense, resistance and vitals");

            baseType.Choice = baseType.Choices.Single(choice => choice.Value == 29);

            Assert.That(editor.Family, Is.EqualTo(ItemFamily.Miscellaneous));
            Assert.That(editor.ShowsStatsTab, Is.False,
                "the tab goes away again rather than staying on screen empty");
        }

        [Test]
        public void StatsTabHidesWhenNoGroupOrEngineEntryApplies()
        {
            using var armor = Open("adren_harness");
            Assert.That(armor.ShowsStatsTab, Is.True);

            // A group-less Custom miscellaneous item with no engine-legacy entries has nothing the
            // tab could show.
            using var trophy = Open("ark_dragon_troph");
            Assert.That(trophy.ShowsStatsTab, Is.False);
        }
    }

    /// <summary>Save-time resref validation and file renaming, against a scratch copy of the corpus.</summary>
    [TestFixture]
    public class ItemDocumentRenameTests
    {
        private string _testRoot = string.Empty;
        private string _root = string.Empty;

        [SetUp]
        public void CreateScratchModule()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-item-rename-" + Guid.NewGuid().ToString("N"));
            _root = Path.Combine(_testRoot, "Module");
            Directory.CreateDirectory(Path.Combine(_root, "uti"));
            Directory.CreateDirectory(Path.Combine(_root, "are"));
            Directory.CreateDirectory(Path.Combine(_root, "utc"));
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json"),
                Scratch("adren_harness"));
        }

        [TearDown]
        public void DeleteScratchModule()
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, recursive: true);
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

        [TestCase(0, "", "The identified description")]
        [TestCase(1, "Authored unidentified text", "Authored identified text")]
        public async Task SaveAfterAnUnrelatedEditPreservesDistinctEngineDescriptions(
            int identified,
            string unidentifiedDescription,
            string identifiedDescription)
        {
            var source = UtiDocument.Load(Scratch("adren_harness"));
            var store = new ItemValueStore(source.Fields);
            store.SetInteger(
                BehaviorFieldStorage.Field,
                "Identified",
                GffFieldType.Byte,
                identified);
            store.SetLocalizedText("Description", unidentifiedDescription);
            store.SetLocalizedText("DescIdentified", identifiedDescription);
            source.Fields.GetLocStringOrNull("Description")!
                .SetText("2", unidentifiedDescription.Length == 0 ? "" : "Localized unidentified text");
            source.Fields.GetLocStringOrNull("DescIdentified")!
                .SetText("2", "Localized identified text");
            File.WriteAllBytes(Scratch("adren_harness"), source.ToBytes());

            var document = OpenScratch("adren_harness");
            try
            {
                document.IsDirty.Should().BeFalse();
                document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text =
                    "description_preservation";
                document.IsDirty.Should().BeTrue();

                Assert.That(await document.TrySaveAsync(), Is.True);

                var savedDocument = UtiDocument.Load(Scratch("adren_harness"));
                var saved = new ItemValueStore(savedDocument.Fields);
                saved.GetLocalizedText("Description")
                    .Should().Be(unidentifiedDescription);
                saved.GetLocalizedText("DescIdentified")
                    .Should().Be(identifiedDescription);
                savedDocument.Fields.GetLocStringOrNull("Description")!
                    .GetText("2").Should().Be(
                        unidentifiedDescription.Length == 0 ? "" : "Localized unidentified text");
                savedDocument.Fields.GetLocStringOrNull("DescIdentified")!
                    .GetText("2").Should().Be("Localized identified text");
                savedDocument.Fields.GetIntOrNull("Identified").Should().Be(identified);
                document.IsDirty.Should().BeFalse();
            }
            finally
            {
                document.OnClose();
            }
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
            Assert.That(
                Directory.EnumerateFiles(
                    _root,
                    ".swlor-toolset-item-rename-*.pending.json",
                    SearchOption.TopDirectoryOnly),
                Is.Empty);
        }

        [Test]
        public async Task OverwritingAnExternalGenerationCanStillRenameTheItem()
        {
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"),
                "adren_harness",
                null,
                new OutputLogService(),
                new StubPrompts(ExternalChangeChoice.Overwrite));
            SetResRef(document, "adren_mk10");

            var oldPath = Scratch("adren_harness");
            File.WriteAllText(
                oldPath,
                File.ReadAllText(oldPath).Replace(
                    "adren_harness",
                    "adren_harnesx",
                    StringComparison.Ordinal));

            Assert.That(await document.TrySaveAsync(), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk10")), Is.True);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.False);
            Assert.That(document.ResRef, Is.EqualTo("adren_mk10"));
        }

        [Test]
        public void OpeningAWorkspaceRecoversAnInterruptedItemRenameAndItsCategories()
        {
            var oldPath = Scratch("adren_harness");
            var oldBytes = File.ReadAllBytes(oldPath);
            var newPath = Scratch("adren_mk8");
            File.Copy(oldPath, newPath);
            File.Delete(oldPath);

            var categoryPath = CategoryCatalog.DefaultPathFor(_root);
            Directory.CreateDirectory(Path.GetDirectoryName(categoryPath)!);
            File.WriteAllText(categoryPath, "new category generation");

            var transactionName = ".swlor-toolset-item-rename-" + Guid.NewGuid().ToString("N");
            var transactionRoot = Path.Combine(_root, transactionName);
            Directory.CreateDirectory(transactionRoot);
            var itemBackup = Path.Combine(transactionRoot, "item.original");
            File.WriteAllBytes(itemBackup, oldBytes);
            var categoryBackup = Path.Combine(transactionRoot, "categories.original");
            File.WriteAllText(categoryBackup, "old category generation");
            var markerPath = transactionRoot + ".pending.json";
            File.WriteAllText(markerPath, JsonSerializer.Serialize(new
            {
                TransactionRoot = transactionRoot,
                OldPath = oldPath,
                NewPath = newPath,
                ItemBackupPath = itemBackup,
                CategoryPath = categoryPath,
                CategoryBackupPath = categoryBackup,
                CategoryExisted = true,
                CategoryOriginalContentSha256 = Hash("old category generation"),
                CategoryInstalledContentSha256 = Hash("new category generation"),
                NewContentSha256 = Convert.ToHexString(
                    SHA256.HashData(File.ReadAllBytes(newPath)))
            }));

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            workspace.Open(_root);

            File.ReadAllBytes(oldPath).Should().Equal(oldBytes);
            File.Exists(newPath).Should().BeFalse();
            File.ReadAllText(categoryPath).Should().Be("old category generation");
            File.Exists(markerPath).Should().BeFalse();
            Directory.Exists(transactionRoot).Should().BeFalse();
        }

        [Test]
        public void InterruptedRenameRecoveryPreservesANewerCategorySidecar()
        {
            var oldPath = Scratch("adren_harness");
            var oldBytes = File.ReadAllBytes(oldPath);
            var newPath = Scratch("adren_mk8");
            File.Copy(oldPath, newPath);
            File.Delete(oldPath);

            var categoryPath = CategoryCatalog.DefaultPathFor(_root);
            Directory.CreateDirectory(Path.GetDirectoryName(categoryPath)!);
            File.WriteAllText(categoryPath, "newer external category generation");

            var transactionName = ".swlor-toolset-item-rename-" + Guid.NewGuid().ToString("N");
            var transactionRoot = Path.Combine(_root, transactionName);
            Directory.CreateDirectory(transactionRoot);
            var itemBackup = Path.Combine(transactionRoot, "item.original");
            File.WriteAllBytes(itemBackup, oldBytes);
            var categoryBackup = Path.Combine(transactionRoot, "categories.original");
            File.WriteAllText(categoryBackup, "old category generation");
            var markerPath = transactionRoot + ".pending.json";
            File.WriteAllText(markerPath, JsonSerializer.Serialize(new
            {
                TransactionRoot = transactionRoot,
                OldPath = oldPath,
                NewPath = newPath,
                ItemBackupPath = itemBackup,
                CategoryPath = categoryPath,
                CategoryBackupPath = categoryBackup,
                CategoryExisted = true,
                CategoryOriginalContentSha256 = Hash("old category generation"),
                CategoryInstalledContentSha256 = Hash("renamed category generation"),
                NewContentSha256 = Convert.ToHexString(
                    SHA256.HashData(File.ReadAllBytes(newPath)))
            }));

            var workspace = new WorkspaceContext(
                path => new ModuleWorkspace(path),
                new OutputLogService());
            var action = () => workspace.Open(_root);

            action.Should().Throw<IOException>()
                .WithMessage("*category sidecar*changed after the interrupted item rename*");
            File.ReadAllText(categoryPath).Should().Be("newer external category generation");
            File.Exists(oldPath).Should().BeFalse("validation happens before any rollback write");
            File.Exists(newPath).Should().BeTrue("the interrupted destination is preserved with the evidence");
            File.Exists(markerPath).Should().BeTrue();
            Directory.Exists(transactionRoot).Should().BeTrue();
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
        public async Task SaveNormalizesAnExistingCaseOnlyFilename()
        {
            var lowerPath = Scratch("adren_harness");
            var upperPath = Scratch("ADREN_HARNESS");
            var intermediatePath = Scratch("case_rename_temp");
            File.Move(lowerPath, intermediatePath);
            File.Move(intermediatePath, upperPath);

            var document = OpenScratch("ADREN_HARNESS");
            var renames = new List<(string OldResRef, string OldPath)>();
            document.Renamed += (_, oldResRef, oldPath) => renames.Add((oldResRef, oldPath));
            SetResRef(document, "ADREN_HARNESS");

            Assert.That(await document.TrySaveAsync(), Is.True);
            var fileNames = Directory.EnumerateFiles(Path.Combine(_root, "uti"))
                .Select(Path.GetFileName)
                .ToList();
            fileNames.Should().Contain("adren_harness.uti.json");
            fileNames.Should().NotContain("ADREN_HARNESS.uti.json");
            document.ResRef.Should().Be("adren_harness");
            document.FilePath.Should().Be(lowerPath);
            UtiDocument.Load(lowerPath).TemplateResRef.Should().Be("adren_harness");
            renames.Should().Equal(new[] { ("ADREN_HARNESS", upperPath) });
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
        public async Task ARenameTargetAppearingMidSaveFailsInsteadOfBeingOverwritten()
        {
            // The destination was free when TryResolveRenameTarget checked, but another process
            // creates it during the reference scan - the exact window the no-overwrite install
            // exists for. The scan callback is that window, so the race is deterministic here.
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"), "adren_harness", null, new OutputLogService(), new StubPrompts(),
                findReferences: (_, _) =>
                {
                    File.WriteAllText(Scratch("adren_mk6"), "someone else's blueprint");
                    return Array.Empty<string>();
                });
            SetResRef(document, "adren_mk6");

            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.ReadAllText(Scratch("adren_mk6")), Is.EqualTo("someone else's blueprint"),
                "the freshly appeared file must survive untouched");
            Assert.That(File.Exists(Scratch("adren_harness")), Is.True, "the original is not deleted");
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));
        }

        [Test]
        public async Task ARenameIsRefusedByAnUnsavedScriptBufferJustAsByADiskReference()
        {
            // Save All writes item editors before script editors, so a rename that only consulted
            // disk could delete the blueprint and then save a script still naming it. The preflight
            // delegate is where the open buffers are folded in, so refusing from one is the shape
            // this test pins.
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"), "adren_harness", null, new OutputLogService(), new StubPrompts(),
                findReferences: (resRef, _) => resRef == "adren_harness"
                    ? new[] { "Module/nss/some_script.nss (unsaved editor buffer)" }
                    : Array.Empty<string>());
            SetResRef(document, "adren_mk7");

            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.True, "the blueprint the script names stays");
            Assert.That(File.Exists(Scratch("adren_mk7")), Is.False);
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));
        }

        [Test]
        public async Task AFailedDeleteOfTheOriginalRollsTheRenameBack()
        {
            var document = OpenScratch("adren_harness");
            SetResRef(document, "adren_mk5");

            // Holding the original open without FileShare.Delete makes File.Delete throw - the
            // "another process has the blueprint open" case. The new file must not survive it,
            // or every retry would be refused because the target path already exists.
            using (File.Open(Scratch("adren_harness"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.That(await document.TrySaveAsync(), Is.False);
            }

            Assert.That(File.Exists(Scratch("adren_harness")), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk5")), Is.False, "the new file was rolled back");
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));

            // With the lock gone the very same save succeeds - nothing was left to collide with.
            Assert.That(await document.TrySaveAsync(), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk5")), Is.True);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.False);
        }

        [Test]
        public async Task AnOriginalChangedDuringRenameIsPreserved()
        {
            const string externalGeneration = "someone else's newer original";
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"),
                "adren_harness",
                null,
                new OutputLogService(),
                new StubPrompts(),
                refileCategories: (_, _) =>
                {
                    // Category refiling occurs after the renamed destination is installed and
                    // immediately before the original delete. It deterministically exercises the
                    // race where an external editor saves during that window.
                    File.WriteAllText(Scratch("adren_harness"), externalGeneration);
                    return CategorySaveResult.Ok();
                });
            SetResRef(document, "adren_mk9");

            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.ReadAllText(Scratch("adren_harness")), Is.EqualTo(externalGeneration),
                "rollback must not overwrite or delete the external generation");
            Assert.That(File.Exists(Scratch("adren_mk9")), Is.False,
                "the uncommitted renamed destination is rolled back");
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));
            Assert.That(
                Directory.EnumerateFiles(
                    _root,
                    ".swlor-toolset-item-rename-*.pending.json",
                    SearchOption.TopDirectoryOnly),
                Is.Empty);
        }

        [Test]
        public async Task SaveRefusesARenameWhileOtherFilesStillReferenceTheOldResRef()
        {
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"), "adren_harness", null, new OutputLogService(), new StubPrompts(),
                findReferences: (resRef, _) => resRef == "adren_harness"
                    ? new[] { "SWLOR.Game.Server/Feature/LootTableDefinition/ViscaraLootTableDefinition.cs" }
                    : Array.Empty<string>());
            SetResRef(document, "adren_mk4");

            // The rename would delete the file that loot table still points at.
            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk4")), Is.False);
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));

            // A save that does not rename ignores references entirely.
            SetResRef(document, "adren_harness");
            var tagRow = document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag");
            tagRow.Text = "adren_harness_c";
            Assert.That(await document.TrySaveAsync(), Is.True);
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
        public async Task SaveRefusesARenameWhenTheCategoryPreflightSaysNo()
        {
            // Stands in for CategoryService.CanRefileMember reporting a locked or externally changed
            // sidecar: whatever the reason, the rename must not proceed while the category membership
            // cannot be carried over, or the sidecar is left naming a resref that no longer exists.
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"), "adren_harness", null, new OutputLogService(), new StubPrompts(),
                canRefileCategories: _ => false);
            SetResRef(document, "adren_mk7");

            Assert.That(await document.TrySaveAsync(), Is.False);
            Assert.That(File.Exists(Scratch("adren_harness")), Is.True);
            Assert.That(File.Exists(Scratch("adren_mk7")), Is.False);
            Assert.That(document.ResRef, Is.EqualTo("adren_harness"));

            // A save that does not rename never consults the category preflight.
            SetResRef(document, "adren_harness");
            var tagRow = document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag");
            tagRow.Text = "adren_harness_d";
            Assert.That(await document.TrySaveAsync(), Is.True);
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

        [Test]
        public async Task SaveFlagsAnUnobtainableItemThatWasPreviouslyUnrestricted()
        {
            var source = UtiDocument.Load(Scratch("adren_harness"));
            new VarTable(source.Fields).Remove(ItemEditorLayout.NoEconomyLocal);
            File.WriteAllBytes(Scratch("adren_harness"), source.ToBytes());
            var document = new ItemDocumentViewModel(
                Scratch("adren_harness"),
                "adren_harness",
                null,
                new OutputLogService(),
                new StubPrompts(),
                sourceLookup: _ => Array.Empty<ItemSourceEntry>(),
                itemSourcesReady: () => true);
            document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text =
                "adren_harness_restricted";

            Assert.That(await document.TrySaveAsync(), Is.True);

            var saved = UtiDocument.Load(Scratch("adren_harness"));
            new VarTable(saved.Fields).GetInt(ItemEditorLayout.NoEconomyLocal).Should().Be(1);
        }

        private static string Hash(string content) =>
            Convert.ToHexString(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));

        private sealed class StubPrompts(
            ExternalChangeChoice externalChangeChoice = ExternalChangeChoice.Cancel)
            : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(externalChangeChoice);

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
