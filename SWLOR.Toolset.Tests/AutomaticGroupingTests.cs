using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The default grouping rule, checked against real area names from this module. A builder has to be
    /// able to predict where a name lands, so the rule is pinned by example rather than described.
    /// </summary>
    [TestFixture]
    public class AutomaticGroupingTests
    {
        [TestCase("Viscara - Veles", "Viscara")]
        [TestCase("Tatooine - Anchorhead - Spaceport", "Tatooine")]
        [TestCase("Smuggler's Moon - Sewers", "Smuggler's Moon")]
        [TestCase("Building Template - Starport Style 1", "Building Template")]
        [TestCase("[Prefab] Nar Shaddaa - Rooftops", "[Prefab] Nar Shaddaa")]
        public void Groups_On_The_First_Spaced_Dash(string name, string expected)
        {
            AutomaticGrouping.GroupNameFor(name).Should().Be(expected);
        }

        /// <summary>
        /// The separator is space-dash-space, not a bare dash, or every CZ-220 area would file under "CZ".
        /// </summary>
        [Test]
        public void A_Hyphenated_Word_Is_Not_A_Separator()
        {
            AutomaticGrouping.GroupNameFor("CZ-220 - Hangar").Should().Be("CZ-220");
            AutomaticGrouping.GroupNameFor("CZ-220").Should().BeNull();
        }

        [TestCase("area_template")]
        [TestCase("*Character Rebuild")]
        [TestCase("[Prefab] City, Industrial Slum")]
        [TestCase("")]
        [TestCase(null)]
        public void Names_Without_A_Separator_Have_No_Group(string? name)
        {
            AutomaticGrouping.GroupNameFor(name).Should().BeNull(
                because: "they belong in Unsorted, which always exists");
        }

        [TestCase("Viscara - Veles", "Veles")]
        [TestCase("Tatooine - Anchorhead - Spaceport", "Anchorhead - Spaceport")]
        [TestCase("area_template", "area_template")]
        public void Leaf_Label_Drops_The_Group_Prefix(string name, string expected)
        {
            AutomaticGrouping.LeafLabelFor(name).Should().Be(expected);
        }

        [Test]
        public void A_Trailing_Separator_Keeps_The_Whole_Name_As_The_Label()
        {
            AutomaticGrouping.LeafLabelFor("Viscara - ").Should().Be("Viscara -");
        }
    }
}
