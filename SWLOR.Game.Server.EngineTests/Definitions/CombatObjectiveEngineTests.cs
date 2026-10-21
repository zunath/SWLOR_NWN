using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class CombatObjectiveEngineTests
{
    [EngineTest("Technique trial counters retain ownership across repeated save and reload cycles", Category = "UnarmedPersistence", TimeoutSeconds = 60f)]
    public static async Task TrialCounterPersistence(EngineTestContext context)
    {
        using var owner = await PlayerAbilityFixture.CreateAsync(context);
        using var other = await PlayerAbilityFixture.CreateAsync(context, 3f);
        const string questId = "perfect_form_mastery";
        owner.Update(player => player.Skills[SkillType.Unarmed].Rank = 50);
        other.Update(player => player.Skills[SkillType.Unarmed].Rank = 50);
        await context.ExecuteInCreatureContextAsync(owner.Creature,
            () => context.Assert(Quest.AcceptQuest(owner.Creature, GetModule(), questId), "The qualified player accepts the trial."));
        await context.ExecuteInCreatureContextAsync(other.Creature,
            () => context.Assert(Quest.AcceptQuest(other.Creature, GetModule(), questId), "The second player independently accepts the trial."));

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await context.ExecuteInCreatureContextAsync(owner.Creature, () =>
            {
                Quest.RecordCombatObjective(owner.Creature, "unarmed_opening_spent", GetModule());
                Quest.RecordCombatObjective(owner.Creature, "unarmed_finish_spent", GetModule());
                Quest.RecordCombatObjective(owner.Creature, "unarmed_distraction_spent", GetModule());
            });
            await context.WaitFrameAsync();
            var saved = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
            var savedOther = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(other.Id));
            context.AssertEqual(30 - cycle, saved.Quests[questId].CombatProgresses["unarmed_opening_spent"], "The opening counter reloads from Redis.");
            context.AssertEqual(30 - cycle, saved.Quests[questId].CombatProgresses["unarmed_finish_spent"], "The finisher counter reloads from Redis.");
            context.AssertEqual(30, savedOther.Quests[questId].CombatProgresses["unarmed_finish_spent"], "Another player's counters remain untouched.");
            context.AssertEqual(2, saved.Quests[questId].CombatProgresses.Count, "An unrelated technique cannot add a counter.");
            context.AssertEqual(1, saved.Quests[questId].CurrentState, "Partial progress cannot finish the trial.");
            await context.ExecuteInCreatureContextAsync(owner.Creature, () => DB.Set(saved));
        }

        await context.ExecuteInCreatureContextAsync(owner.Creature, () => Quest.AbandonQuest(owner.Creature, questId));
        await context.ExecuteInCreatureContextAsync(owner.Creature,
            () => context.Assert(Quest.AcceptQuest(owner.Creature, GetModule(), questId), "An abandoned trial can be accepted again."));
        var restarted = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
        context.AssertEqual(30, restarted.Quests[questId].CombatProgresses["unarmed_finish_spent"], "Reacceptance resets abandoned counters.");
    }
}
