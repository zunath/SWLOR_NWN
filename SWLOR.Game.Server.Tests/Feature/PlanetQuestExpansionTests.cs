using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Guards the quest lines added to every planet: their size, their
/// pay scale, their reward items, the named kill targets and the placed quest givers.
/// </summary>
[NonParallelizable]
public class PlanetQuestExpansionTests
{
    private object _originalItemNames = null!;
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    private static PropertyInfo ItemNameCache => typeof(CacheService)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Instance)!;

    [OneTimeSetUp]
    public void LoadItemNamesForRewardTitles()
    {
        _facades = new FacadeContainer();
        _facadeScope = _facades.Install();
        Guild.LoadData();
        NPCGroup.CacheData();
        var names = Directory.EnumerateFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".uti.json".Length], path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.GetProperty("LocalizedName").GetProperty("value")
                    .TryGetProperty("0", out var name) ? name.GetString() ?? string.Empty : string.Empty;
            });
        _originalItemNames = ItemNameCache.GetValue(Cache)!;
        ItemNameCache.SetValue(Cache, names);
    }

    [OneTimeTearDown]
    public void RestoreItemNames()
    {
        ItemNameCache.SetValue(Cache, _originalItemNames);
        _facadeScope.Dispose();
        _facades.Dispose();
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        }
    }

    private static JsonElement Load(string type, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return document.RootElement.Clone();
    }

    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => Value(o, key).GetString()!;

    private static Dictionary<string, string> Locals(JsonElement o) =>
        o.TryGetProperty("VarTable", out var table)
            ? table.GetProperty("value").EnumerateArray().ToDictionary(v => Text(v, "Name"), v => Value(v, "Value").ToString())
            : new Dictionary<string, string>();

    private static string Resref(ItemReward reward) => (string)typeof(ItemReward)
        .GetField("_resref", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(reward)!;

    private static Dictionary<string, QuestDetail> NewQuests() =>
        new MonCalaQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("mon_") && q.Key != "mon_starlane")
            .Concat(new DathomirQuestDefinition().BuildQuests())
            .Concat(new TatooineQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("tat_")))
            .Concat(new HutlarQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("hut_") && q.Key != "hut_power_invest"))
            .Concat(new CZ220QuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("cz_")))
            .Concat(new ViscaraQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("vis_")))
            .Concat(new KorribanQuestlineDefinition().BuildQuests().Where(q => q.Key.StartsWith("kor_")))
            .Concat(new NarShaddaaQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("ns_")))
            .Concat(new DantooineQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("dan_")))
            .Concat(new EshanQuestDefinition().BuildQuests().Where(q => q.Key.StartsWith("esh_")))
            .ToDictionary(q => q.Key, q => q.Value);

    [TestCase("mon_")]
    [TestCase("dath_")]
    [TestCase("tat_")]
    [TestCase("hut_")]
    [TestCase("cz_")]
    [TestCase("vis_")]
    [TestCase("kor_")]
    [TestCase("ns_")]
    [TestCase("dan_")]
    [TestCase("esh_")]
    public void EachPlanet_GainsAtLeastTwentyQuests(string prefix)
    {
        NewQuests().Keys.Count(id => id.StartsWith(prefix)).Should().BeGreaterThanOrEqualTo(20);
    }

    // Level 30+ zones: Workin' for the Man and Shut Down the Line (20 kills, 750 credits, 1,750 XP).
    // Level 6-30 zones: Put Them to Rest (20 kills, 400 credits, 950 XP). CZ-220 scales that down for level 1.
    [TestCase("mon_swamp_sweep", 20, 750, 1750)]
    [TestCase("mon_reef_patrol", 20, 750, 1750)]
    [TestCase("dath_deep_sweep", 20, 750, 1750)]
    [TestCase("dath_jungle_watch", 20, 750, 1750)]
    [TestCase("tat_desert_sweep", 20, 750, 1750)]
    [TestCase("hut_tundra_watch", 20, 750, 1750)]
    [TestCase("ns_street_sweep", 20, 750, 1750)]
    [TestCase("ns_gang_sweep", 20, 750, 1750)]
    [TestCase("dan_plains_patrol", 20, 750, 1750)]
    [TestCase("dan_south_patrol", 20, 750, 1750)]
    [TestCase("esh_marches_patrol", 20, 750, 1750)]
    [TestCase("esh_garrison_patrol", 20, 750, 1750)]
    [TestCase("esh_pack_patrol", 20, 750, 1750)]
    [TestCase("esh_smuggler_sweep", 20, 750, 1750)]
    [TestCase("vis_outlaw_patrol", 20, 400, 950)]
    [TestCase("vis_wilds_patrol", 20, 400, 950)]
    [TestCase("kor_wastes_patrol", 20, 400, 950)]
    [TestCase("cz_pest_patrol", 10, 50, 150)]
    [TestCase("cz_droid_sweep", 10, 50, 150)]
    public void RepeatableSweep_PaysItsZonesRepeatableRate(string questId, int kills, int gold, int xp)
    {
        var quest = NewQuests()[questId];
        quest.IsRepeatable.Should().BeTrue();
        quest.States[1].GetObjectives().OfType<KillTargetObjective>().Sum(o => o.Amount).Should().Be(kills);
        quest.Rewards.Should().HaveCount(2);
        quest.Rewards.OfType<GoldReward>().Single().Amount.Should().Be(gold);
        quest.Rewards.OfType<XPReward>().Single().Amount.Should().Be(xp);
    }

    [Test]
    public void EveryRepeatableQuest_HasAPinnedRate()
    {
        NewQuests().Values.Count(q => q.IsRepeatable).Should().Be(19, "each repeatable is listed in RepeatableSweep_PaysItsZonesRepeatableRate");
    }

    [Test]
    public void OneTimeQuests_StayInsideTheExistingRewardScale()
    {
        using var scope = new AssertionScope();
        foreach (var quest in NewQuests().Values.Where(q => !q.IsRepeatable))
        {
            // Bounded by the existing planet quests: Slaver Convoy pays the most (15,000 XP, 12,000 credits).
            quest.Rewards.OfType<XPReward>().Single().Amount.Should().BeInRange(200, 15000, quest.QuestId);
            quest.Rewards.OfType<GoldReward>().Sum(r => r.Amount).Should().BeInRange(0, 12000, quest.QuestId);
            quest.States.Count.Should().BeInRange(1, 2, quest.QuestId);
        }
    }

    [TestCase("reefwarden_band", "poach_ring", 52)]
    [TestCase("tidecall_pendant", "seeker_necklace", 19)]
    [TestCase("kwi_spirit_charm", "kwi_hyper_neck", 19)]
    [TestCase("canopy_band", "kwi_light_ring", 52)]
    [TestCase("rancorhide_belt", "kwi_heavy_belt", 21)]
    [TestCase("caravan_sash", "kwi_light_belt", 21)]
    [TestCase("hunter_vambrace", "kwi_heavy_bracer", 78)]
    [TestCase("chieftain_torc", "kwi_heavy_neck", 19)]
    [TestCase("broodguard_ring", "kwi_heavy_ring", 52)]
    [TestCase("fdybreak_bracer", "kwi_heavy_bracer", 78)]
    public void RewardAccessory_CopiesTheStatsOfAnExistingSetPiece(string reward, string setPiece, int baseItem)
    {
        var item = Load("uti", reward);
        var source = Load("uti", setPiece);

        Value(item, "BaseItem").GetInt32().Should().Be(baseItem, "the name describes this slot");
        Value(item, "PropertiesList").GetRawText().Should().Be(Value(source, "PropertiesList").GetRawText(),
            $"{reward} must not out-stat the dropped {setPiece}");
        Value(item, "Cost").GetInt32().Should().Be(Value(source, "Cost").GetInt32());
        Value(item, "ModelPart1").GetInt32().Should().NotBe(Value(source, "ModelPart1").GetInt32(), "the reward has its own look");

        NewQuests().Values.Count(q => q.Rewards.OfType<ItemReward>().Any(r => Resref(r) == reward))
            .Should().Be(1, "each accessory is the reward of exactly one quest");
        NewQuests().Values.Single(q => q.Rewards.OfType<ItemReward>().Any(r => Resref(r) == reward))
            .IsRepeatable.Should().BeFalse();
    }

    [Test]
    public void RewardAccessories_EachHaveTheirOwnAppearance()
    {
        var rewards = new[]
        {
            "reefwarden_band", "tidecall_pendant", "kwi_spirit_charm", "canopy_band", "rancorhide_belt",
            "caravan_sash", "hunter_vambrace", "chieftain_torc", "broodguard_ring", "fdybreak_bracer"
        };
        rewards.Select(r => (Value(Load("uti", r), "BaseItem").GetInt32(), Value(Load("uti", r), "ModelPart1").GetInt32()))
            .Should().OnlyHaveUniqueItems();
    }

    // Named targets keep the group their existing quests and guild tasks count, and add a second one.
    [TestCase("ecoterr_ldr", NPCGroupType.MonCala_EcoTerrorist, NPCGroupType.MonCala_EcoTerroristLeader)]
    [TestCase("sable_quarr", NPCGroupType.MonCala_EcoTerrorist, NPCGroupType.MonCala_SableQuarr)]
    [TestCase("kael_drox", NPCGroupType.MonCala_EcoTerrorist, NPCGroupType.MonCala_KaelDrox)]
    [TestCase("reefmaw", NPCGroupType.MonCala_Aradile, NPCGroupType.MonCala_ReefmawTidebreaker)]
    [TestCase("glassjaw", NPCGroupType.MonCala_Scorchellus, NPCGroupType.MonCala_GlassjawStalker)]
    [TestCase("inkveil", NPCGroupType.MonCala_Octotench, NPCGroupType.MonCala_InkveilMatron)]
    [TestCase("vdathchirodac", NPCGroupType.Dathomir_Chirodactyl, NPCGroupType.Dathomir_GrottoChirodactyl)]
    [TestCase("vdathdarkadept", NPCGroupType.Dathomir_Chirodactyl, NPCGroupType.Dathomir_DarkSideAdept)]
    [TestCase("vdatthrancor", NPCGroupType.Dathomir_Chirodactyl, NPCGroupType.Dathomir_Rancor)]
    [TestCase("tusken_elite1", NPCGroupType.Tatooine_TuskenRaider, NPCGroupType.Tatooine_TuskenElite)]
    [TestCase("tusken_elite2", NPCGroupType.Tatooine_TuskenRaider, NPCGroupType.Tatooine_TuskenElite)]
    [TestCase("tuskwarlord", NPCGroupType.Tatooine_TuskenRaider, NPCGroupType.Tatooine_TuskenWarlord)]
    [TestCase("tuskchampion", NPCGroupType.Tatooine_TuskenRaider, NPCGroupType.Tatooine_UrrokGaderffiiChampion)]
    [TestCase("tusklongeye", NPCGroupType.Tatooine_TuskenRaider, NPCGroupType.Tatooine_SharrakLongEye)]
    [TestCase("tuskwarcaller", NPCGroupType.Tatooine_TuskenRaider, NPCGroupType.Tatooine_WarcallerAarkha)]
    [TestCase("vtattkrayt", NPCGroupType.Dathomir_Chirodactyl, NPCGroupType.Tatooine_KraytDragon)]
    [TestCase("vtattbountyhunt", NPCGroupType.Dathomir_Chirodactyl, NPCGroupType.Tatooine_PeerlessBountyHunter)]
    [TestCase("fdy_smelter", NPCGroupType.Hutlar_FoundryDroid, NPCGroupType.Hutlar_FoundrySmelterHulk)]
    [TestCase("fdy_forgewd", NPCGroupType.Hutlar_FoundryDroid, NPCGroupType.Hutlar_FoundryForgeWarden)]
    [TestCase("fdy_overseer", NPCGroupType.Hutlar_FoundryDroid, NPCGroupType.Hutlar_FoundryOverseer)]
    [TestCase("fdy_sorter", NPCGroupType.Hutlar_FoundryDroid, NPCGroupType.Hutlar_SorterPrime)]
    [TestCase("fdy_foreman", NPCGroupType.Hutlar_FoundryDroid, NPCGroupType.Hutlar_ForemanRX12)]
    [TestCase("fdy_inspector", NPCGroupType.Hutlar_FoundryDroid, NPCGroupType.Hutlar_InspectorQ0)]
    [TestCase("man_hunter", NPCGroupType.Viscara_MandalorianRangers, NPCGroupType.Viscara_MandalorianHunters)]
    [TestCase("nara_venn", NPCGroupType.Viscara_WildwoodsOutlaws, NPCGroupType.Viscara_NaraVenn)]
    [TestCase("soot_rusk", NPCGroupType.Viscara_WildwoodsOutlaws, NPCGroupType.Viscara_SootlineRusk)]
    [TestCase("korr_frostbind", NPCGroupType.Korriban_SithApprenticeGhost, NPCGroupType.Korriban_SithFrostbinder)]
    [TestCase("dantarihunter", NPCGroupType.Invalid, NPCGroupType.Dantooine_DantariHunter)]
    [TestCase("dgraul", NPCGroupType.Dathomir_Chirodactyl, NPCGroupType.Dantooine_Graul)]
    public void NamedTarget_KeepsItsGroupAndAddsASecond(string resref, NPCGroupType primary, NPCGroupType second)
    {
        var locals = Locals(Load("utc", resref));
        locals["QUEST_NPC_GROUP_ID"].Should().Be(((int)primary).ToString());
        locals["QUEST_NPC_GROUP_ID_2"].Should().Be(((int)second).ToString());
    }

    [TestCase("vgapingspider", NPCGroupType.Dathomir_GapingSpider)]
    [TestCase("sandworm", NPCGroupType.Tatooine_SandWorm)]
    [TestCase("byysk_shaman", NPCGroupType.Hutlar_ByyskShaman)]
    [TestCase("byysk_cryoadept", NPCGroupType.Hutlar_ByyskCryoAdept)]
    [TestCase("byysk_champion", NPCGroupType.Hutlar_ByyskChampion)]
    [TestCase("byysk_chieftain", NPCGroupType.Hutlar_ByyskChieftain)]
    [TestCase("qion_slug001", NPCGroupType.Hutlar_QionHiveSlug)]
    [TestCase("qion_hive_tunnel", NPCGroupType.Hutlar_QionHiveTunneler)]
    [TestCase("huthivebroodmoth", NPCGroupType.Hutlar_QionHiveBroodmother)]
    [TestCase("man_scout", NPCGroupType.Viscara_MandalorianScouts)]
    [TestCase("tukata", NPCGroupType.Korriban_TukataHound)]
    [TestCase("vkorrdun1rifle", NPCGroupType.Korriban_FortressGuard)]
    [TestCase("vkorrdun1sword", NPCGroupType.Korriban_FortressGuard)]
    [TestCase("vkorrdunmarauder", NPCGroupType.Korriban_FortressMarauder)]
    [TestCase("vkorrdunsorc", NPCGroupType.Korriban_FortressMarauder)]
    [TestCase("vkorrdungate", NPCGroupType.Korriban_TempleGuardian)]
    [TestCase("vkorrdun4boss", NPCGroupType.Korriban_TempleMaster)]
    [TestCase("vkorrdunwarform", NPCGroupType.Korriban_PrototypeWarform)]
    [TestCase("vkorrduncouncilg", NPCGroupType.Korriban_CouncilGuardian)]
    [TestCase("nar_redblade", NPCGroupType.NarShaddaa_RedBlades)]
    [TestCase("nar_serpent", NPCGroupType.NarShaddaa_BlackSerpents)]
    [TestCase("nar_hiddenblade", NPCGroupType.NarShaddaa_HiddenBlades)]
    [TestCase("nar_troublemaker", NPCGroupType.NarShaddaa_Troublemakers)]
    [TestCase("nar_arenafight", NPCGroupType.NarShaddaa_ArenaFighters)]
    [TestCase("nar_serp_leader", NPCGroupType.NarShaddaa_SerpentLeader)]
    [TestCase("nar_rogue_droid", NPCGroupType.NarShaddaa_RogueDroids)]
    [TestCase("nar_scavenger", NPCGroupType.NarShaddaa_ScavengerDroids)]
    [TestCase("nar_thief", NPCGroupType.NarShaddaa_Thieves)]
    [TestCase("hkinrath", NPCGroupType.Dantooine_HiveKinrath)]
    [TestCase("thune", NPCGroupType.Dantooine_HerdLeaderThune)]
    public void PreviouslyUngroupedTarget_GainsItsGroup(string resref, NPCGroupType group)
    {
        Locals(Load("utc", resref))["QUEST_NPC_GROUP_ID"].Should().Be(((int)group).ToString());
    }

    [Test]
    public void SecondGroups_AreNeverUsedByGuildTasks()
    {
        // A hunting task that counted a second group would double-credit kills the first group already counts.
        var second = Directory.EnumerateFiles(Path.Combine(Root, "Module", "utc"), "*.utc.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement)
            .Select(Locals).Where(l => l.ContainsKey("QUEST_NPC_GROUP_ID_2"))
            .Select(l => int.Parse(l["QUEST_NPC_GROUP_ID_2"])).ToHashSet();
        second.Should().HaveCount(28);

        new HuntersGuildQuestDefinition().BuildQuests().Values
            .SelectMany(q => q.States.Values.SelectMany(s => s.GetObjectives()).OfType<KillTargetObjective>())
            .Select(o => (int)o.Group).Should().NotIntersectWith(second);
    }

    [TestCase("MONCALA_CORAL_CANISTERS", "ecoterr_1", "ecoterr_2")]
    [TestCase("DATHOMIR_KWI_FETISHES", "vdathshaman")]
    [TestCase("TATOOINE_WAR_BANNERS", "tusken_elite1", "tusken_elite2")]
    [TestCase("HUTLAR_FOUNDRY_MEMORY_CORES", "fdy_hauler", "fdy_sentry", "fdy_welder", "fdy_linedef", "fdy_riveter", "fdy_reclaim", "fdy_wardroid", "fdy_siege", "fdy_hunter")]
    public void QuestDrops_HaveTheirOwnTableBeforeTheSongTable(string table, params string[] carriers)
    {
        foreach (var resref in carriers)
        {
            var loot = Locals(Load("utc", resref)).Where(l => l.Key.StartsWith("LOOT_TABLE_"))
                .OrderBy(l => int.Parse(l.Key["LOOT_TABLE_".Length..])).Select(l => l.Value).ToList();
            loot[^1].Should().StartWith("MUSIC_DISCS", resref);
            loot[^2].Should().Be($"{table},35,1", $"{resref} drops the quest item without diluting its other loot");
        }
    }

    [TestCase("mon_warden", "moncaladaccityex")]
    [TestCase("mon_medic", "moncaladaccityex")]
    [TestCase("mon_captain", "moncaladaccityex")]
    [TestCase("mon_quarter", "moncaladaccityex")]
    [TestCase("dath_engineer", "dath_cz_baseok")]
    [TestCase("dath_chief", "dath_cz_baseok")]
    [TestCase("dath_medic", "dath_landingpad")]
    [TestCase("dath_hunter", "dath_landingpad")]
    [TestCase("tat_farmer", "tat_anc_cantina")]
    [TestCase("tat_mechanic", "tat_anc_northdis")]
    [TestCase("tat_outfitter", "tat_anc_southdis")]
    [TestCase("tat_guard", "tat_anc_astropor")]
    [TestCase("tat_broker", "tat_anc_astropor")]
    [TestCase("hut_trapper", "hutlar_outpost")]
    [TestCase("hut_provision", "hutlar_outpost")]
    [TestCase("hut_sergeant", "hutlar_outpost")]
    [TestCase("hut_salvager", "hutlar_outpost")]
    [TestCase("cz_medic", "nanostation015")]
    [TestCase("cz_foreman", "nanostation015")]
    [TestCase("cz_quarter", "nanostation015")]
    [TestCase("vis_deputy", "veles_exterior")]
    [TestCase("vis_ranger", "veles_exterior")]
    [TestCase("vis_cook", "veles_exterior")]
    [TestCase("vis_scholar", "veles_exterior")]
    [TestCase("kor_quarter", "korribanlandingp")]
    [TestCase("kor_warrior", "korribanlandingp")]
    [TestCase("kor_keeper", "ar_scor_korrcan")]
    [TestCase("kor_overseer", "ar_scor_korrcan")]
    [TestCase("ns_fixer", "pw_ar_narscorpd")]
    [TestCase("ns_bounty", "pw_ar_bhbar")]
    [TestCase("ns_czerka", "pw_ar_czoffice")]
    [TestCase("ns_barkeep", "pw_ar_nars_canhd")]
    [TestCase("dan_sergeant", "dan_wildplain")]
    [TestCase("dan_ranger", "dan_fieldtrail")]
    [TestCase("dan_survey", "dan_fieldtrail")]
    [TestCase("esh_captain", "pw_ar_sc_eshanci")]
    [TestCase("esh_hunt", "pw_ar_sc_eshanci")]
    [TestCase("esh_quarter", "pw_ar_sc_eshanci")]
    public void QuestGiver_StandsInItsHubWithItsConversation(string giver, string area)
    {
        var blueprint = Load("utc", giver);
        Text(blueprint, "Conversation").Should().Be(giver);
        Text(blueprint, "ScriptDialogue").Should().Be("dialog_start");

        var placed = Value(Load("git", area), "Creature List").EnumerateArray()
            .Single(c => Text(c, "TemplateResRef") == giver);
        Text(placed, "Tag").Should().Be(giver);
        Text(placed, "Conversation").Should().Be(giver);
        Text(placed, "ScriptDialogue").Should().Be("dialog_start");
        Value(placed, "Plot").GetInt32().Should().Be(1, "quest givers cannot be killed");

        File.Exists(Path.Combine(Root, "SWLOR.Game.Server", "ConversationData", $"{giver}.conversation.json")).Should().BeTrue();
    }

    // Karrav Maubar keeps her own look: her body is not a parts-based model, so clothing colors do not apply.
    private static readonly string[] OutfittedGivers =
    {
        "mon_warden", "mon_medic", "mon_captain", "mon_quarter", "dath_medic", "dath_hunter", "dath_engineer", "dath_chief",
        "tat_farmer", "tat_mechanic", "tat_guard", "tat_broker", "hut_trapper", "hut_provision", "hut_sergeant", "hut_salvager",
        "cz_medic", "cz_foreman", "cz_quarter", "vis_cook", "vis_scholar", "vis_deputy", "vis_ranger",
        "kor_quarter", "kor_warrior", "kor_keeper", "kor_overseer", "ns_fixer", "ns_bounty", "ns_czerka", "ns_barkeep",
        "dan_sergeant", "dan_ranger", "dan_survey", "esh_captain", "esh_hunt", "esh_quarter"
    };

    private static readonly string[] OutfitColors =
        { "Cloth1Color", "Cloth2Color", "Leather1Color", "Leather2Color", "Metal1Color", "Metal2Color" };

    [Test]
    public void EveryQuestGiver_WearsItsOwnOutfitInItsOwnColors()
    {
        using var scope = new AssertionScope();
        var palettes = new List<string>();
        foreach (var giver in OutfittedGivers)
        {
            var outfit = giver + "_o";
            var item = Load("uti", outfit);
            Value(item, "BaseItem").GetInt32().Should().Be(16, outfit);
            item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().StartWith("[NPC] ", outfit);
            Locals(item)["NO_ECONOMY"].Should().Be("1", $"{outfit} is not obtainable by players");
            palettes.Add(string.Join(",", OutfitColors.Select(color => Value(item, color).GetInt32())));

            var worn = Value(Load("utc", giver), "Equip_ItemList").EnumerateArray()
                .Single(slot => slot.GetProperty("__struct_id").GetInt32() == 2);
            Text(worn, "EquippedRes").Should().Be(outfit, giver);
        }

        palettes.Should().OnlyHaveUniqueItems("no two quest givers share a color scheme");
    }

    [Test]
    public void PlacedQuestGivers_WearTheSameOutfitAsTheirBlueprint()
    {
        using var scope = new AssertionScope();
        var placed = Directory.EnumerateFiles(Path.Combine(Root, "Module", "git"), "*.git.json")
            .Where(path => OutfittedGivers.Any(giver => File.ReadAllText(path).Contains($"\"{giver}_o\"")))
            .SelectMany(path => Value(JsonDocument.Parse(File.ReadAllText(path)).RootElement, "Creature List").EnumerateArray().ToArray())
            .Where(creature => OutfittedGivers.Contains(Text(creature, "TemplateResRef")))
            .ToDictionary(creature => Text(creature, "TemplateResRef"));
        placed.Keys.Should().BeEquivalentTo(OutfittedGivers);

        foreach (var (giver, creature) in placed)
        {
            var chest = Value(creature, "Equip_ItemList").EnumerateArray()
                .Single(slot => slot.GetProperty("__struct_id").GetInt32() == 2);
            Text(chest, "TemplateResRef").Should().Be(giver + "_o");
            var item = Load("uti", giver + "_o");
            foreach (var color in OutfitColors)
                Value(chest, color).GetInt32().Should().Be(Value(item, color).GetInt32(), $"{giver} {color}");
        }
    }

    private static double DistanceToSegment(double px, double py, (double X, double Y) a, (double X, double Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var t = dx == 0 && dy == 0 ? 0 : Math.Clamp(((px - a.X) * dx + (py - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
        return Math.Sqrt(Math.Pow(px - a.X - t * dx, 2) + Math.Pow(py - a.Y - t * dy, 2));
    }

    private static bool IsInside(double px, double py, IReadOnlyList<(double X, double Y)> polygon)
    {
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var (x1, y1) = polygon[i];
            var (x2, y2) = polygon[(i + 1) % polygon.Count];
            if (y1 > py != y2 > py && px < (x2 - x1) * (py - y1) / (y2 - y1) + x1)
                inside = !inside;
        }
        return inside;
    }

    [Test]
    public void NoQuestGiver_StandsOnOrBesideATransitionOrDoor()
    {
        using var scope = new AssertionScope();
        var givers = OutfittedGivers.Append("tat_outfitter").ToHashSet();
        var found = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root, "Module", "git"), "*.git.json"))
        {
            var text = File.ReadAllText(path);
            if (!givers.Any(giver => text.Contains($"\"{giver}\""))) continue;
            using var document = JsonDocument.Parse(text);
            var area = document.RootElement;
            var triggers = Value(area, "TriggerList").EnumerateArray()
                .Where(trigger => trigger.TryGetProperty("Geometry", out _))
                .Select(trigger => (Tag: Text(trigger, "Tag"), Points: Value(trigger, "Geometry").EnumerateArray()
                    .Select(point => (Value(trigger, "XPosition").GetDouble() + Value(point, "PointX").GetDouble(),
                        Value(trigger, "YPosition").GetDouble() + Value(point, "PointY").GetDouble())).ToList()))
                .Where(trigger => trigger.Points.Count >= 3).ToList();
            var doors = Value(area, "Door List").EnumerateArray()
                .Select(door => (Tag: Text(door, "Tag"), X: Value(door, "X").GetDouble(), Y: Value(door, "Y").GetDouble())).ToList();

            // Placeables that send the player to another area when used.
            var teleporters = Value(area, "Placeable List").EnumerateArray()
                .Where(placeable => placeable.TryGetProperty("OnUsed", out _) && Text(placeable, "OnUsed") == "teleport")
                .Select(placeable => (Tag: Text(placeable, "Tag"), X: Value(placeable, "X").GetDouble(), Y: Value(placeable, "Y").GetDouble())).ToList();

            foreach (var creature in Value(area, "Creature List").EnumerateArray()
                         .Where(creature => givers.Contains(Text(creature, "TemplateResRef"))))
            {
                found++;
                var giver = Text(creature, "TemplateResRef");
                var x = Value(creature, "XPosition").GetDouble();
                var y = Value(creature, "YPosition").GetDouble();
                foreach (var trigger in triggers)
                {
                    var distance = IsInside(x, y, trigger.Points)
                        ? 0
                        : Enumerable.Range(0, trigger.Points.Count).Min(i =>
                            DistanceToSegment(x, y, trigger.Points[i], trigger.Points[(i + 1) % trigger.Points.Count]));
                    distance.Should().BeGreaterThan(3, $"{giver} must not stand on or beside the trigger {trigger.Tag}");
                }

                foreach (var door in doors)
                    Math.Sqrt(Math.Pow(x - door.X, 2) + Math.Pow(y - door.Y, 2))
                        .Should().BeGreaterThan(2.5, $"{giver} must not block the door {door.Tag}");

                foreach (var teleporter in teleporters)
                    Math.Sqrt(Math.Pow(x - teleporter.X, 2) + Math.Pow(y - teleporter.Y, 2))
                        .Should().BeGreaterThan(3, $"{giver} must not stand beside the teleporter {teleporter.Tag}");
            }
        }

        found.Should().Be(givers.Count);
    }

    [Test]
    public void EveryNewQuest_NamesItsGiverInTheReturnStep()
    {
        var givers = new Dictionary<string, string[]>
        {
            ["mon_"] = new[] { "Issa Tarn", "Orlu Vess", "Serrin Mool", "Dessa Korr" },
            ["dath_"] = new[] { "Halv Renner", "Imra Sol", "Garrik Vane", "Marrok Dane" },
            ["tat_"] = new[] { "Odrin Vass", "Bex Tarrow", "Karrav Maubar", "Jorn Hale", "Dreeko Vant" },
            ["hut_"] = new[] { "Brannoc Tull", "Marla Keth", "Dova Rhun", "Teb Orrin" },
            ["cz_"] = new[] { "Ilsa Marn", "Dorn Ketch", "Tobin Raye" },
            ["vis_"] = new[] { "Kessa Vorn", "Tam Orrel", "Marta Quill", "Edren Pax" },
            ["kor_"] = new[] { "Varn Tolek", "Brask", "Malen Dray", "Karth Veyl" },
            ["ns_"] = new[] { "Vex Tarro", "Dace Korr", "Lena Voss", "Jorro" },
            ["dan_"] = new[] { "Halden Prax", "Mira Tannis", "Anya Kell" },
            ["esh_"] = new[] { "Teral Voss", "Daven Ryl", "Ysa Morn" },
        };

        using var scope = new AssertionScope();
        foreach (var quest in NewQuests().Values)
        {
            var names = givers.Single(g => quest.QuestId.StartsWith(g.Key)).Value;
            var last = quest.States[quest.States.Keys.Max()].JournalText;
            names.Any(last.Contains).Should().BeTrue($"{quest.QuestId} must say who to return to: {last}");
        }
    }
}
