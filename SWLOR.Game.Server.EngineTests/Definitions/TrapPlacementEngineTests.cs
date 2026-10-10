using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
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

        [EngineTest("Ability traps allow walking through their deployed model", Category = "TrapPlacement")]
        public static Task AbilityTrapPassability(EngineTestContext ctx) => VerifyPassability(ctx, false);

        [EngineTest("Concealed kit traps allow walking through their deployed model", Category = "TrapPlacement")]
        public static Task KitTrapPassability(EngineTestContext ctx) => VerifyPassability(ctx, true);

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

        private static async Task VerifyPassability(EngineTestContext ctx, bool kit)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var area = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                .GetGameObject(owner).AsNWSCreature().GetArea();
            var location = FindClearLocation(ctx, area);
            var position = GetPositionFromLocation(location);
            var marker = OBJECT_INVALID;
            var control = OBJECT_INVALID;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    ctx.Assert(Place(owner, location, kit), "The trap is placed on an unobstructed crossing");
                    marker = Markers(ctx.Arena).Single();
                    ctx.Track(marker);
                    var appearance = ObjectPlugin.GetAppearance(marker);
                    ctx.AssertEqual("sw_traprig", Get2DAString("placeables", "ModelName", appearance),
                        "The engine loads the trap-only model from its appearance row");
                    using var mesh = new global::NWN.Native.API.CNWPlaceableSurfaceMesh();
                    using var model = new global::NWN.Native.API.CResRef("sw_traprig");
                    ctx.Assert(mesh.LoadWalkMesh(model) != 0, "The engine successfully parses the trap walkmesh");
                    ctx.AssertEqual(0, mesh.m_nTriangles, "The loaded trap walkmesh has no blocking triangles");
                    ctx.Assert(Math.Abs(mesh.m_pvActionPoints[0].y - 0.567337f) < 0.001f,
                        "The walkmesh retains its original use point for disarming kits");
                });
                await ctx.WaitFrameAsync();

                foreach (var alongX in new[] { true, false })
                {
                    ctx.AssertEqual(1, WalkingLine(area, position, alongX),
                        "The deployed trap leaves the direct walking line clear");
                    var start = Vector3(position.X - (alongX ? 2f : 0f), position.Y - (alongX ? 0f : 2f), position.Z);
                    ObjectPlugin.SetPosition(owner, start);
                    AssignCommand(owner, () =>
                    {
                        ClearAllActions();
                        ActionMoveToLocation(location, true);
                    });
                    await ctx.WaitUntilAsync(() => GetDistanceBetween(owner, marker) < 0.2f,
                        5f, "the owner to walk into the center of the deployed trap");
                    var destination = Location(ctx.Arena,
                        Vector3(position.X + (alongX ? 2f : 0f), position.Y + (alongX ? 0f : 2f), position.Z), 0f);
                    AssignCommand(owner, () =>
                    {
                        ClearAllActions();
                        ActionMoveToLocation(destination, true);
                    });
                    await ctx.WaitUntilAsync(() => GetDistanceBetweenLocations(GetLocation(owner), destination) < 0.2f,
                        5f, "the owner to walk across the deployed trap");
                }

                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    control = CreateObject(ObjectType.Placeable, "_mdrn_pl_emitter", location, false,
                        "engine_trap_collision_control");
                    ctx.Assert(GetIsObjectValid(control), "The unchanged emitter control is created at the same crossing");
                    ctx.Track(control);
                });
                await ctx.WaitFrameAsync();
                foreach (var alongX in new[] { true, false })
                    ctx.Assert(WalkingLine(area, position, alongX) != 1,
                        "The original emitter blocks the same direct walking line");
                ctx.SetResultDetail("The engine loads sw_traprig with zero collision triangles and its kit use point. The owner walks into and across the deployed trap on both axes; the original emitter blocks both control lines.");
            }
            finally
            {
                if (GetIsObjectValid(control)) DestroyObject(control);
                await ctx.ExecuteInCreatureContextAsync(owner, () => Traps.ClearTraps(owner));
            }
        }

        private static Location FindClearLocation(EngineTestContext ctx, global::NWN.Native.API.CNWSArea area)
        {
            foreach (var x in new[] { 0f, -3f, 3f, -6f, 6f })
            foreach (var y in new[] { 0f, -3f, 3f, -6f, 6f })
            {
                var location = ctx.GetArenaLocation(x, y);
                var position = GetPositionFromLocation(location);
                if (WalkingLine(area, position, true) == 1 && WalkingLine(area, position, false) == 1)
                    return location;
            }
            throw new InvalidOperationException("The arena has no unobstructed crossing for the collision control.");
        }

        private static int WalkingLine(global::NWN.Native.API.CNWSArea area, Vector3 position, bool alongX) =>
            area.TestDirectLine(position.X - (alongX ? 2f : 0f), position.Y - (alongX ? 0f : 2f),
                position.X + (alongX ? 2f : 0f), position.Y + (alongX ? 0f : 2f), 0.5f, 1f, 1);

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
