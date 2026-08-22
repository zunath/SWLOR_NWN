using FluentAssertions;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Archives;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class ErfArchiveServiceTests
    {
        private string _root = string.Empty;
        private string _firstModule = string.Empty;
        private string _secondModule = string.Empty;
        private WorkspaceContext _workspace = null!;
        private ErfArchiveService _service = null!;
        private int _workspaceDispatches;
        private int _customContentReloads;
        private IReadOnlyList<string> _reloadedHakNames = Array.Empty<string>();
        private string? _reloadedCustomTlk;

        [SetUp]
        public void SetUp()
        {
            ModuleMutationLock.ModuleWrites = null;
            _workspaceDispatches = 0;
            _customContentReloads = 0;
            _reloadedHakNames = Array.Empty<string>();
            _reloadedCustomTlk = null;
            _root = Path.Combine(Path.GetTempPath(), $"swlor-erf-{Guid.NewGuid():N}");
            _firstModule = Path.Combine(_root, "first", "Module");
            _secondModule = Path.Combine(_root, "second", "Module");
            CreateModuleFolders(_firstModule);
            CreateModuleFolders(_secondModule);

            var log = new OutputLogService();
            _workspace = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            _workspace.Open(_firstModule);

            var tools = FindToolsDirectory();
            _service = new ErfArchiveService(
                _workspace,
                log,
                Path.Combine(tools, "nwn_erf.exe"),
                Path.Combine(tools, "nwn_gff.exe"),
                dispatchToUiThread: action =>
                {
                    _workspaceDispatches++;
                    action();
                    return Task.CompletedTask;
                },
                reloadCustomContent: (moduleRoot, _) =>
                {
                    var ifo = IfoDocument.Load(
                        Path.Combine(moduleRoot, "ifo", "module.ifo.json"));
                    _customContentReloads++;
                    _reloadedHakNames = ifo.HakNames;
                    _reloadedCustomTlk = ifo.CustomTlk;
                    return Task.CompletedTask;
                });
        }

        [TearDown]
        public void TearDown()
        {
            ModuleMutationLock.ModuleWrites = null;
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // A process can briefly retain a handle after an integration test. The test result
                // matters more than best-effort cleanup under the runner's temp directory.
            }
        }

        [Test]
        public async Task ExportValidationAndImportRoundTripScripts()
        {
            var sourceDirectory = Path.Combine(_firstModule, "nss");
            File.WriteAllText(
                Path.Combine(sourceDirectory, "entry.nss"),
                "#include \"shared_inc\"\nvoid main() {}\n");
            File.WriteAllText(
                Path.Combine(sourceDirectory, "shared_inc.nss"),
                "int SharedValue() { return 7; }\n");
            var compiledGeneration = new byte[] { 0x4e, 0x43, 0x53, 0x20 };
            File.WriteAllBytes(
                Path.Combine(_firstModule, "ncs", "entry.ncs"),
                compiledGeneration);

            var entryAsset = _service.EnumerateModuleAssets()
                .Single(asset => asset.FileName == "entry.nss");
            await _service.ValidateExportSelectionAsync(new[] { entryAsset });

            var archivePath = Path.Combine(_root, "scripts.erf");
            var exported = await _service.ExportAsync(
                new[] { "entry.nss", "entry.ncs", "shared_inc.nss" },
                archivePath);
            exported.Exported.Should().Be(3);
            File.Exists(archivePath).Should().BeTrue();

            using var archive = await _service.OpenArchiveAsync(archivePath);
            archive.Assets.Select(asset => asset.FileName)
                .Should().BeEquivalentTo("entry.nss", "entry.ncs", "shared_inc.nss");

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                archive.Assets.Select(asset => asset.FileName).ToList());
            prepared.Should().OnlyContain(item => item.Conflict == ErfConflictKind.New);

            var imported = await _service.ImportAsync(
                prepared.Select(item =>
                    new ErfImportChoice(item, item.DefaultAction, RenameResRef: null)).ToList());

            imported.Imported.Should().Be(3);
            File.ReadAllText(Path.Combine(_secondModule, "nss", "entry.nss"))
                .Should().Contain("#include \"shared_inc\"");
            File.ReadAllText(Path.Combine(_secondModule, "nss", "shared_inc.nss"))
                .Should().Contain("return 7");
            File.ReadAllBytes(Path.Combine(_secondModule, "ncs", "entry.ncs"))
                .Should().Equal(compiledGeneration);
        }

        [Test]
        public async Task ExportAcquiresTheModuleLeaseBeforeEnumeratingResources()
        {
            const string fileName = "late_source.nss";
            var archivePath = Path.Combine(_root, "lease-protected.erf");
            Task<ErfExportResult> export;
            using (var heldLock = ModuleWriteLock.Acquire(_firstModule))
            {
                using (ExecutionContext.SuppressFlow())
                {
                    export = Task.Run(() =>
                        _service.ExportAsync(new[] { fileName }, archivePath));
                }

                await Task.Delay(150);
                export.IsCompleted.Should().BeFalse(
                    "export must wait for the module snapshot lease before enumerating");
                File.WriteAllText(
                    Path.Combine(_firstModule, "nss", fileName),
                    "int IncludedValue() { return 1; }\n");
            }

            var result = await export.WaitAsync(TimeSpan.FromSeconds(15));
            result.Exported.Should().Be(1);
            File.Exists(archivePath).Should().BeTrue();
        }

        [Test]
        public async Task ReturningFromPreparedImportClearsAutomaticallyAddedIncludes()
        {
            File.WriteAllText(
                Path.Combine(_firstModule, "nss", "entry.nss"),
                "#include \"shared_inc\"\nvoid main() {}\n");
            File.WriteAllText(
                Path.Combine(_firstModule, "nss", "shared_inc.nss"),
                "int SharedValue() { return 7; }\n");
            var archivePath = Path.Combine(_root, "back-from-import.erf");
            await _service.ExportAsync(new[] { "entry.nss", "shared_inc.nss" }, archivePath);

            _workspace.Open(_secondModule);
            using var viewModel = new ErfArchiveViewModel(
                _service,
                ToolsetSettings.Load(Path.Combine(_root, "back-from-import-settings.json")));
            (await viewModel.LoadArchiveAsync(archivePath)).Should().BeTrue();
            await viewModel.NextCommand.ExecuteAsync(null);
            var entry = viewModel.Assets.Single(row => row.FileName == "entry.nss");
            var include = viewModel.Assets.Single(row => row.FileName == "shared_inc.nss");
            entry.IsSelected = true;

            await viewModel.NextCommand.ExecuteAsync(null);
            include.IsRequired.Should().BeTrue();
            include.IsSelected.Should().BeTrue();

            viewModel.BackCommand.Execute(null);

            viewModel.CurrentStep.Should().Be(1);
            include.IsRequired.Should().BeFalse();
            include.RequiredReason.Should().BeEmpty();
            include.IsSelected.Should().BeFalse();
            entry.IsSelected.Should().BeTrue("the user's explicit selection is retained");
        }

        [Test]
        public async Task ModuleAssetsAreEnumeratedInBoundedProgressiveBatches()
        {
            var sourceDirectory = Path.Combine(_firstModule, "nss");
            for (var index = 0; index < 10; index++)
            {
                File.WriteAllText(
                    Path.Combine(sourceDirectory, $"batch_{index:00}.nss"),
                    "void main() {}\n");
            }

            var batches = new List<IReadOnlyList<ModuleArchiveAsset>>();
            await foreach (var batch in _service.EnumerateModuleAssetBatchesAsync(batchSize: 3))
                batches.Add(batch);

            batches.Select(batch => batch.Count).Should().Equal(3, 3, 3, 1);
            batches.SelectMany(batch => batch)
                .Select(asset => asset.FileName)
                .Should()
                .BeEquivalentTo(Enumerable.Range(0, 10).Select(index => $"batch_{index:00}.nss"));
        }

        [Test]
        public async Task HeaderSelectionTogglesOnlyShownAssets()
        {
            var sourceDirectory = Path.Combine(_firstModule, "nss");
            foreach (var resRef in new[] { "shown_first", "shown_second", "other" })
            {
                File.WriteAllText(
                    Path.Combine(sourceDirectory, $"{resRef}.nss"),
                    "void main() {}\n");
            }

            var settings = ToolsetSettings.Load(
                Path.Combine(_root, "header-selection-settings.json"));
            using var viewModel = new ErfArchiveViewModel(_service, settings);
            await viewModel.StartExportCommand.ExecuteAsync(null);
            viewModel.SearchText = "shown_";

            viewModel.CanToggleVisibleAssets.Should().BeTrue();
            viewModel.VisibleSelectionState.Should().BeFalse();

            var filteredAssetRefreshes = 0;
            var headerStateRefreshes = 0;
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ErfArchiveViewModel.FilteredAssets))
                    filteredAssetRefreshes++;
                if (args.PropertyName == nameof(ErfArchiveViewModel.VisibleSelectionState))
                    headerStateRefreshes++;
            };
            viewModel.ToggleVisibleSelectionCommand.Execute(null);

            viewModel.Assets.Where(row => row.ResRef.StartsWith("shown_"))
                .Should().OnlyContain(row => row.IsSelected);
            viewModel.Assets.Single(row => row.ResRef == "other")
                .IsSelected.Should().BeFalse();
            viewModel.VisibleSelectionState.Should().BeTrue();
            filteredAssetRefreshes.Should().Be(0);
            headerStateRefreshes.Should().Be(1);

            viewModel.Assets.Single(row => row.ResRef == "shown_first").IsSelected = false;
            viewModel.VisibleSelectionState.Should().BeNull();

            viewModel.ToggleVisibleSelectionCommand.Execute(null);
            viewModel.VisibleSelectionState.Should().BeTrue();

            viewModel.ToggleVisibleSelectionCommand.Execute(null);
            viewModel.FilteredAssets.Should().OnlyContain(row => !row.IsSelected);
            viewModel.VisibleSelectionState.Should().BeFalse();
        }

        [Test]
        public async Task ExportLeavesReferencedAssetsOptional()
        {
            const string areaResRef = "reference_area";
            const string dialogResRef = "optional_dlg";

            File.WriteAllBytes(
                Path.Combine(_firstModule, "are", $"{areaResRef}.are.json"),
                new JsonGffDocument("ARE ", new JsonGffStruct()).ToBytes());

            var gitRoot = new JsonGffStruct();
            gitRoot.Add(
                "Conversation",
                JsonGffField.CreateScalar(
                    GffFieldType.ResRef,
                    System.Text.Encoding.UTF8.GetBytes($"\"{dialogResRef}\"")));
            File.WriteAllBytes(
                Path.Combine(_firstModule, "git", $"{areaResRef}.git.json"),
                new JsonGffDocument("GIT ", gitRoot).ToBytes());
            File.WriteAllBytes(
                Path.Combine(_firstModule, "gic", $"{areaResRef}.gic.json"),
                new JsonGffDocument("GIC ", new JsonGffStruct()).ToBytes());

            File.WriteAllBytes(
                Path.Combine(_firstModule, "dlg", $"{dialogResRef}.dlg.json"),
                new JsonGffDocument("DLG ", new JsonGffStruct()).ToBytes());

            var settings = ToolsetSettings.Load(
                Path.Combine(_root, "optional-references-settings.json"));
            using var viewModel = new ErfArchiveViewModel(_service, settings);
            await viewModel.StartExportCommand.ExecuteAsync(null);
            viewModel.CurrentStep.Should().Be(1);
            viewModel.CanGoBack.Should().BeFalse();

            var area = viewModel.Assets.Should().ContainSingle(row => row.IsArea).Subject;
            var dialog = viewModel.Assets.Should().ContainSingle(
                row => row.FileName == $"{dialogResRef}.dlg").Subject;
            area.IsSelected = true;

            await viewModel.NextCommand.ExecuteAsync(null);

            dialog.IsSelected.Should().BeFalse();
            dialog.IsRequired.Should().BeFalse();
            viewModel.Assets.Should().NotContain(row => row.IsRequired);

            await viewModel.NextCommand.ExecuteAsync(null);
            var archivePath = Path.Combine(_root, "optional-references.erf");
            (await viewModel.ExportAsync(archivePath)).Should().BeTrue();

            using var archive = await _service.OpenArchiveAsync(archivePath);
            archive.Assets.Select(asset => asset.FileName)
                .Should().BeEquivalentTo(
                    $"{areaResRef}.are",
                    $"{areaResRef}.git",
                    $"{areaResRef}.gic");
        }

        [Test]
        public async Task AreaIsOneAssetInExportAndImportViews()
        {
            const string resRef = "logical_area";
            const string displayName =
                "A Very Long Area Name That Remains Available In Full When The Table Truncates It";
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                var root = new JsonGffStruct();
                if (extension == "are")
                {
                    var name = JsonGffField.CreateLocString();
                    var english = new LocStringEntry("0", Array.Empty<byte>());
                    name.AddLocStringEntry(english);
                    english.SetText(displayName);
                    root.Add("Name", name);
                }
                else
                {
                    root.Add(
                        "TestValue",
                        JsonGffField.CreateScalar(
                            GffFieldType.Int,
                            System.Text.Encoding.ASCII.GetBytes("1")));
                }
                File.WriteAllBytes(
                    Path.Combine(_firstModule, extension, $"{resRef}.{extension}.json"),
                    new JsonGffDocument(
                        extension.ToUpperInvariant() + " ",
                        root).ToBytes());
            }
            _workspace.RefreshCatalogEntry(ResourceType.Area, resRef);

            var exportSettings = ToolsetSettings.Load(
                Path.Combine(_root, "export-settings.json"));
            using var exportViewModel = new ErfArchiveViewModel(_service, exportSettings);
            await exportViewModel.StartExportCommand.ExecuteAsync(null);
            exportViewModel.CurrentStep.Should().Be(1);
            exportViewModel.CanGoBack.Should().BeFalse();

            var exportArea = exportViewModel.Assets.Should().ContainSingle().Subject;
            exportArea.IsArea.Should().BeTrue();
            exportArea.FileName.Should().Be(resRef);
            exportArea.TypeName.Should().Be("Area");
            exportArea.FileNames.Should().BeEquivalentTo(
                $"{resRef}.are",
                $"{resRef}.git",
                $"{resRef}.gic");
            exportArea.ResourceName.Should().Be(displayName);
            exportArea.MatchesSearch("remains available in full").Should().BeTrue();
            exportViewModel.TypeFilters.Should().Equal("All types", "Area");
            exportViewModel.StatusFilters.Should().Equal(
                "All assets",
                "Selected");

            var archivePath = Path.Combine(_root, "logical-area.erf");
            exportArea.IsSelected = true;
            var showedValidationProgress = false;
            exportViewModel.PropertyChanged += (_, args) =>
            {
                if ((args.PropertyName is nameof(ErfArchiveViewModel.IsValidatingSelection)
                        or nameof(ErfArchiveViewModel.CurrentStep)) &&
                    exportViewModel.IsValidatingSelection &&
                    exportViewModel.CurrentStep == 2)
                {
                    showedValidationProgress = true;
                }
            };
            await exportViewModel.NextCommand.ExecuteAsync(null);
            showedValidationProgress.Should().BeTrue();
            exportViewModel.IsValidatingSelection.Should().BeFalse();
            exportViewModel.ShowExportValidation.Should().BeTrue();
            await exportViewModel.NextCommand.ExecuteAsync(null);
            (await exportViewModel.ExportAsync(archivePath)).Should().BeTrue();

            EnsureModuleIfo(_secondModule);
            _workspace.Open(_secondModule);
            var importSettings = ToolsetSettings.Load(
                Path.Combine(_root, "import-settings.json"));
            using var importViewModel = new ErfArchiveViewModel(_service, importSettings);
            (await importViewModel.LoadArchiveAsync(archivePath)).Should().BeTrue();

            var importArea = importViewModel.Assets.Should().ContainSingle().Subject;
            importArea.IsArea.Should().BeTrue();
            importArea.FileName.Should().Be(resRef);
            importArea.TypeName.Should().Be("Area");
            importArea.FileNames.Should().BeEquivalentTo(exportArea.FileNames);
            for (var attempt = 0;
                 attempt < 200 && !string.Equals(
                     importArea.ResourceName,
                     displayName,
                     StringComparison.Ordinal);
                 attempt++)
            {
                await Task.Delay(25);
            }
            importArea.ResourceName.Should().Be(displayName);
            importViewModel.TypeFilters.Should().Equal("All types", "Area");
            importViewModel.StatusFilters.Should().Equal(
                "All assets",
                "Selected",
                "Added automatically",
                "Can't import");

            await importViewModel.NextCommand.ExecuteAsync(null);
            importViewModel.CurrentStep.Should().Be(1, importViewModel.StatusText);
            importArea.IsSelected = true;
            importArea.IsSelected.Should().BeTrue();
            var showedImportPreparation = false;
            importViewModel.PropertyChanged += (_, args) =>
            {
                if ((args.PropertyName is nameof(ErfArchiveViewModel.IsValidatingSelection)
                        or nameof(ErfArchiveViewModel.CurrentStep)) &&
                    importViewModel.IsValidatingSelection &&
                    importViewModel.CurrentStep == 2)
                {
                    showedImportPreparation = true;
                }
            };
            await importViewModel.NextCommand.ExecuteAsync(null);
            showedImportPreparation.Should().BeTrue();
            importViewModel.IsValidatingSelection.Should().BeFalse();
            importViewModel.CurrentStep.Should().Be(2, importViewModel.StatusText);
            importViewModel.ConflictAssets.Should().ContainSingle().Which.Should().BeSameAs(importArea);
            await importViewModel.NextCommand.ExecuteAsync(null);
            importViewModel.CurrentStep.Should().Be(3, importViewModel.StatusText);
            (await importViewModel.ImportAsync()).Should().BeTrue();

            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.Exists(Path.Combine(
                    _secondModule,
                    extension,
                    $"{resRef}.{extension}.json")).Should().BeTrue();
            }

            importViewModel.ShowImportAction.Should().BeFalse();
            importViewModel.ShowRestartImportAction.Should().BeTrue();
            importViewModel.CanGoBack.Should().BeFalse();

            importViewModel.RestartImportCommand.Execute(null);

            importViewModel.CurrentStep.Should().Be(0);
            importViewModel.IsComplete.Should().BeFalse();
            importViewModel.ImportArchivePath.Should().BeEmpty();
            importViewModel.Assets.Should().BeEmpty();
            importViewModel.CanGoNext.Should().BeFalse();
            importViewModel.StatusText.Should().Be("Choose an ERF file to begin.");
        }

        [Test]
        public async Task FailedExportValidationReturnsToAssetSelection()
        {
            const string resRef = "broken_asset";
            File.WriteAllText(
                Path.Combine(_firstModule, "utc", $"{resRef}.utc.json"),
                "{ this is not valid GFF JSON");

            var settings = ToolsetSettings.Load(
                Path.Combine(_root, "failed-validation-settings.json"));
            using var viewModel = new ErfArchiveViewModel(_service, settings);
            await viewModel.StartExportCommand.ExecuteAsync(null);
            viewModel.CurrentStep.Should().Be(1);
            viewModel.CanGoBack.Should().BeFalse();

            var asset = viewModel.Assets.Should().ContainSingle().Subject;
            asset.IsSelected = true;
            await viewModel.NextCommand.ExecuteAsync(null);

            viewModel.CurrentStep.Should().Be(1);
            viewModel.IsValidatingSelection.Should().BeFalse();
            viewModel.IsBusy.Should().BeFalse();
            viewModel.StatusText.Should().StartWith("Could not prepare selection:");
        }

        [Test]
        public void OneAreaConflictChoiceControlsEveryPhysicalMember()
        {
            const string resRef = "mixed_area";
            var rows = new List<ErfAssetRow>();
            var prepared = new List<ErfPreparedImport>();
            var conflicts = new[]
            {
                ErfConflictKind.Different,
                ErfConflictKind.New,
                ErfConflictKind.Identical
            };
            var extensions = new[] { "are", "git", "gic" };
            for (var index = 0; index < extensions.Length; index++)
            {
                var extension = extensions[index];
                var asset = new ErfArchiveAsset(
                    $"{resRef}.{extension}",
                    resRef,
                    extension,
                    Size: index + 1,
                    IsSupported: true,
                    TypeName: "Area",
                    UnsupportedReason: null);
                rows.Add(new ErfAssetRow(asset));
                prepared.Add(new ErfPreparedImport(
                    asset,
                    Path.Combine(_root, $"source.{extension}.json"),
                    Path.Combine(_root, $"destination.{extension}.json"),
                    conflicts[index],
                    conflicts[index] switch
                    {
                        ErfConflictKind.New => ErfConflictAction.Add,
                        ErfConflictKind.Identical => ErfConflictAction.Skip,
                        _ => ErfConflictAction.KeepExisting
                    }));
            }

            var area = rows[0];
            area.MergeArea(rows[1]);
            area.MergeArea(rows[2]);
            area.ApplyPrepared(prepared);

            area.ConflictLabel.Should().Be("Different");
            area.ConflictActionLabel.Should().Be("Keep existing");
            area.ToImportChoices().Should().OnlyContain(
                choice => choice.Action == ErfConflictAction.Skip);

            area.ConflictActionLabel = "Replace";
            area.ToImportChoices().Select(choice => choice.Action)
                .Should().Equal(
                    ErfConflictAction.Replace,
                    ErfConflictAction.Add,
                    ErfConflictAction.Replace);

            area.ConflictActionLabel = "Rename imported";
            area.RenameResRef = "renamed_area";
            area.ToImportChoices().Should().OnlyContain(choice =>
                choice.Action == ErfConflictAction.Rename &&
                choice.RenameResRef == "renamed_area");
        }

        [Test]
        public void AssetStatusesExplainWhatTheUserCanDo()
        {
            var supported = new ErfAssetRow(new ErfArchiveAsset(
                "shared.nss",
                "shared",
                "nss",
                Size: 12,
                IsSupported: true,
                TypeName: "Script source",
                UnsupportedReason: null));
            supported.RequiredReason = "Included by entry.nss";
            supported.IsRequired = true;
            supported.StatusLabel.Should().Be(
                "Added automatically · Included by entry.nss");

            var unsupported = new ErfAssetRow(new ErfArchiveAsset(
                "texture.tga",
                "texture",
                "tga",
                Size: 12,
                IsSupported: false,
                TypeName: ".tga resource",
                UnsupportedReason: "This resource cannot be stored in the module."));
            unsupported.StatusLabel.Should().Be("Can't import");
        }

        [Test]
        public async Task OpeningAnArchiveDoesNotWriteIntoModule()
        {
            File.WriteAllText(Path.Combine(_firstModule, "nss", "only.nss"), "void main() {}\n");
            var archivePath = Path.Combine(_root, "read-only-scan.erf");
            await _service.ExportAsync(new[] { "only.nss" }, archivePath);

            _workspace.Open(_secondModule);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            Directory.EnumerateFiles(_secondModule, "*", SearchOption.AllDirectories)
                .Should().BeEmpty("browse and scan must remain read-only until Import to Module");
            archive.Assets.Should().ContainSingle(asset => asset.FileName == "only.nss");
        }

        [Test]
        public async Task GffJsonIsConvertedBothWaysBeforeItIsCommitted()
        {
            var sourcePath = Path.Combine(_firstModule, "uti", "export_item.uti.json");
            var source = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "export_item", "Exported Item"));
            new VarTable(source.Root).Remove(BlueprintTemplateFactory.NoEconomyVariable);
            File.WriteAllBytes(sourcePath, source.ToBytes());
            var archivePath = Path.Combine(_root, "item.erf");

            await _service.ExportAsync(new[] { "export_item.uti" }, archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);
            archive.Assets.Should().ContainSingle(asset =>
                asset.FileName == "export_item.uti" && asset.IsSupported);

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                new[] { "export_item.uti" });
            await _service.ImportAsync(
                prepared.Select(item =>
                    new ErfImportChoice(item, item.DefaultAction, RenameResRef: null)).ToList());

            var importedPath = Path.Combine(_secondModule, "uti", "export_item.uti.json");
            var imported = JsonGffDocument.Load(importedPath);
            imported.DataType.Should().Be("UTI ");
            imported.Root.Get("TemplateResRef").GetString().Should().Be("export_item");
            new VarTable(imported.Root)
                .GetInt(BlueprintTemplateFactory.NoEconomyVariable)
                .Should().Be(1, "a newly imported item with no player source must stay out of economy searches");
            _workspaceDispatches.Should().Be(1,
                "post-import catalog notifications must pass through the UI dispatcher as one batch");
        }

        [Test]
        public async Task ReplacedItemsHaveEconomyRestrictionsReapplied()
        {
            _workspace.Open(_secondModule);
            const string resRef = "replaced_item";
            var sourcePath = Path.Combine(_root, resRef + ".uti.json");
            var source = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    resRef,
                    "Imported Replacement"));
            new VarTable(source.Root).Remove(BlueprintTemplateFactory.NoEconomyVariable);
            File.WriteAllBytes(sourcePath, source.ToBytes());

            var destination = Path.Combine(_secondModule, "uti", resRef + ".uti.json");
            File.WriteAllBytes(
                destination,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    resRef,
                    "Existing Item"));
            var asset = new ErfArchiveAsset(
                resRef + ".uti",
                resRef,
                "uti",
                new FileInfo(sourcePath).Length,
                IsSupported: true,
                TypeName: "Item",
                UnsupportedReason: null);

            await _service.ImportAsync(new[]
            {
                new ErfImportChoice(
                    new ErfPreparedImport(
                        asset,
                        sourcePath,
                        destination,
                        ErfConflictKind.Different,
                        ErfConflictAction.Replace),
                    ErfConflictAction.Replace,
                    RenameResRef: null)
            });

            var replaced = JsonGffDocument.Load(destination);
            new VarTable(replaced.Root)
                .GetInt(BlueprintTemplateFactory.NoEconomyVariable)
                .Should().Be(1, "replacement imports must not clear runtime economy restrictions");
        }

        [Test]
        public async Task StagedStoresCountAsPlayerSourcesBeforeNoEconomyIsApplied()
        {
            _workspace.Open(_secondModule);
            const string itemResRef = "staged_item";
            const string storeResRef = "staged_store";
            var itemSource = Path.Combine(_root, itemResRef + ".uti.json");
            var item = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    itemResRef,
                    "Staged Item"));
            new VarTable(item.Root).Remove(BlueprintTemplateFactory.NoEconomyVariable);
            File.WriteAllBytes(itemSource, item.ToBytes());

            var storeSource = Path.Combine(_root, storeResRef + ".utm.json");
            using (EditScope.EnterConstruction())
            {
                var storeRoot = SyntheticGit.Instance(
                    ("ResRef", GffFieldType.ResRef, storeResRef));
                var storeName = JsonGffField.CreateLocString();
                var englishName = new LocStringEntry("0", Array.Empty<byte>());
                storeName.AddLocStringEntry(englishName);
                englishName.SetText("Staged Store");
                storeRoot.Add("LocName", storeName);
                var inventoryItem = SyntheticGit.Instance(
                    ("InventoryRes", GffFieldType.ResRef, itemResRef));
                var page = new JsonGffStruct();
                page.Add("ItemList", SyntheticGit.ListOf(inventoryItem));
                storeRoot.Add("StoreList", SyntheticGit.ListOf(page));
                File.WriteAllBytes(
                    storeSource,
                    new JsonGffDocument("UTM ", storeRoot).ToBytes());
            }

            ErfImportChoice Choice(string sourcePath, string resRef, string extension)
            {
                var asset = new ErfArchiveAsset(
                    $"{resRef}.{extension}",
                    resRef,
                    extension,
                    new FileInfo(sourcePath).Length,
                    IsSupported: true,
                    TypeName: extension,
                    UnsupportedReason: null);
                return new ErfImportChoice(
                    new ErfPreparedImport(
                        asset,
                        sourcePath,
                        Path.Combine(_secondModule, extension, $"{resRef}.{extension}.json"),
                        ErfConflictKind.New,
                        ErfConflictAction.Add),
                    ErfConflictAction.Add,
                    RenameResRef: null);
            }

            await _service.ImportAsync(new[]
            {
                Choice(itemSource, itemResRef, "uti"),
                Choice(storeSource, storeResRef, "utm")
            });

            var importedItem = JsonGffDocument.Load(
                Path.Combine(_secondModule, "uti", itemResRef + ".uti.json"));
            new VarTable(importedItem.Root)
                .GetInt(BlueprintTemplateFactory.NoEconomyVariable)
                .Should().NotBe(1, "the staged store makes the item obtainable in the same transaction");
        }

        [Test]
        public async Task ImportDependenciesMatchBothResRefAndResourceType()
        {
            const string areaResRef = "typed_deps";
            const string sharedResRef = "shared";
            File.WriteAllBytes(
                Path.Combine(_firstModule, "are", areaResRef + ".are.json"),
                new JsonGffDocument("ARE ", new JsonGffStruct()).ToBytes());
            File.WriteAllBytes(
                Path.Combine(_firstModule, "gic", areaResRef + ".gic.json"),
                new JsonGffDocument("GIC ", new JsonGffStruct()).ToBytes());

            var creatureInstance = SyntheticGit.Instance(
                ("TemplateResRef", GffFieldType.ResRef, sharedResRef));
            var itemInstance = SyntheticGit.Instance(
                ("TemplateResRef", GffFieldType.ResRef, sharedResRef));
            var git = new JsonGffDocument("GIT ", new JsonGffStruct());
            git.Root.Add("Creature List", SyntheticGit.ListOf(creatureInstance));
            git.Root.Add("List", SyntheticGit.ListOf(itemInstance));
            File.WriteAllBytes(
                Path.Combine(_firstModule, "git", areaResRef + ".git.json"),
                git.ToBytes());
            File.WriteAllBytes(
                Path.Combine(_firstModule, "utc", sharedResRef + ".utc.json"),
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    sharedResRef,
                    "Shared Creature"));
            File.WriteAllBytes(
                Path.Combine(_firstModule, "uti", sharedResRef + ".uti.json"),
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    sharedResRef,
                    "Shared Item"));

            var archivePath = Path.Combine(_root, "typed-dependencies.erf");
            await _service.ExportAsync(
                new[]
                {
                    $"{areaResRef}.are",
                    $"{areaResRef}.git",
                    $"{areaResRef}.gic",
                    $"{sharedResRef}.utc",
                    $"{sharedResRef}.uti"
                },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { $"{areaResRef}.git" });

            dependencies.Select(dependency => dependency.FileName)
                .Should().Contain($"{sharedResRef}.utc")
                .And.Contain($"{sharedResRef}.uti");
        }

        [Test]
        public async Task EmbeddedItemTemplateResRefsAreDiscoveredAsDependencies()
        {
            const string creatureResRef = "embedded_owner";
            const string inventoryResRef = "embedded_inv";
            const string equippedResRef = "embedded_equip";
            var creature = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    creatureResRef,
                    "Embedded Item Owner"));
            using (EditScope.EnterConstruction())
            {
                creature.Root.Add(
                    "ItemList",
                    SyntheticGit.ListOf(SyntheticGit.Instance(
                        ("TemplateResRef", GffFieldType.ResRef, inventoryResRef))));
                creature.Root.Get("Equip_ItemList").InsertElement(
                    0,
                    SyntheticGit.Instance(
                        ("TemplateResRef", GffFieldType.ResRef, equippedResRef)));
            }

            File.WriteAllBytes(
                Path.Combine(_firstModule, "utc", $"{creatureResRef}.utc.json"),
                creature.ToBytes());
            foreach (var itemResRef in new[] { inventoryResRef, equippedResRef })
            {
                File.WriteAllBytes(
                    Path.Combine(_firstModule, "uti", $"{itemResRef}.uti.json"),
                    BlueprintTemplateFactory.CreateFileContent(
                        ResourceType.Uti,
                        itemResRef,
                        itemResRef));
            }

            var archivePath = Path.Combine(_root, "embedded-item-dependencies.erf");
            await _service.ExportAsync(
                new[]
                {
                    $"{creatureResRef}.utc",
                    $"{inventoryResRef}.uti",
                    $"{equippedResRef}.uti"
                },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { $"{creatureResRef}.utc" });

            dependencies.Select(dependency => dependency.FileName)
                .Should().BeEquivalentTo(
                    $"{inventoryResRef}.uti",
                    $"{equippedResRef}.uti");
        }

        [Test]
        public async Task ScriptSourceRequiresItsCompiledCompanion()
        {
            File.WriteAllText(
                Path.Combine(_firstModule, "nss", "compiled_pair.nss"),
                "void main() {}\n");
            File.WriteAllBytes(
                Path.Combine(_firstModule, "ncs", "compiled_pair.ncs"),
                new byte[] { 0x4e, 0x43, 0x53, 0x20 });
            var archivePath = Path.Combine(_root, "compiled-pair.erf");
            await _service.ExportAsync(
                new[] { "compiled_pair.nss", "compiled_pair.ncs" },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { "compiled_pair.nss" });

            dependencies.Should().ContainSingle(dependency =>
                dependency.FileName == "compiled_pair.ncs" &&
                dependency.Reason.Contains("compiled script companion"));
        }

        [Test]
        public async Task CompiledScriptRequiresItsSourceWhenTheArchiveContainsOne()
        {
            File.WriteAllText(
                Path.Combine(_firstModule, "nss", "compiled_pair.nss"),
                "void main() {}\n");
            File.WriteAllBytes(
                Path.Combine(_firstModule, "ncs", "compiled_pair.ncs"),
                new byte[] { 0x4e, 0x43, 0x53, 0x20 });
            var archivePath = Path.Combine(_root, "compiled-pair-reverse.erf");
            await _service.ExportAsync(
                new[] { "compiled_pair.nss", "compiled_pair.ncs" },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { "compiled_pair.ncs" });

            dependencies.Should().ContainSingle(dependency =>
                dependency.FileName == "compiled_pair.nss" &&
                dependency.Reason.Contains("script source companion"));
        }

        [Test]
        public async Task DialogRootScriptsAreDiscoveredAndRewritten()
        {
            const string dialogResRef = "scripted_dialog";
            var scripts = new Dictionary<string, string>
            {
                ["dlg_active"] = "active_new",
                ["dlg_end"] = "end_new",
                ["dlg_abort"] = "abort_new"
            };
            var dialog = JsonGffDocument.Parse(
                ModuleResourceTemplateFactory.CreateFileContent(
                    ResourceType.Dlg,
                    dialogResRef,
                    "Scripted Dialog"));
            using (EditScope.EnterConstruction())
            {
                dialog.Root.Add(
                    "Active",
                    JsonGffField.CreateScalar(
                        GffFieldType.ResRef,
                        System.Text.Encoding.UTF8.GetBytes("\"dlg_active\"")));
                dialog.Root.Get("EndConversation").SetString("dlg_end");
                dialog.Root.Get("EndConverAbort").SetString("dlg_abort");
            }
            File.WriteAllBytes(
                Path.Combine(_firstModule, "dlg", $"{dialogResRef}.dlg.json"),
                dialog.ToBytes());
            foreach (var script in scripts.Keys)
            {
                File.WriteAllText(
                    Path.Combine(_firstModule, "nss", $"{script}.nss"),
                    "void main() {}\n");
                File.WriteAllBytes(
                    Path.Combine(_firstModule, "ncs", $"{script}.ncs"),
                    new byte[] { 0x4e, 0x43, 0x53, 0x20 });
            }

            var archivePath = Path.Combine(_root, "dialog-scripts.erf");
            var exportedFiles = new[] { $"{dialogResRef}.dlg" }
                .Concat(scripts.Keys.SelectMany(script =>
                    new[] { $"{script}.nss", $"{script}.ncs" }))
                .ToList();
            await _service.ExportAsync(exportedFiles, archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { $"{dialogResRef}.dlg" });
            dependencies.Select(dependency => dependency.FileName).Should().BeEquivalentTo(
                scripts.Keys.SelectMany(script =>
                    new[] { $"{script}.nss", $"{script}.ncs" }));

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                archive.Assets.Select(asset => asset.FileName).ToList());
            await _service.ImportAsync(prepared.Select(item =>
            {
                var isScript = item.Asset.Extension is "nss" or "ncs";
                return new ErfImportChoice(
                    item,
                    isScript ? ErfConflictAction.Rename : ErfConflictAction.Add,
                    isScript ? scripts[item.Asset.ResRef] : null);
            }).ToList());

            var imported = JsonGffDocument.Load(
                Path.Combine(_secondModule, "dlg", $"{dialogResRef}.dlg.json"));
            imported.Root.Get("Active").GetString().Should().Be("active_new");
            imported.Root.Get("EndConversation").GetString().Should().Be("end_new");
            imported.Root.Get("EndConverAbort").GetString().Should().Be("abort_new");
        }

        [Test]
        public async Task ScriptSourceRequiresBlueprintsReferencedByRuntimeLiterals()
        {
            File.WriteAllText(
                Path.Combine(_firstModule, "nss", "literal_refs.nss"),
                "void main() {\n" +
                "    CreateItemOnObject(\"shared_asset\", OBJECT_SELF);\n" +
                "    CreateObject(OBJECT_TYPE_CREATURE, \"shared_asset\", GetLocation(OBJECT_SELF));\n" +
                "}\n");
            File.WriteAllBytes(
                Path.Combine(_firstModule, "uti", "shared_asset.uti.json"),
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    "shared_asset",
                    "Shared Item"));
            File.WriteAllBytes(
                Path.Combine(_firstModule, "utc", "shared_asset.utc.json"),
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    "shared_asset",
                    "Shared Creature"));
            var archivePath = Path.Combine(_root, "literal-dependencies.erf");
            await _service.ExportAsync(
                new[]
                {
                    "literal_refs.nss",
                    "shared_asset.uti",
                    "shared_asset.utc"
                },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { "literal_refs.nss" });

            dependencies.Select(dependency => dependency.FileName).Should()
                .BeEquivalentTo("shared_asset.uti", "shared_asset.utc");
        }

        [Test]
        public async Task CompiledOnlyEventScriptsAreDiscoveredAndRewritten()
        {
            const string creatureResRef = "compiled_ref";
            const string scriptResRef = "compiled_only";
            const string renamedScriptResRef = "compiled_exec";
            var creature = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    creatureResRef,
                    "Compiled Script User"));
            using (EditScope.EnterConstruction())
            {
                new UtcDocument(creature).ScriptSpawn = scriptResRef;
            }
            File.WriteAllBytes(
                Path.Combine(_firstModule, "utc", $"{creatureResRef}.utc.json"),
                creature.ToBytes());
            File.WriteAllBytes(
                Path.Combine(_firstModule, "ncs", $"{scriptResRef}.ncs"),
                new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x01 });

            var archivePath = Path.Combine(_root, "compiled-event-script.erf");
            await _service.ExportAsync(
                new[] { $"{creatureResRef}.utc", $"{scriptResRef}.ncs" },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { $"{creatureResRef}.utc" });
            dependencies.Should().ContainSingle(dependency =>
                dependency.FileName == $"{scriptResRef}.ncs");

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                archive.Assets.Select(asset => asset.FileName).ToList());
            await _service.ImportAsync(prepared.Select(item =>
                new ErfImportChoice(
                    item,
                    item.Asset.Extension == "ncs"
                        ? ErfConflictAction.Rename
                        : ErfConflictAction.Add,
                    item.Asset.Extension == "ncs" ? renamedScriptResRef : null)).ToList());

            var importedCreature = JsonGffDocument.Load(
                Path.Combine(_secondModule, "utc", $"{creatureResRef}.utc.json"));
            importedCreature.Root.Get("ScriptSpawn").GetString()
                .Should().Be(renamedScriptResRef);
            File.Exists(Path.Combine(
                _secondModule,
                "ncs",
                $"{renamedScriptResRef}.ncs")).Should().BeTrue();
        }

        [Test]
        public async Task ExecuteScriptLiteralsDiscoverAndRewriteCompiledCallees()
        {
            const string callerResRef = "script_caller";
            const string calleeResRef = "script_callee";
            const string renamedCalleeResRef = "renamed_callee";
            File.WriteAllText(
                Path.Combine(_firstModule, "nss", $"{callerResRef}.nss"),
                $"void RunCallee() {{ ExecuteScript(\"{calleeResRef}\", OBJECT_SELF); }}\n");
            File.WriteAllBytes(
                Path.Combine(_firstModule, "ncs", $"{calleeResRef}.ncs"),
                new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x02 });

            var archivePath = Path.Combine(_root, "execute-script.erf");
            await _service.ExportAsync(
                new[] { $"{callerResRef}.nss", $"{calleeResRef}.ncs" },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { $"{callerResRef}.nss" });
            dependencies.Should().ContainSingle(dependency =>
                dependency.FileName == $"{calleeResRef}.ncs");

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                archive.Assets.Select(asset => asset.FileName).ToList());
            await _service.ImportAsync(prepared.Select(item =>
                new ErfImportChoice(
                    item,
                    item.Asset.Extension == "ncs"
                        ? ErfConflictAction.Rename
                        : ErfConflictAction.Add,
                    item.Asset.Extension == "ncs" ? renamedCalleeResRef : null)).ToList());

            File.ReadAllText(Path.Combine(
                    _secondModule,
                    "nss",
                    $"{callerResRef}.nss"))
                .Should().Contain($"ExecuteScript(\"{renamedCalleeResRef}\"");
        }

        [Test]
        public async Task EquippedItemsAreDiscoveredAndRewritten()
        {
            const string creatureResRef = "equipped_user";
            const string itemResRef = "equipped_item";
            const string renamedItemResRef = "renamed_equip";
            var creature = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    creatureResRef,
                    "Equipped Item User"));
            using (EditScope.EnterConstruction())
            {
                var equipment = SyntheticGit.Instance(
                    ("EquippedRes", GffFieldType.ResRef, itemResRef));
                var equipmentList = creature.Root.Get("Equip_ItemList");
                equipmentList.InsertElement(equipmentList.Elements!.Count, equipment);
            }
            File.WriteAllBytes(
                Path.Combine(_firstModule, "utc", $"{creatureResRef}.utc.json"),
                creature.ToBytes());
            File.WriteAllBytes(
                Path.Combine(_firstModule, "uti", $"{itemResRef}.uti.json"),
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    itemResRef,
                    "Equipped Item"));

            var archivePath = Path.Combine(_root, "equipped-item.erf");
            await _service.ExportAsync(
                new[] { $"{creatureResRef}.utc", $"{itemResRef}.uti" },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            var dependencies = await _service.FindImportDependenciesAsync(
                archive,
                new[] { $"{creatureResRef}.utc" });
            dependencies.Should().ContainSingle(dependency =>
                dependency.FileName == $"{itemResRef}.uti");

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                archive.Assets.Select(asset => asset.FileName).ToList());
            await _service.ImportAsync(prepared.Select(item =>
                new ErfImportChoice(
                    item,
                    item.Asset.Extension == "uti"
                        ? ErfConflictAction.Rename
                        : ErfConflictAction.Add,
                    item.Asset.Extension == "uti" ? renamedItemResRef : null)).ToList());

            var importedCreature = JsonGffDocument.Load(
                Path.Combine(_secondModule, "utc", $"{creatureResRef}.utc.json"));
            importedCreature.Root.Get("Equip_ItemList").Elements!
                .Should().ContainSingle()
                .Which.Get("EquippedRes").GetString().Should().Be(renamedItemResRef);
        }

        [Test]
        public async Task ImportingScriptSourceReplacesAStaleCompiledCompanion()
        {
            _workspace.Open(_secondModule);
            var sourceNss = Path.Combine(_root, "compiled_pair.nss");
            var sourceNcs = Path.Combine(_root, "compiled_pair.ncs");
            File.WriteAllText(sourceNss, "void main() {}\n");
            var compiledGeneration = new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x02 };
            File.WriteAllBytes(sourceNcs, compiledGeneration);
            var destinationNss = Path.Combine(_secondModule, "nss", "compiled_pair.nss");
            var destinationNcs = Path.Combine(_secondModule, "ncs", "compiled_pair.ncs");
            File.WriteAllBytes(destinationNcs, new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x01 });

            ErfPreparedImport Prepared(
                string source,
                string destination,
                string extension,
                ErfConflictKind conflict)
            {
                var asset = new ErfArchiveAsset(
                    $"compiled_pair.{extension}",
                    "compiled_pair",
                    extension,
                    new FileInfo(source).Length,
                    IsSupported: true,
                    TypeName: extension,
                    UnsupportedReason: null);
                return new ErfPreparedImport(
                    asset,
                    source,
                    destination,
                    conflict,
                    conflict == ErfConflictKind.New
                        ? ErfConflictAction.Add
                        : ErfConflictAction.KeepExisting);
            }

            await _service.ImportAsync(new[]
            {
                new ErfImportChoice(
                    Prepared(sourceNss, destinationNss, "nss", ErfConflictKind.New),
                    ErfConflictAction.Add,
                    RenameResRef: null),
                new ErfImportChoice(
                    Prepared(sourceNcs, destinationNcs, "ncs", ErfConflictKind.Different),
                    ErfConflictAction.KeepExisting,
                    RenameResRef: null)
            });

            File.Exists(destinationNss).Should().BeTrue();
            File.ReadAllBytes(destinationNcs).Should().Equal(
                compiledGeneration,
                "the runtime must execute the generation that belongs to the imported source");
        }

        [Test]
        public async Task ImportRefusesCompiledBytecodeWhenRenamesRewriteItsSource()
        {
            _workspace.Open(_secondModule);
            var itemSource = Path.Combine(_root, "old_item.uti.json");
            var scriptSource = Path.Combine(_root, "runtime_ref.nss");
            var compiledSource = Path.Combine(_root, "runtime_ref.ncs");
            File.WriteAllBytes(
                itemSource,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    "old_item",
                    "Imported Item"));
            File.WriteAllText(
                scriptSource,
                "void main() { CreateItemOnObject(\"old_item\", OBJECT_SELF); }\n");
            File.WriteAllBytes(
                compiledSource,
                new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x01 });

            ErfPreparedImport Prepared(
                string source,
                string resRef,
                string extension,
                ErfConflictKind conflict)
            {
                var asset = new ErfArchiveAsset(
                    $"{resRef}.{extension}",
                    resRef,
                    extension,
                    new FileInfo(source).Length,
                    IsSupported: true,
                    TypeName: extension,
                    UnsupportedReason: null);
                var fileName = extension is "nss" or "ncs"
                    ? $"{resRef}.{extension}"
                    : $"{resRef}.{extension}.json";
                return new ErfPreparedImport(
                    asset,
                    source,
                    Path.Combine(_secondModule, extension, fileName),
                    conflict,
                    conflict == ErfConflictKind.New
                        ? ErfConflictAction.Add
                        : ErfConflictAction.KeepExisting);
            }

            Func<Task> import = async () => await _service.ImportAsync(new[]
            {
                new ErfImportChoice(
                    Prepared(itemSource, "old_item", "uti", ErfConflictKind.Different),
                    ErfConflictAction.Rename,
                    "renamed_item"),
                new ErfImportChoice(
                    Prepared(scriptSource, "runtime_ref", "nss", ErfConflictKind.New),
                    ErfConflictAction.Add,
                    RenameResRef: null),
                new ErfImportChoice(
                    Prepared(compiledSource, "runtime_ref", "ncs", ErfConflictKind.New),
                    ErfConflictAction.KeepExisting,
                    RenameResRef: null)
            });

            await import.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*source but not its NCS bytecode*");
            File.Exists(Path.Combine(_secondModule, "uti", "renamed_item.uti.json"))
                .Should().BeFalse("the unsafe plan must fail before any module mutation");
            File.Exists(Path.Combine(_secondModule, "nss", "runtime_ref.nss"))
                .Should().BeFalse();
            File.Exists(Path.Combine(_secondModule, "ncs", "runtime_ref.ncs"))
                .Should().BeFalse();
        }

        [TestCase(ErfConflictAction.Add)]
        [TestCase(ErfConflictAction.Replace)]
        [TestCase(ErfConflictAction.Rename)]
        public async Task ImportRefusesEntryPointScriptWithoutCompiledCompanion(
            ErfConflictAction action)
        {
            _workspace.Open(_secondModule);
            const string resRef = "source_only";
            const string renamedResRef = "renamed_source";
            var source = Path.Combine(_root, $"{resRef}.nss");
            var destinationNss = Path.Combine(_secondModule, "nss", $"{resRef}.nss");
            var destinationNcs = Path.Combine(_secondModule, "ncs", $"{resRef}.ncs");
            File.WriteAllText(source, "void main() { int imported = 1; }\n");

            var conflict = action == ErfConflictAction.Add
                ? ErfConflictKind.New
                : ErfConflictKind.Different;
            if (conflict == ErfConflictKind.Different)
                File.WriteAllText(destinationNss, "void main() { int existing = 1; }\n");
            if (action == ErfConflictAction.Replace)
                File.WriteAllBytes(destinationNcs, new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x01 });

            var asset = new ErfArchiveAsset(
                $"{resRef}.nss",
                resRef,
                "nss",
                new FileInfo(source).Length,
                IsSupported: true,
                TypeName: "Script source",
                UnsupportedReason: null);
            var prepared = new ErfPreparedImport(
                asset,
                source,
                destinationNss,
                conflict,
                conflict == ErfConflictKind.New
                    ? ErfConflictAction.Add
                    : ErfConflictAction.KeepExisting);

            Func<Task> import = async () => await _service.ImportAsync(new[]
            {
                new ErfImportChoice(
                    prepared,
                    action,
                    action == ErfConflictAction.Rename ? renamedResRef : null)
            });

            await import.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*entry-point script*without its compiled companion*source_only.ncs*");

            if (action == ErfConflictAction.Add)
                File.Exists(destinationNss).Should().BeFalse();
            else
                File.ReadAllText(destinationNss).Should().Contain("existing");
            if (action == ErfConflictAction.Replace)
                File.ReadAllBytes(destinationNcs).Should().Equal(
                    new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x01 });
            File.Exists(Path.Combine(_secondModule, "nss", $"{renamedResRef}.nss"))
                .Should().BeFalse();
        }

        [Test]
        public async Task ImportRefusesCompiledOnlyReplacementBesideExistingSource()
        {
            _workspace.Open(_secondModule);
            const string resRef = "compiled_only";
            File.WriteAllText(
                Path.Combine(_secondModule, "nss", $"{resRef}.nss"),
                "void main() { int existing = 1; }\n");
            var compiledSource = Path.Combine(_root, $"{resRef}.ncs");
            File.WriteAllBytes(
                compiledSource,
                new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x02 });

            Func<Task> import = async () => await _service.ImportAsync(new[]
            {
                CreateImportChoice(
                    compiledSource,
                    resRef,
                    "ncs",
                    ErfConflictAction.Add)
            });

            await import.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*without its matching source*already exists*");
            File.Exists(Path.Combine(_secondModule, "ncs", $"{resRef}.ncs"))
                .Should().BeFalse();
        }

        [Test]
        public async Task RenameWritesANewResourceAndRewritesImportedResRefs()
        {
            _workspace.Open(_secondModule);
            var sourcePath = Path.Combine(_root, "rename_source.uti.json");
            File.WriteAllBytes(
                sourcePath,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "old_item", "Renamed Item"));
            var existing = Path.Combine(_secondModule, "uti", "old_item.uti.json");
            File.WriteAllBytes(
                existing,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "old_item", "Existing Item"));

            var asset = new ErfArchiveAsset(
                "old_item.uti", "old_item", "uti", new FileInfo(sourcePath).Length,
                IsSupported: true, TypeName: "Item", UnsupportedReason: null);
            var prepared = new ErfPreparedImport(
                asset,
                sourcePath,
                existing,
                ErfConflictKind.Different,
                ErfConflictAction.KeepExisting);

            var result = await _service.ImportAsync(new[]
            {
                new ErfImportChoice(prepared, ErfConflictAction.Rename, "new_item")
            });

            result.Renamed.Should().Be(1);
            File.Exists(existing).Should().BeTrue("rename must preserve the existing resource");
            var renamed = JsonGffDocument.Load(
                Path.Combine(_secondModule, "uti", "new_item.uti.json"));
            renamed.Root.Get("TemplateResRef").GetString().Should().Be("new_item");
        }

        [Test]
        public async Task RenamingOneResourceTypeDoesNotRenameAnotherTypesIdentity()
        {
            _workspace.Open(_secondModule);
            var itemSource = Path.Combine(_root, "shared.uti.json");
            File.WriteAllBytes(
                itemSource,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "shared", "Shared Item"));
            var creatureSource = Path.Combine(_root, "shared.utc.json");
            File.WriteAllBytes(
                creatureSource,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc, "shared", "Shared Creature"));

            var choices = new List<ErfImportChoice>
            {
                Choice(itemSource, "uti", ErfConflictAction.Rename, "renamed_item"),
                Choice(creatureSource, "utc", ErfConflictAction.Add, null)
            };

            await _service.ImportAsync(choices);

            JsonGffDocument.Load(Path.Combine(_secondModule, "uti", "renamed_item.uti.json"))
                .Root.Get("TemplateResRef").GetString().Should().Be("renamed_item");
            JsonGffDocument.Load(Path.Combine(_secondModule, "utc", "shared.utc.json"))
                .Root.Get("TemplateResRef").GetString().Should().Be("shared");

            ErfImportChoice Choice(
                string source,
                string extension,
                ErfConflictAction action,
                string? renamed)
            {
                var asset = new ErfArchiveAsset(
                    $"shared.{extension}",
                    "shared",
                    extension,
                    new FileInfo(source).Length,
                    IsSupported: true,
                    TypeName: extension,
                    UnsupportedReason: null);
                return new ErfImportChoice(
                    new ErfPreparedImport(
                        asset,
                        source,
                        Path.Combine(_secondModule, extension, $"shared.{extension}.json"),
                        ErfConflictKind.New,
                        ErfConflictAction.Add),
                    action,
                    renamed);
            }
        }

        [Test]
        public async Task RenamingResourcesRewritesOnlyMatchingTypedReferences()
        {
            EnsureModuleIfo(_secondModule);
            _workspace.Open(_secondModule);

            var creatureSource = Path.Combine(_root, "shared.utc.json");
            var itemSource = Path.Combine(_root, "shared.uti.json");
            var dialogSource = Path.Combine(_root, "shared.dlg.json");
            var scriptSource = Path.Combine(_root, "shared.nss");
            var areSource = Path.Combine(_root, "typed_refs.are.json");
            var gitSource = Path.Combine(_root, "typed_refs.git.json");
            var gicSource = Path.Combine(_root, "typed_refs.gic.json");
            File.WriteAllBytes(
                creatureSource,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc, "shared", "Imported Creature"));
            File.WriteAllBytes(
                itemSource,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "shared", "Imported Item"));
            File.WriteAllBytes(
                dialogSource,
                new JsonGffDocument("DLG ", new JsonGffStruct()).ToBytes());
            File.WriteAllText(
                scriptSource,
                "void RewriteReferences() {\n" +
                "    CreateItemOnObject(\"shared\", OBJECT_SELF);\n" +
                "    CreateObject(OBJECT_TYPE_CREATURE, \"shared\", GetLocation(OBJECT_SELF));\n" +
                "}\n");
            File.WriteAllBytes(
                areSource,
                new JsonGffDocument("ARE ", new JsonGffStruct()).ToBytes());
            File.WriteAllBytes(
                gicSource,
                new JsonGffDocument("GIC ", new JsonGffStruct()).ToBytes());

            var creature = SyntheticGit.Instance(
                ("TemplateResRef", GffFieldType.ResRef, "shared"),
                ("Conversation", GffFieldType.ResRef, "shared"),
                ("ScriptSpawn", GffFieldType.ResRef, "shared"));
            var placedItem = SyntheticGit.Instance(
                ("TemplateResRef", GffFieldType.ResRef, "shared"));
            var inventoryItem = SyntheticGit.Instance(
                ("TemplateResRef", GffFieldType.ResRef, "shared"));
            var equippedItem = SyntheticGit.Instance(
                ("TemplateResRef", GffFieldType.ResRef, "shared"));
            creature.Add("ItemList", SyntheticGit.ListOf(inventoryItem));
            creature.Add("Equip_ItemList", SyntheticGit.ListOf(equippedItem));
            var git = new JsonGffDocument("GIT ", new JsonGffStruct());
            git.Root.Add("Creature List", SyntheticGit.ListOf(creature));
            git.Root.Add("List", SyntheticGit.ListOf(placedItem));
            File.WriteAllBytes(gitSource, git.ToBytes());

            ErfImportChoice Choice(
                string source,
                string resRef,
                string extension,
                ErfConflictAction action,
                string? renamed)
            {
                var asset = new ErfArchiveAsset(
                    $"{resRef}.{extension}",
                    resRef,
                    extension,
                    new FileInfo(source).Length,
                    IsSupported: true,
                    TypeName: extension,
                    UnsupportedReason: null);
                return new ErfImportChoice(
                    new ErfPreparedImport(
                        asset,
                        source,
                        Path.Combine(_secondModule, extension, $"{resRef}.{extension}.json"),
                        action == ErfConflictAction.Add
                            ? ErfConflictKind.New
                            : ErfConflictKind.Different,
                        action),
                    action,
                    renamed);
            }

            await _service.ImportAsync(new[]
            {
                Choice(creatureSource, "shared", "utc", ErfConflictAction.Rename, "renamed_creature"),
                Choice(itemSource, "shared", "uti", ErfConflictAction.Rename, "renamed_item"),
                Choice(dialogSource, "shared", "dlg", ErfConflictAction.Rename, "renamed_dialog"),
                Choice(scriptSource, "shared", "nss", ErfConflictAction.Rename, "renamed_script"),
                Choice(areSource, "typed_refs", "are", ErfConflictAction.Add, null),
                Choice(gitSource, "typed_refs", "git", ErfConflictAction.Add, null),
                Choice(gicSource, "typed_refs", "gic", ErfConflictAction.Add, null)
            });

            var importedGit = GitDocument.Load(
                Path.Combine(_secondModule, "git", "typed_refs.git.json"));
            var importedCreature = importedGit.Creatures.Should().ContainSingle().Subject;
            importedCreature.Get("TemplateResRef").GetString().Should().Be("renamed_creature");
            importedCreature.Get("Conversation").GetString().Should().Be("renamed_dialog");
            importedCreature.Get("ScriptSpawn").GetString().Should().Be("renamed_script");
            importedCreature.Get("TemplateResRef").GetString().Should().NotBe("renamed_item");
            importedCreature.Get("ItemList").Elements!.Single()
                .Get("TemplateResRef").GetString().Should().Be("renamed_item");
            importedCreature.Get("Equip_ItemList").Elements!.Single()
                .Get("TemplateResRef").GetString().Should().Be("renamed_item");
            importedGit.Items.Should().ContainSingle()
                .Which.Get("TemplateResRef").GetString().Should().Be("renamed_item");
            var importedScript = File.ReadAllText(
                Path.Combine(_secondModule, "nss", "renamed_script.nss"));
            importedScript.Should().Contain("CreateItemOnObject(\"renamed_item\"");
            importedScript.Should().Contain(
                "CreateObject(OBJECT_TYPE_CREATURE, \"renamed_creature\"");
        }

        [Test]
        public async Task RenamingAScriptIncludeUpdatesIncludeDirectives()
        {
            _workspace.Open(_secondModule);
            var entrySource = Path.Combine(_root, "entry.nss");
            var includeSource = Path.Combine(_root, "old_inc.nss");
            File.WriteAllText(
                entrySource,
                "#include \"old_inc\"\nint EntryValue() { return SharedValue(); }\n");
            File.WriteAllText(includeSource, "int SharedValue() { return 1; }\n");

            ErfImportChoice Choice(
                string source,
                string resRef,
                ErfConflictAction action,
                string? renamed)
            {
                var asset = new ErfArchiveAsset(
                    $"{resRef}.nss",
                    resRef,
                    "nss",
                    new FileInfo(source).Length,
                    IsSupported: true,
                    TypeName: "Script source",
                    UnsupportedReason: null);
                return new ErfImportChoice(
                    new ErfPreparedImport(
                        asset,
                        source,
                        Path.Combine(_secondModule, "nss", $"{resRef}.nss"),
                        ErfConflictKind.New,
                        ErfConflictAction.Add),
                    action,
                    renamed);
            }

            await _service.ImportAsync(new[]
            {
                Choice(entrySource, "entry", ErfConflictAction.Add, null),
                Choice(includeSource, "old_inc", ErfConflictAction.Rename, "new_inc")
            });

            File.ReadAllText(Path.Combine(_secondModule, "nss", "entry.nss"))
                .Should().Contain("#include \"new_inc\"");
            File.Exists(Path.Combine(_secondModule, "nss", "new_inc.nss")).Should().BeTrue();
        }

        [Test]
        public async Task StagedScriptsCountAsPlayerSourcesBeforeNoEconomyIsApplied()
        {
            _workspace.Open(_secondModule);
            const string itemResRef = "scripted_item";
            var itemSource = Path.Combine(_root, $"{itemResRef}.uti.json");
            var item = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti,
                    itemResRef,
                    "Scripted Item"));
            new VarTable(item.Root).Remove(BlueprintTemplateFactory.NoEconomyVariable);
            File.WriteAllBytes(itemSource, item.ToBytes());

            var scriptSource = Path.Combine(_root, "grant_item.nss");
            File.WriteAllText(
                scriptSource,
                $"void GrantItem() {{ CreateItemOnObject(\"{itemResRef}\", OBJECT_SELF); }}\n");

            await _service.ImportAsync(new[]
            {
                CreateImportChoice(itemSource, itemResRef, "uti", ErfConflictAction.Add),
                CreateImportChoice(scriptSource, "grant_item", "nss", ErfConflictAction.Add)
            });

            var importedItem = JsonGffDocument.Load(
                Path.Combine(_secondModule, "uti", $"{itemResRef}.uti.json"));
            new VarTable(importedItem.Root)
                .GetInt(BlueprintTemplateFactory.NoEconomyVariable)
                .Should().NotBe(
                    1,
                    "the staged script grants the item in the same transaction");
        }

        [Test]
        public async Task FixedNameModuleResourcesCannotBeRenamed()
        {
            EnsureModuleIfo(_secondModule);
            _workspace.Open(_secondModule);
            var source = Path.Combine(_root, "module.ifo.json");
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"),
                source);
            var asset = new ErfArchiveAsset(
                "module.ifo",
                "module",
                "ifo",
                new FileInfo(source).Length,
                IsSupported: true,
                TypeName: "Module properties",
                UnsupportedReason: null);
            var prepared = new ErfPreparedImport(
                asset,
                source,
                Path.Combine(_secondModule, "ifo", "module.ifo.json"),
                ErfConflictKind.Different,
                ErfConflictAction.KeepExisting);
            var row = new ErfAssetRow(asset);
            row.ApplyPrepared(new[] { prepared });

            row.AvailableActions.Should().NotContain("Rename imported");
            row.ConflictActionLabel = "Rename imported";
            row.CanRename.Should().BeFalse();

            var action = () => _service.ImportAsync(new[]
            {
                new ErfImportChoice(prepared, ErfConflictAction.Rename, "module_imp")
            });

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*fixed-name module resource*cannot be renamed*");
            File.Exists(Path.Combine(
                _secondModule,
                "ifo",
                "module_imp.ifo.json")).Should().BeFalse();
        }

        [Test]
        public async Task AreaRegistrationMergesIntoTheSelectedModuleIfo()
        {
            EnsureModuleIfo(_secondModule);
            _workspace.Open(_secondModule);
            const string areaResRef = "merged_ifo_area";
            var importedIfo = Path.Combine(_root, "selected_module.ifo.json");
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"),
                importedIfo);
            var choices = new List<ErfImportChoice>
            {
                CreateImportChoice(
                    importedIfo,
                    "module",
                    "ifo",
                    ErfConflictAction.Replace)
            };
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                var source = Path.Combine(_root, $"{areaResRef}.{extension}.json");
                File.WriteAllBytes(
                    source,
                    new JsonGffDocument(
                        extension.ToUpperInvariant() + " ",
                        new JsonGffStruct()).ToBytes());
                choices.Add(CreateImportChoice(
                    source,
                    areaResRef,
                    extension,
                    ErfConflictAction.Add));
            }

            var result = await _service.ImportAsync(choices);

            result.Imported.Should().Be(4);
            result.Replaced.Should().Be(1);
            IfoDocument.Load(Path.Combine(_secondModule, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain(areaResRef);
        }

        [Test]
        public async Task ReplacingModuleIfoReloadsTheInstalledCustomContentAssignments()
        {
            EnsureModuleIfo(_secondModule);
            _workspace.Open(_secondModule);
            var source = Path.Combine(_root, "replacement_module.ifo.json");
            var importedIfo = IfoDocument.Load(
                Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"));
            importedIfo.SetHakNames(new[] { "imported_hak_a", "imported_hak_b" });
            importedIfo.CustomTlk = "imported_tlk";
            File.WriteAllBytes(source, importedIfo.ToBytes());
            _customContentReloads = 0;

            var result = await _service.ImportAsync(new[]
            {
                CreateImportChoice(
                    source,
                    "module",
                    "ifo",
                    ErfConflictAction.Replace)
            });

            result.Replaced.Should().Be(1);
            _customContentReloads.Should().Be(1);
            _reloadedHakNames.Should().Equal("imported_hak_a", "imported_hak_b");
            _reloadedCustomTlk.Should().Be("imported_tlk");
        }

        [Test]
        public async Task IncludeOnlyReplacementRefusesToLeaveStaleBytecode()
        {
            _workspace.Open(_secondModule);
            const string resRef = "changed_include";
            var source = Path.Combine(_root, $"{resRef}.nss");
            var destinationSource = Path.Combine(_secondModule, "nss", $"{resRef}.nss");
            var destinationBytecode = Path.Combine(_secondModule, "ncs", $"{resRef}.ncs");
            File.WriteAllText(source, "int NewHelper() { return 2; }\n");
            File.WriteAllText(destinationSource, "void main() {}\n");
            var originalBytecode = new byte[] { 0x4e, 0x43, 0x53, 0x20, 0x03 };
            File.WriteAllBytes(destinationBytecode, originalBytecode);

            var action = () => _service.ImportAsync(new[]
            {
                CreateImportChoice(
                    source,
                    resRef,
                    "nss",
                    ErfConflictAction.Replace)
            });

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*include-only script*stale compiled artifact*");
            File.ReadAllText(destinationSource).Should().Be("void main() {}\n");
            File.ReadAllBytes(destinationBytecode).Should().Equal(originalBytecode);
        }

        [Test]
        public async Task ImportRefusesToReplaceAResourceChangedAfterPreparation()
        {
            _workspace.Open(_secondModule);
            var source = Path.Combine(_root, "replacement.nss");
            var destination = Path.Combine(_secondModule, "nss", "replacement.nss");
            File.WriteAllText(source, "int ImportedValue() { return 1; }\n");
            var original = System.Text.Encoding.UTF8.GetBytes("int OriginalValue() { return 1; }\n");
            File.WriteAllBytes(destination, original);

            var asset = new ErfArchiveAsset(
                "replacement.nss",
                "replacement",
                "nss",
                new FileInfo(source).Length,
                IsSupported: true,
                TypeName: "Script source",
                UnsupportedReason: null);
            var prepared = new ErfPreparedImport(
                asset,
                source,
                destination,
                ErfConflictKind.Different,
                ErfConflictAction.KeepExisting,
                new ErfDestinationFingerprint(
                    original.LongLength,
                    Convert.ToHexString(SHA256.HashData(original))));

            File.WriteAllText(destination, "int NewerValue() { return 1; }\n");

            var action = () => _service.ImportAsync(new[]
            {
                new ErfImportChoice(prepared, ErfConflictAction.Replace, RenameResRef: null)
            });

            await action.Should().ThrowAsync<IOException>()
                .WithMessage("*changed after the ERF import was prepared*");
            File.ReadAllText(destination).Should().Contain("NewerValue");
        }

        [Test]
        public async Task OpeningAnArchiveRejectsAnIncompleteArea()
        {
            const string resRef = "orphan_area";
            File.WriteAllBytes(
                Path.Combine(_firstModule, "are", $"{resRef}.are.json"),
                new JsonGffDocument("ARE ", new JsonGffStruct()).ToBytes());
            var archivePath = Path.Combine(_root, "incomplete-area.erf");
            await _service.ExportAsync(new[] { $"{resRef}.are" }, archivePath);

            Func<Task> action = async () =>
            {
                using var archive = await _service.OpenArchiveAsync(archivePath);
            };

            await action.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*incomplete*missing*.git*.gic*");
        }

        [Test]
        public async Task PreparingASelectionRejectsAPartialAreaTriplet()
        {
            const string resRef = "selected_area";
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.WriteAllBytes(
                    Path.Combine(_firstModule, extension, $"{resRef}.{extension}.json"),
                    new JsonGffDocument(
                        extension.ToUpperInvariant() + " ",
                        new JsonGffStruct()).ToBytes());
            }
            var archivePath = Path.Combine(_root, "complete-area.erf");
            await _service.ExportAsync(
                new[] { $"{resRef}.are", $"{resRef}.git", $"{resRef}.gic" },
                archivePath);
            using var archive = await _service.OpenArchiveAsync(archivePath);

            Func<Task> action = async () => await _service.PrepareImportAsync(
                archive,
                new[] { $"{resRef}.are" });

            await action.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*incomplete*missing*.git*.gic*");
        }

        [Test]
        public void StartupRecoveryRestoresReplacementsAndRemovesPartlyAddedFiles()
        {
            var destination = Path.Combine(_secondModule, "nss", "existing.nss");
            var added = Path.Combine(_secondModule, "nss", "partly_added.nss");
            File.WriteAllText(destination, "new generation");
            File.WriteAllText(added, "partial import");

            var transactionId = Guid.NewGuid().ToString("N");
            var transactionRoot = Path.Combine(
                Path.GetDirectoryName(_secondModule)!,
                ".swlor-toolset-erf-import-" + transactionId);
            var rollback = Path.Combine(transactionRoot, "rollback", "nss", "existing.nss");
            Directory.CreateDirectory(Path.GetDirectoryName(rollback)!);
            File.WriteAllText(rollback, "old generation");

            var manifestPath = Path.Combine(
                _secondModule,
                ".swlor-toolset-erf-import-" + transactionId + ".pending.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
            {
                TransactionRoot = transactionRoot,
                Entries = new[]
                {
                    new
                    {
                        DestinationPath = destination,
                        RollbackPath = rollback,
                        OriginalExisted = true,
                        InstalledContent = Fingerprint("new generation")
                    },
                    new
                    {
                        DestinationPath = added,
                        RollbackPath = Path.Combine(
                            transactionRoot, "rollback", "nss", "partly_added.nss"),
                        OriginalExisted = false,
                        InstalledContent = Fingerprint("partial import")
                    }
                }
            }));

            var recovered = ErfArchiveService.RecoverInterruptedImports(_secondModule);

            recovered.Should().BeEquivalentTo(destination, added);
            File.ReadAllText(destination).Should().Be("old generation");
            File.Exists(added).Should().BeFalse();
            File.Exists(manifestPath).Should().BeFalse();
            Directory.Exists(transactionRoot).Should().BeFalse();
        }

        [Test]
        public void StartupRecoveryPreservesAResourceChangedAfterTheInterruptedImport()
        {
            var destination = Path.Combine(_secondModule, "nss", "existing.nss");
            File.WriteAllText(destination, "new generation");

            var transactionId = Guid.NewGuid().ToString("N");
            var transactionRoot = Path.Combine(
                Path.GetDirectoryName(_secondModule)!,
                ".swlor-toolset-erf-import-" + transactionId);
            var rollback = Path.Combine(transactionRoot, "rollback", "nss", "existing.nss");
            Directory.CreateDirectory(Path.GetDirectoryName(rollback)!);
            File.WriteAllText(rollback, "old generation");
            var manifestPath = Path.Combine(
                _secondModule,
                ".swlor-toolset-erf-import-" + transactionId + ".pending.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
            {
                TransactionRoot = transactionRoot,
                Entries = new[]
                {
                    new
                    {
                        DestinationPath = destination,
                        RollbackPath = rollback,
                        OriginalExisted = true,
                        InstalledContent = Fingerprint("new generation")
                    }
                }
            }));
            File.WriteAllText(destination, "newer external generation");

            var action = () => ErfArchiveService.RecoverInterruptedImports(_secondModule);

            action.Should().Throw<ErfImportRecoveryException>()
                .Which.InnerException!.Message.Should()
                .Contain("changed after the interrupted ERF import");
            File.ReadAllText(destination).Should().Be("newer external generation");
            File.Exists(manifestPath).Should().BeTrue();
            Directory.Exists(transactionRoot).Should().BeTrue();
        }

        [Test]
        public async Task RenamingAnAreaKeepsAllThreeCompanionsOnTheSameResRef()
        {
            EnsureModuleIfo(_secondModule);
            _workspace.Open(_secondModule);
            var choices = new List<ErfImportChoice>();
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                var source = Path.Combine(_root, $"old_area.{extension}.json");
                File.WriteAllBytes(
                    source,
                    new JsonGffDocument(extension.ToUpperInvariant() + " ", new JsonGffStruct())
                        .ToBytes());
                var asset = new ErfArchiveAsset(
                    $"old_area.{extension}", "old_area", extension, new FileInfo(source).Length,
                    IsSupported: true, TypeName: "Area", UnsupportedReason: null);
                var prepared = new ErfPreparedImport(
                    asset,
                    source,
                    Path.Combine(_secondModule, extension, $"old_area.{extension}.json"),
                    ErfConflictKind.New,
                    ErfConflictAction.Add);
                choices.Add(new ErfImportChoice(
                    prepared,
                    extension == "are" ? ErfConflictAction.Rename : ErfConflictAction.Skip,
                    extension == "are" ? "new_area" : null));
            }

            var result = await _service.ImportAsync(choices);

            result.Imported.Should().Be(3);
            result.Renamed.Should().Be(3);
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.Exists(Path.Combine(
                    _secondModule, extension, $"new_area.{extension}.json")).Should().BeTrue();
                File.Exists(Path.Combine(
                    _secondModule, extension, $"old_area.{extension}.json")).Should().BeFalse();
            }
            IfoDocument.Load(Path.Combine(_secondModule, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("new_area");
        }

        private ErfImportChoice CreateImportChoice(
            string sourcePath,
            string resRef,
            string extension,
            ErfConflictAction action,
            string? renameResRef = null)
        {
            var fileName = $"{resRef}.{extension}";
            var destinationName = extension is "nss" or "ncs"
                ? fileName
                : $"{fileName}.json";
            var destination = Path.Combine(_secondModule, extension, destinationName);
            var conflict = File.Exists(destination)
                ? ErfConflictKind.Different
                : ErfConflictKind.New;
            var asset = new ErfArchiveAsset(
                fileName,
                resRef,
                extension,
                new FileInfo(sourcePath).Length,
                IsSupported: true,
                TypeName: extension,
                UnsupportedReason: null);
            return new ErfImportChoice(
                new ErfPreparedImport(
                    asset,
                    sourcePath,
                    destination,
                    conflict,
                    conflict == ErfConflictKind.New
                        ? ErfConflictAction.Add
                        : ErfConflictAction.KeepExisting),
                action,
                renameResRef);
        }

        private static void CreateModuleFolders(string moduleRoot)
        {
            foreach (var extension in new[]
                     {
                         "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl",
                         "ncs", "nss", "utc", "utd", "uti", "utm", "utp", "uts", "utt", "utw"
                     })
            {
                Directory.CreateDirectory(Path.Combine(moduleRoot, extension));
            }

        }

        private static void EnsureModuleIfo(string moduleRoot)
        {
            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"),
                Path.Combine(moduleRoot, "ifo", "module.ifo.json"),
                overwrite: true);
        }

        private static ErfDestinationFingerprint Fingerprint(string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return new ErfDestinationFingerprint(
                bytes.LongLength,
                Convert.ToHexString(SHA256.HashData(bytes)));
        }

        private static string FindToolsDirectory()
        {
            var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "tools", "SWLOR.CLI");
                if (File.Exists(Path.Combine(candidate, "nwn_erf.exe")) &&
                    File.Exists(Path.Combine(candidate, "nwn_gff.exe")))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not find tools/SWLOR.CLI from the test output.");
        }
    }
}
