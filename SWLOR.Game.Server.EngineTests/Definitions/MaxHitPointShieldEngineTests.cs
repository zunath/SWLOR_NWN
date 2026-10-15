using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The native current-HP getter includes temporary HP. Maximum HP changes must compare and
    /// clamp base HP only, so a shield can never become real HP.
    /// </summary>
    public static class MaxHitPointShieldEngineTests
    {
        private const string ShieldCategory = "MaxHitPointShield";
        private const string ShieldKey = "MAX_HP_SHIELD_TEST";
        private const int PlayerRecordHitPoints = 1000;
        // The shield outweighs the missing HP, so reported HP passes maximum HP, yet stays under the
        // 50% temporary HP cap through every maximum HP change below.
        private const int PlayerMissingHitPoints = 400;
        private const int PlayerShieldHitPoints = 500;
        private const int BeltHitPoints = 95;

        [EngineTest("Adjusting a shielded player's maximum HP keeps base HP and the shield", Category = ShieldCategory, TimeoutSeconds = 60f)]
        public static async Task ShieldedPlayerAdjustmentKeepsBaseHitPoints(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var wounded = await WoundAndShieldPlayerAsync(ctx, dbPlayer, player);

            foreach (var adjustment in new[] { 0, 50, -50 })
            {
                await ctx.ExecuteInCreatureContextAsync(player, () => Stat.AdjustPlayerMaxHP(dbPlayer, player, adjustment));
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(wounded, ObjectPlugin.GetCurrentHitPoints(player), $"base HP after adjusting by {adjustment}");
                ctx.AssertEqual(PlayerShieldHitPoints, TemporaryHitPointEffects.GetRemaining(player), $"shield after adjusting by {adjustment}");
            }

            // A real reduction below base HP still lowers base HP, and only base HP.
            ObjectPlugin.SetCurrentHitPoints(player, GetMaxHitPoints(player));
            await ctx.ExecuteInCreatureContextAsync(player, () => Stat.AdjustPlayerMaxHP(dbPlayer, player, -100));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(GetMaxHitPoints(player), ObjectPlugin.GetCurrentHitPoints(player), "base HP after a reduction below it");
            ctx.AssertEqual(PlayerShieldHitPoints, TemporaryHitPointEffects.GetRemaining(player), "shield after a reduction below base HP");
        }

        [EngineTest("A shielded player equipping HP gear keeps base HP and the shield", Category = ShieldCategory, TimeoutSeconds = 60f)]
        public static async Task ShieldedPlayerEquipmentKeepsBaseHitPoints(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var wounded = await WoundAndShieldPlayerAsync(ctx, dbPlayer, player);
            var maximum = GetMaxHitPoints(player);

            var belt = await CreateHitPointBeltAsync(ctx, player);
            await EquipAndUnequipAsync(ctx, player, belt, maximum, wounded, PlayerShieldHitPoints);
        }

        [EngineTest("A shielded NPC equipping HP gear keeps base HP and the shield", Category = ShieldCategory, TimeoutSeconds = 60f)]
        public static async Task ShieldedNPCEquipmentKeepsBaseHitPoints(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            // A module NPC with an SWLOR skin: the NPC HP-gear path rebuilds the budget from it.
            var npc = arena.Spawn("vnpcscons1", 0f, 0f);
            await ctx.DelaySecondsAsync(1f);
            ctx.SuppressNPCNaturalRegen(npc);
            var maximum = GetMaxHitPoints(npc);
            // Half of maximum HP is the whole temporary HP cap; a lighter wound lets it pass maximum HP.
            var wounded = maximum * 3 / 4;
            var shield = maximum / 2;
            ObjectPlugin.SetCurrentHitPoints(npc, wounded);
            TemporaryHitPointEffects.ApplyFlat(npc, ShieldKey, shield, 600f);
            ctx.Assert(GetCurrentHitPoints(npc) > maximum, "The shield must push reported HP above maximum HP.");

            var belt = await CreateHitPointBeltAsync(ctx, npc);
            await EquipAndUnequipAsync(ctx, npc, belt, maximum, wounded, shield);
        }

        private static async Task<int> WoundAndShieldPlayerAsync(EngineTestContext ctx, Player dbPlayer, uint player)
        {
            ctx.Assert(dbPlayer != null, "The player fixture must have a player record.");
            dbPlayer.MaxHP = PlayerRecordHitPoints;
            await ctx.ExecuteInCreatureContextAsync(player, () => Stat.AdjustPlayerMaxHP(dbPlayer, player, 0));
            await ctx.WaitFrameAsync();
            DB.Set(dbPlayer);

            var maximum = GetMaxHitPoints(player);
            var wounded = maximum - PlayerMissingHitPoints;
            ctx.Log($"Maximum HP {maximum}; wounded to {wounded} with a {PlayerShieldHitPoints} HP shield.");
            ObjectPlugin.SetCurrentHitPoints(player, wounded);
            TemporaryHitPointEffects.ApplyFlat(player, ShieldKey, PlayerShieldHitPoints, 600f);
            ctx.AssertEqual(wounded + PlayerShieldHitPoints, GetCurrentHitPoints(player), "reported HP with the shield");
            ctx.Assert(GetCurrentHitPoints(player) > maximum, "The shield must push reported HP above maximum HP.");
            return wounded;
        }

        private static async Task<uint> CreateHitPointBeltAsync(EngineTestContext ctx, uint creature)
        {
            var belt = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(creature, () => belt = CreateItemOnObject("advent_belt", creature));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(belt), "create a real belt");
            ctx.Track(belt);
            for (var property = GetFirstItemProperty(belt); GetIsItemPropertyValid(property); property = GetFirstItemProperty(belt))
                RemoveItemProperty(belt, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, BeltHitPoints), belt);
            return belt;
        }

        private static async Task EquipAndUnequipAsync(EngineTestContext ctx, uint creature, uint belt, int maximum, int wounded, int shield)
        {
            for (var cycle = 0; cycle < 2; cycle++)
            {
                await ctx.ExecuteInCreatureContextAsync(creature, () => ActionEquipItem(belt, InventorySlot.Belt));
                await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.Belt, creature) == belt, 10f, "equip the HP belt");
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(maximum + BeltHitPoints, GetMaxHitPoints(creature), $"cycle {cycle}: the belt raises maximum HP");
                ctx.AssertEqual(wounded, ObjectPlugin.GetCurrentHitPoints(creature), $"cycle {cycle}: base HP after equipping");
                ctx.AssertEqual(shield, TemporaryHitPointEffects.GetRemaining(creature), $"cycle {cycle}: shield after equipping");

                await ctx.ExecuteInCreatureContextAsync(creature, () => ActionUnequipItem(belt));
                await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.Belt, creature) != belt, 10f, "unequip the HP belt");
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(maximum, GetMaxHitPoints(creature), $"cycle {cycle}: unequipping removes the belt HP");
                ctx.AssertEqual(wounded, ObjectPlugin.GetCurrentHitPoints(creature), $"cycle {cycle}: base HP after unequipping");
                ctx.AssertEqual(shield, TemporaryHitPointEffects.GetRemaining(creature), $"cycle {cycle}: shield after unequipping");
            }
        }

        private static async Task<PlayerAbilityFixture> PreparePlayer(EngineTestContext ctx)
        {
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            try
            {
                var player = fixture.Creature;
                // The civilian template is plot-protected; ordinary players are not.
                SetPlotFlag(player, false);
                CreaturePlugin.SetClassByPosition(player, 0, SWLOR.NWN.API.NWScript.Enum.ClassType.Standard);
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    typeof(PlayerInitialization).GetMethod("EnsureNativeLevels", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { player }));
                ctx.AssertEqual(40, GetHitDice(player), "fixture has normal SWLOR native levels");
                CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
                ctx.SuppressNPCNaturalRegen(player);
                return fixture;
            }
            catch
            {
                fixture.Dispose();
                throw;
            }
        }
    }
}
