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

        var migration = Activator.CreateInstance(CharacterMigrations.Named("_27_ConvertSpaceItems"))!;
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

        var migration = Activator.CreateInstance(CharacterMigrations.Named("_27_ConvertSpaceItems"))!;
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
    public void TheSweep_PaysTheTillAndUnlocksDesigns()
    {
        var player = new Player();
        LegacySpaceItemSweep.Pay(player, 1500, new[] { "sd_republicstrik:10073" }).Should().BeTrue();
        player.MarketTill.Should().Be(1500);
        player.UnlockedHullDesigns.Should().ContainKey("sd_republicstrik:10073");
        player.UpgradeNotices.Should().ContainSingle(x => x.Contains("1500 credits"));
        LegacySpaceItemSweep.Pay(player, 0, new[] { "sd_republicstrik:10073" }).Should().BeFalse("nothing new is owed");
    }

    [Test]
    public void RetiredPilotingPerkStamps_NeverBecomeSkillRequirements()
    {
        var method = typeof(LegacySpaceItemCatalog).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.EquipmentRequirementMigration")!
            .GetMethod("TryGetSkillRequirement", Members)!;
        foreach (var perk in new[] { 116, 117, 118, 119, 120, 121, 255 })
        {
            var args = new object?[] { perk, 3, BaseItem.CreatureItem, "x", null, null };
            ((bool)method.Invoke(null, args)!).Should().BeFalse($"perk {perk} stamps belong to retired items, which are replaced whole");
        }
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
}
