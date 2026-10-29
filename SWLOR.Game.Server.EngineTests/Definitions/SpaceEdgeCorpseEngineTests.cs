using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceEdgeCorpseEngineTests
    {
        private const float FloorHeight = 3f;

        [EngineTest("A ship destroyed on the space map edge drops reachable loot", Category = "SpaceLoot", TimeoutSeconds = 90f)]
        public static async Task ShipDestroyedOnMapEdgeDropsReachableLoot(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea("space_dathomir");
            await ctx.WaitFrameAsync();

            // A ship flying toward a point past the north edge stops on the boundary with no walkmesh under it.
            var ship = CreateObject(ObjectType.Creature, "t5gunship", Location(area, new Vector3(140f, 315f, FloorHeight), 90f));
            ctx.Track(ship);
            ChangeToStandardFaction(ship, StandardFaction.Commoner);
            await ctx.ExecuteInCreatureContextAsync(ship, () =>
                ActionForceMoveToLocation(Location(area, new Vector3(140f, 335f, FloorHeight), 90f), true));
            await ctx.WaitUntilAsync(() => GetPosition(ship).Y >= 319.99f, 15f, "the ship to reach the north boundary");
            await ctx.DelaySecondsAsync(1f);

            var bodyPosition = GetPosition(ship);
            ctx.Assert(bodyPosition.Z < FloorHeight - 1f,
                $"The ship stopped on the boundary below the floor (position {bodyPosition}); the test no longer reproduces the report.");

            await ctx.ExecuteInCreatureContextAsync(ship, () =>
            {
                ClearAllActions();
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), ship);
            });

            var corpse = OBJECT_INVALID;
            await ctx.WaitUntilAsync(() =>
            {
                corpse = GetNearestObjectByTag("corpse", ship);
                return GetIsObjectValid(corpse);
            }, 5f, "the ship's corpse to be created");
            ctx.Track(corpse);

            var corpsePosition = GetPosition(corpse);
            ctx.Assert(corpsePosition.Y <= 320f - CorpsePlacement.EdgeMargin + 0.01f,
                $"The corpse is inside the area (position {corpsePosition}).");
            ctx.Assert(MathF.Abs(corpsePosition.Z - (FloorHeight - CorpsePlacement.SinkDepth)) < 0.05f,
                $"The corpse sits on the floor (position {corpsePosition}).");

            var looter = CreateObject(ObjectType.Creature, "t5gunship", Location(area, new Vector3(140f, 300f, FloorHeight), 90f));
            ctx.Track(looter);
            ChangeToStandardFaction(looter, StandardFaction.Commoner);
            await ctx.ExecuteInCreatureContextAsync(looter, () => ActionMoveToObject(corpse, true, 1f));
            await ctx.WaitUntilAsync(() => GetDistanceBetween(looter, corpse) <= 1.5f, 15f,
                "a ship flying from open space to reach the corpse");

            ctx.SetResultDetail($"Ship stopped on the north boundary at {bodyPosition}; its corpse was created at {corpsePosition} and another ship reached it.");
        }
    }
}
