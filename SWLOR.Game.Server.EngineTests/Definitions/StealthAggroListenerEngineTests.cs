using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The notice stealth sends the creature AI. An enemy with AI that stands inside its aggro aura of a hidden player has
    /// not acquired the player; when the player becomes visible, stealth tells the AI, which runs the normal proximity-aggro
    /// path for every enemy beside the player. The notice only reaches the AI once the AI registered itself at boot, so a
    /// missing registration shows here as an enemy that never wakes.
    /// </summary>
    public static class StealthAggroListenerEngineTests
    {
        private const string EnemyResref = "nw_bandit001";

        // No hostile can roll past this, so the player stays undetected while stealthed.
        private const int UndetectableStealth = 1000;

        [EngineTest("An enemy beside a hidden player acquires the player's aggro when the player becomes visible",
            Category = "StealthAggro", TimeoutSeconds = 90f)]
        public static async Task EnemyAcquiresAggroWhenTheHiddenPlayerIsRevealed(EngineTestContext ctx)
        {
            using var player = await CreateStealthPlayerAsync(ctx);
            var creature = player.Creature;
            var enemy = SpawnEnemy(ctx, creature, 3f, 0f);

            await SetStealthAsync(ctx, creature, true);
            await ctx.WaitUntilAsync(() => Stealth.IsHiddenFrom(enemy, creature), 4f, "the enemy to be unable to see the stealthed player");
            await ctx.DelaySecondsAsync(5f);
            ctx.Assert(!Enmity.GetEnmityTable(enemy).ContainsKey(creature), "the enemy has not acquired a player it cannot see");

            await SetStealthAsync(ctx, creature, false);
            await ctx.WaitUntilAsync(() => Enmity.GetEnmityTable(enemy).ContainsKey(creature), 6f,
                "the enemy to acquire the revealed player through the stealth notice");
            ctx.Assert(GetDistanceBetween(enemy, creature) <= 8.5f, "the enemy is inside its aggro range of the player");
        }

        [EngineTest("An enemy outside its aggro range does not acquire a player who becomes visible",
            Category = "StealthAggro", TimeoutSeconds = 90f)]
        public static async Task EnemyOutOfRangeDoesNotAcquireTheRevealedPlayer(EngineTestContext ctx)
        {
            using var player = await CreateStealthPlayerAsync(ctx);
            var creature = player.Creature;
            var enemy = SpawnEnemy(ctx, creature, 20f, 0f);

            await SetStealthAsync(ctx, creature, true);
            await SetStealthAsync(ctx, creature, false);
            await ctx.DelaySecondsAsync(5f);

            ctx.Assert(GetDistanceBetween(enemy, creature) > 8.5f, "the enemy is outside its aggro range");
            ctx.Assert(!Enmity.GetEnmityTable(enemy).ContainsKey(creature), "the enemy far away has not acquired the player");
        }

        private static async Task<PlayerAbilityFixture> CreateStealthPlayerAsync(EngineTestContext ctx)
        {
            var player = await PlayerAbilityFixture.CreateAsync(ctx);
            try
            {
                player.Update(record =>
                {
                    record.Perks[PerkType.Stealth] = 1;
                    record.Stealth = UndetectableStealth;
                });
                // The enemies must not defeat the player during the test.
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), player.Creature, 120f);
                ctx.Assert(!GetActionMode(player.Creature, ActionMode.Stealth), "the player starts out of stealth");
                return player;
            }
            catch
            {
                player.Dispose();
                throw;
            }
        }

        private static uint SpawnEnemy(EngineTestContext ctx, uint player, float xOffset, float yOffset)
        {
            var enemy = ctx.SpawnCreature(EnemyResref, xOffset, yOffset);
            ctx.SuppressNPCNaturalRegen(enemy);
            SetIsTemporaryEnemy(player, enemy);
            SetLocalLocation(enemy, "HOME_LOCATION", GetLocation(enemy));
            ctx.SetNPCResources(enemy, 100, 100);
            return enemy;
        }

        private static async Task SetStealthAsync(EngineTestContext ctx, uint creature, bool enabled)
        {
            await ctx.ExecuteInCreatureContextAsync(creature, () => SetActionMode(creature, ActionMode.Stealth, enabled));
            await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth) == enabled, 5f,
                enabled ? "the player to enter stealth" : "the player to leave stealth");
        }
    }
}
