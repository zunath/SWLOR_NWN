using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TelegraphedAbilityEngineTests
    {
        [EngineTest("A targeted circle hits its warned location after the target dodges", Category = "TelegraphedAbility", TimeoutSeconds = 25f)]
        public static Task CircleCanBeDodged(EngineTestContext ctx) =>
            AssertDodgeAsync(ctx, FeatType.GrenadeBurst, TelegraphType.Sphere, typeof(BurnStatusEffect));

        [EngineTest("A targeted cone keeps its warned direction after the target dodges", Category = "TelegraphedAbility", TimeoutSeconds = 25f)]
        public static Task ConeCanBeDodged(EngineTestContext ctx) =>
            AssertDodgeAsync(ctx, FeatType.BarbedVolley, TelegraphType.Cone, typeof(BleedStatusEffect));

        [EngineTest("A targeted line keeps its warned direction after the target dodges", Category = "TelegraphedAbility", TimeoutSeconds = 25f)]
        public static Task LineCanBeDodged(EngineTestContext ctx) =>
            AssertDodgeAsync(ctx, FeatType.GoringCharge, TelegraphType.Line, typeof(BleedStatusEffect));

        [EngineTest("A second impact warning preserves the original cone after the target dodges", Category = "TelegraphedAbility", TimeoutSeconds = 25f)]
        public static Task SecondaryWarningKeepsOriginalCone(EngineTestContext ctx) =>
            AssertDodgeAsync(ctx, FeatType.InfernoBlast, TelegraphType.Cone, typeof(BurnStatusEffect), true);

        [EngineTest("A separately delayed impact keeps its warned circle after the target dodges", Category = "TelegraphedAbility", TimeoutSeconds = 25f)]
        public static Task SeparateImpactDelayKeepsOriginalCircle(EngineTestContext ctx) =>
            AssertDodgeAsync(ctx, FeatType.GrenadeBurst, TelegraphType.Sphere, typeof(BurnStatusEffect), impactDelay: 0.5f);

        private static async Task AssertDodgeAsync(
            EngineTestContext ctx, FeatType feat, TelegraphType shape, Type statusEffect,
            bool hasSecondaryWarning = false, float? impactDelay = null)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var dodger = ctx.SpawnCreature("nw_rat001", 2f, 0f);
            var stationary = ctx.SpawnCreature("nw_rat001", 1f, 0f);
            var ability = Ability.GetAbilityDetail(feat);
            var wasTechnique = ability.IsMimicryTechnique;
            var originalImpactDelay = ability.ImpactDelay;
            var originalImpact = ability.ImpactAction;
            var impactStarted = false;

            // Keep auto-attacks from masking the ability's outcome and remove normal hit-roll
            // randomness. Damage, targeting, activation and status payloads remain unchanged.
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                ability.IsMimicryTechnique = false;
                if (impactDelay.HasValue)
                    ability.ImpactDelay = impactDelay.Value;
                ability.ImpactAction = (activator, target, level, location) =>
                {
                    impactStarted = true;
                    originalImpact(activator, target, level, location);
                };
                foreach (var target in new[] { dodger, stationary })
                {
                    ctx.MakeHostile(target);
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), target, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), target, 60f);
                    ctx.SuppressNPCNaturalRegen(target);
                }

                await ctx.WaitFrameAsync();
                ctx.SetNPCResources(caster, 100, 100);
                ctx.SuppressNPCNaturalRegen(caster);
                var dodgerHP = GetCurrentHitPoints(dodger);
                var stationaryHP = GetCurrentHitPoints(stationary);
                TelegraphGeometry warning = default;

                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    // Native creation can shift spawns around placed NPCs. Keep the witness
                    // between the caster and its visible selected target instead of across a wall.
                    var witnessPosition = GetPosition(caster) + (GetPosition(dodger) - GetPosition(caster)) * 0.75f;
                    witnessPosition.Y += 0.15f;
                    ObjectPlugin.SetPosition(stationary, witnessPosition);
                    ctx.Assert(LineOfSightObject(caster, stationary) &&
                               LineOfSightVector(GetPosition(caster), GetPosition(stationary)),
                        "The stationary target must be visible before activation.");
                    ctx.Assert(GetIsReactionTypeHostile(stationary, caster), "The stationary target must be hostile to the caster.");
                    ctx.Assert(UsePerkFeat.TryUseAbility(caster, dodger, feat, GetLocation(dodger)),
                        $"{feat} must activate successfully.");
                    var marker = Telegraph.GetTelegraphsInArea(ctx.Arena)
                        .Single(pair => pair.Value.Data.Creator == caster && pair.Value.Data.Shape == shape);
                    warning = Telegraph.CaptureGeometry(new[] { marker.Key }).Single();
                    ctx.Assert(Telegraph.IsCreatureInTelegraph(dodger, marker.Key), "The selected target starts inside the warning.");
                    ctx.Assert(Telegraph.IsCreatureInTelegraph(stationary, marker.Key), "The stationary target starts inside the warning.");

                    // Find a visible escape inside this arena. A hardcoded northward move can
                    // cross a wall and cancel the cast rather than exercising its area damage.
                    var casterPosition = GetPosition(caster);
                    var escaped = false;
                    foreach (var offset in new[] { (-3f, 0f), (0f, -5f), (0f, 5f), (-3f, -3f), (-3f, 3f) })
                    {
                        var escapePosition = casterPosition;
                        escapePosition.X += offset.Item1;
                        escapePosition.Y += offset.Item2;
                        ObjectPlugin.SetPosition(dodger, escapePosition);
                        if (!Telegraph.IsCreatureInTelegraph(dodger, marker.Key) &&
                            GetDistanceBetween(caster, dodger) < ability.MaxRange &&
                            LineOfSightObject(caster, dodger) && LineOfSightVector(GetPosition(caster), GetPosition(dodger)))
                        {
                            escaped = true;
                            break;
                        }
                    }
                    ctx.Assert(escaped, "The target must escape the warning while staying visible and within cast range.");
                    ctx.Log($"{feat}: warning={warning.Position}, caster={GetPosition(caster)}, dodger={GetPosition(dodger)}, stationary={GetPosition(stationary)}.");
                });

                if (hasSecondaryWarning)
                {
                    await ctx.WaitUntilAsync(() => Telegraph.GetTelegraphsInArea(ctx.Arena)
                            .Any(pair => pair.Value.Data.Creator == caster && pair.Value.Data.Action != null),
                        6f, "the ability's second, damaging warning");
                    var marker = Telegraph.GetTelegraphsInArea(ctx.Arena)
                        .Single(pair => pair.Value.Data.Creator == caster && pair.Value.Data.Action != null);
                    var delayedWarning = Telegraph.CaptureGeometry(new[] { marker.Key }).Single();
                    ctx.Assert(warning.Matches(delayedWarning), "The second warning must describe the original footprint.");
                    ctx.Assert(!Telegraph.IsCreatureInTelegraph(dodger, marker.Key), "The second warning must not follow the dodger.");
                }

                await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect(stationary, statusEffect),
                    8f, "the stationary target to receive the ability's status payload");
                await ctx.WaitUntilAsync(() => GetCurrentHitPoints(stationary) < stationaryHP,
                    3f, "the stationary target to take damage from the warned attack");

                ctx.Assert(GetLastDamager(stationary) == caster, "The tested caster must cause the stationary target's damage.");
                ctx.Assert(GetCurrentHitPoints(dodger) == dodgerHP,
                    $"The dodger must take no damage after leaving the warning ({dodgerHP} -> {GetCurrentHitPoints(dodger)}).");
                ctx.Assert(!StatusEffect.HasStatusEffect(dodger, statusEffect), "The dodger must receive no status payload.");
            }
            catch (EngineTestAssertionException ex)
            {
                throw new EngineTestAssertionException($"{ex.Message} (impactStarted={impactStarted}, " +
                    $"denial={Ability.GetLastActivationDenialReason()}, casterHP={GetCurrentHitPoints(caster)}, " +
                    $"distance={GetDistanceBetween(caster, dodger)}, busy={Activity.IsBusy(caster)})");
            }
            finally
            {
                ability.IsMimicryTechnique = wasTechnique;
                ability.ImpactDelay = originalImpactDelay;
                ability.ImpactAction = originalImpact;
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
                foreach (var marker in Telegraph.GetTelegraphsInArea(ctx.Arena)
                             .Where(pair => pair.Value.Data.Creator == caster))
                    Telegraph.CancelTelegraph(marker.Key);
            }
        }
    }
}
