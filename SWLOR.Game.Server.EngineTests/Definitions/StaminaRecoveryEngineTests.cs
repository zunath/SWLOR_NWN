using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public class StaminaRecoveryEngineTests : WeaponActiveAbilityDefinitionBase
{
    [EngineTest("Core stamina and recovery abilities activate with their costs, cooldowns and effects", Category = "StaminaRecovery", TimeoutSeconds = 180f)]
    public static async Task RecoveryAbilityActivations(EngineTestContext ctx)
    {
        var selected = new[] { FeatType.DoubleShot3, FeatType.SavageCleave3, FeatType.AdrenalStim3,
            FeatType.PowerCell1, FeatType.PowerCell3, FeatType.FieldRecovery2, FeatType.ConduitStance1, FeatType.InfiniteConduit1 };
        var cases = new IAbilityBehaviorSource[] { new PistolAbilityBehaviors(), new VibrobladeAbilityBehaviors(),
            new FirstAidAbilityBehaviors(), new DevicesAbilityBehaviors(), new LeadershipAbilityBehaviors(), new SaberstaffAbilityBehaviors() }
            .SelectMany(source => source.BuildCases()).Where(testCase => selected.Contains(testCase.Feat)).ToList();
        ctx.AssertEqual(selected.Length, cases.Count, "each recovery activation has an existing behavior case");
        await AbilityBehaviorExecutor.RunAsync(ctx, cases);
    }

    [EngineTest("Rest, Field Recovery and Adrenal Stim pay their timed stamina recovery", Category = "StaminaRecovery", TimeoutSeconds = 30f)]
    public static async Task RecoveryStatusTicks(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.SetRawAbilityScore(player, AbilityType.Might, 10);
        CreaturePlugin.SetRawAbilityScore(player, AbilityType.Social, 10);
        fixture.Update(record => record.Stamina = 0);
        StatusEffect.ApplyStatusEffect(player, player, new RestStatusEffect(), 30f);
        await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(player) > 0, 7f, "Rest's six-second tick");
        ctx.AssertEqual(11, Stat.GetCurrentStamina(player), "Rest grants 1 + Might");
        StatusEffect.RemoveStatusEffect(player, typeof(RestStatusEffect));

        fixture.Update(record => record.Stamina = 0);
        StatusEffect.ApplyStatusEffect(player, player, new FieldRecovery2StatusEffect(), 30f);
        await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(player) > 0, 5f, "Field Recovery's four-second tick");
        ctx.AssertEqual(2, Stat.GetCurrentStamina(player), "base Field Recovery II payout used by the group model");
        StatusEffect.RemoveStatusEffect(player, typeof(FieldRecovery2StatusEffect));

        fixture.Update(record => record.Stamina = 0);
        StatusEffect.ApplyStatusEffect(player, player, new AdrenalStimStatusEffect(1), 30f);
        await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(player) > 0, 4f, "Adrenal Stim's three-second tick");
        ctx.AssertEqual(1, Stat.GetCurrentStamina(player), "the stim's timed payout is independent of natural recovery");
    }

    [EngineTest("Missed Double Shot spends stamina at impact and insufficient stamina cannot start it", Category = "StaminaRecovery", TimeoutSeconds = 30f)]
    public static async Task MissAndInsufficientStamina(EngineTestContext ctx)
    {
        var caster = ctx.SpawnCreature("nw_bandit001");
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        ctx.SuppressNPCNaturalRegen(caster);
        ctx.SuppressNPCNaturalRegen(target);
        ctx.MakeHostile(target);
        SetAILevel(caster, AILevel.VeryLow);
        SetAILevel(target, AILevel.VeryLow);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 30f);
        await ctx.EquipItemAsync(caster, "b_pistol", InventorySlot.RightHand);
        SetLocalInt(caster, $"PERK_LEVEL_{(int)PerkType.DoubleShot}", 3);
        ctx.SetNPCResources(caster, 100, 25);
        var starting = Stat.GetCurrentStamina(caster);
        Combat.SetAbilityHitResolutionOverride(false);
        Combat.SetAutoAttackHitResolutionOverride(false);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                ctx.Assert(UsePerkFeat.TryUseAbility(caster, target, FeatType.DoubleShot3, GetLocation(target)), "a funded cast starts");
                ctx.AssertEqual(starting, Stat.GetCurrentStamina(caster), "the activation delay has not spent stamina yet");
            });
            await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(caster) < starting, 5f, "the missed impact to spend its cost");
            ctx.AssertEqual(starting - 10, Stat.GetCurrentStamina(caster), "both missed shots cost ten with no critical refund");
            var ability = Ability.GetAbilityDetail(FeatType.DoubleShot3);
            ctx.Assert(Recast.IsOnRecastDelay(caster, ability.RecastGroup).Item1, "a missed attempt retains its cooldown");
            Stat.ReduceStamina(caster, Stat.GetCurrentStamina(caster) - 9);
            ctx.Assert(!string.IsNullOrWhiteSpace(ability.Requirements.OfType<AbilityRequirementStamina>().Single().CheckRequirements(caster, ability)), "nine stamina cannot fund the next ten-stamina cast");
        }
        finally
        {
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
        }
    }

    [EngineTest("Player stamina heartbeat distributes bonuses and preserves HP and FP cadence", Category = "StaminaRecovery")]
    public static async Task PlayerHeartbeatBudget(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.SetRawAbilityScore(player, AbilityType.Might, 10);
        fixture.Update(record => { record.Stamina = 0; record.FP = 0; record.STMRegen = 3; });
        TemporaryStatModifier.Add(player, StatType.StaminaRegen, 5, 60f);
        var fpBudget = 1 + GetAbilityScore(player, AbilityType.Willpower) / 4;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            SetLocalInt(player, "NATURAL_REGENERATION_TICK", 0);
            SetLocalInt(player, "NATURAL_STAMINA_REGEN_REMAINDER", 0);
            SetLocalInt(player, Stat.SuppressNaturalRegenVariable, 0);
            try
            {
                for (var heartbeat = 1; heartbeat <= 5; heartbeat++)
                {
                    NaturalRegeneration.ProcessRegeneration();
                    ctx.AssertEqual(heartbeat * 4, Stat.GetCurrentStamina(player), "equipment and food grant eight per thirty seconds, not per heartbeat");
                    ctx.AssertEqual(heartbeat == 5 ? fpBudget : 0, Stat.GetCurrentFP(player), "FP retains its thirty-second cadence");
                }
            }
            finally { SetLocalInt(player, Stat.SuppressNaturalRegenVariable, 1); }
            NaturalRegeneration.ProcessRegeneration();
            ctx.AssertEqual(20, Stat.GetCurrentStamina(player), "suppressed heartbeats cannot leak recovery into other engine tests");
        });
    }

    [EngineTest("Native player heartbeat recovers stamina from zero within six seconds", Category = "StaminaRecovery", TimeoutSeconds = 20f)]
    public static async Task NativeHeartbeat(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.SetRawAbilityScore(player, AbilityType.Might, 10);
        fixture.Update(record => { record.Stamina = 0; record.STMRegen = 0; });
        SetLocalInt(player, "NATURAL_STAMINA_REGEN_REMAINDER", 0);
        SetEventScript(player, EventScript.Creature_OnHeartbeat, ScriptName.OnPlayerHeartbeat);
        SetLocalInt(player, Stat.SuppressNaturalRegenVariable, 0);
        try
        {
            await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(player) > 0, 7f, "a real native six-second heartbeat");
            ctx.AssertEqual(2, Stat.GetCurrentStamina(player), "first heartbeat restores the low-Might baseline");
        }
        finally
        {
            SetLocalInt(player, Stat.SuppressNaturalRegenVariable, 1);
            SetEventScript(player, EventScript.Creature_OnHeartbeat, "");
        }
    }

    [EngineTest("Authored critical and area stamina refunds share the activation spend even on zero damage", Category = "StaminaRecovery")]
    public static async Task AuthoredRefundBudget(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var ability = Ability.GetAbilityDetail(FeatType.DoubleShot3);
        var profile = new WeaponAbilityProfile { RestoreStaminaIfAnyCriticalHit = 4 };
        var summary = new AbilityImpactSummary { Ability = ability, ImpactedTargetCount = 1, CriticalHitCount = 2 };
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            foreach (var cost in new[] { 10, 3, 1, 0 })
            {
                ctx.SetResources(player, 100, 25);
                Stat.ReduceStamina(player, cost);
                Combat.ApplyAbilityStaminaCostFPRestore(player, ability, cost);
                try
                {
                    var before = Stat.GetCurrentStamina(player);
                    profile.AfterImpact(player, 0, 0, summary);
                    ctx.AssertEqual(before + System.Math.Min(4, System.Math.Max(0, cost - 1)), Stat.GetCurrentStamina(player), "a landed critical refunds once even when damage is fully resisted");
                    Combat.RestoreAbilityHitStamina(player, ability, 100);
                    ctx.AssertEqual(cost > 0 ? 24 : 25, Stat.GetCurrentStamina(player), "combined authored and perk refunds leave at least one stamina spent");
                }
                finally { Combat.CompleteAbilityStaminaCostContext(player, ability); }
            }
        });
    }

    [EngineTest("Blade Vortex authored and passive area refunds cannot generate stamina", Category = "StaminaRecovery")]
    public static async Task BladeVortexCombinedRefunds(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var ability = Ability.GetAbilityDetail(FeatType.BladeVortex2);
        var targets = Enumerable.Range(1, 3).Select(index => ctx.SpawnCreature("nw_rat001", index)).ToArray();
        await ctx.WaitFrameAsync();
        foreach (var target in targets)
        {
            ctx.SuppressNPCNaturalRegen(target);
            ctx.MakeHostile(target);
            Stat.SetNPCMaxHitPoints(target, 10000, true);
            AssignCommand(target, () => ClearAllActions());
            SetCommandable(false, target);
        }
        TemporaryStatModifier.Add(player, StatType.AreaHitStaminaRestorePerTarget, 2, 60f, "stamina-area");
        TemporaryStatModifier.Add(player, StatType.AreaHitStaminaRestoreMaximum, 6, 60f, "stamina-area");
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.SetResources(player, 100, 25);
                ability.Requirements.OfType<AbilityRequirementStamina>().Single().AfterActivationAction(player, ability);
                Ability.BeginAbilityImpact(player, ability);
                AbilityImpactSummary summary;
                try { ability.ImpactAction(player, targets[0], 2, GetLocation(player)); }
                finally { summary = Ability.EndAbilityImpact(player); }
                try
                {
                    ctx.AssertEqual(3, summary.ImpactedTargetCount, "the real area hits three enemies");
                    Combat.ApplyAbilityImpactEffects(player, summary);
                    ctx.AssertEqual(24, Stat.GetCurrentStamina(player), "nine paid, six authored plus six passive requested, eight refunded");
                }
                finally { Combat.CompleteAbilityStaminaCostContext(player, ability); }
            });
        }
        finally { Combat.SetAbilityHitResolutionOverride(null); }
    }
}
