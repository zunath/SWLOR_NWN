using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.CraftSim.Tests.Data
{
    [TestFixture]
    public class GearTests
    {
        private static JObject Item(int baseItem, params (int Property, int Subtype, int Value)[] properties) => new()
        {
            ["BaseItem"] = new JObject { ["value"] = baseItem },
            ["LocalizedName"] = new JObject { ["value"] = new JObject { ["0"] = "Test Item" } },
            ["PropertiesList"] = new JObject
            {
                ["value"] = new JArray(properties.Select(p => new JObject
                {
                    ["PropertyName"] = new JObject { ["value"] = p.Property },
                    ["Subtype"] = new JObject { ["value"] = p.Subtype },
                    ["CostValue"] = new JObject { ["value"] = p.Value }
                }))
            }
        };

        [Test]
        public void Read_MapsSubtypesToSkillsAndSumsEachStat()
        {
            var item = Item((int)BaseItem.Armor, (88, 1, 2), (115, 1, 3), (89, 1, 4), (89, 2, 1), (115, 4, 6));

            var gear = GearItemReader.Read("test_armor", item).ToList();

            gear.Should().HaveCount(3);
            var smithery = gear.Single(g => g.Skill == SkillType.Smithery);
            (smithery.Control, smithery.Craftsmanship, smithery.CpBonus).Should().Be((2, 4, 3));
            var engineering = gear.Single(g => g.Skill == SkillType.Engineering);
            (engineering.Control, engineering.Craftsmanship, engineering.CpBonus).Should().Be((0, 1, 0));
            gear.Single(g => g.Skill == SkillType.Agriculture).CpBonus.Should().Be(6);
            gear.Should().OnlyContain(g => g.BaseItem == BaseItem.Armor && g.Resref == "test_armor");
        }

        [Test]
        public void Read_IgnoresOtherPropertiesZeroValuesAndUnknownSubtypes()
        {
            var item = Item((int)BaseItem.Belt, (12, 1, 5), (89, 1, 0), (88, 0, 4), (88, 9, 4));

            GearItemReader.Read("belt", item).Should().BeEmpty();
        }

        [Test]
        public void Read_AddsAnItemsRepeatedStatOfOneSkill()
        {
            var item = Item((int)BaseItem.Ring, (88, 3, 1), (88, 3, 2));

            GearItemReader.Read("ring", item).Single().Control.Should().Be(3);
        }

        [Test]
        public void ReadAll_ReadsEveryFileInAFolderAndRejectsOneThatIsNotJson()
        {
            var folder = Directory.CreateTempSubdirectory("craftsim-uti-").FullName;
            try
            {
                File.WriteAllText(Path.Combine(folder, "sword.uti.json"), Item((int)BaseItem.Armor, (89, 2, 3)).ToString());
                File.WriteAllText(Path.Combine(folder, "plain.uti.json"), "{ \"BaseItem\": { \"value\": 1 } }");

                var items = GearItemReader.ReadAll(folder);

                items.Should().ContainSingle().Which.Should().Match<GearItem>(i => i.Resref == "sword" && i.Skill == SkillType.Engineering && i.Craftsmanship == 3);

                File.WriteAllText(Path.Combine(folder, "broken.uti.json"), "{ \"PropertyName\": ");
                var act = () => GearItemReader.ReadAll(folder);

                act.Should().Throw<InvalidDataException>().WithMessage("*broken.uti.json*");
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ReadAll_NamesAMissingFolder()
        {
            var act = () => GearItemReader.ReadAll(Path.Combine(Path.GetTempPath(), "craftsim-missing-" + Guid.NewGuid().ToString("N")));

            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Test]
        public void ReadAll_FindsTheModulesCraftingGear()
        {
            SharedData.GearItems.Should().NotBeEmpty();
            var advent = SharedData.GearItems.Single(i => i.Resref == "advent_belt");
            (advent.Skill, advent.BaseItem, advent.Craftsmanship).Should().Be((SkillType.Smithery, BaseItem.Belt, 1));
            SharedData.GearItems.Should().NotContain(i => i.Resref == "ass_rifle", "its stats are zero");
            SharedData.GearItems.Select(i => i.Skill).Distinct().Should().BeEquivalentTo(CraftingGearCatalog.GearSkills);
        }

        [TestCase(SkillType.Smithery)]
        [TestCase(SkillType.Engineering)]
        [TestCase(SkillType.Fabrication)]
        [TestCase(SkillType.Agriculture)]
        public void EveryCraftingSkillHasFiveCompleteTiers(SkillType skill)
        {
            SharedData.Gear.Notes.Should().BeEmpty();
            SharedData.Gear.TierCount(skill).Should().Be(5);
            for (var tier = 1; tier <= 5; tier++)
            {
                SharedData.Gear.Tier(skill, tier).Items.Should().HaveCount(CraftingGearCatalog.SlotCounts.Values.Sum(), $"tier {tier}");
            }
        }

        [TestCase(SkillType.Smithery, 3, 13, 12, 23)]
        [TestCase(SkillType.Smithery, 5, 29, 30, 37)]
        [TestCase(SkillType.Engineering, 3, 13, 12, 20)]
        [TestCase(SkillType.Engineering, 5, 29, 30, 37)]
        [TestCase(SkillType.Fabrication, 3, 13, 12, 26)]
        [TestCase(SkillType.Fabrication, 5, 29, 30, 37)]
        [TestCase(SkillType.Agriculture, 3, 13, 12, 20)]
        [TestCase(SkillType.Agriculture, 5, 29, 30, 37)]
        public void TheTierSetsKeepTodaysCraftingGearStats(SkillType skill, int tier, int control, int craftsmanship, int cpBonus)
        {
            var set = SharedData.Gear.Tier(skill, tier);

            (set.Control, set.Craftsmanship, set.CpBonus).Should().Be((control, craftsmanship, cpBonus));
        }

        [Test]
        public void GearLevels_AreNoGearThenMidThenBestAndRiseInEveryStat()
        {
            foreach (var skill in CraftingGearCatalog.GearSkills)
            {
                var levels = SharedData.Gear.Levels(skill);

                levels.Select(l => l.Name).Should().Equal(CraftingGearCatalog.NoGearName, CraftingGearCatalog.MidGearName, CraftingGearCatalog.BestGearName);
                levels[0].Items.Should().BeEmpty();
                levels[1].Tier.Should().Be(3);
                levels[2].Tier.Should().Be(5);
                levels[1].Control.Should().BeGreaterThan(levels[0].Control);
                levels[2].Control.Should().BeGreaterThan(levels[1].Control);
                levels[2].Craftsmanship.Should().BeGreaterThan(levels[1].Craftsmanship);
                levels[2].CpBonus.Should().BeGreaterThan(levels[1].CpBonus);
            }
        }

        [Test]
        public void TheGearIsMadeBySmitheryRecipesNotEngineeringsDroidCopies()
        {
            var made = SharedData.Recipes.ForSkill(SkillType.Smithery).Select(r => r.Resref.ToLowerInvariant()).ToHashSet();
            var set = SharedData.Gear.Tier(SkillType.Engineering, 5);

            set.Items.Should().OnlyContain(i => made.Contains(i.Resref));
            set.Items.Should().NotContain(i => i.Resref.StartsWith("de") && char.IsDigit(i.Resref.Last()), "droid equipment is not the player's gear");
        }

        [Test]
        public void ASetWearsTwoRings()
        {
            SharedData.Gear.Tier(SkillType.Smithery, 5).Items.Count(i => i.BaseItem == BaseItem.Ring).Should().Be(2);
        }

        [Test]
        public void ACatalogWithoutGear_OffersOnlyNoGear()
        {
            var catalog = CraftingGearCatalog.Build(Array.Empty<GearItem>(), SharedData.Recipes);

            catalog.Levels(SkillType.Smithery).Should().ContainSingle().Which.Name.Should().Be(CraftingGearCatalog.NoGearName);
        }

        [Test]
        public void ASlotMissingATier_IsReported()
        {
            var recipes = RecipeCatalog.From(new[]
            {
                new RecipeEntry(default, SkillType.Smithery, 10, "tunic_1", default, new Dictionary<string, int>()),
                new RecipeEntry(default, SkillType.Smithery, 20, "tunic_2", default, new Dictionary<string, int>()),
                new RecipeEntry(default, SkillType.Smithery, 10, "helm_1", default, new Dictionary<string, int>())
            });
            var items = new[]
            {
                new GearItem("tunic_1", "T1", BaseItem.Armor, SkillType.Smithery, 1, 1, 1),
                new GearItem("tunic_2", "T2", BaseItem.Armor, SkillType.Smithery, 2, 2, 2),
                new GearItem("helm_1", "H1", BaseItem.Helmet, SkillType.Smithery, 1, 1, 1)
            };

            var catalog = CraftingGearCatalog.Build(items, recipes);

            catalog.TierCount(SkillType.Smithery).Should().Be(2);
            catalog.Notes.Should().Contain(n => n.Contains("Helmet") && n.Contains("1 tiers"));
        }

        [Test]
        public void GearLevel_SumsItsItemsAndCanBeFixedByHand()
        {
            var fixedGear = GearLevel.Fixed("test", SkillType.Smithery, 3, 4, 5);

            (fixedGear.Control, fixedGear.Craftsmanship, fixedGear.CpBonus).Should().Be((3, 4, 5));
            GearLevel.None(SkillType.Smithery).Control.Should().Be(0);
        }
    }
}
