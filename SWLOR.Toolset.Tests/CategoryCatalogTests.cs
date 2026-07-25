using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The category sidecar's contract. The behaviours that matter most are the tolerant ones: a missing
    /// file, a malformed file, a resref that no longer exists and a resref nobody filed all have to be
    /// non-events, because the sidecar is metadata and must never be able to break a module.
    /// </summary>
    [TestFixture]
    public class CategoryCatalogTests
    {
        private string _directory = string.Empty;

        private string SidecarPath => Path.Combine(_directory, "toolset", "categories.json");

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "swlor_cat_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void DefaultPath_Sits_Beside_The_Module_Not_Inside_It()
        {
            var moduleRoot = Path.Combine(_directory, "Module");

            var path = CategoryCatalog.DefaultPathFor(moduleRoot);

            path.Should().Be(SidecarPath);
            path.Should().NotContain($"Module{Path.DirectorySeparatorChar}",
                because: "the game and Aurora rewrite files under Module/, so the sidecar would be wiped");
        }

        [Test]
        public void Missing_File_Loads_As_An_Empty_Catalog_Without_Warning()
        {
            var catalog = CategoryCatalog.Load(SidecarPath, out var warning);

            warning.Should().BeNull();
            catalog.Types.Should().BeEmpty();
            catalog.IsDirty.Should().BeFalse();
        }

        [Test]
        public void Malformed_File_Warns_But_Still_Opens()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SidecarPath)!);
            File.WriteAllText(SidecarPath, "{ this is not json");

            var catalog = CategoryCatalog.Load(SidecarPath, out var warning);

            warning.Should().NotBeNull().And.Contain("categories");
            catalog.Types.Should().BeEmpty();
        }

        [Test]
        public void Round_Trips_Nesting_Members_Pinning_And_Grouping()
        {
            var catalog = CategoryCatalog.Load(SidecarPath);
            var section = catalog.Section(ResourceType.Utp);
            section.Grouping = CategoryGrouping.Folders;
            section.Pin("Veles Dressing");

            var interiors = section.AddFolder("Interiors");
            var consoles = interiors.AddChild("Consoles & Terminals");
            var droidRepair = consoles.AddChild("Droid Repair");
            droidRepair.AddMember("_mdrn_pl_conso08");
            interiors.AddMember("aswtor_183");

            catalog.Save();

            var reloaded = CategoryCatalog.Load(SidecarPath);
            var reloadedSection = reloaded.Section(ResourceType.Utp);

            reloadedSection.Grouping.Should().Be(CategoryGrouping.Folders);
            reloadedSection.Pinned.Should().ContainSingle().Which.Should().Be("Veles Dressing");
            reloadedSection.Find("Interiors", "Consoles & Terminals", "Droid Repair")!
                .Members.Should().ContainSingle().Which.Should().Be("_mdrn_pl_conso08");
            reloadedSection.Find("Interiors")!.Members.Should().Contain("aswtor_183");
        }

        [Test]
        public void Save_Creates_Its_Folder_And_Clears_Dirty()
        {
            var catalog = CategoryCatalog.Load(SidecarPath);
            catalog.Section(ResourceType.Utc).AddFolder("Republic");
            catalog.MarkDirty();

            catalog.Save();

            File.Exists(SidecarPath).Should().BeTrue();
            catalog.IsDirty.Should().BeFalse();
        }

        [Test]
        public void Untouched_Sections_Are_Not_Written()
        {
            var catalog = CategoryCatalog.Load(SidecarPath);
            catalog.Section(ResourceType.Uti);          // merely visited
            catalog.Section(ResourceType.Utp).AddFolder("Cargo");

            catalog.Save();

            var text = File.ReadAllText(SidecarPath);
            text.Should().Contain("utp").And.NotContain("uti");
        }

        [Test]
        public void Unknown_Section_Keys_Are_Skipped_Rather_Than_Fatal()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SidecarPath)!);
            File.WriteAllText(SidecarPath, """
                { "version": 1, "sections": {
                    "wibble": { "folders": [ { "name": "Nope" } ] },
                    "utp":    { "folders": [ { "name": "Cargo" } ] } } }
                """);

            var catalog = CategoryCatalog.Load(SidecarPath, out var warning);

            warning.Should().BeNull();
            catalog.Types.Should().BeEquivalentTo(new[] { ResourceType.Utp });
            catalog.Section(ResourceType.Utp).Find("Cargo").Should().NotBeNull();
        }

        [Test]
        public void Nameless_Folders_Are_Dropped()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SidecarPath)!);
            File.WriteAllText(SidecarPath, """
                { "version": 1, "sections": { "utp": { "folders": [
                    { "name": "  " }, { "name": "Cargo" } ] } } }
                """);

            var catalog = CategoryCatalog.Load(SidecarPath);

            catalog.Section(ResourceType.Utp).Folders.Should().HaveCount(1);
        }
    }
}
