using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Section queries. These encode the two rules that keep the sidecar additive: what the module has
    /// but nobody filed shows up as Unsorted, and what the sidecar names but the module no longer has
    /// simply disappears.
    /// </summary>
    [TestFixture]
    public class CategorySectionTests
    {
        private static CategorySection BuildSection()
        {
            var section = new CategorySection();
            var interiors = section.AddFolder("Interiors");
            var consoles = interiors.AddChild("Consoles & Terminals");
            consoles.AddMember("_mdrn_pl_conso08");
            consoles.AddChild("Droid Repair").AddMember("_mdrn_pl_conso09");
            interiors.AddMember("_mdrn_pl_vs8");
            section.AddFolder("Cargo").AddMember("aswtor_183");
            return section;
        }

        [Test]
        public void Unsorted_Is_Whatever_Exists_But_Was_Never_Filed()
        {
            var section = BuildSection();

            var unsorted = section.UnsortedResRefs(new[]
            {
                "_mdrn_pl_conso08", "aswtor_183", "swlor_0005", "_mdrn_pl_flowr03"
            });

            unsorted.Should().BeEquivalentTo(new[] { "swlor_0005", "_mdrn_pl_flowr03" });
        }

        [Test]
        public void Resrefs_The_Module_No_Longer_Has_Are_Ignored_Not_Reported()
        {
            var section = BuildSection();
            section.Find("Cargo")!.AddMember("deleted_blueprint");

            var unsorted = section.UnsortedResRefs(new[] { "aswtor_183" });

            unsorted.Should().BeEmpty();
            section.CountIn(section.Find("Cargo")!, new HashSet<string> { "aswtor_183" })
                .Should().Be(1, because: "the deleted member must not inflate the folder's count");
        }

        [Test]
        public void Counts_Include_Descendants()
        {
            var section = BuildSection();
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "_mdrn_pl_conso08", "_mdrn_pl_conso09", "_mdrn_pl_vs8"
            };

            section.CountIn(section.Find("Interiors")!, existing).Should().Be(3);
            section.CountIn(section.Find("Interiors", "Consoles & Terminals")!, existing).Should().Be(2);
        }

        [Test]
        public void A_Resref_May_Sit_In_Several_Folders()
        {
            var section = BuildSection();
            section.Find("Cargo")!.AddMember("_mdrn_pl_conso08");

            var folders = section.FoldersContaining("_mdrn_pl_conso08").Select(f => f.Name);

            folders.Should().BeEquivalentTo(new[] { "Consoles & Terminals", "Cargo" });
        }

        [Test]
        public void Members_Are_Deduplicated_Case_Insensitively()
        {
            var folder = new CategoryFolder("Cargo");

            folder.AddMember("aswtor_183").Should().BeTrue();
            folder.AddMember("ASWTOR_183").Should().BeFalse();

            folder.Members.Should().ContainSingle();
        }

        [Test]
        public void PathTo_Returns_The_Segments_That_Reach_A_Folder()
        {
            var section = BuildSection();
            var droidRepair = section.Find("Interiors", "Consoles & Terminals", "Droid Repair")!;

            section.PathTo(droidRepair).Should()
                .Equal("Interiors", "Consoles & Terminals", "Droid Repair");
        }

        [Test]
        public void PathTo_Is_Empty_For_A_Folder_From_Another_Section()
        {
            var section = BuildSection();

            section.PathTo(new CategoryFolder("Elsewhere")).Should().BeEmpty();
        }

        [Test]
        public void RemoveFolder_Reaches_Nested_Folders()
        {
            var section = BuildSection();
            var consoles = section.Find("Interiors", "Consoles & Terminals")!;

            section.RemoveFolder(consoles).Should().BeTrue();

            section.Find("Interiors", "Consoles & Terminals").Should().BeNull();
            section.Find("Interiors").Should().NotBeNull();
        }

        [Test]
        public void A_Folder_Cannot_Be_Nameless()
        {
            var act = () => new CategoryFolder("   ");

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void A_Folder_Name_Cannot_Contain_The_Pin_Path_Separator()
        {
            var act = () => new CategoryFolder("Interiors/Consoles");

            act.Should().Throw<ArgumentException>()
                .WithMessage("*cannot contain*/*");
        }
    }
}
