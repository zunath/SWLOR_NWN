using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class AngerShieldEngineTests
    {
        [EngineTest("Anger II's shield lasts the 30 seconds its description promises", Category = "BeastShield", TimeoutSeconds = 50f)]
        public static async Task AngerShieldLastsThirtySeconds(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            var enemy = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(beast);
            Stat.SetNPCMaxHitPoints(beast, 1000, true);
            ctx.MakeHostile(enemy);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 60f);

            var anger = Ability.GetAbilityDetail(FeatType.Anger2);
            Ability.BeginAbilityImpact(beast, anger);
            try
            {
                anger.ImpactAction(beast, enemy, 2, GetLocation(enemy));
            }
            finally
            {
                Ability.EndAbilityImpact(beast);
            }

            // 68 HP plus 3.75% of 1000 = 105.5, rounded up.
            ctx.AssertEqual(106, TemporaryHitPointEffects.GetRemaining(beast, "ANGER"), "Anger II grants its shield");
            await ctx.DelaySecondsAsync(20f);
            ctx.Assert(TemporaryHitPointEffects.GetRemaining(beast, "ANGER") > 0, "the shield outlasts the old 12 seconds");
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(beast, "ANGER") == 0, 15f,
                "the shield to expire at 30 seconds");
        }
    }
}
