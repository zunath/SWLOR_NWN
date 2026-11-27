using System.Threading.Tasks;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class KeyItemActivationEngineTests
    {
        [EngineTest("The Mountain Jungles map works with and without the blueprint's key item ID", Category = "KeyItemActivation")]
        public static async Task MountainJunglesMapCanBeLearned(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var definition = new KeyItemDefinition().BuildItems()["KEY_ITEM"];
            var mapType = KeyItemType.DantooineMountainJunglesMap;

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var map = CreateItemOnObject("map_075", player);
                ctx.Track(map);
                ctx.Assert(GetIsObjectValid(map), "The map blueprint creates a real item");
                ctx.AssertEqual((int)mapType, GetLocalInt(map, "KEY_ITEM_ID"), "The packed blueprint has the map ID");

                DeleteLocalInt(map, "KEY_ITEM_ID");
                var location = GetLocation(player);
                ctx.AssertEqual(string.Empty, definition.ValidateAction(player, map, player, location, 0),
                    "A held copy without an ID can still be used");
                definition.ApplyAction(player, map, player, location, 0);
                ctx.Assert(KeyItem.HasKeyItem(player, mapType), "Using the held copy grants the Mountain Jungles map");
            });
        }
    }
}
