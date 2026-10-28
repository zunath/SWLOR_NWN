using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The station-only buildings (plan section 25): the Shipyard, the Asteroid Drill and the Jump Beacon, their recipes,
/// and Station Fuel.
/// </summary>
public class StationBuildingTests
{
    private static readonly StructureType[] Buildings = { StructureType.Shipyard, StructureType.AsteroidDrill, StructureType.JumpBeacon };

    // The buildings and their recipes

    [Test]
    public void EachBuildingIsAStationOnlyConsoleAtItsReservedSlot()
    {
        StationTuning.Buildings.Select(x => x.Structure).Should().Equal(Buildings);
        foreach (var building in StationTuning.Buildings)
        {
            StationBuildingRules.IsStationBuilding(building.Structure).Should().BeTrue();
            StationDeckRules.GetSlot(building.Structure).Should().Be(building.Slot);
            StationTuning.DeckSlots[building.Slot].Building.Should().Be(building.Structure);

            var attribute = typeof(StructureType).GetField(building.Structure.ToString())!
                .GetCustomAttributes(typeof(StructureAttribute), false).Cast<StructureAttribute>().Single();
            attribute.Name.Should().Be(building.Name);
            attribute.Resref.Should().Be(building.ConsoleResref);
            attribute.RestrictedPropertyTypes.Should().Be(PropertyType.City);
            attribute.LayoutType.Should().Be(PropertyLayoutType.Invalid);
            attribute.CanBeRetrieved.Should().BeTrue();

            File.Exists(Path.Combine(RepositoryRoot, "Module", "utp", $"{building.ConsoleResref}.utp.json")).Should().BeTrue(building.ConsoleResref);
            File.Exists(Path.Combine(RepositoryRoot, "Module", "utp", $"{building.ModelResref}.utp.json")).Should().BeTrue(building.ModelResref);
        }

        StationBuildingRules.IsStationBuilding(StructureType.Starport).Should().BeFalse();
        StationTuning.DeckSlots[StationDeckSlotType.StationFuel].Building.Should().Be(StructureType.Invalid);
    }

    [Test]
    public void BuildingsStandOnlyOnAStationDeckAndNoBeaconInWildSpace()
    {
        StationBuildingRules.GetPlacementError(StructureType.Shipyard, isStation: false, inWildSpace: false).Should().Contain("player station");
        StationBuildingRules.GetPlacementError(StructureType.Shipyard, isStation: true, inWildSpace: true).Should().BeEmpty();
        StationBuildingRules.GetPlacementError(StructureType.AsteroidDrill, isStation: true, inWildSpace: true).Should().BeEmpty();
        StationBuildingRules.GetPlacementError(StructureType.JumpBeacon, isStation: true, inWildSpace: true).Should().Contain("Wild Space");
        StationBuildingRules.GetPlacementError(StructureType.JumpBeacon, isStation: true, inWildSpace: false).Should().BeEmpty();
        StationBuildingRules.GetPlacementError(StructureType.SmallHouseStyle1, isStation: false, inWildSpace: false).Should().BeEmpty();
    }

