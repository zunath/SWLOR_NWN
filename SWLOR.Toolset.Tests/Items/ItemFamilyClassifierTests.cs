using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>Corpus-verified label -> family bucketing, checked against baseitems.2da.</summary>
    [TestFixture]
    public class ItemFamilyClassifierTests
    {
        [TestCase(16, "armor", 3, ItemFamily.Armor)]
        [TestCase(512, "lightsaber", 2, ItemFamily.Lightsaber)]
        [TestCase(0, "saberstaff", 2, ItemFamily.Lightsaber)]
        [TestCase(516, "ess2", 0, ItemFamily.Essence)]
        [TestCase(73, "creatureitem", 0, ItemFamily.CreatureItem)]
        [TestCase(72, "cslshprcweap", 0, ItemFamily.CreatureItem)]
        [TestCase(69, "cslashweapon", 0, ItemFamily.CreatureItem)]
        [TestCase(70, "cpiercweapon", 0, ItemFamily.CreatureItem)]
        [TestCase(71, "cbludgweapon", 0, ItemFamily.CreatureItem)]
        [TestCase(29, "miscmedium", 0, ItemFamily.Miscellaneous)]
        [TestCase(80, "cloak", 1, ItemFamily.Cape)]
        [TestCase(26, "boots", 2, ItemFamily.Accessory)]
        [TestCase(52, "ring", 0, ItemFamily.Accessory)]
        [TestCase(19, "amulet", 0, ItemFamily.Accessory)]
        [TestCase(21, "belt", 0, ItemFamily.Accessory)]
        [TestCase(78, "bracer", 0, ItemFamily.Accessory)]
        [TestCase(36, "gloves", 0, ItemFamily.Accessory)]
        [TestCase(525, "electroblade", 2, ItemFamily.MeleeWeapon)]
        [TestCase(537, "twinelectroblade", 2, ItemFamily.MeleeWeapon)]
        [TestCase(1, "longsword", 2, ItemFamily.MeleeWeapon)]
        [TestCase(111, "Whip", 2, ItemFamily.MeleeWeapon)]
        [TestCase(11, "pistol", 2, ItemFamily.RangedWeapon)]
        [TestCase(514, "legacy_smallarms", 2, ItemFamily.RangedWeapon)]
        [TestCase(14, "smallshield", 2, ItemFamily.Shield)]
        [TestCase(56, "largeshield", 2, ItemFamily.Shield)]
        [TestCase(17, "helmet", 1, ItemFamily.Helmet)]
        [TestCase(45, "fishingrod", 2, ItemFamily.Tool)]
        [TestCase(210, "holdable", 0, ItemFamily.Tool)]
        [TestCase(0, "unknown_future_item", 0, ItemFamily.Miscellaneous)]
        public void Classify_MatchesTheVerifiedCorpusBucketing(
            int baseItemId, string label, int modelType, ItemFamily expected)
        {
            ItemFamilyClassifier.Classify(baseItemId, label, modelType).Should().Be(expected);
        }

        [Test]
        public void Classify_IsCaseInsensitive()
        {
            ItemFamilyClassifier.Classify(16, "ARMOR", 3).Should().Be(ItemFamily.Armor);
            ItemFamilyClassifier.Classify(512, "LightSaber", 2).Should().Be(ItemFamily.Lightsaber);
            ItemFamilyClassifier.Classify(80, "CLOAK", 1).Should().Be(ItemFamily.Cape);
        }

        [Test]
        public void Classify_FromABaseItemRowMatchesTheDirectOverload()
        {
            var row = new BaseItemRow(16, "armor", 3);
            ItemFamilyClassifier.Classify(row).Should().Be(ItemFamily.Armor);
        }
    }
}
