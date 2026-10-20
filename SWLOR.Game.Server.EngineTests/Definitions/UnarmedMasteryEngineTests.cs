using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class UnarmedMasteryEngineTests
{
    [EngineTest("A free Uppercut keeps its combat effects but only paid Uppercuts persist mastery progress", Category = "UnarmedPersistence", TimeoutSeconds = 90f)]
    public static async Task FreeUppercutDoesNotAdvanceAcceptedTrial(EngineTestContext context)
    {
        using var owner = await PlayerAbilityFixture.CreateAsync(context);
        using var other = await PlayerAbilityFixture.CreateAsync(context, 3f);
        const string questId = "haymaker_mastery";
        const string objective = "unarmed_distraction_spent";
        var trainer = context.SpawnCreature("cq_scraplock", -1f);
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
        context.AssertEqual(45, Stat.GetNPCStats(target).Level, "The real hostile opponent qualifies for the accepted trial.");
        Stat.SetNPCMaxHitPoints(target, 20000, true);
        context.SuppressNPCNaturalRegen(target);
        SetAILevel(target, AILevel.VeryLow);
        TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 90f);
        context.MakeHostile(target);
        owner.Update(record =>
        {
            record.Skills[SkillType.Unarmed].Rank = 50;
            record.Skills[SkillType.Spear].Rank = 35;
            record.Skills[SkillType.Pistol].Rank = 50;
            record.Perks[PerkType.DirtyJab] = 1;
            record.Perks[PerkType.Uppercut] = 1;
            record.Perks[PerkType.LowKick] = 1;
            record.Perks[PerkType.CatchYourBreath] = 1;
            record.Perks[PerkType.OpportunistsFlow] = 1;
        });
        other.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        await context.EquipItemAsync(owner.Creature, "t_pglove", InventorySlot.RightHand);
        context.AssertEqual(1, Perk.GetPerkLevel(owner.Creature, PerkType.OpportunistsFlow), "The player owns the real global four-stamina discount perk.");
        var uppercut = Ability.GetAbilityDetail(FeatType.Uppercut1);
        var notifiedPayments = new List<int>();
        void TrackPayment(uint player, uint enemy, AbilityDetail ability, MeleeTechniqueStage stage)
        {
            if (player == owner.Creature && enemy == target && ability.EffectiveLevelPerkType == PerkType.Uppercut)
                notifiedPayments.Add(Combat.GetPaidAbilityStaminaCost(player, uppercut));
        }
        MeleeTechnique.TechniqueLanded += TrackPayment;
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        try
        {
            foreach (var player in new[] { owner, other })
                await context.ExecuteInCreatureContextAsync(player.Creature,
                    () => context.Assert(Quest.AcceptQuest(player.Creature, trainer, questId), "Each real player independently accepts the trial."));

            await Cast(context, owner.Creature, target, FeatType.LowKick1);
            context.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(owner.Creature, target), "The independent heavy trial begins without Rattled.");
            // The civilian-derived player fixture is plot-protected until a damage-taking test opts in.
            SetPlotFlag(owner.Creature, false);
            SetEventScript(owner.Creature, EventScript.Creature_OnDamaged, ScriptName.OnPlayerDamaged);
            context.AssertEqual(ScriptName.OnPlayerDamaged, GetEventScript(owner.Creature, EventScript.Creature_OnDamaged),
                "The fixture uses the connected player damage event rather than its cloned NPC event.");
            // Match the normal native player levels used by damage-taking PvP fixtures.
            CreaturePlugin.SetClassByPosition(owner.Creature, 0, ClassType.Standard);
            await context.ExecuteInCreatureContextAsync(owner.Creature, () =>
                typeof(PlayerInitialization).GetMethod("EnsureNativeLevels", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner.Creature }));
            CreaturePlugin.SetRawAbilityScore(owner.Creature, AbilityType.Vitality, 10);
            await context.WaitFrameAsync();
            ObjectPlugin.SetCurrentHitPoints(owner.Creature, GetMaxHitPoints(owner.Creature));
            var counterHitPoints = GetCurrentHitPoints(owner.Creature);
            context.Assert(counterHitPoints > 5, "The player has enough real HP to survive the counter setup hit.");
            StatusEffect.ApplyStatusEffect(owner.Creature, owner.Creature, new BruiserStanceStatusEffect(), -1);
            await context.EquipItemAsync(owner.Creature, "b_pistol", InventorySlot.RightHand);
            context.AssertEqual(SkillType.Pistol, Skill.GetSkillTypeByBaseItem(GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, owner.Creature))),
                "A different weapon skill is equipped when the player earns the counter.");
            await context.ExecuteInCreatureContextAsync(target, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(target, owner.Creature, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), owner.Creature);
            });
            context.Assert(GetCurrentHitPoints(owner.Creature) < counterHitPoints && !GetIsDead(owner.Creature),
                "The counter setup hit removes real player HP without killing the fixture.");
            await context.WaitUntilAsync(() => MeleeTechnique.GetPendingRetaliationPower(owner.Creature, target) == 24,
                2f, "the real player damage event arms the optional counter while a pistol is equipped");
            await context.EquipItemAsync(owner.Creature, "t_pglove", InventorySlot.RightHand);
            context.AssertEqual(24, MeleeTechnique.GetPendingRetaliationPower(owner.Creature, target),
                "The counter survives switching back to Power Gloves before the free heavy strike.");
            // Exercise the native avoided-attack callback with the owned perk's real stat source.
            await context.ExecuteInCreatureContextAsync(owner.Creature, () => Combat.TrackAvoidedAttack(owner.Creature, target));
            context.AssertEqual(-4, Combat.GetNextSkillAbilityStaminaCostAdjustment(owner.Creature, SkillType.Unarmed, true), "Opportunist's Flow applies its global discount while Power Gloves are equipped.");
            await context.DelaySecondsAsync(1.05f);
            var freeStamina = Stat.GetCurrentStamina(owner.Creature);
            var freeImpact = await Cast(context, owner.Creature, target, FeatType.Uppercut1);
            context.AssertEqual(freeStamina, Stat.GetCurrentStamina(owner.Creature), "The free Uppercut pays zero and receives no Catch Your Breath refund despite missing stamina.");
            context.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(owner.Creature, target), "A valid free heavy strike still spends its optional counter.");
            context.Assert(freeImpact.ImpactedTargetCount > 0 && freeImpact.AttributedDamage > 0, "The free cast still lands and deals its normal combat damage.");
            context.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(owner.Creature, target), "The free heavy strike needs no setup.");
            context.AssertEqual(0, Combat.GetNextSkillAbilityStaminaCostAdjustment(owner.Creature, SkillType.Unarmed, true), "The real discount is consumed once.");
            context.AssertEqual(0, notifiedPayments.Count, "A free landed heavy strike does not emit a paid mastery notification.");
            var unchanged = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
            context.AssertEqual(40, unchanged.Quests[questId].CombatProgresses[objective], "The accepted trial persists no credit for the free cast.");
            context.AssertEqual(1, unchanged.Quests[questId].CurrentState, "The free cast cannot advance the accepted trial.");
            await context.ExecuteInCreatureContextAsync(owner.Creature, () => DB.Set(unchanged));

            for (var cycle = 0; cycle < 2; cycle++)
            {
                foreach (var group in new[] { RecastGroup.UnarmedOpener, RecastGroup.UnarmedResponse })
                    Recast.ReduceRecastDelay(owner.Creature, group, 100f);
                await context.DelaySecondsAsync(1.05f);
                context.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(owner.Creature, target), "Paid trial credit needs no Rattled or counter charge.");
                var paidStamina = Stat.GetCurrentStamina(owner.Creature);
                await Cast(context, owner.Creature, target, FeatType.Uppercut1);
                context.AssertEqual(paidStamina - 4 + (cycle == 0 ? 2 : 0), Stat.GetCurrentStamina(owner.Creature), "Paid Uppercuts refund once under the shared gate; the free cast did not start that gate.");
                context.AssertEqual(cycle + 1, notifiedPayments.Count, "Each landed paid Uppercut emits exactly one mastery notification.");
                context.AssertEqual(4, notifiedPayments[cycle], "The real paid-cost context remains available during the mastery notification.");
                var saved = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(owner.Id));
                context.AssertEqual(39 - cycle, saved.Quests[questId].CombatProgresses[objective], "Paid mastery progress survives each Redis reload.");
                context.AssertEqual(40, DB.Get<Player>(other.Id).Quests[questId].CombatProgresses[objective], "Another player's accepted trial receives no borrowed progress.");
                await context.ExecuteInCreatureContextAsync(owner.Creature, () => DB.Set(saved));
            }
        }
        finally
        {
            MeleeTechnique.TechniqueLanded -= TrackPayment;
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
            AssignCommand(owner.Creature, () => ClearAllActions());
        }
    }

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
            await Cast(context, player, target, feat);
            if (feat != FeatType.FinishingCombination1)
                await context.DelaySecondsAsync(1.05f);
        }
    }

    private static async Task<AbilityImpactSummary> Cast(EngineTestContext context, uint player, uint target, FeatType feat)
    {
        var ability = Ability.GetAbilityDetail(feat);
        Ability.ClearLastCompletedAbilityImpactSummary(player);
        await context.ExecuteInCreatureContextAsync(player, () =>
            context.Assert(UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target)),
                $"The real {ability.Name} activation starts: {Ability.GetLastActivationDenialReason()}"));
        await context.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player)?.Ability == ability,
            6f, $"the real {ability.Name} impact");
        return Ability.GetLastCompletedAbilityImpactSummary(player);
    }
}
