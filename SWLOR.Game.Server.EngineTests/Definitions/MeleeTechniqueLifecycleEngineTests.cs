using System.Linq;
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
    private static uint _watchedVictim = OBJECT_INVALID;
    private static Action _deathObserver;
    private static long _heartbeatGeneration;

    [NWNEventHandler(ScriptName.OnModuleDeath)]
    public static void ObservePlayerDeath()
    {
        if (_deathObserver != null && GetLastPlayerDied() == _watchedVictim)
            _deathObserver();
    }

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void ObserveHeartbeat() => _heartbeatGeneration++;

    [EngineTest("Player subdual clears other fighters' setup, Flow and counter references in the native death event", Category = "MeleeTechnique", TimeoutSeconds = 45f)]
    public static async Task PlayerSubdualClearsTargetReferences(EngineTestContext context)
    {
        AssertObserverOrder(context, ScriptName.OnModuleDeath, ObservePlayerDeath,
            Death.OnPlayerDeath, MeleeTechnique.OnPlayerDeath);
        AssertObserverOrder(context, ScriptName.OnModuleHeartbeat, ObserveHeartbeat, MeleeTechnique.RefreshAll);
        using var killer = await PlayerAbilityFixture.CreateAsync(context, -0.5f);
        using var victim = await PlayerAbilityFixture.CreateAsync(context, 1f);
        var counterOwner = context.SpawnCreature("nw_bandit001", -1f, 1f);
        await context.DelaySecondsAsync(1f);
        // NPC factions and player PvP attitudes use different native hostility paths.
        context.MakeHostile(counterOwner);
        SetPCDislike(killer.Creature, victim.Creature);
        SetPCDislike(victim.Creature, killer.Creature);
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
            // Arm the counter before the killer's short combo: its sweep also strikes this nearby fighter,
            // and the eighteen-second grant gate must keep the counter bound to the victim.
            await context.ExecuteInCreatureContextAsync(victim.Creature, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(victim.Creature, counterOwner, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), counterOwner);
            });
            await context.WaitUntilAsync(() => MeleeTechnique.GetPendingRetaliationPower(counterOwner, victim.Creature) == 12,
                2f, "the victim's direct HP hit to arm the independent counter");
            await Cast(context, killer.Creature, victim.Creature, FeatType.Jab1);
            await context.DelaySecondsAsync(1.05f);
            await Cast(context, killer.Creature, victim.Creature, FeatType.FinishingCombination1);
            context.AssertEqual(12, MeleeTechnique.GetPendingRetaliationPower(counterOwner, victim.Creature),
                "The killer's sweep cannot replace the victim's counter inside the grant gate.");
            context.Assert(StatusEffect.HasStatusEffect<SlipAwayStatusEffect>(killer.Creature), "The killer earned Flow against this victim.");
            context.Assert(StatusEffect.HasStatusEffect<OpeningStatusEffect>(counterOwner), "The counter owner also has unspent setup against this victim.");
            context.Assert(StatusEffect.HasStatusEffect<RetaliationStatusEffect>(counterOwner), "The victim is the counter's live damaging opponent.");
            context.AssertEqual(killer.Creature, GetLastHostileActor(victim.Creature), "The native hostile actor is the player whose subdual setting governs this death.");

            var deathObserved = false;
            Exception deathFailure = null;
            long heartbeatBeforeDeath = 0;
            _deathObserver = () =>
            {
                if (deathObserved) return;
                try
                {
                    context.AssertEqual(heartbeatBeforeDeath, _heartbeatGeneration,
                        "No module heartbeat may refresh technique state between the killing effect and the death observer.");
                    context.Assert(StatusEffect.HasStatusEffect<SubdualPenaltyStatusEffect>(victim.Creature),
                        "The real module death handler has applied the subdual penalty before the observer.");
                    context.Assert(!GetIsDead(victim.Creature), "Subdual revives the player immediately in the same area.");
                    context.AssertEqual(context.Arena, GetArea(victim.Creature), "The victim did not leave the area to clear references.");
                    // Read markers inside the native event, before any technique getter can refresh lazily.
                    context.Assert(!StatusEffect.HasStatusEffect<SlipAwayStatusEffect>(killer.Creature), "Player death clears another fighter's earned Flow window.");
                    context.Assert(!StatusEffect.HasStatusEffect<OpeningStatusEffect>(counterOwner), "Player death clears another fighter's unspent setup.");
                    context.Assert(!StatusEffect.HasStatusEffect<RetaliationStatusEffect>(counterOwner), "Player death clears another fighter's counter marker.");
                    context.Assert(StatusEffect.HasStatusEffect<FlowStanceStatusEffect>(killer.Creature), "The living fighter keeps its stance.");
                    context.Assert(StatusEffect.HasStatusEffect<BruiserStanceStatusEffect>(counterOwner), "The living counter owner keeps its stance.");
                    context.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(counterOwner, victim.Creature), "Setup cannot survive immediate subdual revival.");
                    context.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(counterOwner, victim.Creature), "The old counter cannot be spent on the revived player.");
                }
                catch (Exception exception)
                {
                    // ScriptExecutor logs and swallows event-handler exceptions; report this one through the test task.
                    deathFailure = exception;
                }
                finally
                {
                    deathObserved = true;
                }
            };
            // The source is a real player with subdual enabled; only the native death event invokes cleanup.
            await context.ExecuteInCreatureContextAsync(killer.Creature, () =>
            {
                _watchedVictim = victim.Creature;
                heartbeatBeforeDeath = _heartbeatGeneration;
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), victim.Creature);
            });
            await context.WaitUntilAsync(() => deathObserved, 3f, "the watched player's actual module death event");
            if (deathFailure != null) throw deathFailure;
        }
        finally
        {
            _deathObserver = null;
            _watchedVictim = OBJECT_INVALID;
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
            AssignCommand(killer.Creature, () => ClearAllActions());
            AssignCommand(counterOwner, () => ClearAllActions());
        }
    }

    private static void AssertObserverOrder(EngineTestContext context, string script, Action observer, params Action[] prerequisites)
    {
        var handlers = ServerManager.Scripts.GetActionScripts(script).Select(entry => entry.Action).ToList();
        var observerIndex = handlers.IndexOf(observer);
        context.Assert(observerIndex >= 0, $"{observer.Method.Name} is registered for {script}.");
        foreach (var prerequisite in prerequisites)
        {
            var prerequisiteIndex = handlers.IndexOf(prerequisite);
            context.Assert(prerequisiteIndex >= 0 && prerequisiteIndex < observerIndex,
                $"{script}: {observer.Method.Name} at index {observerIndex} must run after {prerequisite.Method.DeclaringType?.Name}.{prerequisite.Method.Name} at index {prerequisiteIndex}.");
        }
    }

    private static async Task Cast(EngineTestContext context, uint caster, uint target, FeatType feat)
    {
        var ability = Ability.GetAbilityDetail(feat);
        Ability.ClearLastCompletedAbilityImpactSummary(caster);
        await context.ExecuteInCreatureContextAsync(caster, () =>
        {
            // Match the directional hostility check used by ability activation and target-bound technique state.
            context.Assert(GetIsReactionTypeHostile(target, caster),
                $"{ability.Name}: native target {target} (PC={GetIsPC(target)}) is hostile to caster {caster} (PC={GetIsPC(caster)}).");
            context.Assert(UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target)),
                $"{ability.Name} starts: {Ability.GetLastActivationDenialReason()}");
        });
        await context.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability == ability,
            6f, $"{ability.Name} to complete its actual activation");
        AssignCommand(caster, () => ClearAllActions());
    }
}
