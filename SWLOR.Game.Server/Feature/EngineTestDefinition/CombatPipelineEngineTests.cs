using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition
{
    /// <summary>
    /// End-to-end combat smoke test: commands a base-game humanoid to attack a hostile-faction
    /// target and confirms the target's hit points drop, proving the native attack-roll and
    /// damage-roll hook pipeline runs end-to-end against real creatures.
    /// </summary>
    public static class CombatPipelineEngineTests
    {
        private const int Seed = 20260721;

        [EngineTest("A commanded attack against a hostile target reduces its hit points", Category = "Combat", TimeoutSeconds = 120f)]
        public static async Task CommandedAttackReducesTargetHitPoints(EngineTestContext ctx)
        {
            ctx.SeedRandom(Seed);

            var attacker = ctx.SpawnCreature("nw_bandit001", -2f, 0f);
            var target = ctx.SpawnCreature("nw_rat001", 2f, 0f);
            ctx.MakeHostile(target);

            var startingHP = GetCurrentHitPoints(target);

            AssignCommand(attacker, () => ActionAttack(target));

            await ctx.WaitUntilAsync(
                () => !GetIsObjectValid(target) || GetCurrentHitPoints(target) < startingHP,
                90f,
                "the attacker's damage to lower the target's hit points below its starting value");
        }
    }
}