    [Test]
    public void EachBuildingIsAFabricationStructureRecipeAfterTheStationCore()
    {
        var recipes = new StructureRecipes().BuildRecipes();
        foreach (var building in StationTuning.Buildings)
        {
            var recipe = recipes[building.Recipe];
            recipe.Skill.Should().Be(SkillType.Fabrication);
            recipe.Category.Should().Be(RecipeCategoryType.Structure);
            recipe.Resref.Should().Be(building.ItemResref);
            recipe.Level.Should().Be(building.RecipeLevel).And.BeInRange(41, 50);
            recipe.Components.Should().BeEquivalentTo(building.Components.ToDictionary(x => x.Resref, x => x.Quantity));
            recipe.Components.Keys.Should().Contain(new[] { "r_const_parts", "r_pow_supp_unit" });

            building.ItemResref.Should().Be($"structure_{(int)building.Structure}");
            using var item = ReadModule("uti", building.ItemResref);
            Value(item.RootElement, "LocalizedName").GetProperty("0").GetString().Should().Be(building.Name);
            Value(item.RootElement, "TemplateResRef").GetString().Should().Be(building.ItemResref);
            Value(item.RootElement, "Tag").GetString().Should().Be(building.ItemResref);
            File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "itp", "itempalcus.itp.json")).Should().Contain($"\"{building.ItemResref}\"");
        }

        // The Fabrication range, straight after the Station Core and far from the droid parts (7700+).
        ((int)RecipeType.Shipyard).Should().Be((int)RecipeType.StationCore + 1);
        ((int)RecipeType.AsteroidDrill).Should().Be((int)RecipeType.Shipyard + 1);
        ((int)RecipeType.JumpBeacon).Should().Be((int)RecipeType.AsteroidDrill + 1);
        ((int)RecipeType.JumpBeacon).Should().BeLessThan((int)RecipeType.MynockMeatBall).And.BeLessThan(7700);
        ((int)StructureType.Shipyard).Should().Be((int)StructureType.StationCore + 1);
        ((int)StructureType.JumpBeacon).Should().Be((int)StructureType.StationCore + 3);
    }

    // The Shipyard

    [Test]
    public void OnlyCapitalDeedsNeedAShipyard()
    {
        var recipes = new RevampStarshipRecipes().BuildRecipes();
        var hulls = RevampShipCatalog.Hulls.ToList();
        hulls.Should().Contain(x => x.Size == ShipSize.Capital);
        foreach (var hull in hulls)
        {
            var gated = recipes[hull.RecipeType].Requirements.OfType<RecipeShipyardRequirement>().Any();
            gated.Should().Be(hull.Size == ShipSize.Capital, hull.Name);
        }

        new RecipeShipyardRequirement().RequirementText.Should().Contain("Shipyard");
    }

    [TestCase(false, ShipyardAccessType.Anyone, true, 0, ShipyardRefusal.NotAtShipyard)]
    [TestCase(true, ShipyardAccessType.Citizens, false, 0, ShipyardRefusal.CitizensOnly)]
    [TestCase(true, ShipyardAccessType.Citizens, true, 0, ShipyardRefusal.None)]
    [TestCase(true, ShipyardAccessType.Citizens, true, 500, ShipyardRefusal.OwesTaxes)]
    [TestCase(true, ShipyardAccessType.Anyone, false, 0, ShipyardRefusal.None)]
    [TestCase(true, ShipyardAccessType.Anyone, false, 500, ShipyardRefusal.None)]
    public void ShipyardAccessIsCitizensByDefaultOrAnyone(bool atShipyard, ShipyardAccessType access, bool isCitizen, int owed, ShipyardRefusal expected)
    {
        StationBuildingRules.GetShipyardRefusal(atShipyard, access, isCitizen, owed).Should().Be(expected);
        if (expected == ShipyardRefusal.None)
            StationBuildingRules.DescribeShipyardRefusal(expected).Should().BeEmpty();
        else
            StationBuildingRules.DescribeShipyardRefusal(expected).Should().NotBeEmpty();
        new SWLOR.Game.Server.Entity.StationBuildingState().ShipyardAccess.Should().Be(ShipyardAccessType.Citizens);
    }

    [Test]
    public void TheShipyardFeeIsANewTaxAfterTheExchangeTariff()
    {
        ((int)PropertyTaxType.Shipyard).Should().Be((int)PropertyTaxType.Exchange + 1);
        StationBuildingRules.ClampShipyardFee(-5).Should().Be(0);
        StationBuildingRules.ClampShipyardFee(2500).Should().Be(2500);
        StationBuildingRules.ClampShipyardFee(999999).Should().Be(StationTuning.ShipyardFeeMax);

        // Edited in Manage City with the Edit Taxes permission, like the exchange tariff, and paid into the treasury.
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");
        viewModel.Should().Contain("dbCity.Taxes[PropertyTaxType.Shipyard] = shipyardFee");
        Regex.IsMatch(viewModel, @"if \(CanEditTaxes\)\s+dbCity\.Taxes\[PropertyTaxType\.Shipyard\]").Should().BeTrue();
        ReadServer("Feature", "GuiDefinition", "ManageCityDefinition.cs").Should().Contain("model.ShipyardFee");
        ReadServer("Service", "StationBuildings.cs").Should().Contain("dbStation.Treasury += fee");
    }

    [Test]
    public void AShipyardDroidShortensBuilds()
    {
        // The droid hook is StationDroidRules (industrial droids): +0.3% per Industry point.
        StationDroidRules.TakesDroid(StationDeckSlotType.Shipyard).Should().BeTrue();
        StationDroidRules.TakesDroid(StationDeckSlotType.JumpBeacon).Should().BeFalse();
        StationDroidRules.ShipyardBuildSpeedPercent(0).Should().Be(0);
        StationDroidRules.ShipyardBuildSpeedPercent(100).Should().Be(30);
        StationBuildingRules.ApplyShipyardSpeed(100, 0).Should().Be(100);
        StationBuildingRules.ApplyShipyardSpeed(100, 30).Should().Be(130);
        StationBuildingRules.ApplyShipyardSpeed(55, 15).Should().Be(63);

        var craft = ReadServer("Feature", "GuiDefinition", "ViewModel", "CraftViewModel.cs");
        craft.Should().Contain("StationBuildingRules.ApplyShipyardSpeed(progress, _shipyardSpeedPercent)");
        craft.Should().Contain("StationBuildings.PayShipyardFee").And.Contain("ProcessShipyardFee()");
        ReadServer("Service", "StationBuildings.cs").Should().Contain("StationDroidRules.ShipyardBuildSpeedPercent(state.DroidIndustry)")
            .And.Contain("StationDroidRules.DrillOutputPercent(state.DroidIndustry, state.DroidEfficiencyPercent)")
            .And.Contain("StationDroidRules.TakesDroid(detail.Slot)");
    }

    // The Asteroid Drill

    [Test]
    public void TheDrillMinesTheAnchorAreasAsteroidTablesInTheirSpawnShares()
    {
        var spawnTables = new SpaceResourceSpawnDefinition().BuildSpawnTables();
        var lootTables = new HarvestingLootTableDefinition().BuildLootTables();
        var areas = StationTuning.Anchors.Select(x => x.AreaResref).Distinct().ToList();
        StationTuning.DrillTables.Keys.Should().BeEquivalentTo(areas);

        foreach (var area in areas)
        {
            var git = File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "git", $"{area}.git.json"));
            var tableName = Regex.Matches(git, "\"(SPACE_RESOURCES_[A-Z_]+)\"").Select(m => m.Groups[1].Value).Distinct().Single();

            var expected = spawnTables[tableName].Spawns
                .Select(s => (Table: AsteroidTable(s.Resref), s.Weight))
                .Where(x => x.Table != null)
                .ToList();
            StationTuning.DrillTables[area].Select(x => (x.LootTable, x.Weight)).Should().Equal(expected.Select(x => (x.Table, x.Weight)), area);
            StationTuning.DrillTables[area].Should().OnlyContain(x => lootTables.ContainsKey(x.LootTable));
        }
    }

    [Test]
    public void DrillTablesArePickedByWeight()
    {
        var tables = StationBuildingRules.GetDrillTables("viscaraorbit");
        tables.Should().Equal(("ASTEROID_TILARIUM", 50), ("ASTEROID_CURRIAN", 5));
        StationBuildingRules.GetDrillTotalWeight("viscaraorbit").Should().Be(55);
        StationBuildingRules.PickDrillTable("viscaraorbit", 0).Should().Be("ASTEROID_TILARIUM");
        StationBuildingRules.PickDrillTable("viscaraorbit", 49).Should().Be("ASTEROID_TILARIUM");
        StationBuildingRules.PickDrillTable("viscaraorbit", 50).Should().Be("ASTEROID_CURRIAN");
        StationBuildingRules.PickDrillTable("viscaraorbit", 55).Should().BeNull();
        StationBuildingRules.PickDrillTable("nowhere", 0).Should().BeNull();
    }

    [Test]
    public void ADroidRaisesTheDrillsOutputAndStorage()
    {
        StationDroidRules.TakesDroid(StationDeckSlotType.AsteroidDrill).Should().BeTrue();
        StationDroidRules.DrillOutputPercent(100, 0).Should().Be(50);
        StationDroidRules.DrillOutputPercent(100, 40).Should().Be(70);

        StationBuildingRules.DrillUnitsPerDay(0).Should().Be(StationTuning.DrillUnitsPerDay);
        StationBuildingRules.DrillUnitsPerDay(50).Should().Be(StationTuning.DrillUnitsPerDay * 1.5);
        StationBuildingRules.DrillUnitsPerDay(20).Should().Be(StationTuning.DrillUnitsPerDay * 1.2);
        StationBuildingRules.DrillCapacity(0).Should().Be(StationTuning.DrillUnitsPerDay * StationTuning.DrillStorageDays);
        StationBuildingRules.DrillCapacity(70).Should().BeGreaterThan(StationBuildingRules.DrillCapacity(0));
    }

    [Test]
    public void TheDrillAccruesWholeUnitsAndStopsWhenFull()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // A drill that never ran starts counting now.
        StationBuildingRules.AccrueDrill(0, default, start, 0).Should().Be((0, start));

        // A day of output, with the partial unit carried.
        var hoursPerUnit = 24.0 / StationTuning.DrillUnitsPerDay;
        var (units, next) = StationBuildingRules.AccrueDrill(0, start, start.AddHours(24).AddMinutes(1), 0);
        units.Should().Be(StationTuning.DrillUnitsPerDay);
        next.Should().Be(start.AddHours(StationTuning.DrillUnitsPerDay * hoursPerUnit));

        // Less than a unit: nothing yet, and the clock keeps its place.
        StationBuildingRules.AccrueDrill(0, start, start.AddHours(hoursPerUnit / 2), 0).Should().Be((0, start));

        // A long absence fills the storage and no more.
        var capacity = StationBuildingRules.DrillCapacity(0);
        var later = start.AddDays(30);
        StationBuildingRules.AccrueDrill(10, start, later, 0).Should().Be((capacity - 10, later));

        // A full drill mines nothing and restarts from now, so emptying it never pays out the idle time.
        StationBuildingRules.AccrueDrill(capacity, start, later, 0).Should().Be((0, later));

        // The droid's output raises the rate.
        StationBuildingRules.AccrueDrill(0, start, start.AddHours(24).AddMinutes(1), 50).Units
            .Should().Be((int)(StationTuning.DrillUnitsPerDay * 1.5));
    }

    [Test]
    public void TheDrillsStorageIsKeptOnTheBuildingsState()
    {
        var state = new SWLOR.Game.Server.Entity.StationBuildingState();
        state.DrillStorage.Should().BeEmpty();
        state.SerializedDroid.Should().BeEmpty();
        state.BeaconAllyStationIds.Should().BeEmpty();

        var service = ReadServer("Service", "StationBuildings.cs");
        service.Should().Contain("state.DrillStorage[resref] = state.DrillStorage.GetValueOrDefault(resref) + 1");
        service.Should().Contain("Loot.GetLootTableByName(tableName)");
        service.Should().Contain("ReturnDroidToOverflow(state)");
        ReadServer("Service", "Property.cs").Should().Contain("StationBuildings.OnDeleted(property)");
    }

    // The Jump Beacon

    [TestCase(1, ShipSize.Medium)]
    [TestCase(2, ShipSize.Large)]
    [TestCase(3, ShipSize.Capital)]
    [TestCase(0, ShipSize.Medium)]
    [TestCase(9, ShipSize.Capital)]
    public void BeaconLevelCapsTheHull(int level, ShipSize expected)
    {
        StationBuildingRules.GetMaxHull(level).Should().Be(expected);
    }

    [Test]
    public void BothBeaconsMustTakeTheHull()
    {
        StationBuildingRules.TakesHull(3, 3, ShipSize.Capital).Should().BeTrue();
        StationBuildingRules.TakesHull(3, 2, ShipSize.Capital).Should().BeFalse();
        StationBuildingRules.TakesHull(1, 3, ShipSize.Large).Should().BeFalse();
        StationBuildingRules.TakesHull(1, 1, ShipSize.Small).Should().BeTrue();
        StationBuildingRules.TakesHull(1, 1, ShipSize.Medium).Should().BeTrue();
        StationBuildingRules.TakesHull(3, 3, ShipSize.Invalid).Should().BeFalse();
    }

    [Test]
    public void AJumpBurnsStationFuelByHullSize()
    {
        StationBuildingRules.GetJumpFuel(ShipSize.Small, "").Should().Be(1);
        StationBuildingRules.GetJumpFuel(ShipSize.Medium, "").Should().Be(2);
        StationBuildingRules.GetJumpFuel(ShipSize.Large, "").Should().Be(4);
        StationBuildingRules.GetJumpFuel(ShipSize.Capital, null).Should().Be(8);
        StationBuildingRules.GetJumpFuel(ShipSize.Invalid, "").Should().Be(0);

        // The Hutt Grand Barge's signature: jumps cost 50% less fuel.
        var barge = RevampShipCatalog.Hulls.Single(x => x.Name == "Hutt Grand Barge");
        barge.Signature.Should().Contain("Jump Beacon jumps cost 50% less fuel");
        StationTuning.JumpFuelDiscountPercent.Should().ContainKey(barge.Tag);
        StationBuildingRules.GetJumpFuel(barge.Size, barge.Tag).Should().Be(2);
    }

    [TestCase(JumpBeaconAccessType.Citizens, true, false, true)]
    [TestCase(JumpBeaconAccessType.Citizens, false, true, false)]
    [TestCase(JumpBeaconAccessType.Allies, false, true, true)]
    [TestCase(JumpBeaconAccessType.Allies, true, false, true)]
    [TestCase(JumpBeaconAccessType.Allies, false, false, false)]
    [TestCase(JumpBeaconAccessType.Anyone, false, false, true)]
    [TestCase(JumpBeaconAccessType.Invalid, false, false, false)]
    public void BeaconAccessIsCitizensAlliesOrAnyone(JumpBeaconAccessType access, bool isCitizen, bool isAlly, bool expected)
    {
        StationBuildingRules.CanUseBeacon(access, isCitizen, isAlly).Should().Be(expected);
        new SWLOR.Game.Server.Entity.StationBuildingState().BeaconAccess.Should().Be(JumpBeaconAccessType.Citizens);
    }

    private static JumpRequest Jump(
        bool docked = true, bool pilot = true, bool originWild = false, bool destinationWild = false, bool same = false,
        bool originPermits = true, bool destinationPermits = true, int originLevel = 1, int destinationLevel = 1,
        ShipSize size = ShipSize.Small, string hullTag = "", int fuel = 10) => new()
    {
        ShipDocked = docked,
        CanPilot = pilot,
        OriginInWildSpace = originWild,
        DestinationInWildSpace = destinationWild,
        SameStation = same,
        OriginPermits = originPermits,
        DestinationPermits = destinationPermits,
        OriginBeaconLevel = originLevel,
        DestinationBeaconLevel = destinationLevel,
        ShipSize = size,
        HullTag = hullTag,
        FuelAboard = fuel,
    };

    [Test]
    public void AJumpNeedsADockedShipPermissionsTheHullCapAndFuel()
    {
        StationBuildingRules.GetJumpRefusal(Jump()).Should().Be(JumpRefusal.None);
        StationBuildingRules.GetJumpRefusal(Jump(docked: false)).Should().Be(JumpRefusal.ShipNotDocked);
        StationBuildingRules.GetJumpRefusal(Jump(pilot: false)).Should().Be(JumpRefusal.CannotPilot);
        StationBuildingRules.GetJumpRefusal(Jump(same: true)).Should().Be(JumpRefusal.SameStation);
        StationBuildingRules.GetJumpRefusal(Jump(originPermits: false)).Should().Be(JumpRefusal.OriginNotPermitted);
        StationBuildingRules.GetJumpRefusal(Jump(destinationPermits: false)).Should().Be(JumpRefusal.DestinationNotPermitted);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Invalid)).Should().Be(JumpRefusal.UnknownSize);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Large)).Should().Be(JumpRefusal.HullTooLarge);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Large, originLevel: 2, destinationLevel: 2, fuel: 4)).Should().Be(JumpRefusal.None);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Large, originLevel: 2, destinationLevel: 2, fuel: 3)).Should().Be(JumpRefusal.InsufficientFuel);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Large, originLevel: 2, destinationLevel: 2, hullTag: "sd_huttgrandbarg", fuel: 2))
            .Should().Be(JumpRefusal.None);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Capital, originLevel: 3, destinationLevel: 3, fuel: 8)).Should().Be(JumpRefusal.None);
        StationBuildingRules.GetJumpRefusal(Jump(size: ShipSize.Capital, originLevel: 3, destinationLevel: 2, fuel: 8)).Should().Be(JumpRefusal.HullTooLarge);

        var refusal = Jump(size: ShipSize.Large, originLevel: 2, destinationLevel: 2, fuel: 1);
        StationBuildingRules.DescribeJumpRefusal(StationBuildingRules.GetJumpRefusal(refusal), refusal).Should().Contain("4 Station Fuel");
    }

    [Test]
    public void BeaconsNeverConnectIntoOrOutOfWildSpace()
    {
        StationBuildingRules.GetJumpRefusal(Jump(originWild: true)).Should().Be(JumpRefusal.WildSpace);
        StationBuildingRules.GetJumpRefusal(Jump(destinationWild: true)).Should().Be(JumpRefusal.WildSpace);
        StationBuildingRules.DescribeJumpRefusal(JumpRefusal.WildSpace, Jump()).Should().Contain("Wild Space");

        // Destinations leave out Wild Space stations, and the request reads an unknown anchor as Wild Space.
        var service = ReadServer("Service", "StationBuildings.cs");
        service.Should().Contain("anchor.IsWildSpace");
        service.Should().Contain("OriginInWildSpace = Station.GetAnchor(origin)?.IsWildSpace ?? true");
    }

    [Test]
    public void AJumpMovesTheShipBetweenStarportsUnderTheDockingSizeRules()
    {
        var service = ReadServer("Service", "StationBuildings.cs");
        service.Should().Contain("Space.GetStarportRefusal(record, landingArea, starportId)");
        service.Should().Contain("CargoHoldRules.RemoveUnits(record.Cargo, CommodityType.StationFuel, fuel)");
        service.Should().Contain("ship.Positions[PropertyLocationType.DockPosition]");
        service.Should().Contain("ship.Positions[PropertyLocationType.SpacePosition]");
        service.Should().Contain("PropertyChildType.RegisteredStarport");

        // A Capital hull still needs a level 2 station starport at the far end.
        ShipAccessRules.GetStarportRefusal(new StarportDockRequest
        {
            ShipSize = ShipSize.Capital, IsPlayerStarport = true, IsStationStarport = true, PlayerStarportLevel = 1,
        }).Should().Be(StarportDockRefusal.StationStarportTooSmall);
    }

    // Station upgrades

    [Test]
    public void TheJumpBeaconLevelIsACityUpgrade()
    {
        ((int)PropertyUpgradeType.JumpBeaconLevel).Should().Be(6);
        StationTuning.BeaconMaxLevel.Should().Be(3);
        StationBuildingRules.ClampBeaconLevel(5).Should().Be(3);

        // Each level above 1 adds the usual 10,000 a week, like the other building upgrades.
        StationRules.GetWeeklyUpkeep(0, 1, 1, 1, 3).Should().Be(StationRules.GetWeeklyUpkeep(0, 1, 1, 1) + 2 * CityUpkeepRules.UpgradeBasePrice);
        CityUpkeepRules.GetWeeklyUpkeep(5000, 0, 1, 1, 1).Should().Be(35000);

        ReadServer("Service", "Property.cs").Should().Contain("property.Upgrades[PropertyUpgradeType.JumpBeaconLevel] = 1");
        ReadServer("Feature", "GuiDefinition", "ManageCityDefinition.cs").Should().Contain("model.UpgradeJumpBeaconLevel()");
    }

    // Station Fuel

    [Test]
    public void StationFuelIsALegalCargoGoodMadeAtCz220()
    {
        ((int)CommodityType.StationFuel).Should().Be(24);
        var fuel = TradeLookup.GetCommodity(CommodityType.StationFuel);
        fuel.Name.Should().Be("Station Fuel");
        fuel.BasePrice.Should().Be(250);
        fuel.Volume.Should().Be(2);
        fuel.Legality.Should().Be(CommodityLegality.Legal);
        fuel.Producers.Should().Equal(TradeExchangeType.CZ220);
        fuel.Consumers.Should().BeEmpty();

        // Worth more than its inputs (Reactor Cores and Tibanna Gas), and every exchange trades it.
        fuel.BasePrice.Should().BeGreaterThan(TradeLookup.GetCommodity(CommodityType.ReactorCores).BasePrice);
        TradeCatalog.Exchanges.Should().OnlyContain(x => x.FindMarket(CommodityType.StationFuel) != null);
        TradeLookup.GetExchange(TradeExchangeType.CZ220).FindMarket(CommodityType.StationFuel).Role.Should().Be(CommodityRole.Producer);
    }

    // Wiring

    [Test]
    public void BuildingsAreWiredIntoTheCityAndCraftingSystems()
    {
        var property = ReadServer("Service", "Property.cs");
        property.Should().Contain("StationBuildingRules.GetPlacementError(");
        property.Should().Contain("StationBuildingRules.IsStationBuilding(x.StructureType)");

        var changed = ReadServer("Service", "PropertyService", "StructureChangedAction.cs");
        foreach (var building in Buildings)
        {
            changed.Should().Contain($"Assign(StructureType.{building}, StructureChangeType.PositionChanged, StationBuildings.OnPositionChanged)");
            changed.Should().Contain($"Assign(StructureType.{building}, StructureChangeType.Retrieved, StationBuildings.OnRetrieved)");
        }

        ScriptName.OnStationBuildingUsed.Length.Should().BeLessThanOrEqualTo(16);
        ReadServer("Feature", "DialogDefinition", "StationBuildingDialog.cs").Should().Contain("GuiWindowType.Recipes");
    }

    // Files

    private static string AsteroidTable(string placeableResref)
    {
        using var utp = ReadModule("utp", placeableResref);
        if (!utp.RootElement.TryGetProperty("VarTable", out var vars))
            return null;

        foreach (var variable in vars.GetProperty("value").EnumerateArray())
        {
            if (Value(variable, "Name").GetString() == "ASTEROID_LOOT_TABLE_ID")
                return Value(variable, "Value").GetString();
        }

        return null;
    }

    private static JsonElement Value(JsonElement element, string name) => element.GetProperty(name).GetProperty("value");

    private static JsonDocument ReadModule(string kind, string resref)
    {
        var bytes = File.ReadAllBytes(Path.Combine(RepositoryRoot, "Module", kind, $"{resref}.{kind}.json"));
        return JsonDocument.Parse(Encoding.Latin1.GetString(bytes));
    }

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
