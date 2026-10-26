using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// NPC area abilities are used against a lone player. Reported in CZ-220: a mynock fighting a
    /// solo player only ever used Disorienting Screech and never Sonic Shriek.
    /// </summary>
    public static class LoneTargetAreaAbilityEngineTests
    {
        // CZ-220 mynock from the CZ220_MYNOCKS spawn table; it knows both abilities.
        private const string MynockResref = "mynock";

        [EngineTest("NPC AI selects Sonic Shriek and Disorienting Screech against a lone player",
            Category = "LoneTargetAreaAbility", TimeoutSeconds = 60f)]
        public static async Task LonePlayer_IsATargetForBothAreaAbilities(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var mynock = await SpawnMynockAsync(ctx, player.Creature, AILevel.VeryLow);
            Enmity.ModifyEnmity(player.Creature, mynock, 10);

            var resolveTarget = typeof(NPCAI).GetMethod("ResolveTarget", BindingFlags.NonPublic | BindingFlags.Static)!;
            foreach (var (feat, expected, label) in new[]
                     {
                         (FeatType.SonicShriek, player.Creature, "Sonic Shriek aims at the lone player"),
                         (FeatType.DisorientingScreech, mynock, "Disorienting Screech centers on the mynock with the lone player in range")
                     })
            {
                var action = NPCAI.Profiles[AIProfileType.Generic].Actions
                    .Single(candidate => candidate.Type == AIActionType.Ability && candidate.Feat == feat);
                var context = new AIContext(mynock, AITriggerType.CombatRound, player.Creature,
                    new AIProfile { Type = AIProfileType.Generic }, new AIState(), Array.Empty<uint>());
                var selected = (uint)resolveTarget.Invoke(null, new object[] { context, action })!;
                ctx.AssertEqual(expected, selected, label);
                context.SetEvaluatedTarget(selected);
                ctx.Assert(action.Score(context) > 0, $"{feat} scores against the lone player");
            }
        }

        [EngineTest("A mynock uses both Sonic Shriek and Disorienting Screech on a lone player",
            Category = "LoneTargetAreaAbility", TimeoutSeconds = 60f)]
        public static async Task Mynock_UsesBothAbilitiesOnALonePlayer(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            // Both stay alive for the whole fight.
            SetPlotFlag(player.Creature, true);
            var mynock = await SpawnMynockAsync(ctx, player.Creature, AILevel.High);
            SetPlotFlag(mynock, true);
            Enmity.ModifyEnmity(player.Creature, mynock, 10);
            AssignCommand(mynock, () => ActionAttack(player.Creature));

            // Disorienting Screech wins the first tie; Sonic Shriek follows while Screech recasts.
            await ctx.WaitUntilAsync(() => IsOnRecast(mynock, FeatType.DisorientingScreech), 15f,
                "the mynock to use Disorienting Screech on the lone player");
            await ctx.WaitUntilAsync(() => IsOnRecast(mynock, FeatType.SonicShriek), 20f,
                "the mynock to use Sonic Shriek on the lone player");
        }

        private static async Task<uint> SpawnMynockAsync(EngineTestContext ctx, uint player, AILevel aiLevel)
        {
            var mynock = ctx.SpawnCreature(MynockResref, 2f);
            SetAILevel(mynock, aiLevel);
            SetIsTemporaryEnemy(player, mynock);
            // Spawn scripts reset the FP and STM locals a frame after creation.
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(mynock, 50, 50);
            ctx.Assert(GetHasFeat(FeatType.SonicShriek, mynock), "the mynock knows Sonic Shriek");
            ctx.Assert(GetHasFeat(FeatType.DisorientingScreech, mynock), "the mynock knows Disorienting Screech");
            ctx.Assert(GetIsEnemy(player, mynock), "the mynock treats the player as an enemy");
            return mynock;
        }

        private static bool IsOnRecast(uint creature, FeatType feat)
        {
            return Recast.IsOnRecastDelay(creature, Ability.GetAbilityDetail(feat).RecastGroup).Item1;
        }
    }
}
