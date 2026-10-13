using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    // The headless player fixture has no client to drive its swings, so these tests read the
    // player's action queue: an attack at the head of the queue is what the engine swings from.
    public static class InterruptedAttackResumeEngineTests
    {
        [EngineTest("An interrupted player's attack is queued again behind the interrupt's own action", Category = "AbilityAttackResume", TimeoutSeconds = 40f)]
        public static async Task ResumeWaitsForQueuedInterruptAction(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, -2f);
            var pc = player.Creature;
            var enemy = await SpawnEnemyAsync(ctx);
            await QueueAttackAsync(ctx, pc, enemy);

            await ctx.ExecuteInCreatureContextAsync(pc, () => ActionInterruption.ClearActions(pc, () => ActionWait(2f)));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(OBJECT_INVALID, ActionInterruption.GetQueuedAttackTarget(pc),
                "the interrupt clears the attack and runs its own action first");
            await ctx.WaitUntilAsync(() => ActionInterruption.GetQueuedAttackTarget(pc) == enemy,
                6f, "the attack on the same enemy to come back after the interrupt's action");
            await ctx.ExecuteInCreatureContextAsync(pc, () => ClearAllActions(true));
        }

        [EngineTest("A player hit by Force Choke goes back to attacking the same enemy", Category = "AbilityAttackResume", TimeoutSeconds = 60f)]
        public static async Task PlayerResumesAttackAfterForceChoke(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, -2f);
            var pc = player.Creature;
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(1000), pc, 120f);
            var enemy = await SpawnEnemyAsync(ctx);

            var choker = ctx.SpawnCreature("nw_bandit001", -6f);
            SetAILevel(choker, AILevel.VeryLow);
            ctx.MakeHostile(choker);
            await ctx.WaitFrameAsync();
            ctx.SetResources(choker, 9999, 9999);
            ctx.SetNPCPerkLevel(choker, PerkType.ForceChoke, 1);
            CreaturePlugin.AddFeat(choker, FeatType.ForceChoke1);

            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                await QueueAttackAsync(ctx, pc, enemy);

                var activated = false;
                var denial = string.Empty;
                await ctx.ExecuteInCreatureContextAsync(choker, () =>
                {
                    activated = UsePerkFeat.TryUseAbility(choker, pc, FeatType.ForceChoke1, GetLocation(pc));
                    denial = Ability.GetLastActivationDenialReason();
                });
                ctx.Assert(activated, $"Force Choke activates: {denial}");
                await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<ImmobilizedStatusEffect>(pc),
                    10f, "Force Choke to land on the player");
                await ctx.WaitUntilAsync(() => ActionInterruption.GetQueuedAttackTarget(pc) == enemy,
                    10f, "the player's attack on the same enemy to be queued after the choke");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                await ctx.ExecuteInCreatureContextAsync(pc, () => ClearAllActions(true));
            }
        }

        private static async Task<uint> SpawnEnemyAsync(EngineTestContext ctx)
        {
            var enemy = ctx.SpawnCreature("nw_bandit001", -1f);
            SetAILevel(enemy, AILevel.VeryLow);
            ctx.MakeHostile(enemy);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 120f);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), enemy, 120f);
            await ctx.WaitFrameAsync();
            return enemy;
        }

        private static async Task QueueAttackAsync(EngineTestContext ctx, uint pc, uint enemy)
        {
            await ctx.ExecuteInCreatureContextAsync(pc, () => ActionAttack(enemy));
            await ctx.WaitUntilAsync(() => ActionInterruption.GetQueuedAttackTarget(pc) == enemy,
                5f, "the player's attack on the enemy to be queued");
        }
    }
}
