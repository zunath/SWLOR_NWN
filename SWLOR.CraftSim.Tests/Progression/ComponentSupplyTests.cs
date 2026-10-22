using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    [TestFixture]
    public class ComponentSupplyTests
    {
        private static readonly ProgressionAssumptions Assumptions = new();
        private static ComponentSupply Supply => ProgressionFixture.Supply;

        [TestCase("ref_veldite", ComponentSource.Refined)]
        [TestCase("ref_gostian", ComponentSource.Refined)]
        [TestCase("raw_veldite", ComponentSource.Harvested)]
        [TestCase("wood", ComponentSource.Harvested)]
        [TestCase("jade", ComponentSource.Harvested)]
        [TestCase("fiberp_ruined", ComponentSource.Harvested)]
        [TestCase("herb_m", ComponentSource.Harvested)]
        [TestCase("lth_ruined", ComponentSource.Other)]
        [TestCase("elec_ruined", ComponentSource.Other)]
        [TestCase("distilled_water", ComponentSource.Other)]
        public void EachComponentComesFromWhereTheLootTablesAndTheRefineryPutIt(string resref, ComponentSource expected)
        {
            Supply.Classify(resref).Should().Be(expected);
        }

        [Test]
        public void ARecipesProductIsCraftedNotGathered()
        {
            var made = SharedData.Recipes.Recipes.First(r => r.Resref != null && Supply.Classify(r.Resref) == ComponentSource.Crafted);
            Supply.Classify(made.Resref).Should().Be(ComponentSource.Crafted);
            Supply.IsModelled(made.Resref).Should().BeFalse();
        }

        [Test]
        public void AHarvestedUnitCostsTheUseOfTheNodeWithTheBestChanceOfIt()
        {
            // HARVESTING_TREE holds wood 50, fine_wood 5 and jade 5, so a use gives wood on 50 of 60 rolls.
            var node = Supply.NodeFor("wood");
            node.Should().NotBeNull();
            node.Value.Table.Should().Be("HARVESTING_TREE");
            node.Value.Share.Should().BeApproximately(50 / 60.0, 1e-12);

            var useSeconds = Assumptions.SecondsPerHarvest + Assumptions.NodeTravelSeconds / HarvestModel.ExpectedHarvestsPerNode;
            ComponentSupply.SecondsPerHarvestUse(Assumptions).Should().BeApproximately(useSeconds, 1e-12);
            Supply.SecondsPerUnit("wood", Assumptions).Should().BeApproximately(useSeconds / (50 / 60.0), 1e-9);
        }

        [TestCase("HARVESTING_TREE", GatheringMechanism.Harvest, 0)]
        [TestCase("HARVESTING_JASIOCLASE", GatheringMechanism.Harvest, 0)]
        [TestCase("SCAVENGING_BUSH_4", GatheringMechanism.Scavenge, 4)]
        [TestCase("HERBS_2", GatheringMechanism.Scavenge, 2)]
        [TestCase("VEGETABLES_PATCH_5", GatheringMechanism.Scavenge, 5)]
        [TestCase("SUPPLY_CRATE_3", GatheringMechanism.Scavenge, 3)]
        [TestCase("ASTEROID_CURRIAN_STRIP", GatheringMechanism.Asteroid, 0)]
        public void ATableIsHarvestedScavengedOrMinedByItsName(string table, GatheringMechanism mechanism, int level)
        {
            ComponentSupply.MechanismOf(table).Should().Be(mechanism);
            if (mechanism == GatheringMechanism.Scavenge)
                ComponentSupply.SiteLevelOf(table).Should().Be(level);
        }

        [Test]
        public void TheBlueprintsOfEachTableUseTheMechanismTheModelPricesItWith()
        {
            var blueprints = Directory.EnumerateFiles(Path.Combine(SharedData.Root, "Module", "utp"), "*.utp.json")
                .ToDictionary(path => Path.GetFileName(path), File.ReadAllText);
            var tables = new[]
            {
                "HARVESTING_TREE", "HARVESTING_VELDITE", "HARVESTING_ARKOXIT", "SCAVENGING_BUSH_1", "HERBS_1", "VEGETABLES_PATCH_1", "SUPPLY_CRATE_3", "SUPPLY_CRATE_5"
            };

            foreach (var table in tables)
            {
                var variable = ComponentSupply.MechanismOf(table) == GatheringMechanism.Harvest ? "\"HARVESTING_LOOT_TABLE\"" : "\"SCAVENGE_POINT_LOOT_TABLE_NAME\"";
                var script = ComponentSupply.MechanismOf(table) == GatheringMechanism.Harvest ? "\"res_used\"" : "\"scav_opened\"";
                var named = blueprints.Where(b => b.Value.Contains($"\"{table}\"", StringComparison.Ordinal)).ToList();

                named.Should().NotBeEmpty($"a placeable blueprint names {table}");
                named.Should().OnlyContain(b => b.Value.Contains(variable) && b.Value.Contains(script), $"{table} is worked with {variable} and {script}");
            }
        }

        [Test]
        public void AScavengedUnitCostsTheVisitOverTheSuccessfulSearchesAndTheTablesChance()
        {
            // HERBS_2 holds only herb_m, so every successful search gives one.
            var source = Supply.CheapestSource("herb_m", Assumptions);

            source.Table.Should().Be("HERBS_2");
            source.Mechanism.Should().Be(GatheringMechanism.Scavenge);
            source.SiteLevel.Should().Be(2);
            source.Share.Should().Be(1.0);

            var visit = Assumptions.NodeTravelSeconds + ScavengeModel.ExpectedAttemptsPerSite(2, 0) * Assumptions.SecondsPerScavengeAttempt;
            ComponentSupply.SecondsPerScavengeVisit(2, Assumptions).Should().BeApproximately(visit, 1e-12);
            Supply.SecondsPerUnit("herb_m", Assumptions).Should().BeApproximately(visit / ScavengeModel.ExpectedSuccessesPerSite(2, 0), 1e-9);
        }

        [Test]
        public void AScavengeSitesMaximumQuantityRaisesTheYieldOfASuccessfulSearch()
        {
            var one = new GatheringSource("T", GatheringMechanism.Scavenge, 0.5, 1, 1);
            var three = one with { MaxQuantity = 3 };

            // A roll gives 1 to the maximum, so a maximum of 3 gives 2 on average.
            (ComponentSupply.SecondsPerUnit(one, Assumptions) / ComponentSupply.SecondsPerUnit(three, Assumptions)).Should().BeApproximately(2.0, 1e-9);
        }

        [Test]
        public void TheCheapestSourceBeatsTheMostLikelyOneWhenItIsWorkedFaster()
        {
            foreach (var resref in new[] { "jade", "citrine", "emerald", "elec_good", "lth_good", "wood", "raw_veldite" })
            {
                var cheapest = Supply.CheapestSource(resref, Assumptions);
                Supply.SourcesOf(resref).Min(s => ComponentSupply.SecondsPerUnit(s, Assumptions))
                    .Should().BeApproximately(ComponentSupply.SecondsPerUnit(cheapest, Assumptions), 1e-12, resref);
            }
        }

        private static RecipeEntry Recipe(params (string Resref, int Quantity)[] components) =>
            new(default, SkillType.Smithery, 1, "test_item", default, components.ToDictionary(c => c.Resref, c => c.Quantity));

        [Test]
        public void ItemsFromOneTableAreGatheredTogetherNotOneVeinRunEach()
        {
            // HARVESTING_VELDITE holds raw_veldite 50 and jade 5. A recipe needing refined veldite and jade farms the rare jade, and the
            // veldite comes with it.
            var recipe = Recipe(("ref_veldite", 2), ("jade", 1));
            var priced = Supply.PriceRecipe(recipe, Assumptions);

            var jade = Supply.CheapestSource("jade", Assumptions);
            jade.Table.Should().Be("HARVESTING_VELDITE");
            var uses = 1 / ComponentSupply.UnitsPerVisit(jade, Assumptions);
            var refining = 2 * Assumptions.RefineSecondsPerBatch / Assumptions.RefineBatchUnits;
            priced.Seconds.Should().BeApproximately(uses * ComponentSupply.SecondsPerHarvestUse(Assumptions) + refining, 1e-9);

            var apart = 2 * Supply.SecondsPerUnit("ref_veldite", Assumptions) + Supply.SecondsPerUnit("jade", Assumptions);
            priced.Seconds.Should().BeLessThan(apart, "pricing each alone would charge a second vein run for the veldite");
            priced.SecondsByComponent.Values.Sum().Should().BeApproximately(priced.Seconds, 1e-9);
        }

        [Test]
        public void ItemsFromDifferentTablesAddUp()
        {
            var recipe = Recipe(("wood", 3), ("herb_m", 2));
            var priced = Supply.PriceRecipe(recipe, Assumptions);

            priced.Seconds.Should().BeApproximately(3 * Supply.SecondsPerUnit("wood", Assumptions) + 2 * Supply.SecondsPerUnit("herb_m", Assumptions), 1e-9);
        }

        [Test]
        public void AUnitNoTableSuppliesIsCountedAndPricedAtTheFlatEstimateOnly()
        {
            var recipe = Recipe(("wood", 1), ("lth_ruined", 3));

            var free = Supply.PriceRecipe(recipe, Assumptions);
            var flat = Supply.PriceRecipe(recipe, Assumptions with { UnmodelledUnitSeconds = 20 });

            free.UnmodelledUnits.Should().Be(3);
            free.ModelledUnits.Should().Be(1);
            (flat.Seconds - free.Seconds).Should().BeApproximately(60, 1e-9);
        }

        [Test]
        public void AHigherGatheringTierGivesAScavengerHardLookAtLowLevelSites()
        {
            // The server derives Hard Look from the player's Gathering rank, so a tier-5 gatherer searches a level-1 bush twice far more often.
            ComponentSupply.HardLookAt(1, Assumptions).Should().Be(1);
            ComponentSupply.HardLookAt(1, Assumptions with { GatheringTier = 5 }).Should().Be(5);
            ComponentSupply.HardLookAt(4, Assumptions with { GatheringTier = 2 }).Should().Be(4, "a site needs at least its own tier");

            var bush = Supply.CheapestSource("fiberp_ruined", Assumptions);
            bush.Mechanism.Should().Be(GatheringMechanism.Scavenge);
            ComponentSupply.SecondsPerUnit(bush, Assumptions with { GatheringTier = 5 })
                .Should().BeLessThan(ComponentSupply.SecondsPerUnit(bush, Assumptions));
        }

        [Test]
        public void ARefinedUnitAddsItsShareOfARefiningOperation()
        {
            var raw = Supply.SecondsPerUnit("raw_veldite", Assumptions);
            var refined = Supply.SecondsPerUnit("ref_veldite", Assumptions);

            (refined - raw).Should().BeApproximately(Assumptions.RefineSecondsPerBatch / Assumptions.RefineBatchUnits, 1e-12);
            Supply.OreFor("ref_veldite").Ore.Should().Be("raw_veldite");
        }

        [Test]
        public void AMightBonusMakesTheSecondItemFreeAndShortensTheUnit()
        {
            var plain = Supply.SecondsPerUnit("wood", Assumptions);
            var strong = Supply.SecondsPerUnit("wood", Assumptions with { MightModifier = 2 });

            (plain / strong).Should().BeApproximately(1.5, 1e-9, "a Might modifier of +2 is a 50% chance of a second item");
        }

        [Test]
        public void AUnitNoNodeSuppliesCostsOnlyWhatTheAssumptionSays()
        {
            Supply.SecondsPerUnit("lth_ruined", Assumptions).Should().Be(0);
            Supply.SecondsPerUnit("lth_ruined", Assumptions with { UnmodelledUnitSeconds = 20 }).Should().Be(20);
            Supply.Family("lth_ruined", Assumptions).Should().Be(ComponentSupply.NoNodeFamily);
        }

        [TestCase("wood", "trees")]
        [TestCase("raw_veldite", "ore veins")]
        [TestCase("ref_scordspar", "ore veins")]
        [TestCase("fiberp_ruined", "bushes")]
        [TestCase("herb_m", "herbs")]
        [TestCase("tomato", "vegetable patches")]
        public void TheReportNamesTheKindOfNode(string resref, string family)
        {
            Supply.Family(resref, Assumptions).Should().Be(family);
        }

        [Test]
        public void EveryRefinedOreOfTheRealRecipesIsPriced()
        {
            var refined = SharedData.Recipes.Recipes.SelectMany(r => r.Components.Keys).Distinct()
                .Where(c => Supply.Classify(c) == ComponentSource.Refined)
                .ToList();

            refined.Should().NotBeEmpty();
            refined.Select(c => Supply.SecondsPerUnit(c, Assumptions)).Should().OnlyContain(seconds => seconds > 0 && !double.IsInfinity(seconds));
        }
    }
}
