using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatObjectiveTests
{
    [Test]
    public void LegacyQuestDataCanAcceptNewObjectivesAndPreservesProgressAcrossRepeatedReloads()
    {
        var progress = JsonConvert.DeserializeObject<PlayerQuest>("{\"CurrentState\":1,\"KillProgresses\":{},\"ItemProgresses\":{}}")!;
        var objective = new CombatObjective("sequence", "Completed sequences", 3);
        Assert.That(progress.CombatProgresses, Is.Empty);
        objective.Initialize(progress);
        for (var cycle = 1; cycle <= 3; cycle++)
        {
            Assert.That(objective.TryAdvance(progress), Is.True);
            progress = JsonConvert.DeserializeObject<PlayerQuest>(JsonConvert.SerializeObject(progress))!;
            Assert.That(progress.CombatProgresses["sequence"], Is.EqualTo(3 - cycle));
        }
        Assert.That(objective.IsComplete(progress), Is.True);
        Assert.That(objective.TryAdvance(progress), Is.False, "A completed counter cannot underflow or credit another stage.");
    }

    [Test]
    public void ReconciliationPreservesRelevantCountersAndRemovesObsoleteCounters()
    {
        var state = new QuestStateDetail();
        state.AddObjective(new CombatObjective("sequence", "Completed sequences", 30));
        state.AddObjective(new CombatObjective("finisher", "Completed finishers", 30));
        var progress = new PlayerQuest();
        progress.CombatProgresses["sequence"] = 12;
        progress.CombatProgresses["old"] = 7;
        Assert.That(state.ReconcileProgress(progress), Is.True);
        Assert.That(progress.CombatProgresses, Is.EquivalentTo(new Dictionary<string, int> { ["sequence"] = 12, ["finisher"] = 30 }));
        Assert.That(state.ReconcileProgress(progress), Is.False);
    }

    [Test]
    public void UninitializedObjectiveDoesNotAwardProgressOrCountAsComplete()
    {
        var objective = new CombatObjective("sequence", "Completed sequences", 30);
        var progress = new PlayerQuest();
        Assert.That(objective.TryAdvance(progress), Is.False);
        Assert.That(objective.IsComplete(progress), Is.False);
        Assert.That(progress.CombatProgresses, Is.Empty);
    }
}
