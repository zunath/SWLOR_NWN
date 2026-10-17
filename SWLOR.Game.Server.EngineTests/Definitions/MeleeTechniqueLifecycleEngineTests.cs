using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class MeleeTechniqueLifecycleEngineTests
{
    [EngineTest("Player subdual clears other fighters' setup, Flow and counter references before immediate revival", Category = "MeleeTechnique", TimeoutSeconds = 45f)]
    public static async Task PlayerSubdualClearsTargetReferences(EngineTestContext context)
    {
        using var killer = await PlayerAbilityFixture.CreateAsync(context, -0.5f);
        using var victim = await PlayerAbilityFixture.CreateAsync(context, 1f);
        var counterOwner = context.SpawnCreature("nw_bandit001", -1f, 1f);
        await context.DelaySecondsAsync(1f);
        context.MakeHostile(victim.Creature);
        SetPlotFlag(victim.Creature, false);
        SetImmortal(victim.Creature, false);
        SetEventScript(victim.Creature, EventScript.Creature_OnDeath, ScriptName.OnPlayerDeath);
        SetEventScript(victim.Creature, EventScript.Creature_OnDamaged, ScriptName.OnPlayerDamaged);
        context.AssertEqual(ScriptName.OnPlayerDeath, GetEventScript(victim.Creature, EventScript.Creature_OnDeath),
            "The victim does not forward the cloned NPC death event.");
        context.AssertEqual(ScriptName.OnModuleDeath, GetEventScript(GetModule(), EventScript.Module_OnPlayerDeath),
            "The actual module player death handler is installed.");
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), victim.Creature, 45f);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), victim.Creature, 45f);
        foreach (var creature in new[] { killer.Creature, victim.Creature, counterOwner })
        {
            context.SuppressNPCNaturalRegen(creature);
            SetAILevel(creature, AILevel.VeryLow);
        }
        Stat.SetNPCMaxHitPoints(counterOwner, 20000, true);
        context.SetNPCResources(counterOwner, 100, 100);
        foreach (var perk in new[]
        {
            PerkType.FlowingForm, PerkType.PracticedForm, PerkType.MeasuredTechnique,
            PerkType.ControlledBreathing, PerkType.RootedTechnique, PerkType.DirtyFighting,
            PerkType.HeavyBlows, PerkType.HardKnocks, PerkType.CatchYourBreath, PerkType.Payback
        })
            context.SetNPCPerkLevel(counterOwner, perk, 0);
        context.SetNPCPerkLevel(counterOwner, PerkType.Jab, 1);
        killer.Update(record =>
        {
            record.Settings.IsSubdualModeEnabled = true;
            record.Skills[SkillType.Unarmed].Rank = 50;
            record.Perks[PerkType.Jab] = 1;
            record.Perks[PerkType.FinishingCombination] = 1;
        });
        await context.EquipItemAsync(killer.Creature, "t_pglove", InventorySlot.RightHand);
        await context.EquipItemAsync(counterOwner, "t_pglove", InventorySlot.RightHand);
        StatusEffect.ApplyStatusEffect(killer.Creature, killer.Creature, new FlowStanceStatusEffect(), -1);
        StatusEffect.ApplyStatusEffect(counterOwner, counterOwner, new BruiserStanceStatusEffect(), -1);
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        try
        {
            await Cast(context, counterOwner, victim.Creature, FeatType.Jab1);
            await Cast(context, killer.Creature, victim.Creature, FeatType.Jab1);
            await context.DelaySecondsAsync(1.05f);
            await Cast(context, killer.Creature, victim.Creature, FeatType.FinishingCombination1);
            await context.ExecuteInCreatureContextAsync(victim.Creature, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(victim.Creature, counterOwner, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), counterOwner);
            });
            await context.WaitUntilAsync(() => MeleeTechnique.GetPendingRetaliationPower(counterOwner, victim.Creature) == 24,
                2f, "the victim's direct HP hit to arm the independent counter");
            context.Assert(StatusEffect.HasStatusEffect<SlipAwayStatusEffect>(killer.Creature), "The killer earned Flow against this victim.");
            context.Assert(StatusEffect.HasStatusEffect<OpeningStatusEffect>(counterOwner), "The counter owner also has unspent setup against this victim.");
            context.Assert(StatusEffect.HasStatusEffect<RetaliationStatusEffect>(counterOwner), "The victim is the counter's live damaging opponent.");
            context.AssertEqual(killer.Creature, GetLastHostileActor(victim.Creature), "The native hostile actor is the player whose subdual setting governs this death.");

            // The source is a real player with subdual enabled; only the native death event invokes cleanup.
            await context.ExecuteInCreatureContextAsync(killer.Creature, () =>
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), victim.Creature));
            await context.WaitUntilAsync(() => StatusEffect.HasStatusEffect<SubdualPenaltyStatusEffect>(victim.Creature),
                3f, "the actual module player death route to resurrect and subdue the victim");
            context.Assert(!GetIsDead(victim.Creature), "Subdual revives the player immediately in the same area.");
            context.AssertEqual(context.Arena, GetArea(victim.Creature), "The victim did not leave the area to clear references.");
            // Read the markers first: a technique getter could otherwise hide a missing event cleanup by refreshing lazily.
            context.Assert(!StatusEffect.HasStatusEffect<SlipAwayStatusEffect>(killer.Creature), "Player death clears another fighter's earned Flow window.");
            context.Assert(!StatusEffect.HasStatusEffect<OpeningStatusEffect>(counterOwner), "Player death clears another fighter's unspent setup.");
            context.Assert(!StatusEffect.HasStatusEffect<RetaliationStatusEffect>(counterOwner), "Player death clears another fighter's counter marker.");
            context.Assert(StatusEffect.HasStatusEffect<FlowStanceStatusEffect>(killer.Creature), "The living fighter keeps its stance.");
            context.Assert(StatusEffect.HasStatusEffect<BruiserStanceStatusEffect>(counterOwner), "The living counter owner keeps its stance.");
            context.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(counterOwner, victim.Creature), "Setup cannot survive immediate subdual revival.");
            context.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(counterOwner, victim.Creature), "The old counter cannot be spent on the revived player.");
        }
        finally
        {
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
            AssignCommand(killer.Creature, () => ClearAllActions());
            AssignCommand(counterOwner, () => ClearAllActions());
        }
    }

    private static async Task Cast(EngineTestContext context, uint caster, uint target, FeatType feat)
    {
        var ability = Ability.GetAbilityDetail(feat);
        Ability.ClearLastCompletedAbilityImpactSummary(caster);
        await context.ExecuteInCreatureContextAsync(caster, () =>
            context.Assert(UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target)),
                $"{ability.Name} starts: {Ability.GetLastActivationDenialReason()}"));
        await context.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability == ability,
            6f, $"{ability.Name} to complete its actual activation");
        AssignCommand(caster, () => ClearAllActions());
    }
}
