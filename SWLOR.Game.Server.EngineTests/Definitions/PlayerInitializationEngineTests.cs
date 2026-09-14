using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerInitializationEngineTests
    {
        [EngineTest("Male starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task MaleStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Human, Gender.Male, "nw_maletatcivout");

        [EngineTest("Female starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task FemaleStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Human, Gender.Female, "nw_femtatcivoutf");

        [EngineTest("Droid starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task DroidStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Droid, Gender.Male, "dlarproto");

        private static async Task VerifyStarterItems(EngineTestContext ctx, RacialType race, Gender gender, string outfit)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                CreaturePlugin.SetRacialType(owner, race);
                global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner)
                    .AsNWSCreature().m_pStats.m_nGender = (byte)gender;
                typeof(Feature.PlayerInitialization).GetMethod("ClearInventory", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                typeof(Feature.PlayerInitialization).GetMethod("GiveStartingItems", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                var saved = ObjectPlugin.Serialize(owner);
                var restored = ObjectPlugin.Deserialize(saved);
                ctx.Track(restored);
                ctx.AssertEqual(outfit, GetResRef(GetItemInSlot(InventorySlot.Chest, restored)), "The first saved file already has the starting outfit equipped");
                ctx.Assert(GetIsObjectValid(GetItemPossessedBy(restored, "survival_knife")), "The starting knife is present");
                ctx.AssertEqual(2, Item.GetInventoryItemCount(restored), "Both starting consumables are retained");
                ctx.Assert(GetGold(restored) >= 200, "Starting credits survive the first save");
            });
        }
    }
}
