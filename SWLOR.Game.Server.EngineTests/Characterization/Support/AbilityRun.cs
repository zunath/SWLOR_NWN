using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>
/// Runs one ability from the ability behavior suite through the real activation pipeline (<c>UsePerkFeat.TryUseAbility</c>)
/// between fixed creatures and records what it left behind: whether it was accepted, what it cost, the damage or healing
/// it dealt, the status effects it applied with their remaining durations, and the recast it started. The fixtures
/// (weapon, perk levels, wounds, pre-applied effects) come from the ability's behavior case.
/// </summary>
public static class AbilityRun
{
    private const int ResourcePool = 9999;
    private const float ImpactWaitSeconds = 25f;
    private const float StanceWaitSeconds = 4f;
    private const float DamageWaitSeconds = 6f;
    private const int FramesToSettle = 4;
    private const string QueuedFallbackWeapon = "nw_wswss001";
    private const string NpcRecastLocalPrefix = "ABILITY_RECAST_ID_";

    public static readonly string[] InputColumns = { "ability", "feat", "activation", "target", "recastGroup" };

    public static readonly string[] OutputColumns =
    {
        "accepted", "denial", "impact", "fpSpent", "staminaSpent", "casterHP", "targetHP", "targetEnmity", "casterEffects", "targetEffects", "queued",
        "recast"
    };

    /// <summary>Runs the case and returns the snapshot row: the input cells followed by the output cells.</summary>
    public static async Task<object[]> RunAsync(EngineTestContext ctx, AbilityBehaviorCase behaviorCase, int seed)
    {
        ctx.Assert(Ability.IsFeatRegistered(behaviorCase.Feat), $"{behaviorCase.Feat} is registered to an ability");
        var ability = Ability.GetAbilityDetail(behaviorCase.Feat);

        var caster = await ScenarioActors.SpawnQuietAsync(ctx, ScenarioActors.Humanoid, -0.5f, 0f);
        var target = caster;
        switch (behaviorCase.Target)
        {
            case AbilityTargetKind.HostileCreature:
                target = await ScenarioActors.SpawnQuietAsync(ctx, ScenarioActors.Humanoid, -0.5f + behaviorCase.TargetDistanceMeters, 0f);
                await ScenarioActors.MakeDummyAsync(ctx, target);
                break;
            case AbilityTargetKind.FriendlyCreature:
                target = await ScenarioActors.SpawnQuietAsync(ctx, ScenarioActors.Humanoid, -0.5f + behaviorCase.TargetDistanceMeters, 0f);
                break;
        }

        try
        {
            foreach (var effectType in behaviorCase.TargetSetupStatusEffects)
                StatusEffect.ApplyStatusEffect(caster, target, effectType, 60f);

            ctx.SetResources(caster, ResourcePool, ResourcePool);
            CreaturePlugin.AddFeat(caster, behaviorCase.Feat);

            foreach (var (perk, level) in behaviorCase.SetupNPCPerkLevels)
                ctx.SetNPCPerkLevel(caster, perk, level);

            if (!string.IsNullOrWhiteSpace(behaviorCase.EquipOffHandResref))
                await ctx.EquipItemAsync(caster, behaviorCase.EquipOffHandResref, InventorySlot.LeftHand);
            if (!string.IsNullOrWhiteSpace(behaviorCase.EquipMainHandResref))
                await ctx.EquipItemAsync(caster, behaviorCase.EquipMainHandResref, InventorySlot.RightHand);
            if (ability.ActivationType == AbilityActivationType.Weapon && !GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, caster)))
                await ctx.EquipItemAsync(caster, QueuedFallbackWeapon, InventorySlot.RightHand);
            if (ability.ActivationType == AbilityActivationType.Weapon)
                ctx.ApplyStandardOnHitProperty(GetItemInSlot(InventorySlot.RightHand, caster));

