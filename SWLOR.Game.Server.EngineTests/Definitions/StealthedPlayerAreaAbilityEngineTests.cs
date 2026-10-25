using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A stealthed player that an NPC has not detected must not draw that NPC's area abilities.
    /// Reported on Tatooine: a Tusken Raider spawning beside a player who entered the area in
    /// stealth used Disorienting Screech on them although it never detected them.
    /// </summary>
    public static class StealthedPlayerAreaAbilityEngineTests
    {
        // Tusken Raider from the TATOOINE_TUSKEN_RAIDER spawn table; it knows Disorienting Screech.
        private const string TuskenResref = "ext_tusken_tr003";
        private const string DecoyResref = "nw_bandit001";
        // No hostile can roll past this, so the player stays undetected for the whole test.
        private const int UndetectableStealth = 1000;

        [EngineTest("An NPC with no visible enemy does not use an area ability on an undetected stealthed player",
            Category = "StealthAreaAbility", TimeoutSeconds = 60f)]
        public static async Task NoVisibleEnemy_DoesNotUseAreaAbility(EngineTestContext ctx)
        {
            using var player = await CreateStealthedPlayerAsync(ctx);
            var tusken = await SpawnTuskenAsync(ctx, player.Creature, 3f, AILevel.High);

            ctx.Assert(Stealth.IsHiddenFrom(tusken, player.Creature), "the Tusken has not detected the stealthed player");
            ctx.Assert(!Enmity.GetEnmityTable(tusken).Any(), "the Tusken has no enmity toward anyone");
            ctx.Assert(!Activity.IsBusy(tusken), "the Tusken is not already using an ability on the undetected player");

            // The trigger a creature runs when a player's arrival spawns it, and the one it runs each combat round.
            foreach (var trigger in new[] { AITriggerType.Spawn, AITriggerType.CombatRound })
            {
                var acted = true;
                await ctx.ExecuteInCreatureContextAsync(tusken,
                    () => acted = AI.ProcessTrigger(tusken, trigger, OBJECT_INVALID, true));
                ctx.Assert(!acted, $"the Tusken takes no action on {trigger} with only an undetected player nearby");
                ctx.Assert(!IsScreechOnRecast(tusken),
                    $"the Tusken does not use Disorienting Screech on {trigger} with only an undetected player nearby");
            }

            // Its own AI keeps running while the player waits beside it.
            await ctx.DelaySecondsAsync(8f);
            ctx.Assert(!IsScreechOnRecast(tusken), "the Tusken's own AI never uses Disorienting Screech on the undetected player");
            ctx.Assert(GetActionMode(player.Creature, ActionMode.Stealth), "the player is still stealthed");
        }

        [EngineTest("An NPC does not count an undetected stealthed player toward an area ability's targets",
            Category = "StealthAreaAbility", TimeoutSeconds = 60f)]
        public static async Task UndetectedPlayer_IsNotCountedAsAreaTarget(EngineTestContext ctx)
        {
            using var player = await CreateStealthedPlayerAsync(ctx);
            // AI off: the test drives target selection itself.
            var tusken = await SpawnTuskenAsync(ctx, player.Creature, 4f, AILevel.VeryLow);
            var decoy = ctx.SpawnCreature(DecoyResref, 2f);
            SetAILevel(decoy, AILevel.VeryLow);
            // Members of one faction never count as enemies, so the decoy leaves the Tusken's faction.
            ChangeToStandardFaction(decoy, StandardFaction.Commoner);
            SetIsTemporaryEnemy(decoy, tusken);
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsEnemy(decoy, tusken), "the Tusken treats the decoy as an enemy");

            // The Tusken is fighting the visible decoy; the stealthed player stands beside it.
            Enmity.ModifyEnmity(decoy, tusken, 10);
            var screech = NPCAI.Profiles[AIProfileType.Generic].Actions
                .Single(action => action.Type == AIActionType.Ability && action.Feat == FeatType.DisorientingScreech);
            var resolveTarget = typeof(NPCAI).GetMethod("ResolveTarget", BindingFlags.NonPublic | BindingFlags.Static)!;
            uint ResolveTarget() => (uint)resolveTarget.Invoke(null, new object[]
            {
                new AIContext(tusken, AITriggerType.CombatRound, decoy,
                    new AIProfile { Type = AIProfileType.Generic }, new AIState(), Array.Empty<uint>()),
                screech
            })!;

            ctx.Assert(Stealth.IsHiddenFrom(tusken, player.Creature), "the Tusken has not detected the stealthed player");
            ctx.AssertEqual(OBJECT_INVALID, ResolveTarget(),
                "Disorienting Screech needs two enemies, and the Tusken sees only the decoy");

            // Control: once the player is visible, the same two enemies are enough.
            await SetStealthAsync(ctx, player.Creature, false);
            ctx.Assert(!Stealth.IsHiddenFrom(tusken, player.Creature), "the Tusken sees the player once stealth ends");
            ctx.AssertEqual(tusken, ResolveTarget(), "Disorienting Screech centers on the Tusken with two visible enemies in range");
        }

        private static async Task<PlayerAbilityFixture> CreateStealthedPlayerAsync(EngineTestContext ctx)
        {
            var player = await PlayerAbilityFixture.CreateAsync(ctx);
            player.Update(record =>
            {
                record.Perks[PerkType.Stealth] = 1;
                record.Stealth = UndetectableStealth;
            });
            await SetStealthAsync(ctx, player.Creature, true);
            return player;
        }

        private static async Task<uint> SpawnTuskenAsync(EngineTestContext ctx, uint player, float xOffset, AILevel aiLevel)
        {
            var tusken = ctx.SpawnCreature(TuskenResref, xOffset);
            SetAILevel(tusken, aiLevel);
            SetIsTemporaryEnemy(player, tusken);
            // Spawn scripts reset the FP and STM locals a frame after creation.
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(tusken, 50, 50);
            ctx.Assert(GetHasFeat(FeatType.DisorientingScreech, tusken), "the Tusken knows Disorienting Screech");
            ctx.Assert(GetIsEnemy(player, tusken), "the Tusken treats the player as an enemy");
            return tusken;
        }

        private static async Task SetStealthAsync(EngineTestContext ctx, uint creature, bool enabled)
        {
            await ctx.ExecuteInCreatureContextAsync(creature, () => SetActionMode(creature, ActionMode.Stealth, enabled));
            await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth) == enabled, 5f,
                enabled ? "the player to enter stealth" : "the player to leave stealth");
        }

        private static bool IsScreechOnRecast(uint creature)
        {
            return Recast.IsOnRecastDelay(creature, Ability.GetAbilityDetail(FeatType.DisorientingScreech).RecastGroup).Item1;
        }
    }
}
