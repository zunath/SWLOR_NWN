using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The toolset's category lists, search results and Properties header are named for what a
    /// builder is looking at ("Creatures"), never the raw blueprint extension the enum is named
    /// after ("Utc"). These lock the mapping in both forms and prove no type falls back to
    /// <c>ToString()</c>.
    /// </summary>
    [TestFixture]
    public class ResourceTypeDisplayNameTests
    {
        [TestCase(ResourceType.Area, "Areas")]
        [TestCase(ResourceType.Utc, "Creatures")]
        [TestCase(ResourceType.Uti, "Items")]
        [TestCase(ResourceType.Utp, "Placeables")]
        [TestCase(ResourceType.Utd, "Doors")]
        [TestCase(ResourceType.Utm, "Merchants")]
        [TestCase(ResourceType.Utt, "Triggers")]
        [TestCase(ResourceType.Uts, "Sound Sets")]
        [TestCase(ResourceType.Utw, "Waypoints")]
        public void DisplayName_Names_The_Collection(ResourceType type, string expected)
        {
            type.DisplayName().Should().Be(expected);
        }

        [TestCase(ResourceType.Area, "Area")]
        [TestCase(ResourceType.Utc, "Creature")]
        [TestCase(ResourceType.Uti, "Item")]
        [TestCase(ResourceType.Utp, "Placeable")]
        [TestCase(ResourceType.Utd, "Door")]
        [TestCase(ResourceType.Utm, "Merchant")]
        [TestCase(ResourceType.Utt, "Trigger")]
        [TestCase(ResourceType.Uts, "Sound Set")]
        [TestCase(ResourceType.Utw, "Waypoint")]
        public void SingularDisplayName_Names_One_Resource(ResourceType type, string expected)
        {
            type.SingularDisplayName().Should().Be(expected);
        }

        /// <summary>A new blueprint type must be given real names, not leak "Utx" into the UI.</summary>
        [Test]
        public void Every_ResourceType_Has_Friendly_Names()
        {
            foreach (var type in Enum.GetValues<ResourceType>())
            {
                type.DisplayName().Should().NotBeNullOrWhiteSpace().And.NotStartWith("Ut");
                type.SingularDisplayName().Should().NotBeNullOrWhiteSpace().And.NotStartWith("Ut");
            }
        }

        [Test]
        public void CatalogEntry_Exposes_The_Friendly_Type_Name()
        {
            var entry = new CatalogEntry(ResourceType.Utm, "veles_general", "General Store", null, "path");

            entry.ResourceTypeDisplayName.Should().Be("Merchant");
        }
    }
}
