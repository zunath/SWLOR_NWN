using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class UnarmedMasteryConversationTests
{
    [TestCase("cq_adamguard", "nanostation015", "perfect_form_mastery")]
    [TestCase("cq_scraplock", "czs220_hangar", "haymaker_mastery")]
    public void ExistingPlacedTrainerOffersAndCompletesIndependentTechniqueTrial(string trainer, string area, string questId)
    {
        var root = FindRoot();
        var graph = JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server", "ConversationData", trainer + ".conversation.json")))!;
        Assert.That(ConversationGraphValidator.Validate(graph), Is.Empty);
        Assert.That(graph.Nodes.Where(pair => !pair.Key.StartsWith("unarmed-")).All(pair =>
            pair.Value.Choices.Any(choice => choice.ChoiceId == "unarmed-training")), Is.True,
            "Every existing quest route must retain access to the independent Unarmed trial.");
        var routes = graph.Choices["unarmed-training"].Next;
        Assert.That(routes.Single(route => route.TargetNodeId == "unarmed-offer").Conditions.Single().Key, Is.EqualTo("condition-can-accept-quest"));
        Assert.That(routes.Single(route => route.TargetNodeId == "unarmed-ready").Conditions.Single().Arguments,
            Is.EqualTo(new[] { questId, "2" }));
        Assert.That(graph.Choices["unarmed-accept"].Actions.Single().Arguments, Is.EqualTo(new[] { questId }));
        Assert.That(graph.Choices["unarmed-turnin"].Actions.Single().Arguments, Is.EqualTo(new[] { questId }));
        Assert.That(graph.Choices["unarmed-purpose"].Actions, Is.Empty);
        Assert.That(graph.Choices["unarmed-rules"].Actions, Is.Empty);
        Assert.That(graph.Choices["unarmed-back"].Next.Select(route => route.TargetNodeId),
            Is.EqualTo(graph.EntryPoints.Select(route => route.TargetNodeId)), "Returning preserves the original ordered quest routes.");

        var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(root, "Module", "utc", trainer + ".utc.json")));
        Assert.That(blueprint["Conversation"]!["value"]!.Value<string>(), Is.EqualTo(trainer));
        Assert.That(blueprint["ScriptDialogue"]!["value"]!.Value<string>(), Is.EqualTo("dialog_start"));
        var placed = JObject.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", area + ".git.json")));
        Assert.That(placed["Creature List"]!["value"]!.Children<JObject>().Any(creature =>
            creature["Conversation"]?["value"]?.Value<string>() == trainer &&
            creature["ScriptDialogue"]?["value"]?.Value<string>() == "dialog_start"), Is.True);

        var quest = new UnarmedMasteryQuestDefinition().BuildQuests()[questId];
        var gate = quest.Prerequisites.OfType<RequiredSkillRankPrerequisite>().Single();
        Assert.That(gate.SkillType, Is.EqualTo(SkillType.Unarmed));
        Assert.That(gate.RequiredRank, Is.EqualTo(50));
        Assert.That(quest.Prerequisites.OfType<RequiredQuestPrerequisite>(), Is.Empty,
            "Katar mastery cannot be a hidden prerequisite to the new weapon skill.");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
