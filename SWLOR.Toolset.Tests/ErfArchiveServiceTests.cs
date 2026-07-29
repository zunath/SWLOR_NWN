using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using SWLOR.Toolset.Archives;
using SWLOR.Toolset.Domain.Documents;
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

        [SetUp]
        public void SetUp()
        {
            ModuleMutationLock.ModuleWrites = null;
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
                Path.Combine(tools, "nwn_gff.exe"));
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
        public async Task ExportDependencyScanAndImportRoundTripScripts()
        {
            var sourceDirectory = Path.Combine(_firstModule, "nss");
            File.WriteAllText(
                Path.Combine(sourceDirectory, "entry.nss"),
                "#include \"shared_inc\"\nvoid main() {}\n");
            File.WriteAllText(
                Path.Combine(sourceDirectory, "shared_inc.nss"),
                "int SharedValue() { return 7; }\n");

            var dependencies = await _service.FindExportDependenciesAsync(new[] { "entry.nss" });
            dependencies.Should().ContainSingle(dependency =>
                dependency.FileName == "shared_inc.nss" &&
                dependency.Reason.Contains("entry.nss", StringComparison.Ordinal));

            var archivePath = Path.Combine(_root, "scripts.erf");
            var exported = await _service.ExportAsync(
                new[] { "entry.nss", "shared_inc.nss" },
                archivePath);
            exported.Exported.Should().Be(2);
            File.Exists(archivePath).Should().BeTrue();

            using var archive = await _service.OpenArchiveAsync(archivePath);
            archive.Assets.Select(asset => asset.FileName)
                .Should().BeEquivalentTo("entry.nss", "shared_inc.nss");

            _workspace.Open(_secondModule);
            var prepared = await _service.PrepareImportAsync(
                archive,
                archive.Assets.Select(asset => asset.FileName).ToList());
            prepared.Should().OnlyContain(item => item.Conflict == ErfConflictKind.New);

            var imported = await _service.ImportAsync(
                prepared.Select(item =>
                    new ErfImportChoice(item, item.DefaultAction, RenameResRef: null)).ToList());

            imported.Imported.Should().Be(2);
            File.ReadAllText(Path.Combine(_secondModule, "nss", "entry.nss"))
                .Should().Contain("#include \"shared_inc\"");
            File.ReadAllText(Path.Combine(_secondModule, "nss", "shared_inc.nss"))
                .Should().Contain("return 7");
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

            var archivePath = Path.Combine(_root, "logical-area.erf");
            await exportViewModel.NextCommand.ExecuteAsync(null);
            exportArea.IsSelected = true;
            await exportViewModel.NextCommand.ExecuteAsync(null);
            await exportViewModel.NextCommand.ExecuteAsync(null);
            (await exportViewModel.ExportAsync(archivePath)).Should().BeTrue();

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

            await importViewModel.NextCommand.ExecuteAsync(null);
            importViewModel.CurrentStep.Should().Be(1, importViewModel.StatusText);
            importArea.IsSelected = true;
            importArea.IsSelected.Should().BeTrue();
            await importViewModel.NextCommand.ExecuteAsync(null);
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
            File.WriteAllBytes(
                sourcePath,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "export_item", "Exported Item"));
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
                        OriginalExisted = true
                    },
                    new
                    {
                        DestinationPath = added,
                        RollbackPath = Path.Combine(
                            transactionRoot, "rollback", "nss", "partly_added.nss"),
                        OriginalExisted = false
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
        public async Task RenamingAnAreaKeepsAllThreeCompanionsOnTheSameResRef()
        {
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
