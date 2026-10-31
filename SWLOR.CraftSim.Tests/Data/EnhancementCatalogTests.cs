using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.CraftSim.Tests.Data
{
    [TestFixture]
    public class EnhancementItemReaderTests
    {
        private static JObject Item(string name, params (ItemPropertyType Type, int Cost)[] properties) => new()
        {
            ["LocalizedName"] = new JObject { ["value"] = new JObject { ["0"] = name } },
            ["PropertiesList"] = new JObject
            {
                ["value"] = new JArray(properties.Select(p => new JObject
                {
                    ["PropertyName"] = new JObject { ["value"] = (int)p.Type },
                    ["CostValue"] = new JObject { ["value"] = p.Cost }
                }))
            }
        };

        [Test]
        public void AnEnhancementHasItsKindLevelAndProgressPenaltyFromItsProperties()
        {
            var json = Item("Armor Enhancement - Accuracy III",
                (ItemPropertyType.ArmorEnhancement, 3), (ItemPropertyType.EnhancementLevel, 25), (ItemPropertyType.ProgressPenalty, 40));

            var item = EnhancementItemReader.Read("aen_acc3", json, out var reason);

            reason.Should().BeNull();
            item.Resref.Should().Be("aen_acc3");
            item.Name.Should().Be("Armor Enhancement - Accuracy III");
            item.Type.Should().Be(RecipeEnhancementType.Armor);
            item.Level.Should().Be(25);
            item.ProgressPenalty.Should().Be(40);
            item.Tier.Should().Be(3);
        }

        [Test]
        public void SeveralProgressPenaltiesAddUpAsTheOldWindowAddsThem()
        {
            var json = Item("Memory Augmentation V",
                (ItemPropertyType.DroidEnhancement, 1), (ItemPropertyType.ProgressPenalty, 70), (ItemPropertyType.EnhancementLevel, 45), (ItemPropertyType.ProgressPenalty, 50));

            EnhancementItemReader.Read("de_x", json, out _).ProgressPenalty.Should().Be(120);
        }

        [TestCase(ItemPropertyType.ArmorEnhancement, RecipeEnhancementType.Armor)]
        [TestCase(ItemPropertyType.WeaponEnhancement, RecipeEnhancementType.Weapon)]
        [TestCase(ItemPropertyType.StructureEnhancement, RecipeEnhancementType.Structure)]
        [TestCase(ItemPropertyType.FoodEnhancement, RecipeEnhancementType.Food)]
        [TestCase(ItemPropertyType.StarshipEnhancement, RecipeEnhancementType.Starship)]
        [TestCase(ItemPropertyType.ModuleEnhancement, RecipeEnhancementType.Module)]
        [TestCase(ItemPropertyType.DroidEnhancement, RecipeEnhancementType.Droid)]
        public void EachEnhancementPropertyNamesTheKindOfRecipeThatAcceptsIt(ItemPropertyType property, RecipeEnhancementType kind)
        {
            var json = Item("x", (property, 1), (ItemPropertyType.EnhancementLevel, 5), (ItemPropertyType.ProgressPenalty, 10));

            EnhancementItemReader.Read("x", json, out _).Type.Should().Be(kind);
            EnhancementItemReader.KindByProperty.Should().HaveCount(7);
        }

        [Test]
        public void AnItemWithoutAProgressPenaltyIsNotAnEnhancementAndSaysNothing()
        {
            var json = Item("Plain", (ItemPropertyType.ArmorEnhancement, 1), (ItemPropertyType.EnhancementLevel, 5));

            EnhancementItemReader.Read("plain", json, out var reason).Should().BeNull();
            reason.Should().BeNull();
        }

        [Test]
        public void ItemsThatAreNotOneEnhancementAreLeftOutWithTheReason()
        {
            EnhancementItemReader.Read("a", Item("a", (ItemPropertyType.ProgressPenalty, 10)), out var noKind).Should().BeNull();
            noKind.Should().Contain("no enhancement property");

            EnhancementItemReader.Read("b", Item("b", (ItemPropertyType.ProgressPenalty, 10), (ItemPropertyType.ArmorEnhancement, 1)), out var noLevel).Should().BeNull();
            noLevel.Should().Contain("no enhancement level");

            var two = Item("c", (ItemPropertyType.ProgressPenalty, 10), (ItemPropertyType.EnhancementLevel, 5),
                (ItemPropertyType.ArmorEnhancement, 1), (ItemPropertyType.ArmorEnhancement, 2));
            EnhancementItemReader.Read("c", two, out var several).Should().BeNull();
            several.Should().Contain("2 enhancement properties");
        }

        [Test]
        public void TheModulesRealEnhancementsAreReadAsTheOldWindowReadsThem()
        {
            var items = SharedData.Enhancements.Items.ToDictionary(i => i.Resref);

            items.Count.Should().BeGreaterThan(700, "hundreds of armor, weapon, food, starship, droid and structure enhancements exist");
            items["aen_acc1"].Should().Match<EnhancementItem>(i => i.Type == RecipeEnhancementType.Armor && i.Level == 5 && i.ProgressPenalty == 20);
            items["aen_acc5"].Should().Match<EnhancementItem>(i => i.Type == RecipeEnhancementType.Armor && i.Level == 45 && i.ProgressPenalty == 60);
            items["de_memaug5"].ProgressPenalty.Should().Be(120, "this droid enhancement carries two penalties");
            SharedData.Enhancements.Kinds.Should().BeEquivalentTo(new[]
            {
                RecipeEnhancementType.Armor, RecipeEnhancementType.Weapon, RecipeEnhancementType.Structure, RecipeEnhancementType.Food,
                RecipeEnhancementType.Starship, RecipeEnhancementType.Module, RecipeEnhancementType.Droid
            });
            SharedData.Enhancements.Skipped.Should().Contain(line => line.StartsWith("ssscan1:"), "a scanning array has a penalty but is not an enhancement");
            SharedData.Enhancements.Items.Should().OnlyContain(i => i.ProgressPenalty > 0 && i.Level >= 0);
        }

        [Test]
        public void EveryRecipeThatCanHoldAnEnhancementHasARepresentativeToUse()
        {
            var enhanceable = SharedData.Recipes.Recipes.Where(r => r.EnhancementSlots > 0).ToList();

            enhanceable.Should().NotBeEmpty();
            enhanceable.Should().OnlyContain(r => r.EnhancementType != RecipeEnhancementType.None, "a recipe with a slot names its kind");
            var without = enhanceable.Where(r => SharedData.Enhancements.ForRecipe(r.EnhancementType, r.Level) == null).Select(r => $"{r.Resref} (level {r.Level}, {r.EnhancementType})");
            without.Should().BeEmpty("a recipe the gates play needs an enhancement item it accepts");
        }
    }

    [TestFixture]
    public class EnhancementCatalogTests
    {
        private static EnhancementItem Armor(string resref, int level, int penalty) =>
            new(resref, resref, RecipeEnhancementType.Armor, level, penalty);

        private static EnhancementCatalog Catalog() => EnhancementCatalog.Build(new[]
        {
            Armor("a5_10", 5, 10), Armor("a5_30", 5, 30), Armor("a5_20", 5, 20),
            Armor("a15_x", 15, 40), Armor("a15_y", 15, 40), Armor("a15_z", 15, 60), Armor("a15_w", 15, 50),
            Armor("a45", 45, 70),
            new EnhancementItem("w45", "w45", RecipeEnhancementType.Weapon, 45, 90)
        });

        [Test]
        public void TheRepresentativeIsTheLowerMedianPenaltyWithTiesByResref()
        {
            var catalog = Catalog();

            catalog.Representative(RecipeEnhancementType.Armor, 5).Resref.Should().Be("a5_20", "the middle of three");
            catalog.Representative(RecipeEnhancementType.Armor, 15).Resref.Should().Be("a15_y", "four items 40, 40, 50, 60: the lower median is the second, and a15_x sorts before a15_y");
            catalog.Representative(RecipeEnhancementType.Armor, 45).Resref.Should().Be("a45");
            catalog.Representative(RecipeEnhancementType.Armor, 25).Should().BeNull();
            catalog.Representative(RecipeEnhancementType.Food, 5).Should().BeNull();
        }

        [TestCase(0, 5)]
        [TestCase(10, 15)]
        [TestCase(9, 5)]
        [TestCase(40, 45)]
        [TestCase(39, 15)]
        [TestCase(53, 45)]
        public void ARecipeUsesTheBestLevelItAccepts(int recipeLevel, int expectedLevel)
        {
            Catalog().TopLevelFor(RecipeEnhancementType.Armor, recipeLevel).Should().Be(expectedLevel);
        }

        [Test]
        public void AKindWithNoAcceptableItemGivesNoRepresentative()
        {
            var catalog = EnhancementCatalog.Build(new[] { Armor("high", 45, 70) });

            catalog.TopLevelFor(RecipeEnhancementType.Armor, 10).Should().BeNull("level 45 is more than 5 above a level 10 recipe");
            catalog.ForRecipe(RecipeEnhancementType.Armor, 10).Should().BeNull();
            catalog.ForRecipe(RecipeEnhancementType.None, 50).Should().BeNull();
            catalog.ForRecipe(RecipeEnhancementType.Armor, 40).Resref.Should().Be("high");
        }

        [Test]
        public void ARecipeIsOfferedItsOwnKindAtItsBestLevel()
        {
            var catalog = Catalog();

            catalog.ForRecipe(RecipeEnhancementType.Armor, 41).Resref.Should().Be("a45");
            catalog.ForRecipe(RecipeEnhancementType.Weapon, 41).Resref.Should().Be("w45");
            catalog.ForRecipe(RecipeEnhancementType.Armor, 12).Resref.Should().Be("a15_y");
        }

        [Test]
        public void TheSummaryGivesTheSpreadOfPenaltiesAtAKindAndLevel()
        {
            var catalog = Catalog();

            catalog.Summarize(RecipeEnhancementType.Armor, 15).Should().Be(new EnhancementSummary(4, 40, 45, 60));
            catalog.Summarize(RecipeEnhancementType.Armor, 5).Should().Be(new EnhancementSummary(3, 10, 20, 30));
            catalog.Summarize(RecipeEnhancementType.Armor, 25).Should().Be(new EnhancementSummary(0, 0, 0, 0));
        }

        [Test]
        public void LevelsAndKindsAreListedInOrder()
        {
            var catalog = Catalog();

            catalog.Levels(RecipeEnhancementType.Armor).Should().Equal(5, 15, 45);
            catalog.Kinds.Should().Equal(RecipeEnhancementType.Weapon, RecipeEnhancementType.Armor);
            catalog.ItemsOf(RecipeEnhancementType.Armor, 5).Select(i => i.Resref).Should().Equal("a5_10", "a5_20", "a5_30");
        }

        [TestCase(1, 1, "I")]
        [TestCase(5, 1, "I")]
        [TestCase(15, 2, "II")]
        [TestCase(25, 3, "III")]
        [TestCase(35, 4, "IV")]
        [TestCase(45, 5, "V")]
        [TestCase(50, 6, "VI")]
        public void ATierIsNamedByItsEnhancementLevel(int level, int tier, string name)
        {
            EnhancementItem.TierOf(level).Should().Be(tier);
            EnhancementItem.TierName(tier).Should().Be(name);
        }

        [Test]
        public void TheConfigurationTotalsItsPenaltyAndRejectsNegatives()
        {
            new CraftEnhancementConfiguration(3, 60).TotalProgressPenalty.Should().Be(180);
            CraftEnhancementConfiguration.None.IsNone.Should().BeTrue();
            CraftEnhancementConfiguration.None.TotalProgressPenalty.Should().Be(0);
            new CraftEnhancementConfiguration(2, 30).Should().Be(new CraftEnhancementConfiguration(2, 30));
            ((Action)(() => new CraftEnhancementConfiguration(-1, 0))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new CraftEnhancementConfiguration(1, -1))).Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
