using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TemporaryHitPointCapEngineTests
    {
        [EngineTest("Temporary HP from every source tops out at half of maximum HP", Category = "StatusEffect", TimeoutSeconds = 20f)]
        public static async Task PoolsShareOneCap(EngineTestContext ctx)
        {
            var target = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            ctx.AssertEqual(500, TemporaryHitPointEffects.GetMaximumTotal(target), "cap is half of maximum HP");

            ctx.AssertEqual(300, TemporaryHitPointEffects.ApplyFlat(target, "CAP_FIRST", 300, 60f), "first pool fits under the cap");
            ctx.AssertEqual(200, TemporaryHitPointEffects.ApplyFlat(target, "CAP_SECOND", 400, 60f), "second pool tops up to the cap");
            ctx.AssertEqual(500, TemporaryHitPointEffects.GetRemaining(target), "pools total the cap");
            ctx.AssertEqual(0, TemporaryHitPointEffects.ApplyFlat(target, "CAP_THIRD", 50, 60f), "a full cap grants nothing");
            ctx.AssertEqual(500, TemporaryHitPointEffects.GetRemaining(target), "a refused grant adds nothing");

            ctx.AssertEqual(300, TemporaryHitPointEffects.ApplyFlat(target, "CAP_FIRST", 450, 60f),
                "a replaced pool is measured without its own old amount");
            ctx.AssertEqual(500, TemporaryHitPointEffects.GetRemaining(target), "replacement keeps the total at the cap");

            AssignCommand(target, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(100), target));
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(target) == 400,
                3f, "absorbed damage to open room under the cap");
            ctx.AssertEqual(100, TemporaryHitPointEffects.ApplyFlat(target, "CAP_THIRD", 250, 60f), "spent shields make room again");
        }

        [EngineTest("Temporary HP is trimmed back under the cap when maximum HP drops", Category = "StatusEffect", TimeoutSeconds = 20f)]
        public static async Task PoolsTrimWhenMaximumHitPointsDrop(EngineTestContext ctx)
        {
            var target = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            TemporaryHitPointEffects.ApplyFlatFromSource(target, target, "TRIM_SHORT", 200, 20f);
            TemporaryHitPointEffects.ApplyFlatFromSource(target, target, "TRIM_LONG", 300, 60f);
            ctx.AssertEqual(500, TemporaryHitPointEffects.GetRemaining(target), "pools fill the cap");

            Stat.SetNPCMaxHitPoints(target, 600);
            ctx.AssertEqual(300, TemporaryHitPointEffects.GetRemaining(target), "pools shrink to the new cap");
            ctx.Assert(!TemporaryHitPointEffects.IsActivePoolFromSource(target, "TRIM_SHORT", target), "the pool ending soonest is trimmed first");
            ctx.Assert(TemporaryHitPointEffects.IsActivePoolFromSource(target, "TRIM_LONG", target), "the longer pool keeps its source");

            AssignCommand(target, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(100), target));
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(target) == 200,
                3f, "the trimmed pool to keep absorbing damage");
        }

        [EngineTest("A Vitality penalty trims temporary HP on any creature", Category = "StatusEffect", TimeoutSeconds = 20f)]
        public static async Task VitalityPenaltyTrimsPools(EngineTestContext ctx)
        {
            var target = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            TemporaryHitPointEffects.ApplyFlat(target, "TRIM_VITALITY", 500, 60f);
            ctx.AssertEqual(500, TemporaryHitPointEffects.GetRemaining(target), "the pool fills the cap");

            StatusEffect.ApplyStatusEffect(target, target, typeof(DiseaseVitalityPenaltyStatusEffect), 30f);
            ctx.Assert(GetMaxHitPoints(target) < 1000, $"the penalty lowers max HP (now {GetMaxHitPoints(target)})");
            ctx.AssertEqual(TemporaryHitPointEffects.GetMaximumTotal(target), TemporaryHitPointEffects.GetRemaining(target),
                "the pool is trimmed to the lowered cap as the penalty lands");
        }

        [EngineTest("A status effect tracking a trimmed pool is told about the trim", Category = "StatusEffect", TimeoutSeconds = 20f)]
        public static async Task PoolTrackersFollowTrims(EngineTestContext ctx)
        {
            var target = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            TemporaryHitPointEffects.ApplyFlat(target, "TRIM_OTHER", 200, 60f);
            TemporaryHitPointEffects.ApplyFlat(target, TemporaryHitPointEffectKey.GuardiansResolve, 300, 20f);
            StatusEffect.ApplyStatusEffect(target, target, new GuardiansResolveStatusEffect(300), 20f);
            await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<GuardiansResolveStatusEffect>(target),
                5f, "Guardian's Resolve to apply");

            Stat.SetNPCMaxHitPoints(target, 400);
            ctx.AssertEqual(200, TemporaryHitPointEffects.GetRemaining(target), "the Guardian's Resolve pool ends soonest and is trimmed away");
            ctx.Assert(!StatusEffect.HasStatusEffect<GuardiansResolveStatusEffect>(target),
                "Guardian's Resolve ends with its pool instead of healing for shield it no longer has");
        }
    }
}
