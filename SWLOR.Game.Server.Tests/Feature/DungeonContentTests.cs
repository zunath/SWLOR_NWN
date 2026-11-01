using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DungeonContentTests
{
    [Test]
    public void DungeonObjectivesUseTheEnemyNamesPlayersSee()
    {
        var quests = new LightsaberCapstoneQuestDefinition().BuildQuests()
            .Concat(new SaberstaffCapstoneQuestDefinition().BuildQuests())
            .Concat(new ForceCapstoneQuestDefinition().BuildQuests()).ToArray();
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        foreach (var line in new[] { "sabstorm", "guardmst", "sabcycl", "lightstand", "darkhung", "eclipse" })
        foreach (var tier in new[] { "ad", "sp", "wd", "ic", "ms" })
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory!.FullName,
                "Module", "utc", $"cp_{line}_{tier}.utc.json")));
            var root = doc.RootElement;
            var name = root.GetProperty("FirstName").GetProperty("value").GetProperty("0").GetString()!;
            var group = (NPCGroupType)root.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Single(v => v.GetProperty("Name").GetProperty("value").GetString() == "QUEST_NPC_GROUP_ID")
                .GetProperty("Value").GetProperty("value").GetInt32();
            // Bosses are named; adepts, specialists and inner circle are one faction tier shared by every line in the
            // dungeon, so the counter names the tier in the plural.
            var counterName = tier is "wd" or "ms" ? name : name + "s";
            typeof(NPCGroupType).GetField(group.ToString())!.GetCustomAttribute<NPCGroupAttribute>()!
                .Name.Should().Be(counterName, "the kill counter must identify the visible target");
            foreach (var quest in quests.Select(q => q.Value).Where(q => q.States[1].GetObjectives()
                         .OfType<KillTargetObjective>().Any(o => o.Group == group)))
                quest.States[1].JournalText.Should().Contain(name);
        }
    }
}
