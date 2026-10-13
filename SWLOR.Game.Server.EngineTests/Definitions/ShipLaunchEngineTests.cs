using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShipLaunchEngineTests
    {
        /// <summary>Rejects malformed destinations without changing appearance, quickbars, or return state.</summary>
        [EngineTest("Invalid ship destinations leave the pilot unchanged", Category = "ShipLaunch", TimeoutSeconds = 30f)]
        public static Task InvalidDestinations(EngineTestContext ctx)
        {
            return WithShipComputer(ctx, async (actor, computer, property) =>
            {
                var appearance = GetAppearanceType(actor.Creature);
                var position = GetPosition(actor.Creature);
                var quickbar = CreaturePlugin.SerializeQuickbar(actor.Creature);
                var record = DB.Get<Player>(actor.Id);
                var savedQuickbar = record.SerializedHotBar;
                var savedAppearance = record.OriginalAppearanceType;

                // Missing property, missing position, null position, empty resref, unknown area,
                // stale cached area, and an invalid current position with a valid dock fallback.
                var cases = new PropertyLocation[]
                {
                    null,
                    new(),
                    new() { AreaResref = "missing_launch" },
                    new() { AreaResref = "engine_launch" },
                };
                DB.Delete<WorldProperty>(property.Id);
                await UseAndAssertUnchanged();
                DB.Set(property);
                await UseAndAssertUnchanged();
                foreach (var destination in cases)
                {
                    property.Positions[PropertyLocationType.SpacePosition] = destination;
                    DB.Set(property);
                    Area.GetAreas()["engine_launch"] = OBJECT_INVALID;
                    await UseAndAssertUnchanged();
                }
                Area.GetAreas()["engine_launch"] = ctx.Arena;
                property.Positions[PropertyLocationType.CurrentPosition] = new PropertyLocation { AreaResref = "missing_launch" };
                DB.Set(property);
                await UseAndAssertUnchanged();

                async Task UseAndAssertUnchanged()
                {
                    await ctx.ExecuteInCreatureContextAsync(computer, Space.UseShipComputer);
                    var after = DB.Get<Player>(actor.Id);
                    ctx.AssertEqual(Guid.Empty.ToString(), after.ActiveShipId, "failed launch does not activate the ship");
                    ctx.AssertEqual(appearance, GetAppearanceType(actor.Creature), "failed launch preserves the character model");
                    ctx.AssertEqual(position, GetPosition(actor.Creature), "failed launch keeps the character in place");
                    ctx.AssertEqual(quickbar, CreaturePlugin.SerializeQuickbar(actor.Creature), "failed launch preserves the native quickbar");
                    ctx.AssertEqual(savedQuickbar, after.SerializedHotBar, "failed launch preserves the saved quickbar");
                    ctx.AssertEqual(savedAppearance, after.OriginalAppearanceType, "failed launch preserves the saved appearance");
                    ctx.Assert(!GetIsObjectValid(GetLocalObject(actor.Creature, "SPACE_PILOT_CLONE")), "failed launch creates no Pilot clone");
                    ctx.Assert(!GetLocalBool(actor.Creature, "SPACE_INSTANCE_LOCATION_SET"), "failed launch creates no return location");
                }
            });
        }

        /// <summary>Cancels movement before departure, then completes a fresh request through cross-area entry.</summary>
        [EngineTest("Canceled ship launch preserves the character and controls remain reusable", Category = "ShipLaunch", TimeoutSeconds = 15f)]
        public static Task CanceledLaunch(EngineTestContext ctx)
        {
            return WithShipComputer(ctx, async (actor, computer, property) =>
            {
                var appearance = GetAppearanceType(actor.Creature);
                var quickbar = CreaturePlugin.SerializeQuickbar(actor.Creature);
                var destination = GetPositionFromLocation(ctx.GetArenaLocation(6f));
                var spaceArea = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
                property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    AreaResref = "engine_launch", X = destination.X, Y = destination.Y, Z = destination.Z
                };
                Area.GetAreas()["engine_launch"] = spaceArea;
                DB.Set(property);
                await ctx.ExecuteInCreatureContextAsync(computer, () =>
                {
                    Space.UseShipComputer();
                    AssignCommand(actor.Creature, () => ClearAllActions());
                });
                await ctx.DelaySecondsAsync(0.5f);
                ctx.Assert(!Space.IsPlayerInSpaceMode(actor.Creature), "canceling launch leaves the player on foot");
                ctx.AssertEqual(appearance, GetAppearanceType(actor.Creature), "canceling launch preserves the character model");
                ctx.AssertEqual(quickbar, CreaturePlugin.SerializeQuickbar(actor.Creature), "canceling launch preserves the quickbar");
                ctx.Assert(!GetIsObjectValid(GetLocalObject(actor.Creature, "SPACE_PILOT_CLONE")), "canceling launch leaves no Pilot clone");
                ctx.Assert(!GetLocalBool(actor.Creature, "SPACE_INSTANCE_LOCATION_SET"), "canceling launch leaves no return location");
                await UseWithoutClientTransfer(ctx, actor, computer);
                await CompleteAreaLoad(ctx, actor, spaceArea, destination, true);
                await ctx.WaitUntilAsync(() => Space.IsPlayerInSpaceMode(actor.Creature), 5f, "the controls can launch again after cancellation");
            });
        }

        /// <summary>Exercises docked and resumed launches between distinct areas, including Pilot placement and exit restoration.</summary>
        [EngineTest("Successful ship launch preserves the Pilot and restores the character on exit", Category = "ShipLaunch", TimeoutSeconds = 30f)]
        public static Task SuccessfulLaunch(EngineTestContext ctx)
        {
            return WithShipComputer(ctx, async (actor, computer, property) =>
            {
                var origin = GetPosition(actor.Creature);
                var appearance = GetAppearanceType(actor.Creature);
                var quickbar = CreaturePlugin.SerializeQuickbar(actor.Creature);
                var destination = GetPositionFromLocation(ctx.GetArenaLocation(6f));
                var spaceArea = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
                property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    AreaResref = "engine_launch", X = destination.X, Y = destination.Y, Z = destination.Z
                };
                Area.GetAreas()["engine_launch"] = spaceArea;

                // Check both docked launches and resuming a ship's current space position.
                foreach (var resume in new[] { false, true })
                {
                    if (resume)
                    {
                        property.Positions[PropertyLocationType.CurrentPosition] = property.Positions[PropertyLocationType.SpacePosition];
                        property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation { AreaResref = "missing_launch" };
                    }
                    DB.Set(property);
                    await ctx.ExecuteInCreatureContextAsync(computer, () =>
                    {
                        Space.UseShipComputer();
                        Space.UseShipComputer();
                        // A fake client cannot acknowledge the native area's load request.
                        AssignCommand(actor.Creature, () => ClearAllActions());
                    });
                    await CompleteAreaLoad(ctx, actor, spaceArea, destination, true);
                    await ctx.WaitUntilAsync(() => Space.IsPlayerInSpaceMode(actor.Creature), 5f, "launch enables space mode after arrival");
                    ctx.Assert(Vector3.Distance(GetPosition(actor.Creature), destination) < 1f, "the transformed pilot is at the launch destination");
                    ctx.Assert(Space.IsPlayerInSpaceMode(actor.Creature), "successful launch activates the ship");
                    ctx.AssertEqual(spaceArea, GetArea(actor.Creature), "the pilot crossed into a distinct space area");
                    var ship = Space.GetShipDetailByItemTag("ShipDeedLightFreighter");
                    ctx.AssertEqual(ship.Appearance, GetAppearanceType(actor.Creature), "successful launch applies the ship model");
                    var clone = GetLocalObject(actor.Creature, "SPACE_PILOT_CLONE");
                    ctx.Assert(GetIsObjectValid(clone), "successful launch creates a Pilot clone");
                    ctx.AssertEqual(appearance, GetAppearanceType(clone), "the Pilot clone retains the character model");
                    ctx.Assert(Vector3.Distance(GetPosition(clone), origin) < 1f, "the Pilot clone remains at the controls");
                    ctx.AssertEqual(ctx.Arena, GetArea(clone), "the Pilot remains in the interior area");
                    ctx.AssertEqual(quickbar, DB.Get<Player>(actor.Id).SerializedHotBar, "repeated panel use preserves the original quickbar");
                    ctx.AssertEqual(origin, GetPositionFromLocation(GetLocalLocation(actor.Creature, "SPACE_INSTANCE_LOCATION")),
                        "repeated panel use preserves the return location");
                    var savedPosition = DB.Get<WorldProperty>(property.Id).Positions[PropertyLocationType.CurrentPosition];
                    ctx.AssertEqual(GetResRef(spaceArea), savedPosition.AreaResref, "launch records the current space area");
                    ctx.Assert(Vector3.Distance(GetPosition(actor.Creature), new Vector3(savedPosition.X, savedPosition.Y, savedPosition.Z)) < 0.1f,
                        "launch records the actual space position after arrival");

                    // Keep cleanup docked so ExitSpaceMode cannot leave an NPC ship in the arena.
                    property.Positions.Remove(PropertyLocationType.CurrentPosition);
                    DB.Set(property);
                    await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
                    {
                        Space.WarpPlayerInsideShip(actor.Creature);
                        AssignCommand(actor.Creature, () => ClearAllActions());
                    });
                    await CompleteAreaLoad(ctx, actor, ctx.Arena, origin, false);
                    await ctx.WaitUntilAsync(() => Vector3.Distance(GetPosition(actor.Creature), origin) < 1f, 5f, "pilot returns to the controls");
                    await ctx.WaitUntilAsync(() => !GetIsObjectValid(clone), 3f, "Pilot clone is removed on exit");
                    ctx.AssertEqual(appearance, GetAppearanceType(actor.Creature), "exit restores the character model");
                    ctx.AssertEqual(quickbar, CreaturePlugin.SerializeQuickbar(actor.Creature), "exit restores the original quickbar");
                }
            });
        }

        /// <summary>Completes a delayed arrival through the scheduler after losing both queued actions and the arrival event.</summary>
        [EngineTest("Late area loading completes a launch after actions are cleared", Category = "ShipLaunch", TimeoutSeconds = 15f)]
        public static Task LateArrival(EngineTestContext ctx)
        {
            return WithShipComputer(ctx, async (actor, computer, property) =>
            {
                var destination = GetPositionFromLocation(ctx.GetArenaLocation(6f));
                var spaceArea = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
                Area.GetAreas()["engine_launch"] = spaceArea;
                property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    AreaResref = "engine_launch", X = destination.X, Y = destination.Y, Z = destination.Z
                };
                DB.Set(property);
                await UseWithoutClientTransfer(ctx, actor, computer);
                MarkDeparture(actor, ctx.Arena);
                ExpirePendingLaunch(actor);
                await ctx.DelaySecondsAsync(0.4f);
                ctx.Assert(PendingLaunches().Contains(actor.Creature), "a loading player retains the pending launch beyond the idle timeout");
                ctx.Assert(!Space.IsPlayerInSpaceMode(actor.Creature), "area loading does not transform a character still inside");

                // Suppress the area-entry notification as well as clearing all actions.
                // The server scheduler must still notice arrival and finish the launch.
                await CompleteAreaLoad(ctx, actor, spaceArea, destination, false);
                await ctx.WaitUntilAsync(() => Space.IsPlayerInSpaceMode(actor.Creature), 3f, "scheduled completion after delayed cross-area arrival");
                ctx.Assert(!PendingLaunches().Contains(actor.Creature), "successful completion releases the pending reservation");
                ctx.AssertEqual(ctx.Arena, GetArea(GetLocalObject(actor.Creature, "SPACE_PILOT_CLONE")), "late completion leaves the Pilot inside");
            });
        }

        /// <summary>Verifies that timeout and death release reservations without transforming the character.</summary>
        [EngineTest("Unstarted ship launches expire and death releases their reservation", Category = "ShipLaunch", TimeoutSeconds = 15f)]
        public static Task PendingLaunchCleanup(EngineTestContext ctx)
        {
            return WithShipComputer(ctx, async (actor, computer, property) =>
            {
                var destination = GetPositionFromLocation(ctx.GetArenaLocation(6f));
                Area.GetAreas()["engine_launch"] = ctx.CreateInstancedArea(GetResRef(ctx.Arena));
                property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    AreaResref = "engine_launch", X = destination.X, Y = destination.Y, Z = destination.Z
                };
                DB.Set(property);
                await UseWithoutClientTransfer(ctx, actor, computer);
                ExpirePendingLaunch(actor);
                await ctx.WaitUntilAsync(() => !PendingLaunches().Contains(actor.Creature), 3f, "canceled launch reservation expires");
                ctx.Assert(!Space.IsPlayerInSpaceMode(actor.Creature), "expiration leaves the character on foot");
                await UseWithoutClientTransfer(ctx, actor, computer);
                ctx.Assert(PendingLaunches().Contains(actor.Creature), "a later launch obtains a fresh reservation");
                Space.ApplyDeath(actor.Creature);
                ctx.Assert(!PendingLaunches().Contains(actor.Creature), "death cleanup removes an uncompleted launch");
            });
        }

        /// <summary>Starts the real computer flow while preventing a headless client from initiating an unsupported load handshake.</summary>
        private static Task UseWithoutClientTransfer(EngineTestContext ctx, PlayerAbilityFixture actor, uint computer)
        {
            return ctx.ExecuteInCreatureContextAsync(computer, () =>
            {
                Space.UseShipComputer();
                AssignCommand(actor.Creature, () => ClearAllActions());
            });
        }

        /// <summary>Dispatches the ship's area-exit handler using the native area's last exiting creature.</summary>
        private static void MarkDeparture(PlayerAbilityFixture actor, uint origin)
        {
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(origin).AsNWSArea().m_oidLastLeft = actor.Creature;
            ScriptExecutionProvider.Current.ExecuteInScriptContext(Space.ClearTargetOnAreaExit, origin);
        }

        /// <summary>
        /// Simulates a client's area-load acknowledgment by temporarily unregistering the
        /// fake client for native movement, then restoring it before Space handles arrival.
        /// No jump callback is used, and all queued actions are cleared after movement.
        /// </summary>
        private static Task CompleteAreaLoad(EngineTestContext ctx, PlayerAbilityFixture actor,
            uint area, Vector3 position, bool notifyArrival)
        {
            return ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
            {
                MarkDeparture(actor, GetArea(actor.Creature));
                ClearAllActions();
                var server = NWNXLib.g_pAppManager.m_pServerExoApp;
                var client = (CNWSPlayer)typeof(PlayerAbilityFixture).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(actor);
                var creature = server.GetGameObject(actor.Creature).AsNWSCreature();
                var playerFlag = creature.m_bPlayerCharacter;
                server.GetPlayerList().Remove(client);
                creature.m_bPlayerCharacter = 0;
                try
                {
                    creature.RemoveFromArea();
                    creature.AddToArea(server.GetGameObject(area).AsNWSArea(), position.X, position.Y, position.Z, 0, 0);
                }
                finally
                {
                    creature.m_bPlayerCharacter = playerFlag;
                    server.GetPlayerList().Add(client);
                }
                ClearAllActions();
                ctx.AssertEqual(area, GetArea(actor.Creature), "native fixture reaches the distinct destination area");
                if (notifyArrival)
                {
                    server.GetGameObject(area).AsNWSArea().m_oidLastEntered = actor.Creature;
                    ScriptExecutionProvider.Current.ExecuteInScriptContext(Space.UpdateSpacePosition, area);
                    ctx.Assert(Space.IsPlayerInSpaceMode(actor.Creature), "area-entry handling completes launch synchronously without an action callback");
                }
            });
        }

        /// <summary>Reads live reservations so tests can assert cleanup without exposing production test seams.</summary>
        private static IDictionary PendingLaunches() => (IDictionary)typeof(Space)
            .GetField("_pendingShipLaunches", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

        /// <summary>Advances only the request's timeout to test expiry without a thirty-second wall-clock delay.</summary>
        private static void ExpirePendingLaunch(PlayerAbilityFixture actor)
        {
            var launch = PendingLaunches()[actor.Creature];
            launch.GetType().GetProperty("ExpiresAt").SetValue(launch, DateTime.UtcNow.AddSeconds(-1));
        }

        /// <summary>Creates an isolated native pilot, GUI state, ship computer, and persisted ship records with deterministic cleanup.</summary>
        private static async Task WithShipComputer(EngineTestContext ctx,
            Func<PlayerAbilityFixture, uint, WorldProperty, Task> test)
        {
            using var actor = await PlayerAbilityFixture.CreateAsync(ctx);
            actor.Update(player => player.Perks[PerkType.Starships] = 1);
            var statusWindows = new[] { GuiWindowType.PlayerStatus, GuiWindowType.PlayerStatusPortrait, GuiWindowType.PlayerStatusPortraitSpace };
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            windows[actor.Id] = new Dictionary<GuiWindowType, GuiPlayerWindow>();
            foreach (var type in statusWindows)
            {
                var template = Gui.GetWindowTemplate(type);
                var window = template.CreatePlayerWindowAction();
                window.ViewModel.Geometry = template.InitialGeometry;
                windows[actor.Id][type] = window;
            }
            var property = new WorldProperty { Id = "engine-launch-property/" + actor.Id, OwnerPlayerId = actor.Id };
            var ship = new PlayerShip
            {
                Id = "engine-launch-ship/" + actor.Id, PropertyId = property.Id, OwnerPlayerId = actor.Id,
                Status = new ShipStatus { ItemTag = "ShipDeedLightFreighter", Hull = 50, MaxHull = 50 }
            };
            var permission = new WorldPropertyPermission { Id = "engine-launch-permission/" + actor.Id, PropertyId = property.Id, PlayerId = actor.Id };
            permission.Permissions[PropertyPermissionType.PilotShip] = true;
            var originalPropertyId = Property.GetPropertyId(ctx.Arena);
            var computer = CreateObject(ObjectType.Placeable, "shipcomputer", ctx.GetArenaLocation(2f));
            ctx.Assert(GetIsObjectValid(computer), "spawn the real ship computer blueprint");
            ctx.Track(computer);
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(computer).AsNWSPlaceable().m_oidLastUser = actor.Creature;
            SetLocalString(ctx.Arena, "PROPERTY_ID", property.Id);
            DB.Set(property);
            DB.Set(ship);
            DB.Set(permission);
            try
            {
                await test(actor, computer, property);
            }
            finally
            {
                PendingLaunches().Remove(actor.Creature);
                property.Positions.Remove(PropertyLocationType.CurrentPosition);
                DB.Set(property);
                if (Space.IsPlayerInSpaceMode(actor.Creature))
                    await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => Space.ExitSpaceMode(actor.Creature));
                foreach (var type in statusWindows)
                    if (Gui.IsWindowOpen(actor.Creature, type))
                        Gui.CloseWindow(actor.Creature, type, actor.Creature);
                windows.Remove(actor.Id);
                Area.GetAreas().Remove("engine_launch");
                SetLocalString(ctx.Arena, "PROPERTY_ID", originalPropertyId);
                DB.Delete<WorldPropertyPermission>(permission.Id);
                DB.Delete<PlayerShip>(ship.Id);
                DB.Delete<WorldProperty>(property.Id);
            }
        }
    }
}
