using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class HutlarDroidFoundryQuestTests
{
    private const string QuestId = "fdy_shut_line";
    private const string Giver = "fdy_kesa";
    private object _originalItemNames = null!;

    private static PropertyInfo ItemNameCache => typeof(Cache)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)!;

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

    // The guild service is an instance behind the static Guild class: the fixture loads it once into a container of its
    // own and installs that container for every test, as the server's boot does for the facades.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [OneTimeSetUp]
    public void LoadItemNamesForGuildTaskTitles()
    {
        _facades = new FacadeContainer();
        using (_facades.Install())
        {
            Guild.LoadData();
        }

        NPCGroup.CacheData();
        var names = Directory.EnumerateFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".uti.json".Length], path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.GetProperty("LocalizedName").GetProperty("value")
                    .TryGetProperty("0", out var name) ? name.GetString() ?? string.Empty : string.Empty;
            });
        _originalItemNames = ItemNameCache.GetValue(null)!;
        ItemNameCache.SetValue(null, names);
    }

    [OneTimeTearDown]
    public void RestoreItemNames()
    {
        ItemNameCache.SetValue(null, _originalItemNames);
        _facades.Dispose();
    }

    private static JsonElement Load(string type, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return document.RootElement.Clone();
    }

    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => Value(o, key).GetString()!;
    private static double Number(JsonElement o, string key) => Value(o, key).GetDouble();

    [Test]
    public void FoundryDroids_CountTowardTheFoundryDroidGroup()
    {
        var droids = Directory.EnumerateFiles(Path.Combine(Root, "Module", "utc"), "fdy_*.utc.json")
            .Select(path => Path.GetFileName(path)[..^".utc.json".Length])
            .Where(resref => resref != Giver)
            .ToList();
        droids.Should().HaveCount(16, "the foundry has 12 floor droids, 3 named rares and the Overseer");

        foreach (var resref in droids)
        {
            var group = Value(Load("utc", resref), "VarTable").EnumerateArray()
                .Single(v => Text(v, "Name") == "QUEST_NPC_GROUP_ID");
            Value(group, "Value").GetInt32().Should().Be((int)NPCGroupType.Hutlar_FoundryDroid, resref);
        }
    }

    [Test]
    public void ShutDownTheLine_IsASmallRepeatableKillQuest()
    {
        var quest = new HutlarQuestDefinition().BuildQuests()[QuestId];
        quest.IsRepeatable.Should().BeTrue();
        quest.GuildType.Should().Be(GuildType.Invalid);

        quest.States.Keys.Should().Equal(1, 2);
        var kill = quest.States[1].GetObjectives().Should().ContainSingle().Which.Should().BeOfType<KillTargetObjective>().Subject;
        kill.Group.Should().Be(NPCGroupType.Hutlar_FoundryDroid);
        kill.Amount.Should().Be(20);
        quest.States[2].GetObjectives().Should().BeEmpty("the second state is the return to Kesa Morn");

        // Players repeat this freely, so it matches the other open-zone repeatable (Workin' for the Man: 20 kills, 750 cr, 1750 XP).
        quest.Rewards.Should().HaveCount(2);
        quest.Rewards.OfType<GoldReward>().Single().Amount.Should().Be(750);
        quest.Rewards.OfType<XPReward>().Single().Amount.Should().Be(1750);
    }

    [Test]
    public void HuntersGuild_OffersAFoundryDroidTaskAtTierFive()
    {
        var task = new HuntersGuildQuestDefinition().BuildQuests().Values
            .Single(q => q.States.Values.SelectMany(s => s.GetObjectives()).OfType<KillTargetObjective>()
                .Any(o => o.Group == NPCGroupType.Hutlar_FoundryDroid));
        task.GuildType.Should().Be(GuildType.HuntersGuild);
        task.GuildRank.Should().Be(4, "level 41-55 droids sit with the other tier 5 hunting tasks");
        task.States[1].GetObjectives().OfType<KillTargetObjective>().Single().Amount.Should().Be(10);
    }

    [Test]
    public void KesaMorn_StandsAtTheFoundryAccessDoorWithTheQuestConversation()
    {
        var blueprint = Load("utc", Giver);
        Text(blueprint, "Conversation").Should().Be(Giver);
        Text(blueprint, "ScriptDialogue").Should().Be("dialog_start");

        var outpost = Load("git", "hutlar_smuggleba");
        var placed = Value(outpost, "Creature List").EnumerateArray()
            .Single(c => Text(c, "TemplateResRef") == Giver);
        Text(placed, "Conversation").Should().Be(Giver);
        Text(placed, "ScriptDialogue").Should().Be("dialog_start");
        Number(placed, "Plot").Should().Be(1);

        var door = Value(outpost, "Door List").EnumerateArray().Single(d => Text(d, "Tag") == "HUT_FDY_OUTPOST_DOOR");
        var distance = Math.Sqrt(Math.Pow(Number(placed, "XPosition") - Number(door, "X"), 2) +
                                 Math.Pow(Number(placed, "YPosition") - Number(door, "Y"), 2));
        distance.Should().BeLessThan(10, "Kesa Morn points players through the Foundry Access door behind her");

        var graph = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "ConversationData", $"{Giver}.conversation.json"));
        graph.Should().Contain("action-accept-quest").And.Contain("action-advance-quest").And.Contain(QuestId);
    }
}
