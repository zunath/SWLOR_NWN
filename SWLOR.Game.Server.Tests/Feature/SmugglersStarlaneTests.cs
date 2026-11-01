using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class SmugglersStarlaneTests
{
    private object _originalItemNames = null!;
    private static PropertyInfo ItemNameCache => typeof(Cache)
        .GetProperty("ItemNamesByResref", BindingFlags.NonPublic | BindingFlags.Static)!;

    [OneTimeSetUp]
    public void LoadItemNamesWithoutAnEngine()
    {
        NPCGroup.CacheData();
        _originalItemNames = ItemNameCache.GetValue(null)!;
        var names = Directory.EnumerateFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json")
            .ToDictionary(path => Path.GetFileName(path)[..^".uti.json".Length], path =>
                JObject.Parse(File.ReadAllText(path))["LocalizedName"]?["value"]?["0"]?.Value<string>() ?? string.Empty);
        ItemNameCache.SetValue(null, names);
    }

    [OneTimeTearDown]
    public void RestoreItemNames() => ItemNameCache.SetValue(null, _originalItemNames);

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

    private static JObject Read(string kind, string name) => JObject.Parse(File.ReadAllText(
        Path.Combine(Root, "Module", kind, $"{name}.{kind}.json")));
    private static JToken Value(JToken o, string key) => o[key]!["value"]!;
    private static string Text(JToken o, string key) => Value(o, key).Value<string>()!;
    private static double Number(JToken o, string key) => Value(o, key).Value<double>();
    private static JToken Local(JToken o, string name) => Value(o, "VarTable")
        .Single(v => Text(v, "Name") == name)["Value"]!["value"]!;

    [Test]
    public void Quest_RequiresTenKrakenborneKillsAndReturnBeforeAwardingPermanentPass()
    {
        var quest = new MonCalaQuestDefinition().BuildQuests()["mon_starlane"];
        quest.IsRepeatable.Should().BeFalse();
        quest.States.Keys.Should().Equal(1, 2);
        var kill = quest.States[1].GetObjectives().Should().ContainSingle().Which
            .Should().BeOfType<KillTargetObjective>().Subject;
        kill.Group.Should().Be(NPCGroupType.MonCala_Krakenborne);
        Enum.GetValues<NPCGroupType>().Count(group => group == kill.Group).Should().Be(1);
        kill.Amount.Should().Be(10);
        kill.MaximumCreditDistance.Should().Be(55f);
        quest.States[2].GetObjectives().Should().BeEmpty();
        quest.OnAcceptActions.Should().BeEmpty("the pass must not be given on acceptance");
        quest.States.Values.Should().OnlyContain(s => s.KeyItemsGrantedOnAdvance.Count == 0);
        var reward = quest.Rewards.Should().ContainSingle().Which.Should().BeOfType<KeyItemReward>().Subject;
        reward.KeyItemType.Should().Be(KeyItemType.SmugglersMoonStarlanePass);
        reward.IsSelectable.Should().BeFalse();
        quest.KeyItemsRemovedOnComplete.Should().BeEmpty();
    }

    [Test]
    public void KrakenborneGroup_CoversAllSevenMonCalaShipsAndNoOtherBlueprints()
    {
        var tagged = Directory.EnumerateFiles(Path.Combine(Root, "Module", "utc"), "*.utc.json")
            .Where(path => File.ReadAllText(path).Contains("QUEST_NPC_GROUP_ID"))
            .Where(path => JObject.Parse(File.ReadAllText(path))["VarTable"]!["value"]!
                .Any(v => Text(v, "Name") == "QUEST_NPC_GROUP_ID" &&
                          Number(v, "Value") == (int)NPCGroupType.MonCala_Krakenborne))
            .Select(path => Path.GetFileName(path)[..^".utc.json".Length]).ToArray();
        tagged.Should().BeEquivalentTo(new[] { "t2bomber", "t2capital", "t2cargo", "t2fighter", "t2gunship", "t2interceptor", "t2platform" });
        Text(Read("git", "moncalaorbit")["VarTable"]!["value"]!
            .Single(v => Text(v, "Name") == "CREATURE_SPAWN_TABLE_ID"), "Value")
            .Should().Be("SPACE_MONCALA_ORBIT");
    }

    [TestCase("moncalaorbit", "narshadorbit", "STARLANE_MONCALA_SMUGGLERS", "STARLANE_LANDING_MON_SMUGGLERS")]
    [TestCase("narshadorbit", "moncalaorbit", "STARLANE_SMUGGLERS_MONCALA", "STARLANE_LANDING_SMUGGLERS_MON")]
    public void NewLane_RequiresEachPilotsPassAndHasAProtectedArrival(
        string area, string destinationArea, string laneTag, string arrivalTag)
    {
        var git = Read("git", area);
        var lane = Value(git, "Placeable List").Single(p => Text(p, "Tag") == laneTag);
        Text(lane, "OnUsed").Should().Be("teleport");
        Number(lane, "Useable").Should().Be(1);
        Number(lane, "Static").Should().Be(0);
        Local(lane, "KEY_ITEM_ID").Value<int>().Should().Be((int)KeyItemType.SmugglersMoonStarlanePass);
        Local(lane, "TELEPORT_PARTY_MEMBERS").Value<int>().Should().Be(0);
        Local(lane, "MISSING_KEY_ITEM_MESSAGE").Value<string>().Should().Contain("Nera Venn");
        var destination = Local(lane, "DESTINATION").Value<string>();
        Value(Read("git", destinationArea), "WaypointList").Should().ContainSingle(w => Text(w, "Tag") == destination);

        var arrival = Value(git, "WaypointList").Single(w => Text(w, "Tag") == arrivalTag);
        var zone = Value(git, "TriggerList").Single(t => Text(t, "Tag") == laneTag + "_SAFE");
        Text(zone, "TemplateResRef").Should().Be("anti_spawn_trigg");
        var vertices = Value(zone, "Geometry").Select(p => (
            X: Number(zone, "XPosition") + Number(p, "PointX"),
            Y: Number(zone, "YPosition") + Number(p, "PointY"))).ToArray();
        foreach (var (x, y) in new[] { (Number(lane, "X"), Number(lane, "Y")),
                     (Number(arrival, "XPosition"), Number(arrival, "YPosition")) })
        {
            // Both endpoints have at least ten metres of spawn exclusion on every side.
            x.Should().BeInRange(vertices.Min(p => p.X) + 10, vertices.Max(p => p.X) - 10);
            y.Should().BeInRange(vertices.Min(p => p.Y) + 10, vertices.Max(p => p.Y) - 10);
        }
        var are = Read("are", area);
        vertices.Should().OnlyContain(p => p.X >= 0 && p.X <= Number(are, "Width") * 10 &&
                                           p.Y >= 0 && p.Y <= Number(are, "Height") * 10);
        var dx = Number(lane, "X") - Number(arrival, "XPosition");
        var dy = Number(lane, "Y") - Number(arrival, "YPosition");
        Math.Sqrt(dx * dx + dy * dy).Should().BeGreaterThanOrEqualTo(10, "arrivals must not overlap the beacon");
        var gic = Read("gic", area);
        foreach (var key in new[] { "Placeable List", "WaypointList", "TriggerList" })
            Value(gic, key).Count().Should().Be(Value(git, key).Count(), "toolset comments stay aligned");
    }

    [Test]
    public void ExistingMonCalaRoutes_KeepTheirDestinationsAndRemainUngated()
    {
        var oldRoutes = Value(Read("git", "moncalaorbit"), "Placeable List")
            .Where(p => Text(p, "OnUsed") == "teleport" && Text(p, "Tag") != "STARLANE_MONCALA_SMUGGLERS").ToArray();
        oldRoutes.Select(p => Local(p, "DESTINATION").Value<string>()).Should().BeEquivalentTo(new[] {
            "STARLANE_LANDING_HUTLAR_1", "STARLANE_LANDING_VISCARA_1", "STARLANE_LANDING_DANTOOINE_1" });
        oldRoutes.Should().OnlyContain(p => Local(p, "KEY_ITEM_ID").Value<int>() == 0);
    }

    [Test]
    public void NewGiver_IsProtectedAndUsesHerOwnConversationBesideTheUnchangedDockhand()
    {
        var git = Read("git", "moncaladaccityex");
        var giver = Value(git, "Creature List").Single(c => Text(c, "TemplateResRef") == "mon_nera");
        foreach (var npc in new JToken[] { giver, Read("utc", "mon_nera") })
        {
            Text(npc, "Conversation").Should().Be("mon_nera");
            Text(npc, "ScriptDialogue").Should().Be("dialog_start");
            Number(npc, "Plot").Should().Be(1);
        }
        var dockhand = Value(git, "Creature List").Single(c => Text(c, "TemplateResRef") == "spc_dockhand");
        Local(dockhand, "CONVERSATION").Value<string>().Should().Be("StarportDialog");
        var dx = Number(giver, "XPosition") - Number(dockhand, "XPosition");
        var dy = Number(giver, "YPosition") - Number(dockhand, "YPosition");
        Math.Sqrt(dx * dx + dy * dy).Should().BeInRange(2, 8);
        Value(Read("gic", "moncaladaccityex"), "Creature List").Count()
            .Should().Be(Value(git, "Creature List").Count());
    }
}
