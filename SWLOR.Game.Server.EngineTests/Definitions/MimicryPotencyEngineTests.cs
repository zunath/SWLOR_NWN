using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class MimicryPotencyEngineTests
    {
        [EngineTest("Finishing Drive and Overload amplify complete technique damage", Category = "PerkTracker", TimeoutSeconds = 90f)]
        public static async Task TechniqueDamageScalesWithMomentumAndOverload(EngineTestContext ctx)
        {
            var caster = await CreateCasterAsync(ctx);
            var momentum = Ability.GetAbilityDetail(FeatType.FinishingDriveTechnique);
            var overload = Ability.GetAbilityDetail(FeatType.Overload);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                // The two reported failures cover the innate single-target and area factories;
                // Inferno Blast has a custom impact delayed by half a second.
                foreach (var feat in new[] { FeatType.BarbedVolleyTechnique, FeatType.BrutalBashTechnique, FeatType.InfernoBlastTechnique })
                {
                    StatusEffect.RemoveStatusEffect<FinishingDriveMomentumStatusEffect>(caster);
                    StatusEffect.RemoveStatusEffect<OverloadStatusEffect>(caster);
                    ctx.AssertEqual(0, Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent), "unbuffed fixture potency");
                    var baseline = (await MeasureImpactAsync(ctx, caster, feat))[0];
                    for (var stack = 1; stack <= 3; stack++)
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster,
                            () => momentum.ImpactAction(caster, caster, 1, GetLocation(caster)));
                        var damage = (await MeasureImpactAsync(ctx, caster, feat))[0];
                        ctx.AssertEqual(WithPotency(baseline, stack * 8), damage,
                            $"{feat} with {stack} Momentum stacks scales its entire damage once");
                    }

                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => overload.ImpactAction(caster, caster, 1, GetLocation(caster)));
                    var overloadedDamage = (await MeasureImpactAsync(ctx, caster, feat))[0];
                    ctx.AssertEqual(WithPotency(baseline, 74), overloadedDamage,
                        $"{feat} adds 50% Overload to 24% Momentum before applying the multiplier");
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Mimicry potency scales primary and chain impacts exactly once", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task PrimaryAndChainDamageReceiveOnePotencyBonus(EngineTestContext ctx)
        {
            var caster = await CreateCasterAsync(ctx);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                var baseline = await MeasureImpactAsync(ctx, caster, FeatType.InnerCircleSurgeTechnique, withChainTarget: true);
                StatusEffect.ApplyStatusEffect(caster, caster, new FinishingDriveMomentumStatusEffect(3), 30f);
                var boosted = await MeasureImpactAsync(ctx, caster, FeatType.InnerCircleSurgeTechnique, withChainTarget: true);
                for (var index = 0; index < baseline.Length; index++)
                    ctx.AssertEqual(WithPotency(baseline[index], 24), boosted[index], $"impact {index} gains exactly 24%");
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Mimicry potency respects outgoing damage limits and attack boundaries", Category = "PerkTracker", TimeoutSeconds = 15f)]
        public static async Task PotencyDamageLimitsAndExclusions(EngineTestContext ctx)
        {
            var caster = await CreateCasterAsync(ctx);
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            PrepareCreature(ctx, target);
            StatusEffect.ApplyStatusEffect(caster, caster, new FinishingDriveMomentumStatusEffect(3), 30f);
            StatusEffect.ApplyStatusEffect(caster, caster, new OverloadStatusEffect(), 30f);
            ctx.AssertEqual(74, Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent), "potency sources add");
            ctx.AssertEqual(16, Combat.ApplyMimicryAbilityDamageModifier(caster, 9, SkillType.Mimicry, true), "fractional bonuses round up once");
            ctx.AssertEqual(100, Combat.ApplyMimicryAbilityDamageModifier(caster, 100, SkillType.Mimicry, false), "auto-attacks remain unchanged");
            ctx.AssertEqual(100, Combat.ApplyMimicryAbilityDamageModifier(caster, 100, SkillType.BeastMastery, true), "NPC originals remain unchanged");
            TemporaryStatModifier.Add(caster, StatType.DamageDealtPercentAdjustment, 100, 30f);
            ctx.AssertEqual(200, Combat.ApplyDamageDealtModifiers(caster, target, 100, SkillType.Mimicry, CombatDamageType.Physical,
                isAbilityDamage: true, canApplyRandomFlatBonuses: false), "combined buffs obey the shared 100% outgoing damage bonus cap");
        }

        private static async Task<uint> CreateCasterAsync(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareCreature(ctx, caster);
            // Large attribute scaling makes an incorrect base-only bonus distinguishable.
            foreach (var ability in new[] { AbilityType.Might, AbilityType.Perception, AbilityType.Social })
                CreaturePlugin.SetRawAbilityScore(caster, ability, 28);
            return caster;
        }

        private static async Task<int[]> MeasureImpactAsync(EngineTestContext ctx, uint caster, FeatType feat, bool withChainTarget = false)
        {
            var targets = withChainTarget
                ? new[] { ctx.SpawnCreature("nw_rat001", 2f), ctx.SpawnCreature("nw_rat001", 4f) }
                : new[] { ctx.SpawnCreature("nw_rat001", 2f) };
            await ctx.WaitFrameAsync();
            foreach (var target in targets)
            {
                PrepareCreature(ctx, target);
                ctx.MakeHostile(target);
            }

            try
            {
                var startingHP = targets.Select(GetCurrentHitPoints).ToArray();
                var ability = Ability.GetAbilityDetail(feat);
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    ctx.SeedRandom(20260908);
                    Ability.BeginAbilityImpact(caster, ability);
                    try { ability.ImpactAction(caster, targets[0], 1, GetLocation(targets[0])); }
                    finally { Ability.EndAbilityImpact(caster); }
                });
                await ctx.WaitUntilAsync(() => targets.Select((target, index) =>
                    GetCurrentHitPoints(target) < startingHP[index] && GetLastDamager(target) == caster).All(hit => hit),
                    3f, $"{feat} direct impacts to land before any damage-over-time tick");
                return targets.Select((target, index) => startingHP[index] - GetCurrentHitPoints(target)).ToArray();
            }
            finally
            {
                foreach (var target in targets)
                    DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
        }

        private static int WithPotency(int damage, int percent)
        {
            return damage + (int)Math.Ceiling(damage * percent / 100m);
        }

        private static void PrepareCreature(EngineTestContext ctx, uint creature)
        {
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 120f);
        }
    }
}
