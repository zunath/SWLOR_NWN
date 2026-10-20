using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Players keep the login cutscene ghost, which stops other players bumping them, after death and space mode strip
    /// every effect.
    /// </summary>
    public static class PlayerGhostEngineTests
    {
        private const string ActorResref = "nw_bandit001";

        [EngineTest("A player revived by Resuscitation gets the login ghost back", Category = "PlayerGhost", TimeoutSeconds = 40f)]
        public static async Task ReviveRestoresGhost(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx, 1f);
            var player = fixture.Creature;
            var caster = ctx.SpawnCreature(ActorResref, -0.5f);
            SetAILevel(caster, AILevel.VeryLow);
            await ctx.WaitFrameAsync();
            Stat.SetNPCMaxHitPoints(caster, 1000, true);
            ctx.SetResources(caster, 9999, 9999);
            ctx.SuppressNPCNaturalRegen(caster);
            ctx.SetNPCPerkLevel(caster, PerkType.Resuscitation, 1);
            CreaturePlugin.AddFeat(caster, FeatType.Resuscitation1);

            SetGhostFlag(player, false);
            await ctx.ExecuteInCreatureContextAsync(player, () => PlayerTemporaryEffects.ApplyCutsceneGhostToPlayer(player));
            ctx.Assert(GetGhostFlag(player), "The login ghost applies");

            try
            {
                // The fixture's template is plot, which blocks death.
                SetPlotFlag(player, false);
                await ctx.ExecuteInCreatureContextAsync(player, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), player));
                await ctx.WaitUntilAsync(() => GetIsDead(player), 5f, "the player to die");
                await ctx.WaitFrameAsync();
                ReviveRulesEngineTests.DestroyCorpsePlaceable(player);
                ctx.Assert(!GetGhostFlag(player) && CountGhosts(player) == 0, "Death strips the login ghost");

                var activated = false;
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    activated = UsePerkFeat.TryUseAbility(caster, player, FeatType.Resuscitation1, GetLocation(player)));
                ctx.Assert(activated, $"Resuscitation activates: {Service.Ability.GetLastActivationDenialReason()}");

                await ctx.WaitUntilAsync(() => !GetIsDead(player) && GetGhostFlag(player), 15f, "the revived player to be ghosted");
                ctx.AssertEqual(1, CountGhosts(player), "Revived player's ghost effects");
            }
            finally
            {
                ReviveRulesEngineTests.ReleaseBody(player);
            }
        }

        [EngineTest("Leaving space mode gives the player the login ghost back without stacking it", Category = "PlayerGhost")]
        public static async Task SpaceExitRestoresGhost(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;

            // Entering space mode strips every effect, which clears the flag.
            SetGhostFlag(player, false);
            await ctx.ExecuteInCreatureContextAsync(player, () => ExecuteScript(ScriptName.OnSpaceExit, player));
            ctx.Assert(GetGhostFlag(player), "Leaving space mode restores the ghost");
            ctx.AssertEqual(1, CountGhosts(player), "Ghost effects after leaving space mode");

            await ctx.ExecuteInCreatureContextAsync(player, () => ExecuteScript(ScriptName.OnSpaceExit, player));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(1, CountGhosts(player), "Ghost effects after leaving space mode twice");
        }

        private static int CountGhosts(uint creature)
        {
            var count = 0;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.CutsceneGhost)
                    count++;
            }

            return count;
        }

        private static CNWSCreature Native(uint creature) =>
            NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);

        private static bool GetGhostFlag(uint creature) => Native(creature).m_bCutsceneGhost != 0;

        private static void SetGhostFlag(uint creature, bool isGhost) => Native(creature).m_bCutsceneGhost = isGhost ? 1 : 0;
    }
}
