using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The enemy retarget the stealth service asks of the enmity service. When a player enters stealth the enemies that
    /// were fighting the player choose their attack target again, drop the player they can no longer see, and keep the player on
    /// their enmity table; when the player is revealed they choose again and attack the player. The notice is deferred to the
    /// next frame, so the checks wait for the target to change. This is the proof for the deferred notice that replaces the
    /// stealth service's two calls into the enmity service.
    /// </summary>
    public static class StealthRetargetEngineTests
    {
        private const string EnemyResref = "nw_bandit001";

        // No hostile can roll past this, so the player stays undetected while stealthed.
        private const int UndetectableStealth = 1000;

        [EngineTest("An enemy drops a player who enters stealth, keeps the enmity, and attacks again when the player is revealed",
            Category = "StealthRetarget", TimeoutSeconds = 90f)]
        public static async Task EnemyDropsAndResumesAttackingAStealthedPlayer(EngineTestContext ctx)
        {
            using var player = await CreateStealthPlayerAsync(ctx);
            var creature = player.Creature;
            var enemy = SpawnEnemy(ctx, creature, 2f, 0f);

            // The enemy is attacking the player, and the player is on its enmity table.
            EnmityChange.ModifyEnmity(creature, enemy, 100);
            await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == creature, 8f, "the enemy to attack the player");
            ctx.Assert(Enmity.GetEnmityTable(enemy).ContainsKey(creature), "the player is on the enemy's enmity table");
            ctx.AssertEqual(creature, EnmityAttack.GetHighestEnmityAttackTarget(enemy), "the player is the enemy's attack target");

            // The player enters stealth: the retarget drops the player as the attack target and keeps the enmity.
            await EnterStealthInCombatAsync(ctx, creature);
            await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) != creature, 4f, "the enemy to stop attacking the player who entered stealth");
            ctx.Assert(Stealth.IsHiddenFrom(enemy, creature), "the enemy has not detected the stealthed player");
            ctx.AssertEqual(OBJECT_INVALID, EnmityAttack.GetHighestEnmityAttackTarget(enemy), "no creature on the enemy's table can be attacked while hidden");
            ctx.Assert(Enmity.GetEnmityTable(enemy).ContainsKey(creature), "the enemy keeps the stealthed player on its enmity table");
            ctx.Assert(Enmity.GetEnmityTable(enemy)[creature] > 0, "the enmity toward the stealthed player is kept");

            // The player is revealed: the retarget makes the enemy attack the player again.
            await SetStealthAsync(ctx, creature, false);
            await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == creature, 6f, "the enemy to attack the revealed player");
            ctx.Assert(!Stealth.IsHiddenFrom(enemy, creature), "the enemy sees the player once stealth ends");
            ctx.AssertEqual(creature, EnmityAttack.GetHighestEnmityAttackTarget(enemy), "the revealed player is the enemy's attack target again");
            ctx.Assert(Enmity.GetEnmityTable(enemy).ContainsKey(creature), "the player is still on the enemy's enmity table");
        }

        [EngineTest("Every enemy that has the player on its table chooses its target again when the player enters stealth",
            Category = "StealthRetarget", TimeoutSeconds = 90f)]
        public static async Task EveryEnemyOfThePlayerRetargets(EngineTestContext ctx)
        {
            using var player = await CreateStealthPlayerAsync(ctx);
            var creature = player.Creature;
            var enemies = new[] { SpawnEnemy(ctx, creature, 2f, 0f), SpawnEnemy(ctx, creature, 0f, 2f) };
            foreach (var enemy in enemies)
                EnmityChange.ModifyEnmity(creature, enemy, 100);
            await ctx.WaitUntilAsync(() => enemies.All(enemy => GetAttackTarget(enemy) == creature), 8f, "both enemies to attack the player");

            await EnterStealthInCombatAsync(ctx, creature);
            await ctx.WaitUntilAsync(() => enemies.All(enemy => GetAttackTarget(enemy) != creature), 4f, "both enemies to drop the stealthed player");
            foreach (var enemy in enemies)
                ctx.Assert(Enmity.GetEnmityTable(enemy).ContainsKey(creature), "each enemy keeps the player on its enmity table");

            await SetStealthAsync(ctx, creature, false);
            await ctx.WaitUntilAsync(() => enemies.All(enemy => GetAttackTarget(enemy) == creature), 6f, "both enemies to attack the revealed player");
        }

        private static async Task<PlayerAbilityFixture> CreateStealthPlayerAsync(EngineTestContext ctx)
        {
            var player = await PlayerAbilityFixture.CreateAsync(ctx);
            try
            {
                player.Update(record =>
                {
                    record.Perks[PerkType.Stealth] = 1;
                    record.Perks[PerkType.GhostProtocol] = 1;
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
            // The enemy's own AI is off, so only the retarget the stealth service asks for changes what it attacks.
            SetAILevel(enemy, AILevel.VeryLow);
            SetIsTemporaryEnemy(player, enemy);
            SetLocalLocation(enemy, "HOME_LOCATION", GetLocation(enemy));
            ctx.SetNPCResources(enemy, 100, 100);
            return enemy;
        }

        // The engine refuses stealth while the player is in combat; Ghost Protocol is the ability that enters it there.
        private static async Task EnterStealthInCombatAsync(EngineTestContext ctx, uint creature)
        {
            var ability = AbilityCatalog.GetAbilityDetail(FeatType.GhostProtocol);
            await ctx.ExecuteInCreatureContextAsync(creature,
                () => ability.ImpactAction(creature, creature, 1, GetLocation(creature)));
            await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth), 5f, "Ghost Protocol to enter stealth");
        }

        private static async Task SetStealthAsync(EngineTestContext ctx, uint creature, bool enabled)
        {
            await ctx.ExecuteInCreatureContextAsync(creature, () => SetActionMode(creature, ActionMode.Stealth, enabled));
            await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth) == enabled, 5f,
                enabled ? "the player to enter stealth" : "the player to leave stealth");
        }
    }
}
