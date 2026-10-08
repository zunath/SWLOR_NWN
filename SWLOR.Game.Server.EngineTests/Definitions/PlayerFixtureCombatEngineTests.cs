using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerFixtureCombatEngineTests
    {
        private const string EnemyResref = "nw_bandit001";
        private const int TemporaryHP = 5000;
        private const float CombatSeconds = 10f;

        /// <summary>
        /// Movement reorders the arena's native object list. A fixture missing from that list
        /// corrupted the server heap when it moved, which surfaced as a crash during shutdown.
        /// </summary>
        [EngineTest("Player fixture survives hostile melee", Category = "PlayerFixture", TimeoutSeconds = 60f)]
        public static async Task HostileMeleeAgainstPlayerFixture(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(TemporaryHP), player, 3600f);
            var enemies = new[] { ctx.SpawnCreature(EnemyResref, 2f), ctx.SpawnCreature(EnemyResref, 0f, 2f) };
            foreach (var enemy in enemies)
            {
                ctx.MakeHostile(enemy);
                SetAILevel(enemy, AILevel.High);
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(TemporaryHP), enemy, 3600f);
            }

            await ctx.WaitFrameAsync();
            foreach (var enemy in enemies)
                Enmity.ModifyEnmity(player, enemy, 100);
            AssignCommand(player, () => ActionAttack(enemies[0]));

            await ctx.WaitUntilAsync(
                () => enemies.All(enemy => GetAttackTarget(enemy) == player && Combat.HasRecentAttackActivity(enemy, 3f)),
                15f,
                "both enemies to swing at the player fixture");

            // The fixture's own AI may hold it in place, so it is stepped across the enemies'
            // X coordinates, where the engine reorders the position-sorted list.
            var start = GetPosition(player);
            foreach (var xOffset in new[] { 3f, -3f, 0f })
            {
                ObjectPlugin.SetPosition(player, start + new Vector3(xOffset, 0f, 0f));
                await ctx.DelaySecondsAsync(CombatSeconds / 3f);
                ctx.Assert(PlayerAbilityFixture.IsListedInArena(ctx, player),
                    $"the player fixture stays in the arena's object list after moving {xOffset}m");
            }

            ctx.Assert(!GetIsDead(player), "the player fixture survives the fight");
        }
    }
}
