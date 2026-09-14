using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// <see cref="ModuleFolderSeeder"/>: the folder tree Module Contents opens with.
    /// </summary>
    [TestFixture]
    public class ModuleFolderSeederTests
    {
        private static IReadOnlyList<string> AreaPath(string displayName) =>
            ModuleFolderSeeder.PathFor(ResourceType.Area, new SeedableResource("probe", displayName));

        [Test]
        public void AnArea_NestsByItsOwnNameSegments()
        {
            AreaPath("Tatooine - Anchorhead - North Entrance")
                .Should().Equal("Tatooine", "Anchorhead");

            AreaPath("Tatooine - Anchorhead").Should().Equal("Tatooine");
        }

        /// <summary>
        /// Prefabs are reusable set pieces, not places in the world, so they collect in one tree instead of
        /// scattering "[Prefab] Korriban" alongside the real Korriban.
        /// </summary>
        [Test]
        public void APrefabArea_GoesUnderPrefabs()
        {
            AreaPath("[Prefab] Korriban - Tomb").Should().Equal("Prefabs", "Korriban");
            AreaPath("[Prefab] Ebon Hawk").Should().Equal("Prefabs");
        }

        [Test]
        public void ASystemArea_GoesUnderSystem()
        {
            AreaPath("*Character Rebuild").Should().Equal("System");
            AreaPath("*No Access").Should().Equal("System");
        }

        /// <summary>
        /// A name with no separator says nothing about where it belongs. Inventing a folder of one for it
        /// is worse than leaving it in Unsorted, where it is visible and can be filed by hand.
        /// </summary>
        [Test]
        public void AnAreaWithNoStructureInItsName_StaysUnsorted()
        {
            AreaPath("area_template").Should().BeEmpty();
            AreaPath("").Should().BeEmpty();
        }

        [Test]
        public void AnAreaLeafLabel_DropsWhatItsFoldersAlreadySay()
        {
            ModuleFolderSeeder.LeafLabel(
                    ResourceType.Area, new SeedableResource("probe", "Tatooine - Anchorhead - North Entrance"))
                .Should().Be("North Entrance");
        }

        [TestCase("veles_bankteller", new[] { "Viscara", "Veles" })]
        [TestCase("cq_absdef", new[] { "Contract quests" })]
        [TestCase("nw_dragon", new[] { "Base game" })]
        [TestCase("mcdce_doctor", new[] { "Mon Cala", "Dac City" })]
        public void ADialog_IsFiledByItsResrefPrefix(string resRef, string[] expected)
        {
            ModuleFolderSeeder.PathFor(ResourceType.Dlg, new SeedableResource(resRef, null))
                .Should().Equal(expected);
        }

        /// <summary>
        /// "repbase" must not be swallowed by "rep". Both happen to land in the same folder today, so this
        /// asserts the longest-prefix rule rather than the outcome, which would pass either way.
        /// </summary>
        [Test]
        public void ADialogPrefix_MatchesTheWholeToken()
        {
            ModuleFolderSeeder.PathFor(ResourceType.Dlg, new SeedableResource("repair_droid", null))
                .Should().BeEmpty("'repair' is not the 'rep' prefix, it is a different word");
        }

        /// <summary>
        /// Most dialog resrefs are a single word ("bartender", "cardmaster") and carry no grouping
        /// information at all. Measured: 332 of 609.
        /// </summary>
        [Test]
        public void ADialogWithNoPrefix_StaysUnsorted()
        {
            ModuleFolderSeeder.PathFor(ResourceType.Dlg, new SeedableResource("bartender", null))
                .Should().BeEmpty();
        }

        [TestCase("dmfi_activate", new[] { "DMFI toolkit" })]
        [TestCase("zep_dye", new[] { "ZEP toolkit" })]
        [TestCase("nw_c2_default1", new[] { "Base game" })]
        public void AScript_IsFiledByTheToolkitItCameFrom(string resRef, string[] expected)
        {
            ModuleFolderSeeder.PathFor(ResourceType.Nss, new SeedableResource(resRef, null))
                .Should().Equal(expected);
        }

        [Test]
        public void Seeding_BuildsTheNestedFoldersAndFilesEveryResource()
        {
            var section = new CategorySection();

            var created = ModuleFolderSeeder.Seed(section, ResourceType.Area, new[]
            {
                new SeedableResource("anchor_nor", "Tatooine - Anchorhead - North Entrance"),
                new SeedableResource("anchor_sou", "Tatooine - Anchorhead - South Entry"),
                new SeedableResource("moseisley", "Tatooine - Mos Eisley"),
                new SeedableResource("ebonhawk", "[Prefab] Ebon Hawk"),
                new SeedableResource("no_access", "*No Access"),
                new SeedableResource("area_template", "area_template")
            });

            // Tatooine, Tatooine/Anchorhead, Prefabs, System. Mos Eisley adds none - it is a row inside
            // Tatooine, not a folder, because it is the last segment of its own name.
            created.Should().Be(4);

            section.Find("Tatooine", "Anchorhead")!.Members.Should().HaveCount(2);
            section.Find("Tatooine")!.Members.Should().Contain("moseisley");
            section.Find("Prefabs")!.Members.Should().Contain("ebonhawk");
            section.Find("System")!.Members.Should().Contain("no_access");

            // An unstructured name stays visible in Unsorted rather than being hidden in a folder of one.
            section.UnsortedResRefs(new[] { "area_template" }).Should().Equal(new[] { "area_template" });
        }

        /// <summary>
        /// Segments come out of a blueprint's own name, and the convention splits on " - " - so a name
        /// with a '/' inside a segment reaches the folder constructor intact and used to throw, taking the
        /// whole seed with it. These names belong to the module rather than to anyone who could be asked
        /// to correct them, so the segment is repaired the same way a sidecar's is.
        /// </summary>
        [Test]
        public void ASegmentHoldingThePathSeparator_IsRepairedRatherThanThrownAt()
        {
            AreaPath("Tatooine - Anchorhead/Docks - Bay 1")
                .Should().Equal("Tatooine", "Anchorhead-Docks");
        }

        [Test]
        public void Seeding_SurvivesANameWhoseSegmentHoldsThePathSeparator()
        {
            var section = new CategorySection();

            var act = () => ModuleFolderSeeder.Seed(section, ResourceType.Area, new[]
            {
                new SeedableResource("bay01", "Tatooine - Anchorhead/Docks - Bay 1")
            });

            act.Should().NotThrow();
            section.Find("Tatooine", "Anchorhead-Docks")!.Members.Should().Contain("bay01");
        }

        /// <summary>
        /// The seed is a starting point, never a correction. Once a builder has folders, re-seeding would
        /// silently undo whatever they arranged.
        /// </summary>
        [Test]
        public void Seeding_DoesNothingWhenFoldersAlreadyExist()
        {
            var section = new CategorySection();
            section.AddFolder("My own arrangement");

            var created = ModuleFolderSeeder.Seed(section, ResourceType.Area, new[]
            {
                new SeedableResource("anchor_nor", "Tatooine - Anchorhead")
            });

            created.Should().Be(0);
            section.Folders.Should().HaveCount(1);
            section.Folders[0].Name.Should().Be("My own arrangement");
        }
    }
}
