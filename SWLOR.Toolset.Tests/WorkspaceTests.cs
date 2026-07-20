using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP2.6 workspace layer: <see cref="ModuleWorkspace"/> (over the real repo
    /// Module directory - enumeration only, no full parse) and <see cref="BlueprintCatalog"/> (over
    /// a small synthetic module directory, so the full ~17,900-file corpus is never parsed inside
    /// the test suite).
    /// </summary>
    public class WorkspaceTests
    {
        private static string ModuleDirectory => CorpusLocator.ModuleDirectory;

        [Test]
        public void Constructor_OverRealModuleDirectory_Succeeds()
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);

            workspace.ModuleRoot.Should().Be(Path.GetFullPath(ModuleDirectory));
        }

        [Test]
        public void Constructor_MissingDirectory_ThrowsDirectoryNotFound()
        {
            var missing = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));

            var act = () => new ModuleWorkspace(missing);

            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Test]
        public void Constructor_DirectoryThatIsNotAModuleRoot_Throws()
        {
            var notAModule = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", "not_a_module_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(notAModule);

            try
            {
                var act = () => new ModuleWorkspace(notAModule);

                act.Should().Throw<InvalidOperationException>();
            }
            finally
            {
                Directory.Delete(notAModule, recursive: true);
            }
        }

        [Test]
        public void EnumerateResRefs_OverRealModule_ReportsExpectedCounts()
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);

            workspace.EnumerateResRefs(ResourceType.Utc).Count.Should().Be(897, "the module corpus should have 897 creature blueprints");
            workspace.EnumerateAreaResRefs().Count.Should().Be(438, "the module corpus should have 438 areas");
        }

        [Test]
        public void EnumerateResRefs_ReturnsPlainResRefsWithoutParsing()
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);

            var resRefs = workspace.EnumerateResRefs(ResourceType.Utc);

            resRefs.Should().Contain("alask");
            resRefs.Should().OnlyContain(r => !r.Contains('.'), "resrefs must have the .utc.json suffix stripped, not just the extension");
        }

        [Test]
        public void LoadArea_OverRealModule_ReturnsTripletThatRoundTripsForTheRequestedResRef()
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);
            var resRef = workspace.EnumerateAreaResRefs().First();

            var (are, git, gic) = workspace.LoadArea(resRef);

            are.Should().NotBeNull();
            git.Should().NotBeNull();
            gic.Should().NotBeNull();

            // Proves the triplet actually corresponds to the requested resref (not just "some"
            // are/git/gic file): each document's re-serialized bytes must match the exact file
            // that resref names on disk, byte for byte.
            are.ToBytes().Should().Equal(File.ReadAllBytes(workspace.GetResourcePath(ResourceType.Area, resRef)));
            git.ToBytes().Should().Equal(File.ReadAllBytes(Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json")));
            gic.ToBytes().Should().Equal(File.ReadAllBytes(Path.Combine(workspace.ModuleRoot, "gic", resRef + ".gic.json")));
        }

        [Test]
        public void LoadBlueprint_Utc_ReturnsTypedDocumentMatchingTheFileOnDisk()
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);
            var resRef = "alask";

            var document = workspace.LoadBlueprint(ResourceType.Utc, resRef);

            document.Should().BeOfType<UtcDocument>();
            var utc = (UtcDocument)document;
            utc.Tag.Should().Be("Alask");
            utc.FirstName.Text.Should().Be("Alask");
        }

        [Test]
        public void LoadBlueprint_AreaType_ThrowsBecauseLoadAreaMustBeUsedInstead()
        {
            var workspace = new ModuleWorkspace(ModuleDirectory);

            var act = () => workspace.LoadBlueprint(ResourceType.Area, "anchor_roche01");

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void BlueprintCatalog_OverSyntheticModule_IndexesEntriesWithNameAndTag()
        {
            using var synthetic = SyntheticModule.CreateFromRealFiles(ModuleDirectory);
            var workspace = new ModuleWorkspace(synthetic.Path);

            var catalog = new BlueprintCatalog(workspace);
            catalog.BuildTask.GetAwaiter().GetResult();

            catalog.Entries.Should().HaveCount(synthetic.ExpectedEntryCount);

            var alask = catalog.Entries.Should().ContainSingle(e => e.ResourceType == ResourceType.Utc && e.ResRef == "alask").Subject;
            alask.Name.Should().Be("Alask Firith", "the catalog joins FirstName and LastName for the display name");
            alask.Tag.Should().Be("Alask");
            alask.FilePath.Should().Be(workspace.GetResourcePath(ResourceType.Utc, "alask"));

            var area = catalog.Entries.Should().ContainSingle(e => e.ResourceType == ResourceType.Area && e.ResRef == synthetic.AreaResRef).Subject;
            area.FilePath.Should().Be(workspace.GetResourcePath(ResourceType.Area, synthetic.AreaResRef));
        }

        [Test]
        public void BlueprintCatalog_Progress_ReachesTotalCountOnCompletion()
        {
            using var synthetic = SyntheticModule.CreateFromRealFiles(ModuleDirectory);
            var workspace = new ModuleWorkspace(synthetic.Path);

            var progressSnapshots = new List<(int Processed, int Total)>();
            var catalog = new BlueprintCatalog(workspace, (processed, total) => progressSnapshots.Add((processed, total)));
            catalog.BuildTask.GetAwaiter().GetResult();

            catalog.TotalCount.Should().Be(synthetic.ExpectedEntryCount);
            catalog.ProcessedCount.Should().Be(synthetic.ExpectedEntryCount);
            progressSnapshots.Should().Contain((synthetic.ExpectedEntryCount, synthetic.ExpectedEntryCount));
        }

        [Test]
        public void Search_RanksExactMatchBeforePrefixBeforeContains()
        {
            using var synthetic = SyntheticModule.CreateFromRealFiles(ModuleDirectory);
            var workspace = new ModuleWorkspace(synthetic.Path);

            var catalog = new BlueprintCatalog(workspace);
            catalog.BuildTask.GetAwaiter().GetResult();

            // "alask" is an exact resref match; "ala" is a resref-prefix match (also on "alask");
            // "ask" only matches by containment within "alask".
            var exactResults = catalog.Search("alask");
            exactResults.Should().NotBeEmpty();
            exactResults[0].Entry.ResRef.Should().Be("alask");
            exactResults[0].MatchKind.Should().Be(CatalogMatchKind.ExactResRef);

            var prefixResults = catalog.Search("ala");
            prefixResults.Should().Contain(r => r.Entry.ResRef == "alask" && r.MatchKind == CatalogMatchKind.Prefix);

            var containsResults = catalog.Search("ask");
            containsResults.Should().Contain(r => r.Entry.ResRef == "alask" && r.MatchKind == CatalogMatchKind.Contains);

            // Tier ordering: every Prefix-or-better result must sort before every Contains-only result.
            var kinds = containsResults.Select(r => r.MatchKind).ToList();
            var firstContainsIndex = kinds.IndexOf(CatalogMatchKind.Contains);
            if (firstContainsIndex >= 0)
                kinds.Skip(firstContainsIndex).Should().OnlyContain(k => k == CatalogMatchKind.Contains);
        }

        [Test]
        public void Search_EmptyOrWhitespaceQuery_ReturnsNoResults()
        {
            using var synthetic = SyntheticModule.CreateFromRealFiles(ModuleDirectory);
            var workspace = new ModuleWorkspace(synthetic.Path);

            var catalog = new BlueprintCatalog(workspace);
            catalog.BuildTask.GetAwaiter().GetResult();

            catalog.Search("").Should().BeEmpty();
            catalog.Search("   ").Should().BeEmpty();
        }

        /// <summary>
        /// A small on-disk module directory built by copying a handful of real files out of the
        /// repo's Module directory, so <see cref="BlueprintCatalog"/> tests exercise real GFF JSON
        /// content without paying to parse the full ~17,900-file corpus inside the test suite.
        /// </summary>
        private sealed class SyntheticModule : IDisposable
        {
            public string Path { get; }
            public string AreaResRef { get; }
            public int ExpectedEntryCount { get; }

            private SyntheticModule(string path, string areaResRef, int expectedEntryCount)
            {
                Path = path;
                AreaResRef = areaResRef;
                ExpectedEntryCount = expectedEntryCount;
            }

            public static SyntheticModule CreateFromRealFiles(string realModuleDirectory)
            {
                var root = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "SWLOR.Toolset.Tests", "workspace_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root);

                var areaResRef = CopyFirst(realModuleDirectory, root, "are", 1).Single();
                CopyNamed(realModuleDirectory, root, "git", areaResRef);
                CopyNamed(realModuleDirectory, root, "gic", areaResRef);
                var utcCount = CopyFirst(realModuleDirectory, root, "utc", 2).Count;
                var utiCount = CopyFirst(realModuleDirectory, root, "uti", 1).Count;
                var utpCount = CopyFirst(realModuleDirectory, root, "utp", 1).Count;
                var utdCount = CopyFirst(realModuleDirectory, root, "utd", 1).Count;

                // Ensure the "alask" utc (used by name/tag/search assertions above) is present
                // regardless of directory enumeration order.
                if (!File.Exists(System.IO.Path.Combine(root, "utc", "alask.utc.json")))
                {
                    File.Copy(
                        System.IO.Path.Combine(realModuleDirectory, "utc", "alask.utc.json"),
                        System.IO.Path.Combine(root, "utc", "alask.utc.json"));
                    utcCount++;
                }

                var expectedCount = 1 /* area */ + utcCount + utiCount + utpCount + utdCount;
                return new SyntheticModule(root, areaResRef, expectedCount);
            }

            private static List<string> CopyFirst(string sourceRoot, string destRoot, string folder, int count)
            {
                var sourceDir = System.IO.Path.Combine(sourceRoot, folder);
                var destDir = System.IO.Path.Combine(destRoot, folder);
                Directory.CreateDirectory(destDir);

                var resRefs = new List<string>();
                var suffix = "." + folder + ".json";
                foreach (var file in Directory.EnumerateFiles(sourceDir, "*" + suffix).Take(count))
                {
                    var fileName = System.IO.Path.GetFileName(file);
                    File.Copy(file, System.IO.Path.Combine(destDir, fileName));
                    resRefs.Add(fileName[..^suffix.Length]);
                }

                return resRefs;
            }

            private static void CopyNamed(string sourceRoot, string destRoot, string folder, string resRef)
            {
                var destDir = System.IO.Path.Combine(destRoot, folder);
                Directory.CreateDirectory(destDir);
                var fileName = resRef + "." + folder + ".json";
                File.Copy(
                    System.IO.Path.Combine(sourceRoot, folder, fileName),
                    System.IO.Path.Combine(destDir, fileName));
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch (Exception)
                {
                    // Best-effort cleanup; leftover temp dirs from a killed test run are harmless.
                }
            }
        }
    }
}