            if (behaviorCase.ExpectsActivatorHealing)
                ApplyEffectToObject(DurationType.Instant, EffectDamage(GetMaxHitPoints(caster) / 2), caster);
            if (behaviorCase.ExpectsTargetHealing && target != caster)
                ApplyEffectToObject(DurationType.Instant, EffectDamage(GetMaxHitPoints(target) / 2), target);
            if (behaviorCase.TargetHitPointPercentBeforeActivation is { } percent)
            {
                var desired = Math.Max(1, GetMaxHitPoints(target) * percent / 100);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(GetCurrentHitPoints(target) - desired), target);
            }

            await Settle(ctx);

            var fpBefore = Stat.GetCurrentFP(caster);
            var staminaBefore = Stat.GetCurrentStamina(caster);
            var casterHPBefore = GetCurrentHitPoints(caster);
            var targetHPBefore = GetCurrentHitPoints(target);
            var casterEffectsBefore = StatusEffect.GetCreatureStatusEffects(caster).GetAllEffects().Count;
            Ability.ClearLastCompletedAbilityImpactSummary(caster);

            var accepted = false;
            var denial = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                ctx.SeedRandom(seed);
                accepted = UsePerkFeat.TryUseAbility(caster, target, behaviorCase.Feat, GetLocation(target));
                if (!accepted)
                    denial = Ability.GetLastActivationDenialReason();
            });

            if (accepted)
            {
                if (ability.ActivationType == AbilityActivationType.Casted)
                {
                    await WaitFramesUntil(ctx, () => Ability.GetLastCompletedAbilityImpactSummary(caster) != null, ImpactWaitSeconds);
                }
                else if (ability.ActivationType == AbilityActivationType.Stance)
                {
                    await WaitFramesUntil(ctx, () => StatusEffect.GetCreatureStatusEffects(caster).GetAllEffects().Count > casterEffectsBefore
                                                     || Ability.GetLastCompletedAbilityImpactSummary(caster) != null, StanceWaitSeconds);
                }

                // The impact summary is completed before the queued damage lands; wait for the hit points to move.
                var impact = Ability.GetLastCompletedAbilityImpactSummary(caster);
                if (impact is { AttributedDamage: > 0 } && target != caster)
                    await WaitFramesUntil(ctx, () => GetCurrentHitPoints(target) < targetHPBefore, DamageWaitSeconds);

                // Stop the activator's resumed auto-attack before it swings.
                await ctx.ExecuteInCreatureContextAsync(caster, () => ClearAllActions());
                await Settle(ctx);
            }

            var summary = Ability.GetLastCompletedAbilityImpactSummary(caster);
            var queued = ability.ActivationType == AbilityActivationType.Weapon && UsePerkFeat.IsWeaponAbilityQueued(caster, behaviorCase.Feat);
            var row = new List<object>
            {
                behaviorCase.Feat.ToString(),
                (int)behaviorCase.Feat,
                ability.ActivationType.ToString(),
                behaviorCase.Target.ToString(),
                ability.RecastGroup.ToString(),
                accepted,
                denial,
                summary == null
                    ? "none"
                    : $"targets {summary.ImpactedTargetCount}, critical hits {summary.CriticalHitCount}, damage {summary.AttributedDamage}, skill {summary.SkillType}",
                fpBefore - Stat.GetCurrentFP(caster),
                staminaBefore - Stat.GetCurrentStamina(caster),
                GetCurrentHitPoints(caster) - casterHPBefore,
                target == caster ? 0 : GetCurrentHitPoints(target) - targetHPBefore,
                target == caster ? 0 : Enmity.GetEnmityTable(target).GetValueOrDefault(caster),
                DescribeEffects(caster),
                target == caster ? new List<string>() : DescribeEffects(target),
                queued,
                DescribeRecast(caster, ability.RecastGroup)
            };

            if (queued)
                UsePerkFeat.ClearQueuedAbility(caster);
            return row.ToArray();
        }
        finally
        {
            StatusEffect.RemoveAllStatusEffects(caster);
            if (target != caster)
                StatusEffect.RemoveAllStatusEffects(target);
        }
    }

    /// <summary>
    /// The recast the activation started on the caster, as seconds from the frozen clock until it ends, or "none".
    /// <c>Recast</c> keeps a non-player creature's cooldown as a local string holding the end time, which is read back here.
    /// </summary>
    private static string DescribeRecast(uint creature, RecastGroup group)
    {
        if (group == RecastGroup.Invalid)
            return "none";

        if (!RecastTimestamp.TryParse(GetLocalString(creature, NpcRecastLocalPrefix + (int)group), out var endsAt))
            return "none";

        var seconds = Math.Round((endsAt - Clock.UtcNow).TotalSeconds, 3);
        return seconds.ToString("0.###", CultureInfo.InvariantCulture) + "s";
    }

    private static List<string> DescribeEffects(uint creature)
    {
        var now = Clock.UtcNow;
        return StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects()
            .Select(effect => $"{effect.Name}:{Math.Round(effect.GetRemainingDurationSeconds(now))}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task Settle(EngineTestContext ctx)
    {
        for (var frame = 0; frame < FramesToSettle; frame++)
            await ctx.WaitFrameAsync();
    }

    private static async Task WaitFramesUntil(EngineTestContext ctx, Func<bool> condition, float timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!condition() && DateTime.UtcNow < deadline)
            await ctx.WaitFrameAsync();
    }
}
