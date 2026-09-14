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
        /// <summary>Compares seeded real technique impacts across all Momentum stacks and additive Overload potency.</summary>
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
                    ctx.Log($"Testing {feat}: resetting Momentum and Overload for its baseline.");
                    StatusEffect.RemoveStatusEffect<FinishingDriveMomentumStatusEffect>(caster);
                    StatusEffect.RemoveStatusEffect<OverloadStatusEffect>(caster);
                    ctx.AssertEqual(0, Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent), "unbuffed fixture potency");
                    var baseline = (await MeasureImpactAsync(ctx, caster, feat))[0];
                    ctx.Log($"{feat}: baseline damage={baseline} at zero potency.");
                    for (var stack = 1; stack <= 3; stack++)
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster,
                            () => momentum.ImpactAction(caster, caster, 1, GetLocation(caster)));
                        var damage = (await MeasureImpactAsync(ctx, caster, feat))[0];
                        var expected = WithPotency(baseline, stack * 8);
                        ctx.Log($"{feat}: stacks={stack}, potency={Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent)}%, baseline={baseline}, actual={damage}, expected={expected}.");
                        ctx.AssertEqual(expected, damage,
                            $"{feat} with {stack} Momentum stacks scales its entire damage once");
                    }

                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => overload.ImpactAction(caster, caster, 1, GetLocation(caster)));
                    var overloadedDamage = (await MeasureImpactAsync(ctx, caster, feat))[0];
                    var overloadedExpected = WithPotency(baseline, 74);
                    ctx.Log($"{feat} with Overload: stacks=3, potency={Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent)}%, baseline={baseline}, actual={overloadedDamage}, expected={overloadedExpected}.");
                    ctx.AssertEqual(overloadedExpected, overloadedDamage,
                        $"{feat} adds 50% Overload to 24% Momentum before applying the multiplier");
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        /// <summary>Checks primary and secondary chain damage independently to detect missing or duplicate potency scaling.</summary>
        [EngineTest("Mimicry potency scales primary and chain impacts exactly once", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task PrimaryAndChainDamageReceiveOnePotencyBonus(EngineTestContext ctx)
        {
            var caster = await CreateCasterAsync(ctx);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                ctx.Log("Testing Inner Circle Surge primary and chain impacts with zero and three Momentum stacks.");
                var baseline = await MeasureImpactAsync(ctx, caster, FeatType.InnerCircleSurgeTechnique, withChainTarget: true);
                StatusEffect.ApplyStatusEffect(caster, caster, new FinishingDriveMomentumStatusEffect(3), 30f);
                var boosted = await MeasureImpactAsync(ctx, caster, FeatType.InnerCircleSurgeTechnique, withChainTarget: true);
                for (var index = 0; index < baseline.Length; index++)
                {
                    var expected = WithPotency(baseline[index], 24);
                    ctx.Log($"Inner Circle Surge impact {index}: potency={Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent)}%, baseline={baseline[index]}, actual={boosted[index]}, expected={expected}.");
                    ctx.AssertEqual(expected, boosted[index], $"impact {index} gains exactly 24%");
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        /// <summary>Verifies live potency aggregation, rounding, skill and attack exclusions, and the shared outgoing damage cap.</summary>
        [EngineTest("Mimicry potency respects outgoing damage limits and attack boundaries", Category = "PerkTracker", TimeoutSeconds = 15f)]
        public static async Task PotencyDamageLimitsAndExclusions(EngineTestContext ctx)
        {
            var caster = await CreateCasterAsync(ctx);
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            PrepareCreature(ctx, target);
            StatusEffect.ApplyStatusEffect(caster, caster, new FinishingDriveMomentumStatusEffect(3), 30f);
            StatusEffect.ApplyStatusEffect(caster, caster, new OverloadStatusEffect(), 30f);
            ctx.Log($"Rounding and exclusion setup: caster={caster}, target={target}, stacks=3, Overload active, potency={Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent)}%, expected potency=74%.");
            ctx.AssertEqual(74, Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent), "potency sources add");
            var roundedDamage = Combat.ApplyMimicryAbilityDamageModifier(caster, 9, SkillType.Mimicry, true);
            ctx.Log($"Fractional bonus: baseline=9, actual={roundedDamage}, expected=16.");
            ctx.AssertEqual(16, roundedDamage, "fractional bonuses round up once");
            var autoAttackDamage = Combat.ApplyMimicryAbilityDamageModifier(caster, 100, SkillType.Mimicry, false);
            ctx.Log($"Auto-attack exclusion: baseline=100, actual={autoAttackDamage}, expected=100.");
            ctx.AssertEqual(100, autoAttackDamage, "auto-attacks remain unchanged");
            var npcAbilityDamage = Combat.ApplyMimicryAbilityDamageModifier(caster, 100, SkillType.BeastMastery, true);
            ctx.Log($"NPC skill exclusion: skill=BeastMastery, baseline=100, actual={npcAbilityDamage}, expected=100.");
            ctx.AssertEqual(100, npcAbilityDamage, "NPC originals remain unchanged");
            TemporaryStatModifier.Add(caster, StatType.DamageDealtPercentAdjustment, 100, 30f);
            var cappedDamage = Combat.ApplyDamageDealtModifiers(caster, target, 100, SkillType.Mimicry, CombatDamageType.Physical,
                isAbilityDamage: true, canApplyRandomFlatBonuses: false);
            ctx.Log($"Outgoing cap: potency=74%, generic damage bonus=100%, baseline=100, actual={cappedDamage}, expected=200.");
            ctx.AssertEqual(200, cappedDamage, "combined buffs obey the shared 100% outgoing damage bonus cap");
        }

        /// <summary>Creates a stationary caster whose high scaling attributes expose an incorrect base-only damage bonus.</summary>
        private static async Task<uint> CreateCasterAsync(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareCreature(ctx, caster);
            // Large attribute scaling makes an incorrect base-only bonus distinguishable.
            foreach (var ability in new[] { AbilityType.Might, AbilityType.Perception, AbilityType.Social })
                CreaturePlugin.SetRawAbilityScore(caster, ability, 28);
            ctx.Log($"Prepared caster={caster} with Might, Perception, and Social set to 28.");
            return caster;
        }

        /// <summary>Measures seeded direct HP loss against fresh targets, waits for delayed impacts, and destroys targets before status ticks.</summary>
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
                var stacks = (StatusEffect.GetStatusEffect(caster, typeof(FinishingDriveMomentumStatusEffect)) as FinishingDriveMomentumStatusEffect)?.Stacks ?? 0;
                ctx.Log($"Measuring {feat}: caster={caster}, targets=[{string.Join(", ", targets)}], chain={withChainTarget}, stacks={stacks}, potency={Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent)}%, starting HP=[{string.Join(", ", startingHP)}], seed=20260908.");
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    ctx.SeedRandom(20260908);
                    Ability.BeginAbilityImpact(caster, ability);
                    try { ability.ImpactAction(caster, targets[0], 1, GetLocation(targets[0])); }
                    finally { Ability.EndAbilityImpact(caster); }
                });
                ctx.Log($"{feat}: impact dispatched; waiting up to 3 seconds for all immediate or delayed direct hits.");
                await ctx.WaitUntilAsync(() => targets.Select((target, index) =>
                    GetCurrentHitPoints(target) < startingHP[index] && GetLastDamager(target) == caster).All(hit => hit),
                    3f, $"{feat} direct impacts to land before any damage-over-time tick");
                var damage = targets.Select((target, index) => startingHP[index] - GetCurrentHitPoints(target)).ToArray();
                ctx.Log($"{feat}: observed direct damage=[{string.Join(", ", damage)}].");
                return damage;
            }
            finally
            {
                ctx.Log($"{feat}: destroying impact targets and settling one frame after measurement or delayed-impact failure.");
                foreach (var target in targets)
                    DestroyObject(target);
                await ctx.WaitFrameAsync();
                ctx.Log($"{feat}: target cleanup frame completed.");
            }
        }

        /// <summary>Calculates the expected total damage using exact decimal arithmetic and one upward rounding step.</summary>
        private static int WithPotency(int damage, int percent)
        {
            return damage + (int)Math.Ceiling(damage * percent / 100m);
        }

        /// <summary>Disables regeneration and movement and supplies enough HP to survive every measured impact.</summary>
        private static void PrepareCreature(EngineTestContext ctx, uint creature)
        {
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 120f);
        }
    }
}
