using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class UnarmedMasteryEngineTests
{
    [EngineTest("A player's paid combination persists trial progress and unlocks Perfect Form through its trainer", Category = "UnarmedPersistence", TimeoutSeconds = 90f)]
    public static async Task PaidCombinationCompletesTrainerTrial(EngineTestContext context)
    {
        using var owner = await PlayerAbilityFixture.CreateAsync(context);
        using var other = await PlayerAbilityFixture.CreateAsync(context, 3f);
        const string questId = "perfect_form_mastery";
        var trainer = context.SpawnCreature("cq_adamguard", -1f);
        var target = context.SpawnCreature("nw_rat001", 1f);
        await context.DelaySecondsAsync(1f);
        var skin = await context.EquipItemAsync(target, "beast_skin", InventorySlot.CreatureArmor);
        await context.ExecuteInCreatureContextAsync(target, () =>
        {
            for (var property = GetFirstItemProperty(skin); GetIsItemPropertyValid(property); property = GetNextItemProperty(skin))
                if (GetItemPropertyType(property) == ItemPropertyType.NPCLevel)
                    RemoveItemProperty(skin, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.NPCLevel, -1, 45), skin);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 90f);
        });
        context.AssertEqual(45, Stat.GetNPCStats(target).Level, "The live opponent meets the trial's combat-level requirement.");
        Stat.SetNPCMaxHitPoints(target, 20000, true);
        context.SuppressNPCNaturalRegen(target);
        SetAILevel(target, AILevel.VeryLow);
        TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 90f);
        context.MakeHostile(target);
        foreach (var player in new[] { owner, other })
            player.Update(record =>
            {
                record.Skills[SkillType.Unarmed].Rank = 50;
                record.Perks[PerkType.Jab] = 1;
                record.Perks[PerkType.PalmStrike] = 1;
                record.Perks[PerkType.FinishingCombination] = 1;
                record.Settings.DisplayAchievementNotification = false;
            });
        await context.EquipItemAsync(owner.Creature, "t_pglove", InventorySlot.RightHand);
        var cdKey = GetPCPublicCDKey(owner.Creature);
        var originalAccount = DB.Get<Account>(cdKey);
        DB.Set(new Account(cdKey));
        var gate = new PerkRequirementQuest(questId);
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        try
        {
            await context.ExecuteInCreatureContextAsync(owner.Creature, () =>
            {
                var session = StartTrainerSession(context, owner.Creature, trainer);
                Select(context, session, "unarmed-training");
                Select(context, session, "unarmed-accept");
            });
            await context.ExecuteInCreatureContextAsync(other.Creature,
                () => context.Assert(Quest.AcceptQuest(other.Creature, trainer, questId), "Another fighter independently accepts the same trial."));
            context.Assert(!string.IsNullOrEmpty(gate.CheckRequirements(owner.Creature)), "Accepting the trial does not unlock the capstone.");
            var stamina = Stat.GetCurrentStamina(owner.Creature);
            await CastCombination(context, owner.Creature, target);
            context.AssertEqual(stamina - 13, Stat.GetCurrentStamina(owner.Creature), "The actual Jab, Palm Strike and finisher pay their normal 3 + 4 + 6 stamina.");
            var saved = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
            context.AssertEqual(29, saved.Quests[questId].CombatProgresses["unarmed_opening_spent"], "The landed paid bridge reaches Redis through the registered mastery hook.");
            context.AssertEqual(29, saved.Quests[questId].CombatProgresses["unarmed_finish_spent"], "The two-hit paid finisher credits exactly once and persists.");
            context.AssertEqual(30, DB.Get<Player>(other.Id).Quests[questId].CombatProgresses["unarmed_finish_spent"], "The other fighter receives no borrowed progress.");
            await context.ExecuteInCreatureContextAsync(owner.Creature, () => DB.Set(saved));

            // Put the accepted trial one combination from completion; the final state transition
            // must still come from real paid activations, not a direct quest advance.
            owner.Update(record =>
            {
                record.Quests[questId].CombatProgresses["unarmed_opening_spent"] = 1;
                record.Quests[questId].CombatProgresses["unarmed_finish_spent"] = 1;
            });
            foreach (var group in new[] { RecastGroup.UnarmedOpener, RecastGroup.UnarmedResponse, RecastGroup.UnarmedTechnique })
                Recast.ReduceRecastDelay(owner.Creature, group, 100f);
            await context.DelaySecondsAsync(1.05f);
            await CastCombination(context, owner.Creature, target);
            var ready = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
            context.AssertEqual(2, ready.Quests[questId].CurrentState, "The last qualified combination persists the return-to-trainer state.");
            context.AssertEqual(0, ready.Quests[questId].TimesCompleted, "Combat does not grant the trainer's completion reward.");
            context.Assert(!string.IsNullOrEmpty(gate.CheckRequirements(owner.Creature)), "The capstone remains locked before trainer turn-in.");
            context.Assert(!DB.Get<Account>(cdKey).Achievements.ContainsKey(AchievementType.PerfectForm), "Training achievement waits for turn-in.");
            await context.ExecuteInCreatureContextAsync(owner.Creature, () =>
            {
                DB.Set(ready);
                var session = StartTrainerSession(context, owner.Creature, trainer);
                Select(context, session, "unarmed-training");
                Select(context, session, "unarmed-turnin");
            });
            var completed = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
            context.AssertEqual(1, completed.Quests[questId].TimesCompleted, "The authored trainer choice completes the real trial once.");
            context.AssertEqual(string.Empty, gate.CheckRequirements(owner.Creature), "Trainer completion opens the Perfect Form purchase requirement.");
            context.Assert(DB.Get<Account>(cdKey).Achievements.ContainsKey(AchievementType.PerfectForm), "The real completion action grants its achievement.");
            context.Assert(!DB.Get<Account>(cdKey).Achievements.ContainsKey(AchievementType.Haymaker), "The independent Brawler reward remains unearned.");
            context.Assert(!string.IsNullOrEmpty(gate.CheckRequirements(other.Creature)), "Another fighter's capstone remains locked.");
            await context.ExecuteInCreatureContextAsync(owner.Creature,
                () => context.Assert(!Quest.AdvanceQuest(owner.Creature, trainer, questId), "A completed trial cannot complete twice."));
        }
        finally
        {
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
            AssignCommand(owner.Creature, () => ClearAllActions());
            if (originalAccount == null)
                DB.Delete<Account>(cdKey);
            else
                DB.Set(originalAccount);
        }
    }

    private static ConversationSession StartTrainerSession(EngineTestContext context, uint player, uint trainer)
    {
        context.Assert(Conversation.TryGetGraph("cq_adamguard", out var graph), "The trainer's embedded authored graph is registered.");
        var session = new ConversationSession(graph, new ConversationContext(player, trainer), Conversation.Runtime);
        context.Assert(session.Start(), "The actual trainer graph starts for the live player.");
        return session;
    }

    private static void Select(EngineTestContext context, ConversationSession session, string choiceId)
    {
        var choice = session.VisibleChoices.Select((value, index) => (value, index)).Single(value => value.value.Id == choiceId);
        context.Assert(session.SelectChoice(choice.index) == ConversationSelectionResult.MovedToNextNode, $"The trainer executes {choiceId}.");
    }

    private static async Task CastCombination(EngineTestContext context, uint player, uint target)
    {
        foreach (var feat in new[] { FeatType.Jab1, FeatType.PalmStrike1, FeatType.FinishingCombination1 })
        {
            var ability = Ability.GetAbilityDetail(feat);
            Ability.ClearLastCompletedAbilityImpactSummary(player);
            await context.ExecuteInCreatureContextAsync(player, () =>
                context.Assert(UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target)),
                    $"The real {ability.Name} activation starts: {Ability.GetLastActivationDenialReason()}"));
            await context.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player)?.Ability == ability,
                6f, $"the real {ability.Name} impact");
            if (feat != FeatType.FinishingCombination1)
                await context.DelaySecondsAsync(1.05f);
        }
    }
}
