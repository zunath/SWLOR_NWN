using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Starting a craft restores the player's login cutscene ghost, so other creatures walk through the crafter instead
    /// of bumping them away from the station, which would close the window and fail the craft.
    /// </summary>
    public static class CraftCollisionEngineTests
    {
        private const string Walker = "nw_bandit001";
        private const float WalkerLane = 0.1f;
        private const RecipeType ForgeRecipe = RecipeType.TwinElectroblade1;

        [EngineTest("Creatures walk through a crafter who had lost the login ghost", Category = "CraftCollision", TimeoutSeconds = 90f)]
        public static async Task CrafterIsGhosted(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            fixture.Update(record => record.Skills[SkillType.Smithery].Rank = 10);
            var payload = new CraftPayload(ForgeRecipe, OBJECT_INVALID);

            // Death and space mode strip a player's login ghost, leaving them solid until they log in again.
            SetGhostFlag(player, false);
            try
            {
                var (solidApproach, solidPassed) = await WalkPast(ctx, player);
                ctx.Log($"Walk past a solid player: closest approach {solidApproach:F2}, reached the far side {solidPassed}");

                ForgeViewModel craft = null;
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    GiveComponents(player);
                    craft = OpenWindow<ForgeViewModel>(fixture, GuiWindowType.Forge, payload);
                    craft.OnClickCraft()();
                    ctx.Assert(CraftSessionService.IsCrafting(player), "The craft starts");
                    ctx.Assert(HasEffect(player, EffectTypeScript.CutsceneImmobilize, PlayerActivityEffectTag.CraftingImmobilize),
                        "Crafting holds the crafter in place");
                    ctx.Assert(HasEffect(player, EffectTypeScript.CutsceneGhost, string.Empty),
                        "Crafting restores the untagged login ghost");
                    ctx.Assert(GetGhostFlag(player), "The engine treats the crafter as a ghost");
                });

                var (craftingApproach, craftingPassed) = await WalkPast(ctx, player);
                ctx.Log($"Walk past a crafting player: closest approach {craftingApproach:F2}, reached the far side {craftingPassed}");
                ctx.Assert(craftingPassed && craftingApproach < 0.3f,
                    $"A creature walks straight through the crafter (closest approach {craftingApproach:F2}, passed {craftingPassed})");
                ctx.Assert(solidApproach > craftingApproach + 0.3f,
                    $"The same walk collides with a solid player (closest approach {solidApproach:F2})");

                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    craft.OnWindowClosed()();
                    ctx.Assert(!CraftSessionService.IsCrafting(player), "Closing the window ends the craft");
                    ctx.Assert(!HasEffectByTag(player, PlayerActivityEffectTag.CraftingImmobilize), "Ending the craft releases the crafter");
                });

                // The engine finishes removing effects after the script that removed them.
                await ctx.WaitFrameAsync();
                ctx.Assert(GetGhostFlag(player) && HasEffect(player, EffectTypeScript.CutsceneGhost, string.Empty),
                    "The restored login ghost stays after the craft");
            }
            finally
            {
                CloseWindows(fixture);
            }
        }

        [EngineTest("A crafter who keeps the login ghost is still held in place and keeps the ghost after the craft", Category = "CraftCollision")]
        public static async Task GhostedCrafterIsHeld(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            fixture.Update(record => record.Skills[SkillType.Smithery].Rank = 10);
            var payload = new CraftPayload(ForgeRecipe, OBJECT_INVALID);
            SetGhostFlag(player, false);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    // The login ghost, as PlayerTemporaryEffects applies it.
                    ApplyEffectToObject(DurationType.Permanent, SupernaturalEffect(EffectCutsceneGhost()), player);
                    ctx.Assert(GetGhostFlag(player), "The login ghost applies");

                    // The engine rejects a second ghost, so the crafting immobilize must not depend on it.
                    GiveComponents(player);
                    var craft = OpenWindow<ForgeViewModel>(fixture, GuiWindowType.Forge, payload);
                    craft.OnClickCraft()();
                    ctx.Assert(CraftSessionService.IsCrafting(player), "The craft starts");
                    ctx.Assert(HasEffect(player, EffectTypeScript.CutsceneImmobilize, PlayerActivityEffectTag.CraftingImmobilize),
                        "An already ghosted crafter is still held in place");

                    craft.OnWindowClosed()();
                    ctx.Assert(!HasEffectByTag(player, PlayerActivityEffectTag.CraftingImmobilize), "Ending the craft releases the crafter");
                });

                await ctx.WaitFrameAsync();
                ctx.Assert(HasEffect(player, EffectTypeScript.CutsceneGhost, string.Empty) && GetGhostFlag(player),
                    "The login ghost outlasts the craft");
            }
            finally
            {
                CloseWindows(fixture);
            }
        }

        private static void GiveComponents(uint player)
        {
            foreach (var (resref, quantity) in Craft.GetRecipe(ForgeRecipe).Components)
                CreateItemOnObject(resref, player, quantity);
        }

        /// <summary>
        /// Walks a creature along a straight line through the player's position. Returns its closest approach and whether
        /// it reached the far side.
        /// </summary>
        private static async Task<(float Closest, bool Passed)> WalkPast(EngineTestContext ctx, uint player)
        {
            var center = GetPosition(player);
            var walker = ctx.SpawnCreature(Walker, -4f, WalkerLane);
            // The engine only collides a mover with creatures it perceives.
            await ctx.WaitUntilAsync(() => GetObjectSeen(player, walker), 15f, "the walker to see the player");
            AssignCommand(walker, () => ActionMoveToLocation(ctx.GetArenaLocation(4f, WalkerLane)));

            var closest = float.MaxValue;
            for (var elapsed = 0f; elapsed < 7f; elapsed += 0.1f)
            {
                await ctx.DelaySecondsAsync(0.1f);
                var position = GetPosition(walker);
                var dx = position.X - center.X;
                var dy = position.Y - center.Y;
                closest = MathF.Min(closest, MathF.Sqrt(dx * dx + dy * dy));
            }

            var passed = GetPosition(walker).X > center.X + 3f;
            DestroyObject(walker);
            await ctx.DelaySecondsAsync(0.5f);
            return (closest, passed);
        }

        private static bool HasEffect(uint creature, EffectTypeScript type, string tag)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == type && GetEffectTag(effect) == tag)
                    return true;
            }

            return false;
        }

        private static CNWSCreature Native(uint creature) =>
            NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);

        private static bool GetGhostFlag(uint creature) => Native(creature).m_bCutsceneGhost != 0;

        private static void SetGhostFlag(uint creature, bool isGhost) => Native(creature).m_bCutsceneGhost = isGhost ? 1 : 0;
    }
}
