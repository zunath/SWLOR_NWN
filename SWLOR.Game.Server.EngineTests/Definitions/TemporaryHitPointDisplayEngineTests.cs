using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TemporaryHitPointDisplayEngineTests
    {
        [EngineTest("Temporary HP display follows stacked pools, damage, replacement and expiry", Category = "StatusEffect", TimeoutSeconds = 20f)]
        public static async Task RemainingHitPointsFollowNativePools(EngineTestContext ctx)
        {
            var target = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            ctx.Assert(TemporaryHitPointEffects.GetRemaining(target) == 0, "no effects must display zero");

            TemporaryHitPointEffects.ApplyFlat(target, "DISPLAY_FIRST", 100, 60f);
            TemporaryHitPointEffects.ApplyFlat(target, "DISPLAY_SECOND", 50, 60f);
            ctx.Assert(TemporaryHitPointEffects.GetRemaining(target) == 150, "distinct pools must be summed");

            AssignCommand(target, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(20), target));
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(target) == 130,
                3f, "absorbed damage to reduce the displayed pool");

            TemporaryHitPointEffects.Remove(target, "DISPLAY_FIRST");
            TemporaryHitPointEffects.Remove(target, "DISPLAY_SECOND");
            ctx.Assert(TemporaryHitPointEffects.GetRemaining(target) == 0, "removed pools must disappear");

            TemporaryHitPointEffects.ApplyFlat(target, "DISPLAY_FIRST", 100, 60f);
            TemporaryHitPointEffects.ApplyFlat(target, "DISPLAY_FIRST", 25, 1f);
            ctx.Assert(TemporaryHitPointEffects.GetRemaining(target) == 25, "replacement must not retain the old amount");
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(target) == 0,
                5f, "expiry to clear the displayed amount");
        }
    }
}
