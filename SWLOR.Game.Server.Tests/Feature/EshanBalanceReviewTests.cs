using System.Text;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class EshanBalanceReviewTests
{
    private object _originalItemNames = null!;
    private static PropertyInfo ItemNameCache => typeof(Cache)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)!;

    [OneTimeSetUp]
    public void LoadQuestRewardNamesWithoutTheEngine()
    {
        var names = Directory.EnumerateFiles(Path.Combine(Root(), "Module/uti"), "*.uti.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".uti.json".Length], path =>
                JObject.Parse(File.ReadAllText(path, Encoding.Latin1))["LocalizedName"]?["value"]?["0"]?.Value<string>() ?? "");
        _originalItemNames = ItemNameCache.GetValue(null)!;
        ItemNameCache.SetValue(null, names);
    }

    [OneTimeTearDown]
    public void RestoreQuestRewardNames() => ItemNameCache.SetValue(null, _originalItemNames);

    private static readonly string[] Hostiles =
    [
        "esh_direwolf", "esh_frostwolf", "esh_wolfalpha", "esh_gorakvesh", "esh_nc_scout",
        "esh_nc_hunter", "esh_nc_vanguard", "esh_nc_heavy", "esh_nc_medic", "esh_nc_captain",
        "esh_sunguard", "esh_scrap_smug", "esh_veinwolf", "esh_veinboss", "esh_echaolm"
    ];

    [Test]
    public void HostileLoot_IsReachableAndUsesPeerMusicChances()
    {
        var tables = new EshanLootTableDefinition().BuildLootTables();
        foreach (var resref in Hostiles)
        {
            var locals = Read($"Module/utc/{resref}.utc.json")["VarTable"]!["value"]!
                .Where(local => Value(local, "Name").StartsWith("LOOT_TABLE_", StringComparison.Ordinal))
                .ToArray();
            locals.Select(local => int.Parse(Value(local, "Name")["LOOT_TABLE_".Length..])).Order()
                .Should().Equal(Enumerable.Range(1, locals.Length), $"{resref}: loot stops at a missing index");
            var rolls = locals.Select(local => Value(local, "Value")).ToArray();
            rolls.Should().Contain(resref == "esh_veinboss" ? "MUSIC_DISCS_BOSS,5,1" : "MUSIC_DISCS,1,1");
            foreach (var roll in rolls.Where(roll => roll.StartsWith("ESHAN_", StringComparison.Ordinal)))
                tables.Should().ContainKey(roll.Split(',')[0]);
        }
    }

    [Test]
    public void Warden_UsesTheLevel45BossRespawnAndDedicatedRareLoot()
    {
        var boss = new EshanSpawnDefinition().BuildSpawnTables()["ESHAN_SHATTERVEIN_WARDEN"];
        var peer = new TatooineSpawnDefinition().BuildSpawnTables()["TATOOINE_TUSKEN_WARLORD"];
        boss.RespawnDelayMinutes.Should().Be(peer.RespawnDelayMinutes);
        boss.RespawnDelayMaximumMinutes.Should().Be(peer.RespawnDelayMaximumMinutes);
        var rolls = Read("Module/utc/esh_veinboss.utc.json")["VarTable"]!["value"]!
            .Where(local => Value(local, "Name").StartsWith("LOOT_TABLE_", StringComparison.Ordinal))
            .Select(local => Value(local, "Value")).ToArray();
        rolls.Should().Contain("ESHAN_WARDEN_RARES,30,1");
        rolls.Should().Contain("ESHAN_WARDEN_FIBERPLAST,100,1");
        new EshanLootTableDefinition().BuildLootTables()["ESHAN_WARDEN_RARES"].IsRare.Should().BeTrue();
    }

    [Test]
    public void Gorak_UsesTheWarlordRespawnOnItsOwnSilverwoodSpawnPoint()
    {
        var tables = new EshanSpawnDefinition().BuildSpawnTables();
        var gorak = tables["ESHAN_GORAK_VESH"];
        var peer = new TatooineSpawnDefinition().BuildSpawnTables()["TATOOINE_TUSKEN_WARLORD"];
        gorak.Spawns.Should().ContainSingle(spawn => spawn.Resref == "esh_gorakvesh" && !spawn.IsRare);
        gorak.RespawnDelayMinutes.Should().Be(peer.RespawnDelayMinutes);
        gorak.RespawnDelayMaximumMinutes.Should().Be(peer.RespawnDelayMaximumMinutes);
        tables["ESHAN_SILVERWOOD_EXPANSE"].Spawns.Should().NotContain(spawn => spawn.Resref == "esh_gorakvesh");
        Read("Module/git/pw_sc_eshanwilds.git.json")["WaypointList"]!["value"]!
            .Should().ContainSingle(waypoint => Value(waypoint, "Tag") == "ESHAN_GORAK_VESH");
    }

    [TestCase("esh_wolf_pelt", "esh_wolfalpha:ESHAN_DIRE_WOLF_ALPHA_COMP,20,1", "esh_veinboss:ESHAN_DIRE_WOLF_ALPHA_COMP,100,1")]
    [TestCase("esh_frost_fang", "esh_wolfalpha:ESHAN_DIRE_WOLF_ALPHA_COMP,20,1", "esh_veinboss:ESHAN_DIRE_WOLF_ALPHA_COMP,100,1")]
    [TestCase("esh_mando_salv", "esh_nc_captain:ESHAN_NEOCRUSADER_CAPTAIN_COMP,10,1")]
    [TestCase("esh_sun_insignia", "esh_nc_captain:ESHAN_NEOCRUSADER_CAPTAIN_COMP,10,1")]
    [TestCase("bp_rec_scope", "esh_nc_captain:ESHAN_NEOCRUSADER_CAPTAIN_RARES,10,1")]
    [TestCase("bp_rec_warhelm", "esh_nc_captain:ESHAN_NEOCRUSADER_CAPTAIN_RARES,10,1")]
    public void PatternMaterialsAndBooks_DropOnlyFromElitesAndTheWarden(string item, params string[] expectedRolls)
    {
        var tables = new EshanLootTableDefinition().BuildLootTables();
        var rolls = Hostiles.SelectMany(resref => Read($"Module/utc/{resref}.utc.json")["VarTable"]!["value"]!
                .Where(local => Value(local, "Name").StartsWith("LOOT_TABLE_", StringComparison.Ordinal))
                .Select(local => Value(local, "Value"))
                .Where(roll => tables.TryGetValue(roll.Split(',')[0], out var table) &&
                               table.Any(entry => entry.Resref == item))
                .Select(roll => $"{resref}:{roll}"))
            .ToArray();
        rolls.Should().BeEquivalentTo(expectedRolls);
    }

    [Test]
    public void KillRewards_FollowExistingBountyRatesRatherThanIntroducingAHigherTier()
    {
        var standard = new DantooineQuestDefinition().BuildQuests()["voritor_lizard_threat"];
        var count = standard.States[1].GetObjectives().OfType<KillTargetObjective>().Single().Amount;
        var xpPerKill = standard.Rewards.OfType<XPReward>().Single().Amount / count;
        var goldPerKill = standard.Rewards.OfType<GoldReward>().Single().Amount / count;
        var elite = new KorribanQuestlineDefinition().BuildQuests()["prove_dominance"];
        var quests = new EshanQuestDefinition().BuildQuests();
        quests.Should().HaveCount(12);
        foreach (var quest in quests.Values)
        {
            var objective = quest.States[1].GetObjectives().OfType<KillTargetObjective>().Single();
            quest.Rewards.OfType<XPReward>().Single().Amount.Should().Be(objective.Amount == 1
                ? elite.Rewards.OfType<XPReward>().Single().Amount : xpPerKill * objective.Amount);
            quest.Rewards.OfType<GoldReward>().Single().Amount.Should().Be(objective.Amount == 1
                ? elite.Rewards.OfType<GoldReward>().Single().Amount : goldPerKill * objective.Amount);
            quest.IsRepeatable.Should().BeFalse();
        }
    }

    [TestCase("ESHAN_THYRSIAN_EXILE")]
    [TestCase("ESHAN_SCRAPYARD_SMUGGLER")]
    public void RegularCreditDrops_DoNotExceedTheFoundryCoreMaximum(string table)
    {
        var gold = new EshanLootTableDefinition().BuildLootTables()[table].Single(item => item.Resref == "nw_it_gold001");
        var peer = new HutlarLootTableDefinition().BuildLootTables()["HUTLAR_FOUNDRY_CORE"]
            .Single(item => item.Resref == "nw_it_gold001");
        gold.MaxQuantity.Should().BeLessThanOrEqualTo(peer.MaxQuantity);
    }

    [TestCase(RecipeType.SilverpeltHideCuirass, RecipeType.SunCuredHideCuirass, "esh_wolf_pelt", "sun_bantha_hide", "bp_silverpelt")]
    [TestCase(RecipeType.FrostFangGauntlet, RecipeType.SalvagedFangGauntlet, "esh_frost_fang", "alpha_fang", "bp_frostgaunt")]
    [TestCase(RecipeType.RidgedHideMantle, RecipeType.WindcallerMantle, "esh_gorak_hide", "bone_fetish", "bp_ridgemantle")]
    [TestCase(RecipeType.ReclaimedScopewrightCap, RecipeType.ScopewrightCap, "esh_mando_salv", "cycler_scope", "bp_rec_scope")]
    [TestCase(RecipeType.ReclaimedWarbandHelm, RecipeType.WarbandHelm, "esh_sun_insignia", "bone_totem", "bp_rec_warhelm")]
    public void RetainedDrops_HaveLearnableRecipesAtTheExistingEquipmentCost(
        RecipeType recipeType, RecipeType baselineType, string component, string baselineComponent, string blueprint)
    {
        var recipe = new ColdweatherFieldGearRecipes().BuildRecipes()[recipeType];
        var peers = new TuskenWarGearRecipes().BuildRecipes()
            .Concat(new SalvagedFieldGearRecipes().BuildRecipes()).ToDictionary(pair => pair.Key, pair => pair.Value);
        var baseline = peers[baselineType];
        (recipe.Resref, recipe.Level, recipe.Quantity, recipe.Category, recipe.EnhancementType, recipe.EnhancementSlots)
            .Should().Be((baseline.Resref, baseline.Level, baseline.Quantity, baseline.Category,
                baseline.EnhancementType, baseline.EnhancementSlots));
        var expected = new Dictionary<string, int>(baseline.Components);
        expected.Remove(baselineComponent).Should().BeTrue();
        expected.Add(component, baseline.Components[baselineComponent]);
        recipe.Components.Should().BeEquivalentTo(expected);
        recipe.Requirements.Should().ContainSingle(requirement => requirement is RecipeUnlockRequirement);
        var item = Read($"Module/uti/{blueprint}.uti.json");
        Value(item, "Tag").Should().Be("RECIPE");
        item["VarTable"]!["value"]!.Single(local => Value(local, "Name") == "RECIPES")["Value"]!["value"]!
            .Value<string>().Should().Be(((int)recipeType).ToString());
        new EshanLootTableDefinition().BuildLootTables().Values.SelectMany(table => table)
            .Should().Contain(item => item.Resref == blueprint && item.IsRare);
        Read("Module/itp/itempalcus.itp.json").Descendants().OfType<JProperty>()
            .Where(property => property.Name == "RESREF").Select(property => property.Value["value"]!.Value<string>())
            .Should().Contain(blueprint);
    }

    [Test]
    public void Starport_OffersMedicalRegistrationAndTheGlobalMarketThroughUsableTerminals()
    {
        var terminals = Read("Module/git/pwsc_eshstarport.git.json")["Placeable List"]!["value"]!
            .Where(terminal => Value(terminal, "Tag").StartsWith("esh_med_", StringComparison.Ordinal) ||
                Value(terminal, "Tag").StartsWith("esh_market_", StringComparison.Ordinal)).ToArray();
        terminals.Should().HaveCount(3);
        foreach (var terminal in terminals)
        {
            terminal["Useable"]!["value"]!.Value<int>().Should().Be(1);
            terminal["Static"]!["value"]!.Value<int>().Should().Be(0);
            terminal["Plot"]!["value"]!.Value<int>().Should().Be(1);
            var locals = terminal["VarTable"]!["value"]!.ToDictionary(local => Value(local, "Name"), local => Value(local, "Value"));
            if (Value(terminal, "Tag") == "esh_med_reg")
            {
                locals["CONVERSATION"].Should().Be("MedicalRegistrationDialog");
                Value(terminal, "OnUsed").Should().Be("generic_convo");
            }
            else
            {
                locals["CONVERSATION"].Should().Be("MarketDialog");
                locals["MARKET_ID"].Should().Be("1");
                Value(terminal, "OnUsed").Should().Be("dialog_start");
            }
        }
    }

    [Test]
    public void NativeCombatMetadata_UsesStandardClassLevelsAndAnUntypedElectroblade()
    {
        foreach (var resref in new[] { "esh_veinwolf", "esh_veinboss" })
            Read($"Module/utc/{resref}.utc.json")["ClassList"]!["value"]![0]!["ClassLevel"]!["value"]!
                .Value<int>().Should().Be(40);
        Read("Module/uti/esh_ncvang_wp.uti.json")["PropertiesList"]!["value"]!
            .Should().NotContain(property => property["PropertyName"]!["value"]!.Value<int>() == 134);
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        return directory!.FullName;
    }

    private static JObject Read(string path) => JObject.Parse(File.ReadAllText(Path.Combine(Root(), path), Encoding.Latin1));
    private static string Value(JToken token, string field) => token[field]?["value"]?.ToString() ?? "";
}
