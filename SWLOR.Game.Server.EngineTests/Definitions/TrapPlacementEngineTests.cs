using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TrapPlacementEngineTests
    {
        [EngineTest("Failed kit placement preserves traps at capacity", Category = "TrapPlacement")]
        public static Task FailedKitPlacement(EngineTestContext ctx) => VerifyReplacement(ctx, true);

        [EngineTest("Failed ability trap placement preserves traps at capacity", Category = "TrapPlacement")]
        public static Task FailedAbilityPlacement(EngineTestContext ctx) => VerifyReplacement(ctx, false);

        private static async Task VerifyReplacement(EngineTestContext ctx, bool kit)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var oldest = OBJECT_INVALID;
            var newer = OBJECT_INVALID;
            var firstLocation = ctx.GetArenaLocation();
            var secondLocation = ctx.GetArenaLocation(5f);
            var replacementLocation = ctx.GetArenaLocation(10f);

            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    TemporaryStatModifier.Add(owner, StatType.AdditionalTrapCapacity, 1, 60f, "ENGINE_TEST_TRAP_CAPACITY");
                    ctx.AssertEqual(2, Traps.GetTrapCapacity(owner), "The fixture can hold two traps");
                    ctx.Assert(Place(owner, firstLocation, kit), "The first trap is placed");
                    oldest = Markers(ctx.Arena).Single();
                    ctx.Track(oldest);
                    ctx.Assert(Place(owner, secondLocation, kit), "The second trap fills capacity");
                    newer = Markers(ctx.Arena).Single(marker => marker != oldest);
                    ctx.Track(newer);

                    var invalidLocation = Location(OBJECT_INVALID, Vector3(0f, 0f, 0f), 0f);
                    ctx.Assert(!Place(owner, invalidLocation, kit), "Marker creation fails outside a valid area");
                });
                await ctx.WaitFrameAsync();

                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    ctx.Assert(GetIsObjectValid(oldest), "Failed replacement preserves the oldest trap marker");
                    ctx.Assert(GetIsObjectValid(newer), "Failed replacement preserves the newer trap marker");
                    ctx.AssertEqual(2, Markers(ctx.Arena).Count, "Failed placement creates no extra marker");
                    ctx.Assert(!Place(owner, firstLocation, kit), "The original trap remains live and still enforces spacing");
                });
                await ctx.WaitFrameAsync();

                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    ctx.Assert(GetIsObjectValid(oldest), "Rejected spacing also preserves the oldest trap");
                    ctx.Assert(Place(owner, replacementLocation, kit), "A valid replacement succeeds at capacity");
                    foreach (var marker in Markers(ctx.Arena)) ctx.Track(marker);
                });
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(oldest), 3f, "successful replacement to remove the oldest marker");
                ctx.Assert(GetIsObjectValid(newer), "Successful replacement retains the newer trap");
                ctx.AssertEqual(2, Markers(ctx.Arena).Count, "Successful replacement keeps the owner at capacity");
            }
            finally
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () => Traps.ClearTraps(owner));
            }
        }

        private static bool Place(uint owner, Location location, bool kit) => kit
            ? Traps.TryPlaceKitTrap(owner, location, 1)
            : Traps.TryPlaceTrap(owner, location, 14, CombatDamageType.Physical, typeof(BleedStatusEffect), 30,
                VisualEffect.Vfx_Com_Blood_Spark_Medium, VisualEffect.Vfx_Dur_Aura_Pulse_Orange_White);

        private static List<uint> Markers(uint area)
        {
            var markers = new List<uint>();
            for (var obj = GetFirstObjectInArea(area, ObjectType.Placeable); GetIsObjectValid(obj);
                 obj = GetNextObjectInArea(area, ObjectType.Placeable))
                if (GetTag(obj) is "espn_trap" or "field_engineer_pulse_marker") markers.Add(obj);
            return markers;
        }
    }
}
