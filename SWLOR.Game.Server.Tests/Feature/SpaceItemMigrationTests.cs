#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;
using static SWLOR.Game.Server.Tests.Feature.SpaceItemFixtures;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The space revamp's migration (plan section 15) on representative saved data for every kind of retired item: hull
/// deeds, tiered and capital modules, configuration modules, items in bags and droids, registered ships and characters.
/// </summary>
[NonParallelizable]
public class SpaceItemMigrationTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const int UseLimitationPerk = 100;
    private const int RequiresSkill = 131;
    private const int ModuleBonus = 113;
    private const int StarshipBonus = 114;

    [SetUp]
    public void Load() => LoadBlueprints();

    [Test]
    public void TheCatalog_MapsEveryRetiredItemToAnExistingItem()
    {
        var hulls = RevampShipCatalog.Hulls.ToDictionary(x => x.Tag);
        foreach (var hull in LegacySpaceItemCatalog.Hulls)
        {
            HasBlueprint(hull.Resref).Should().BeTrue(hull.Resref);
            Read(Item(hull.Resref)).Tag.Should().Be(hull.Tag, "a registered ship stores its deed's tag");
            hulls.Should().ContainKey(hull.ShipTag);
        }
        foreach (var starter in new[] { "sdeed_freighter", "sdeed_escort" })
        {
            LegacySpaceItemCatalog.TryGetHullByResref(starter, out var hull).Should().BeTrue();
            hulls[hull.ShipTag].Size.Should().Be(ShipSize.Small, "the store starters stay flyable at Piloting 0");
            hulls[hull.ShipTag].PilotingRank.Should().Be(0);
        }

        var steps = RevampModuleCatalog.Steps.ToDictionary(x => x.Tag);
        foreach (var (legacy, replacement) in LegacySpaceItemCatalog.Modules)
            steps.Should().ContainKey(replacement, legacy);
        foreach (var (legacy, rank) in new[] { ("com_laser_b", 0), ("com_laser_1", 10), ("com_laser_2", 20), ("com_laser_3", 30), ("com_laser_4", 40),
                     ("beamcannon1", 0), ("cap_inject5", 40), ("las_armor_3", 20), ("protbomblnch", 40), ("cap_minlas", 40), ("turbolas3", 40) })
            steps[LegacySpaceItemCatalog.Modules[legacy]].Rank.Should().Be(rank, legacy);
        steps[LegacySpaceItemCatalog.Modules["hvy_armor_1"]].Family.Should().Be(ShipModuleFamily.AdaptivePlating);
        steps[LegacySpaceItemCatalog.Modules["shld_boost_b"]].Row.Should().Be(ShipModulePowerType.Mid, "Shield Boosters move to the Mid row");

        foreach (var (configuration, credits) in LegacySpaceItemCatalog.ConfigurationModules.Where(x => HasBlueprint(x.Key)))
            Read(Item(configuration)).Cost.Should().Be(credits, $"{configuration} is refunded at its value");
    }

    [Test]
    public void AModule_BecomesItsRankStepAndKeepsItsCraftedBonusesIdentityAndLocals()
    {
        var original = Item("com_laser_2",
            new[] { Text("UUID", "laser-uuid"), Scalar(2, "Repos_PosX", 3), Scalar(2, "Repos_Posy", 4), List("VarTable", IntLocal("CRAFTED_QUALITY", 27)) },
            new[] { Property(ModuleBonus, 65535, 0, 6), Property(UseLimitationPerk, 118, 33, 3), Property(RequiresSkill, 0, 48, 100) });
        var tally = new LegacySpaceItemTally();

        LegacySpaceItemConversion.Migrate(original, out var migrated, tally).Should().BeTrue();

        var item = Read(migrated);
        (item.Resref, item.Tag, item.Name).Should().Be(("sm_laser_20", "sm_laser_20", BlueprintName("sm_laser_20")));
        item.Properties.Should().Contain((RequiresSkill, (int)SkillType.Piloting, 20), "the item carries its rank as Requires Skill");
        item.Properties.Should().Contain((ModuleBonus, 65535, 6), "crafted module bonuses are kept");
        item.Properties.Should().NotContain(p => p.Type == UseLimitationPerk, "the retired perk stamp is gone");
        item.Properties.Should().NotContain(p => p.Type == RequiresSkill && p.Value == 100, "the broken stamp is gone");
        item.Uuid.Should().Be("laser-uuid");
        (item.X, item.Y).Should().Be((3, 4));
        item.Locals.Should().Contain(("CRAFTED_QUALITY", 27));
        tally.RootReplacementResref.Should().Be("sm_laser_20");
        tally.Items.Should().Contain("com_laser_2", 1);

        LegacySpaceItemConversion.Migrate(migrated, out var rerun, new LegacySpaceItemTally()).Should().BeFalse("the conversion is idempotent");
        rerun.Should().BeSameAs(migrated);
    }

    [TestCase("sdeed_hound", "sd_republicstrik", "sd_republicstrik:10073")]
    [TestCase("capdeed_sthran", "sd_siththranta", null)]
    [TestCase("sdeed_freighter", "sd_striker", null)]
    public void ADeed_BecomesItsShipAndKeepsItsLookAsAHullDesign(string deed, string ship, string? design)
    {
        var original = Item(deed, extraProperties: new[] { Property(StarshipBonus, 46, 0, 12) });
        var tally = new LegacySpaceItemTally();

        LegacySpaceItemConversion.Migrate(original, out var migrated, tally).Should().BeTrue();

        var item = Read(migrated);
        item.Resref.Should().Be(ship);
        item.Properties.Should().NotContain(p => p.Type == UseLimitationPerk);
        item.Properties.Should().Contain((StarshipBonus, 46, 12), "starship enhancements are kept");
        var gate = RevampShipCatalog.Hulls.Single(x => x.Tag == ship).PilotingRank;
        if (gate > 0)
            item.Properties.Should().Contain((RequiresSkill, (int)SkillType.Piloting, gate));
        if (design == null)
            tally.HullDesigns.Should().BeEmpty("the old model is the new ship's own or not one of its designs");
        else
            tally.HullDesigns.Should().Equal(design);
    }

    [Test]
    public void ConfigurationModules_AreRefundedWhereverTheyAre()
    {
        var tally = new LegacySpaceItemTally();
        LegacySpaceItemConversion.Migrate(Item("config_fig3"), out _, tally).Should().BeFalse("a stored configuration module is removed by its caller");
        tally.RootIsConfigurationModule.Should().BeTrue();
        tally.Credits.Should().Be(750);

        var bag = Encode(ItemNode("bag_sm", new[]
        {
            List("ItemList",
                ItemNode("config_int1", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0),
                ItemNode("min_laser_4", new[] { Scalar(2, "Repos_PosX", 1), Scalar(2, "Repos_Posy", 0) }, type: 1)),
        }), "UTI ");
        tally = new LegacySpaceItemTally();

        LegacySpaceItemConversion.Migrate(bag, out var migrated, tally).Should().BeTrue();

        tally.RootIsConfigurationModule.Should().BeFalse();
        tally.Credits.Should().Be(200);
        var contents = GffReader.Read(Convert.FromBase64String(migrated)).RootStruct.Fields.Single(x => x.Label == "ItemList").Value as GffList;
        contents!.Elements.Should().ContainSingle();
        Describe(contents.Elements[0]).Resref.Should().Be("sm_mine_40");
        contents.Elements[0].Type.Should().Be(0u, "the bag's items are numbered again");
    }

    [Test]
    public void ADroidsSavedInventory_IsConverted()
    {
        var droid = new ConstructedDroid { Name = "R5" };
        droid.Inventory["laser"] = Item("ion_cann_b");
        droid.Inventory["config"] = Item("config_ind5");
        droid.Inventory["other"] = Item("bag_sm");
        var controller = Item("droid_control", new[] { List("VarTable", StringLocal("CONSTRUCTED_DROID", JsonConvert.SerializeObject(droid))) });
        var tally = new LegacySpaceItemTally();

        LegacySpaceItemConversion.Migrate(controller, out var migrated, tally).Should().BeTrue();

        var saved = JsonConvert.DeserializeObject<ConstructedDroid>(Read(migrated).StringLocals["CONSTRUCTED_DROID"])!;
        saved.Inventory.Keys.Should().BeEquivalentTo("laser", "other");
        Read(saved.Inventory["laser"]).Resref.Should().Be("sm_ion_00");
        saved.Inventory["other"].Should().Be(droid.Inventory["other"]);
        tally.Credits.Should().Be(2500);
    }

    [Test]
    public void ARegisteredShip_BecomesItsNewShipWithItsModulesInTheirNewRows()
    {
        var ship = LegacyShip();
        var hull = RevampShipCatalog.Hulls.Single(x => x.Tag == "sd_republicgunsh");

        var result = PlayerShipConversion.Convert(ship, willpower: 10);

        result.Changed.Should().BeTrue();
        result.HullConverted.Should().BeTrue();
        var status = ship.Status;
        status.ItemTag.Should().Be("sd_republicgunsh");
        Read(ship.SerializedItem).Resref.Should().Be("sd_republicgunsh");
        status.RequiredSkills.Should().Contain(SkillType.Piloting, hull.PilotingRank);
        ship.HullDesignAppearance.Should().Be(10063, "the Mule's model is a Republic Gunship design");
        result.Items.HullDesigns.Should().Equal("sd_republicgunsh:10063");
        result.Items.Credits.Should().Be(400, "the Fighter Configuration I is refunded");
        status.ConfigurationModules.Should().BeEmpty();
        ship.PlayerHotBars.Should().BeEmpty("the hotbar feats change");
        status.ActiveModules.Should().BeEmpty();

        status.HighPowerModules.Count.Should().BeLessThanOrEqualTo(hull.High);
        status.MidPowerModules.Count.Should().BeLessThanOrEqualTo(hull.Mid);
        status.LowPowerModules.Count.Should().BeLessThanOrEqualTo(hull.Low);
        var fitted = status.HighPowerModules.Values.Concat(status.MidPowerModules.Values).Concat(status.LowPowerModules.Values).ToList();
        (fitted.Count + result.ReturnedModules.Count).Should().Be(10, "every module is kept or returned, never deleted");
        result.ReturnedModules.Select(x => x.Resref).Should().Contain("sm_turbo_40", "a capital weapon does not fit a Medium hull");
        result.ReturnedModules.Should().OnlyContain(x => x.PlayerId == ship.OwnerPlayerId && x.Quantity == 1 && x.Id.StartsWith(ship.Id));
        foreach (var module in fitted)
        {
            Read(module.SerializedItem).Resref.Should().Be(module.ItemTag);
            RevampModuleCatalog.Steps.Should().Contain(x => x.Tag == module.ItemTag);
        }
        status.MidPowerModules.Values.Select(x => x.ItemTag).Should().Contain("sm_shbst_00", "Shield Boosters move from Low to Mid");
        status.HighPowerModules[1].ModuleBonus.Should().Be(3, "the module bonus stays");
        status.HighPowerModules[1].RequiredSkills.Should().BeNull("a rank 0 module has no requirement");

        status.Size.Should().Be(ShipSize.Medium);
        status.MaxHull.Should().BeGreaterThan(0);
        (status.Hull, status.Shield, status.Capacitor).Should().Be((status.MaxHull, status.MaxShield, status.MaxCapacitor), "a converted ship starts full");

        var returned = result.ReturnedModules.Select(x => x.Id).ToList();
        var rerun = PlayerShipConversion.Convert(ship, 10);
        rerun.HullConverted.Should().BeFalse("a converted ship is not converted again");
        rerun.ReturnedModules.Should().BeEmpty();
        rerun.Items.Credits.Should().Be(0);
        returned.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void AConvertedShip_KeepsItsDeedEnhancementsInItsStats()
    {
        PlayerShip Ship(params object[] properties)
        {
            var ship = LegacyShip();
            ship.SerializedItem = Item("sdeed_mule", extraProperties: properties);
            return ship;
        }

        var plain = Ship();
        var enhanced = Ship(Property(StarshipBonus, 46, 0, 12), Property(StarshipBonus, 48, 0, 5), Property(StarshipBonus, 53, 0, 3));
        PlayerShipConversion.Convert(plain, 10);
        PlayerShipConversion.Convert(enhanced, 10);

        enhanced.Status.DeedBonuses.Should().BeEquivalentTo(new ShipBonuses { Hull = 12, Shield = 5, Accuracy = 3 });
        plain.Status.DeedBonuses.Should().BeNull("the deed has no enhancements");
        enhanced.Status.MaxHull.Should().BeGreaterThan(plain.Status.MaxHull, "12 hull points stay on the converted ship");
        enhanced.Status.MaxShield.Should().BeGreaterThan(plain.Status.MaxShield);
        enhanced.Status.Accuracy.Should().BeGreaterThan(plain.Status.Accuracy);
    }

    [Test]
    public void ARevampShip_KeepsItsSlotsAndOnlyRebuildsItsStats()
    {
        var ship = new PlayerShip
        {
            Id = "revamp-ship",
            OwnerPlayerId = "owner",
            SerializedItem = Item("sd_striker"),
            Status = new ShipStatus { ItemTag = "sd_striker", Hull = 1, MaxHull = 1 },
        };
        ship.Status.HighPowerModules[3] = Module("sm_laser_00");
        ship.Status.MidPowerModules[2] = Module("sm_shbst_00");

        var result = PlayerShipConversion.Convert(ship, 0);

        result.HullConverted.Should().BeFalse();
        ship.Status.HighPowerModules.Keys.Should().Equal(3);
        ship.Status.MidPowerModules.Keys.Should().Equal(2);
        ship.Status.Hull.Should().Be(1, "only a converted hull starts full");
        ship.Status.MaxHull.Should().BeGreaterThan(1);
    }

    [Test]
    public void ModulesFromDifferentRowsAndTheSameSlot_ReturnToDistinctStableOverflowRecords()
    {
        ShipStatus.ShipStatusModule WithoutId(string resref)
        {
            var module = Module(resref);
            module.ItemInstanceId = null;
            return module;
        }

        PlayerShip Ship()
        {
            var ship = LegacyShip();
            ship.Status.HighPowerModules.Clear();
            ship.Status.LowPowerModules.Clear();
            ship.Status.ConfigurationModules.Clear();
            // Shield repairers and boosters both move to Mid. Without saved item IDs, the overflow record ID comes from the slot.
            for (var slot = 1; slot <= 4; slot++)
                ship.Status.HighPowerModules[slot] = WithoutId("shld_rep_b");
            ship.Status.LowPowerModules[4] = WithoutId("shld_boost_b");
            return ship;
        }

        var hull = RevampShipCatalog.Hulls.Single(x => x.Tag == "sd_republicgunsh");
        hull.Mid.Should().BeLessThan(5, "the test needs the Mid row to overflow");

        var first = PlayerShipConversion.Convert(Ship(), 10).ReturnedModules.Select(x => x.Id).ToList();
        var retry = PlayerShipConversion.Convert(Ship(), 10).ReturnedModules.Select(x => x.Id).ToList();

        first.Should().HaveCount(5 - hull.Mid).And.OnlyHaveUniqueItems("a second record with the same ID would overwrite the first");
        retry.Should().Equal(first, "a retried conversion writes the same records");
    }

    [Test]
    public void TheRecord_RefundsTheRetiredPerksAndGetsATokenAndTheNotice()
    {
        var player = new Player { Name = "Pilot", UnallocatedSP = 1 };
        player.Perks[PerkType.Starships] = 5;
        player.Perks[PerkType.MiningModules] = 3;
        player.Perks[PerkType.EnergyManagement] = 2;
        player.Perks[PerkType.IntuitivePiloting] = 1;
        player.Perks[PerkType.Prospector] = 1;
        player.UnlockedPerks[PerkType.Starships] = DateTime.UtcNow;
        player.UnlockedRecipes[RecipeType.AurekStrikefighter] = DateTime.UtcNow;

        var migration = Activator.CreateInstance(CharacterMigrations.Named("_28_ConvertSpaceItems"))!;
        migration.GetType().GetMethod("MigratePlayerData")!.Invoke(migration, new object[] { player });

        player.UnallocatedSP.Should().Be(1 + 11 + 4 + 10 + 3);
        player.Perks.Keys.Should().Equal(PerkType.Prospector);
        player.UnlockedPerks.Should().BeEmpty();
        player.UnlockedRecipes.Should().ContainKey(RecipeType.ShipDeedAurekStrikefighter, "an unlocked hull recipe stays unlocked");
        player.Currencies[CurrencyType.RebuildToken].Should().Be(1);
        player.UpgradeNotices.First().Should().Contain("Piloting now covers every space hull and combat module").And.Contain("Rebuild Token")
            .And.Contain("Gathering").And.Contain("rank limits").And.Contain("move ranks into Gathering");
        player.UpgradeNotices.Should().Contain("Perk 'Starships' was automatically refunded. You reclaimed 11 SP.");
        player.UpgradeNotices.Should().OnlyContain(notice => notice.All(c => c < 128), "game text is ASCII");
    }

    [Test]
    public void TheCharacterFile_LosesThePerkFeatsAndConvertsItsItems()
    {
        var feats = new[] { FeatType.StarshipsTrait, FeatType.OffensiveModulesTrait, FeatType.PropertyMenu }
            .Select(feat => Node(1, Scalar(2, "Feat", (int)feat))).ToArray();
        var root = Node(uint.MaxValue, Text("UUID", "space-character"), Scalar(0, "IsDM", 0), Scalar(4, "Gold", 100), List("ClassList"),
            List("FeatList", feats), List("Equip_ItemList"),
            List("ItemList",
                ItemNode("com_laser_b", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0),
                ItemNode("config_bmb1", new[] { Scalar(2, "Repos_PosX", 2), Scalar(2, "Repos_Posy", 0) }, type: 0),
                ItemNode("sdeed_hound", new[] { Scalar(2, "Repos_PosX", 4), Scalar(2, "Repos_Posy", 0) }, type: 0)));
        var file = Convert.FromBase64String(Encode(root, "BIC "));
        var record = new Player { Name = "Pilot" };
        var characterType = typeof(Migration).Assembly.GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacter")!;
        var character = Activator.CreateInstance(characterType, Members, null, new object?[] { file, record }, null)!;

        var migration = Activator.CreateInstance(CharacterMigrations.Named("_28_ConvertSpaceItems"))!;
        migration.GetType().GetMethod("Migrate")!.Invoke(migration, new[] { character });

        var saved = GffReader.Read((byte[])characterType.GetMethod("Serialize", Members)!.Invoke(character, null)!).RootStruct;
        ((GffList)saved.Fields.Single(x => x.Label == "FeatList").Value!).Elements
            .Select(x => Convert.ToInt32(x.Fields.Single(f => f.Label == "Feat").Value)).Should().Equal((int)FeatType.PropertyMenu);
        Convert.ToInt64(saved.Fields.Single(x => x.Label == "Gold").Value).Should().Be(300, "the Bomber Configuration I is refunded in credits");
        ((GffList)saved.Fields.Single(x => x.Label == "ItemList").Value!).Elements.Select(x => Describe(x).Resref)
            .Should().Equal("sm_laser_00", "sd_republicstrik");
        record.UnlockedHullDesigns.Should().ContainKey("sd_republicstrik:10073", "a carried deed keeps its look as a design");
        ((List<string>)characterType.GetProperty("Notices")!.GetValue(character)!).Should().ContainSingle(x => x.Contains("200 credits"));
    }

    [Test]
    public void AConvertedShip_GivesFittedModulesWithoutAnIdAStableOne()
    {
        PlayerShip Ship()
        {
            var ship = LegacyShip();
            foreach (var module in ship.Status.HighPowerModules.Values.Concat(ship.Status.LowPowerModules.Values))
                module.ItemInstanceId = null;
            ship.Status.HighPowerModules[1].ItemInstanceId = "kept-id";
            return ship;
        }

        var first = Ship();
        PlayerShipConversion.Convert(first, 10);
        var modules = first.Status.HighPowerModules.Values.Concat(first.Status.MidPowerModules.Values).Concat(first.Status.LowPowerModules.Values).ToList();
        modules.Should().NotBeEmpty();
        modules.Select(x => x.ItemInstanceId).Should().OnlyContain(id => !string.IsNullOrWhiteSpace(id), "uninstalling keys its records by the id")
            .And.OnlyHaveUniqueItems();
        modules.Select(x => x.ItemInstanceId).Should().Contain("kept-id", "a module that has an id keeps it");

        var retried = Ship();
        PlayerShipConversion.Convert(retried, 10);
        retried.Status.LowPowerModules.Values.Select(x => x.ItemInstanceId)
            .Should().Equal(first.Status.LowPowerModules.Values.Select(x => x.ItemInstanceId), "a retried conversion gives the same ids");
        var ids = modules.Select(x => x.ItemInstanceId).ToList();
        PlayerShipConversion.Convert(first, 10);
        first.Status.HighPowerModules.Values.Concat(first.Status.MidPowerModules.Values).Concat(first.Status.LowPowerModules.Values)
            .Select(x => x.ItemInstanceId).Should().Equal(ids, "a converted ship converted again keeps its ids");
    }

    [Test]
    public void AModuleWithoutAnId_GetsOneOnce()
    {
        var module = new ShipStatus.ShipStatusModule { ItemTag = "sm_plate_30" };
        ShipModuleInstanceIds.AssignMissing(module, () => "new-id").Should().BeTrue();
        module.ItemInstanceId.Should().Be("new-id");
        ShipModuleInstanceIds.AssignMissing(module, () => "other-id").Should().BeFalse();
        module.ItemInstanceId.Should().Be("new-id");

        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server", "Service", "SpaceShipItemMoves.cs")).Replace("\r\n", "\n");
        source.Should().Contain("if (ShipModuleInstanceIds.AssignMissing(module, () => Guid.NewGuid().ToString()))\n                DB.Set(ship);",
            "the id is saved with the ship before the module is handed over");
    }

    [Test]
    public void AConvertedShip_CountsItsPlatingsModuleBonus()
    {
        PlayerShip Ship(int moduleBonus)
        {
            var ship = LegacyShip();
            ship.Status.HighPowerModules.Clear();
            ship.Status.LowPowerModules.Clear();
            ship.Status.ConfigurationModules.Clear();
            ship.Status.LowPowerModules[1] = Module("hull_boost_4", moduleBonus);
            return ship;
        }

        var plain = Ship(0);
        var enhanced = Ship(5);
        PlayerShipConversion.Convert(plain, 10);
        PlayerShipConversion.Convert(enhanced, 10);

        enhanced.Status.LowPowerModules[1].ModuleBonus.Should().Be(5, "the enhancement survives the conversion");
        enhanced.Status.MaxHull.Should().BeGreaterThan(plain.Status.MaxHull, "the plating's +5 Module Bonus adds hull");
        (enhanced.Status.MaxShield, enhanced.Status.MaxCapacitor).Should().Be((plain.Status.MaxShield, plain.Status.MaxCapacitor));
    }

    [Test]
    public void AContractItem_CarriesTheCreditsAndDesignItsConversionOwesToWhoeverClaimsIt()
    {
        var bag = new QuestContractItem
        {
            Name = "Bag",
            Resref = "bag_sm",
            Data = Encode(ItemNode("bag_sm", new[]
            {
                List("ItemList",
                    ItemNode("config_int1", new[] { Scalar(2, "Repos_PosX", 0), Scalar(2, "Repos_Posy", 0) }, type: 0),
                    ItemNode("min_laser_4", new[] { Scalar(2, "Repos_PosX", 1), Scalar(2, "Repos_Posy", 0) }, type: 1)),
            }), "UTI "),
        };
        var deed = new QuestContractItem { Name = "Hound", Resref = "sdeed_hound", Data = Item("sdeed_hound") };
        var sweep = new LegacySpaceItemSweep();

        sweep.MigrateContractItem(bag).Should().BeTrue();
        sweep.MigrateContractItem(deed).Should().BeTrue();

        bag.RetiredSpaceCredits.Should().Be(200, "the Configuration module taken out of the bag is paid to its recipient");
        bag.HullDesigns.Should().BeNull();
        deed.Resref.Should().Be("sd_republicstrik");
        deed.Name.Should().Be(BlueprintName("sd_republicstrik"));
        deed.HullDesigns.Should().Equal("sd_republicstrik:10073");
        deed.RetiredSpaceCredits.Should().Be(0);

        sweep.MigrateContractItem(bag).Should().BeFalse("a rerun finds nothing left to convert");
        sweep.MigrateContractItem(deed).Should().BeFalse();
        (bag.RetiredSpaceCredits, deed.HullDesigns!.Count).Should().Be((200, 1), "the recipient receives them once");

        // The claim copies its delivery through JSON, which keeps what the items carry.
        var delivery = new QuestContractDelivery { PlayerId = "recipient", Items = { bag, deed } };
        var copy = JsonConvert.DeserializeObject<QuestContractDelivery>(JsonConvert.SerializeObject(delivery))!;
        copy.Items[0].RetiredSpaceCredits.Should().Be(200);
        copy.Items[1].HullDesigns.Should().Equal("sd_republicstrik:10073");
        JsonConvert.DeserializeObject<QuestContractItem>("{}")!.HullDesigns.Should().BeNull("older items carry nothing");
    }

    [Test]
    public void ContractItemsAndObjectives_AreConvertedThroughTheEntitlementPath()
    {
        string Server(params string[] path) => File.ReadAllText(Path.Combine(new[] { RepositoryRoot(), "SWLOR.Game.Server" }.Concat(path).ToArray())).Replace("\r\n", "\n");

        var sweep = Server("Feature", "MigrationDefinition", "StoredItemSweep.cs");
        sweep.Should().Contain("changed |= space?.MigrateContractItem(item) ?? false;", "contract rewards and deliveries carry their entitlements");
        sweep.Should().Contain("(space != null && LegacySpaceItemSweep.MigrateObjectives(contract.Objectives))");

        var board = Server("Service", "QuestContractBoard.cs");
        var claim = board[board.IndexOf("public static void ClaimDeliveries(uint player)", StringComparison.Ordinal)..];
        claim.IndexOf("GrantCarriedEntitlements(player, itemData);", StringComparison.Ordinal)
            .Should().BeGreaterThan(claim.IndexOf("ObjectPlugin.AcquireItem(player, item)", StringComparison.Ordinal), "the claimant is granted them with the item");
        var removal = board[board.IndexOf("public static string RemoveRewardItem(uint player, int index)", StringComparison.Ordinal)..];
        removal = removal[..removal.IndexOf("public static string DeleteDraft(", StringComparison.Ordinal)];
        removal.Should().NotContain("GrantCarriedEntitlements(", "a removed reward is claimed through the delivery checkpoint, which grants them");
        var moved = removal.IndexOf("delivery.Items.Add(rewardItem);", StringComparison.Ordinal);
        moved.Should().BeGreaterThan(0);
        removal.IndexOf("DB.Set(delivery);", moved, StringComparison.Ordinal)
            .Should().BeLessThan(removal.IndexOf("draft.RewardItems.RemoveAt(index);", StringComparison.Ordinal), "the delivery holds the item before the draft lets it go");
        removal.IndexOf("ClaimDeliveries(player, SaveCharacterOrThrow, delivery.Id);", StringComparison.Ordinal)
            .Should().BeGreaterThan(removal.IndexOf("DB.Set(draft);", StringComparison.Ordinal));
        board.Should().Contain("GiveGoldToCreature(player, item.RetiredSpaceCredits);")
            .And.Contain("unlocked |= dbPlayer.UnlockedHullDesigns.TryAdd(design, DateTime.UtcNow);");

        var migration = Server("Feature", "MigrationDefinition", "ServerMigration", "_33_ConvertSpaceItems.cs");
        migration.IndexOf("QuestContractBoard.RefreshRegisteredContracts();", StringComparison.Ordinal)
            .Should().BeGreaterThan(migration.IndexOf("StoredItemSweep.Migrate(Version);", StringComparison.Ordinal),
                "contracts registered as quests at boot pick up their converted objectives");
    }

    [Test]
    public void AContractObjectiveForARetiredItem_AsksForTheItemItBecame()
    {
        var beam = LegacySpaceItemCatalog.Modules["beamcannon1"];
        var objectives = new List<QuestContractObjective>
        {
            new() { ItemResref = "beamcannon1", ItemName = "Beam Cannon I", Quantity = 2 },
            new() { ItemResref = "config_fig3", ItemName = "Fighter Configuration III", Quantity = 1 },
            new() { ItemResref = "ref_tilarium", ItemName = "Tilarium", Quantity = 5 },
        };

        LegacySpaceItemSweep.MigrateObjectives(objectives).Should().BeTrue();

        objectives.Select(x => (x.ItemResref, x.ItemName, x.Quantity)).Should().Equal(
            (beam, BlueprintName(beam), 2),
            ("config_fig3", "Fighter Configuration III", 1),
            ("ref_tilarium", "Tilarium", 5));
        LegacySpaceItemSweep.MigrateObjectives(objectives).Should().BeFalse("the conversion is idempotent");

        var shared = LegacySpaceItemCatalog.Modules.GroupBy(x => x.Value).FirstOrDefault(x => x.Count() > 1);
        if (shared != null)
        {
            var (a, b) = (shared.First().Key, shared.Skip(1).First().Key);
            var merged = new List<QuestContractObjective> { new() { ItemResref = a, Quantity = 3 }, new() { ItemResref = b, Quantity = 4 } };
            LegacySpaceItemSweep.MigrateObjectives(merged).Should().BeTrue();
            merged.Select(x => (x.ItemResref, x.Quantity)).Should().Equal((shared.Key, 7));
        }

        // An accepted contract's remaining counts follow, so the converted item can be turned in.
        var player = new Player { Name = "Pilot" };
        player.Quests["qcontract_c1"] = new PlayerQuest { ItemProgresses = { ["beamcannon1"] = 2, ["ref_tilarium"] = 5 } };
        player.Quests["some_quest"] = new PlayerQuest { ItemProgresses = { ["beamcannon1"] = 1 } };
        var migration = Activator.CreateInstance(CharacterMigrations.Named("_28_ConvertSpaceItems"))!;
        migration.GetType().GetMethod("MigratePlayerData")!.Invoke(migration, new object[] { player });
        player.Quests["qcontract_c1"].ItemProgresses.Should().BeEquivalentTo(new Dictionary<string, int> { [beam] = 2, ["ref_tilarium"] = 5 });
        player.Quests["some_quest"].ItemProgresses.Should().ContainKey("beamcannon1", "only player contracts are rebuilt from converted records");
    }

    [Test]
    public void APublishedContractForAConfigurationModule_IsWithdrawnAndRefundedOnce()
    {
        QuestContract Contract(string id, QuestContractStatus status, params string[] resrefs) => new()
        {
            Id = id,
            AuthorPlayerId = "author",
            Title = "Wanted",
            Status = status,
            CompletionsRemaining = 1,
            RewardCredits = 4000,
            RewardItems = { new QuestContractItem { Name = "Reward", Resref = "ref_tilarium", Data = Item("ref_tilarium") } },
            Objectives = resrefs.Select(x => new QuestContractObjective { ItemResref = x, Quantity = 1 }).ToList(),
        };

        var sweep = new LegacySpaceItemSweep();
        sweep.NoteContract(Contract("config", QuestContractStatus.Published, "ref_tilarium", "config_fig3"));
        sweep.NoteContract(Contract("beam", QuestContractStatus.Published, "beamcannon1"));
        sweep.NoteContract(Contract("draft", QuestContractStatus.Draft, "config_fig3"));
        sweep.NoteContract(Contract("closed", QuestContractStatus.Expired, "config_fig3"));
        sweep.UnachievableContracts.Should().Equal(new[] { "config" }, "only a published contract for a configuration module can never be completed");

        var contract = Contract("config", QuestContractStatus.Published, "config_fig3");
        var reward = contract.RewardItems[0];
        var fee = QuestContractBoard.CalculatePostingFee(contract.RewardCredits);
        var now = new DateTime(2026, 11, 23);

        var refund = QuestContractRetirement.Retire(contract, refundSaved: false, fee, LegacySpaceItemSweep.RetiredContractReason, now)!;

        refund.Id.Should().Be(QuestContractRetirement.RefundDeliveryId("config"), "a fixed id lets a rerun see the refund was saved");
        refund.PlayerId.Should().Be("author");
        refund.Credits.Should().Be(4000 + fee, "the escrowed reward and the posting fee go back to the author");
        refund.Items.Should().Equal(reward);
        refund.HeldForCompletion.Should().BeFalse();
        refund.IsRewardPayment.Should().BeFalse();
        QuestContractBoard.IsClaimable(refund).Should().BeTrue();
        contract.Status.Should().Be(QuestContractStatus.Cancelled, "a withdrawal is not a moderation takedown");
        QuestContractBoard.CanPostAgain(contract).Should().BeTrue("the author can repost it with the retired objective replaced");
        contract.TakedownReason.Should().Be(LegacySpaceItemSweep.RetiredContractReason).And.Match(x => x.All(c => c < 128));
        contract.DateClosed.Should().Be(now);
        contract.RewardItems.Should().BeEmpty();

        var retried = Contract("config", QuestContractStatus.Published, "config_fig3");
        QuestContractRetirement.Retire(retried, refundSaved: true, fee, LegacySpaceItemSweep.RetiredContractReason, now)
            .Should().BeNull("a rerun after the refund was saved refunds nothing again");
        retried.Status.Should().Be(QuestContractStatus.Cancelled);

        string Server(params string[] path) => File.ReadAllText(Path.Combine(new[] { RepositoryRoot(), "SWLOR.Game.Server" }.Concat(path).ToArray())).Replace("\r\n", "\n");
        var migrate = Server("Feature", "MigrationDefinition", "StoredItemSweep.cs");
        migrate.Should().Contain("space?.NoteContract(contract);");
        migrate.IndexOf("space.RetireUnachievableContracts();", StringComparison.Ordinal)
            .Should().BeGreaterThan(migrate.IndexOf("var paid = space.Apply(version);", StringComparison.Ordinal), "contracts are withdrawn after the pass saved them");
        var board = Server("Service", "QuestContractBoard.cs");
        var retire = board[board.IndexOf("public static void RetireContract(", StringComparison.Ordinal)..board.IndexOf("private static void VoidActiveContractQuests(", StringComparison.Ordinal)];
        retire.Should().Contain("if (contract == null || contract.Status != QuestContractStatus.Published)\n                return;", "a withdrawn contract is skipped on a rerun");
        var steps = new[] { "DB.Set(refund);", "DB.Set(contract);", "ReleaseSubmissions(contract.Id);",
            "Quest.UnregisterRuntimeQuest(QuestContractFactory.BuildQuestId(contract.Id));", "VoidActiveContractQuests(contract, playerReason);" };
        steps.Select(step => retire.IndexOf(step, StringComparison.Ordinal)).Should().BeInAscendingOrder().And.OnlyContain(i => i >= 0,
            "the refund is saved first, then the contract closes as a cancellation does: submissions back, quest unregistered, journals cleared");
    }

    [Test]
    public void TheSweep_PaysTheTillAndUnlocksDesigns()
    {
        var player = new Player();
        LegacySpaceItemSweep.Pay(player, 1500, new[] { "sd_republicstrik:10073" }).Should().BeTrue();
        player.MarketTill.Should().Be(1500);
        player.UnlockedHullDesigns.Should().ContainKey("sd_republicstrik:10073");
        player.UpgradeNotices.Should().ContainSingle(x => x.Contains("1500 credits"));
        LegacySpaceItemSweep.Pay(player, 0, new[] { "sd_republicstrik:10073" }).Should().BeFalse("nothing new is owed");
    }

    private static PlayerShip LegacyShip()
    {
        var ship = new PlayerShip
        {
            Id = "legacy-ship",
            OwnerPlayerId = "owner",
            SerializedItem = Item("sdeed_mule"),
            Status = new ShipStatus { ItemTag = "sdeed_mule", Hull = 30, MaxHull = 200 },
        };
        ship.PlayerHotBars["owner"] = "hotbar";
        ship.Status.ActiveModules.Add(new ShipStatus.ShipActiveModule { Slot = 1 });
        ship.Status.HighPowerModules[1] = Module("com_laser_b", 3);
        ship.Status.HighPowerModules[2] = Module("ion_cann_1");
        ship.Status.HighPowerModules[3] = Module("msl_launch_2");
        ship.Status.HighPowerModules[4] = Module("beamcannon5");
        ship.Status.HighPowerModules[5] = Module("turbolas1");
        ship.Status.LowPowerModules[1] = Module("shld_boost_b");
        ship.Status.LowPowerModules[2] = Module("hull_boost_3");
        ship.Status.LowPowerModules[3] = Module("hvy_armor_2");
        ship.Status.LowPowerModules[4] = Module("cap_boost_1");
        ship.Status.LowPowerModules[5] = Module("hull_rep_4");
        ship.Status.ConfigurationModules[1] = Module("config_fig2");
        return ship;
    }

    private static ShipStatus.ShipStatusModule Module(string resref, int moduleBonus = 0) => new()
    {
        ItemInstanceId = "id-" + resref,
        ItemTag = resref,
        ModuleBonus = moduleBonus,
        SerializedItem = Item(resref, extraProperties: moduleBonus > 0 ? new[] { Property(ModuleBonus, 65535, 0, moduleBonus) } : null),
        RecastTime = DateTime.UtcNow.AddMinutes(5),
    };

    private sealed record SavedItemView(string Resref, string Tag, string Name, int Cost, string? Uuid, int X, int Y,
        List<(int Type, int Subtype, int Value)> Properties, List<(string, int)> Locals, Dictionary<string, string> StringLocals);

    private static SavedItemView Read(string data) => Describe(GffReader.Read(Convert.FromBase64String(data)).RootStruct);

    private static SavedItemView Describe(GffStruct item)
    {
        object? Get(string label) => item.Fields.FirstOrDefault(x => x.Label == label)?.Value;
        int Int(GffStruct node, string label) => Convert.ToInt32(node.Fields.First(x => x.Label == label).Value);
        var properties = (Get("PropertiesList") as GffList)?.Elements
            .Select(p => (Int(p, "PropertyName"), Int(p, "Subtype"), Int(p, "CostValue"))).ToList() ?? new();
        var variables = (Get("VarTable") as GffList)?.Elements ?? new List<GffStruct>();
        var name = Get("LocalizedName") as CExoLocString;
        return new SavedItemView(
            (string)Get("TemplateResRef")!, (string)Get("Tag")!, name?.LocalizedStrings.Values.FirstOrDefault() ?? string.Empty,
            Convert.ToInt32(Get("Cost") ?? 0), Get("UUID") as string,
            Convert.ToInt32(Get("Repos_PosX") ?? -1), Convert.ToInt32(Get("Repos_Posy") ?? -1), properties,
            variables.Where(v => Int(v, "Type") == 1).Select(v => ((string)v.Fields.First(f => f.Label == "Name").Value!, Int(v, "Value"))).ToList(),
            variables.Where(v => Int(v, "Type") == 3).ToDictionary(v => (string)v.Fields.First(f => f.Label == "Name").Value!,
                v => (string)v.Fields.First(f => f.Label == "Value").Value!));
    }

    [Test]
    public void TheSpaceSweep_AlsoConvertsOverflowStorage_AndRefundsAStoredConfigurationModule()
    {
        // Retired ship deeds and modules an earlier character upgrade put in overflow storage wait there unclaimed.
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent!;
        string Read(string name) => File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "MigrationDefinition", name)).Replace("\r\n", "\n");

        var sweep = Read("StoredItemSweep.cs");
        sweep.Should().Contain("var progress = new ServerMigrationProgress(version, CountSections(space != null));");
        sweep.Should().Contain("return overflow ? sections.Append(ServerMigrationProgress.CountRecords<OverflowItem>()).ToArray() : sections;");
        sweep.Should().Contain("if (space != null)\n            changed += pass.Process<OverflowItem>(item => space.Migrate(item.Data, value => item.Data = value, item.PlayerId,");
        sweep.Should().Contain("credits => space.QueueRemoval<OverflowItem>(item.Id, item.PlayerId, credits)), space.Flush);");

        var legacy = Read("LegacySpaceItemSweep.cs");
        legacy.Should().Contain("recordType != typeof(OverflowItem)").And.Contain("DB.Delete<OverflowItem>(id);");
    }
}
