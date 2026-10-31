using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Docks a ship through the real space dock menus and runs the module-load cleanup a restart performs.
/// </summary>
public static class StarshipDockReturnEngineTests
{
    private const string DockMenu = "StarportDockDialog";
    private static readonly string[] StationAreas = { "zomb_abanstation", "zomb_abanstatio2", "zomb_abanstatio3" };

    [EngineTest("A restart returns a ship left at the Abandoned Station to its last regular starport", Category = "StarshipDockReturn", TimeoutSeconds = 90f)]
    public static async Task RestartReturnsShipFromStation(EngineTestContext ctx)
    {
        var planet = Planet.GetPlanetByType(PlanetType.Viscara);
        var viscaraOrbit = GetWaypointByTag(planet.SpaceOrbitWaypointTag);
        var stationLanding = GetWaypointByTag("ABANDONED_STATION_DOCK_LANDING");
        var stationOrbit = GetWaypointByTag("Abandoned_Station_Orbit");
        var stationDock = GetObjectByTag("ABANDONED_STATION_DOCK");
        var viscaraDock = FindPlanetDock(GetArea(viscaraOrbit), PlanetType.Viscara);
        var velesDock = Space.GetDockPointsByPlanet(PlanetType.Viscara).Values
            .Single(dock => dock.IsNPC && GetResRef(GetAreaFromLocation(dock.Location)) == "velesinterior");
        var veles = GetAreaFromLocation(velesDock.Location);
        var station = GetArea(stationLanding);
        ctx.Assert(GetIsObjectValid(viscaraOrbit) && GetIsObjectValid(stationOrbit), "both orbit waypoints resolve in the packed module");
        ctx.Assert(GetIsObjectValid(stationDock) && GetIsObjectValid(viscaraDock), "both space docks resolve in Viscara orbit");
        ctx.AssertEqual("zomb_abanstation", GetResRef(station), "the station landing is in the station's docking bay");

        foreach (var resref in StationAreas)
            ctx.Assert(StarshipDockReturn.IsShipOnlyArea(Area.GetAreaByResref(resref)), $"{resref} is marked as a ship-only location in the packed module");
        ctx.Assert(!StarshipDockReturn.IsShipOnlyArea(veles), "the Veles starport is a regular starport");
        ctx.Assert(!StarshipDockReturn.IsShipOnlyArea(GetArea(viscaraOrbit)), "Viscara orbit is not a ship-only location");

        var velesContext = new EngineTestContext(ctx.TestName, veles, velesDock.Location);
        PlayerAbilityFixture actor = null;
        WorldProperty property = null;
        PlayerShip ship = null;
        Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> windows = null;
        try
        {
            actor = await PlayerAbilityFixture.CreateAsync(velesContext);
            windows = InstallWindows(actor);
            await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
                property = Property.CreateStarship(actor.Creature, PropertyLayoutType.LightFreighter1,
                    PlanetType.Viscara, GetLocation(viscaraOrbit), velesDock.Location));
            ship = new PlayerShip
            {
                OwnerPlayerId = actor.Id, PropertyId = property.Id,
                Status = new ShipStatus
                {
                    ItemTag = "ShipDeedLightFreighter", Hull = 50, MaxHull = 50,
                    Shield = 50, MaxShield = 50, Capacitor = 50, MaxCapacitor = 50
                }
            };
            DB.Set(ship);

            var created = DB.Get<WorldProperty>(property.Id).Positions;
            AssertAt(ctx, created[PropertyLocationType.LastNPCDockPosition], GetWaypointByTag(planet.LandingWaypointTag), "a new ship's safe dock is its planet's starport");
            AssertAt(ctx, created[PropertyLocationType.LastNPCSpacePosition], viscaraOrbit, "a new ship's safe orbit is its planet's orbit");

            for (var cycle = 1; cycle <= 2; cycle++)
            {
                // A regular starport becomes the safe dock, and a restart leaves the ship there.
                await Dock(ctx, actor, ship, viscaraDock, $"[NPC] {velesDock.Name}");
                var positions = DB.Get<WorldProperty>(property.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], velesDock.Location, $"cycle {cycle}: the menu docks the ship at Veles");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCDockPosition], velesDock.Location, $"cycle {cycle}: Veles becomes the safe dock");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCSpacePosition], viscaraOrbit, $"cycle {cycle}: Viscara orbit becomes the safe orbit");

                await Restart(ctx, actor);
                positions = DB.Get<WorldProperty>(property.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], velesDock.Location, $"cycle {cycle}: a restart leaves a ship at a regular starport alone");
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], viscaraOrbit, $"cycle {cycle}: a restart leaves its orbit alone");
                await AssertShipWindow(ctx, actor, viscaraOrbit, velesDock.Location, GetName(veles), true, $"cycle {cycle}: docked at Veles");

                // The station never becomes the safe dock.
                await Dock(ctx, actor, ship, stationDock, $"[NPC] {GetName(stationLanding)}");
                positions = DB.Get<WorldProperty>(property.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], stationLanding, $"cycle {cycle}: the menu docks the ship at the station");
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], stationOrbit, $"cycle {cycle}: the ship launches from the station's orbit");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCDockPosition], velesDock.Location, $"cycle {cycle}: docking at the station keeps Veles as the safe dock");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCSpacePosition], viscaraOrbit, $"cycle {cycle}: docking at the station keeps the safe orbit");
                await AssertShipWindow(ctx, actor, viscaraOrbit, velesDock.Location, GetName(station), false, $"cycle {cycle}: docked at the station");

                if (cycle == 2)
                {
                    // The second cycle restarts while the ship is out in space after leaving the station.
                    var inSpace = DB.Get<WorldProperty>(property.Id);
                    inSpace.Positions[PropertyLocationType.CurrentPosition] = ToPropertyLocation(GetLocation(stationOrbit));
                    DB.Set(inSpace);
                }

                await Restart(ctx, actor);
                positions = DB.Get<WorldProperty>(property.Id).Positions;
                ctx.Assert(!positions.ContainsKey(PropertyLocationType.CurrentPosition), $"cycle {cycle}: a restart clears the ship's space position");
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], velesDock.Location, $"cycle {cycle}: a restart returns the ship to Veles");
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], viscaraOrbit, $"cycle {cycle}: a restart returns the ship's orbit to Viscara");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCDockPosition], velesDock.Location, $"cycle {cycle}: the safe dock is unchanged");

                var dbShip = DB.Get<PlayerShip>(ship.Id);
                ctx.AssertEqual(actor.Id, dbShip.OwnerPlayerId, $"cycle {cycle}: the player still owns the ship");
                ctx.AssertEqual(property.Id, dbShip.PropertyId, $"cycle {cycle}: the ship keeps its property");
                var owned = DB.Search(new DBQuery<PlayerShip>().AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), actor.Id, false)).ToList();
                ctx.AssertEqual(1, owned.Count, $"cycle {cycle}: the player has exactly one ship");
                await AssertShipWindow(ctx, actor, viscaraOrbit, velesDock.Location, GetName(veles), true, $"cycle {cycle}: returned to Veles");
            }
        }
        finally
        {
            if (actor != null)
            {
                if (windows != null)
                {
                    foreach (var type in windows[actor.Id].Keys)
                        Gui.ClosePlayerWindow(actor.Creature, type);
                    windows.Remove(actor.Id);
                }
                actor.Dispose();
            }
            velesContext.Cleanup();
            if (ship != null) DB.Delete<PlayerShip>(ship.Id);
            if (property != null) Property.DeleteProperty(property);
        }

        ctx.SetResultDetail("Two cycles docked through the real Viscara and Abandoned Station dock menus and ran the module-load cleanup. Veles stayed the safe dock, ships at Veles stayed put, and ships left at the station (docked or in space) returned to Veles and Viscara orbit with ownership intact; the ship window showed the ship at the station and back at Veles. Headless: no client UI or network handshake.");
    }

    [EngineTest("A restart recovers ships whose safe dock was recorded at the station or that have no dock", Category = "StarshipDockReturn", TimeoutSeconds = 30f)]
    public static async Task RestartRecoversLegacyShips(EngineTestContext ctx)
    {
        var planet = Planet.GetPlanetByType(PlanetType.Viscara);
        var velesLanding = GetWaypointByTag(planet.LandingWaypointTag);
        var viscaraOrbit = GetWaypointByTag(planet.SpaceOrbitWaypointTag);
        var stationLanding = GetLocation(GetWaypointByTag("ABANDONED_STATION_DOCK_LANDING"));
        var stationOrbit = GetLocation(GetWaypointByTag("Abandoned_Station_Orbit"));
        var tatooineLanding = GetWaypointByTag(Planet.GetPlanetByType(PlanetType.Tatooine).LandingWaypointTag);
        var tatooineOrbit = GetWaypointByTag(Planet.GetPlanetByType(PlanetType.Tatooine).SpaceOrbitWaypointTag);

        // Docked at the station before it was marked, so the station was also recorded as the safe dock.
        var stranded = CreateShip(new()
        {
            [PropertyLocationType.DockPosition] = ToPropertyLocation(stationLanding),
            [PropertyLocationType.SpacePosition] = ToPropertyLocation(stationOrbit),
            [PropertyLocationType.LastNPCDockPosition] = ToPropertyLocation(stationLanding),
        });
        // Left the station for a regular starport without a recorded safe orbit; only the safe dock needs repair.
        var repaired = CreateShip(new()
        {
            [PropertyLocationType.DockPosition] = ToPropertyLocation(GetLocation(tatooineLanding)),
            [PropertyLocationType.LastNPCDockPosition] = ToPropertyLocation(stationLanding),
        });
        var missingDock = CreateShip(new()
        {
            [PropertyLocationType.LastNPCDockPosition] = ToPropertyLocation(GetLocation(tatooineLanding)),
        });
        var unrecoverable = CreateShip(new());
        var created = new[] { stranded, repaired, missingDock, unrecoverable };

        try
        {
            var context = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            for (var restart = 1; restart <= 2; restart++)
            {
                await ctx.ExecuteInCreatureContextAsync(context, RunCleanUpData);

                var positions = DB.Get<WorldProperty>(stranded.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], velesLanding, $"restart {restart}: a stranded ship returns to the Viscara starport");
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], viscaraOrbit, $"restart {restart}: a stranded ship returns to Viscara orbit");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCDockPosition], velesLanding, $"restart {restart}: the Viscara starport replaces the station as the safe dock");

                positions = DB.Get<WorldProperty>(repaired.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], tatooineLanding, $"restart {restart}: a ship at a regular starport stays there");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCDockPosition], velesLanding, $"restart {restart}: its safe dock is moved off the station");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCSpacePosition], viscaraOrbit, $"restart {restart}: its safe orbit is recorded");

                positions = DB.Get<WorldProperty>(missingDock.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], tatooineLanding, $"restart {restart}: a ship without a dock gets its safe dock");
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], tatooineOrbit, $"restart {restart}: a safe dock without a recorded orbit uses its planet's orbit");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCSpacePosition], tatooineOrbit, $"restart {restart}: that orbit is saved as the safe orbit");

                ctx.Assert(!DB.Get<WorldProperty>(unrecoverable.Id).Positions.ContainsKey(PropertyLocationType.DockPosition),
                    $"restart {restart}: a ship with no dock information is reported and left alone without stopping the cleanup");
            }
        }
        finally
        {
            foreach (var property in created)
            {
                if (DB.Exists<WorldProperty>(property.Id))
                    DB.Delete<WorldProperty>(property.Id);
            }
        }

        ctx.SetResultDetail("The module-load cleanup resolved the station's planet through the live area data, returned a ship stranded with a station safe dock to Veles, repaired a station safe dock on a ship elsewhere, restored a missing dock, and kept going past a ship with no dock data, across two restarts.");
    }

    [EngineTest("A restart keeps ships in the Viscara system instead of sending them to a Tatooine safe dock", Category = "StarshipDockReturn", TimeoutSeconds = 30f)]
    public static async Task RestartKeepsShipsInTheirSystem(EngineTestContext ctx)
    {
        var viscara = Planet.GetPlanetByType(PlanetType.Viscara);
        var cz220 = Planet.GetPlanetByType(PlanetType.CZ220);
        var tatooine = Planet.GetPlanetByType(PlanetType.Tatooine);
        var velesLanding = GetWaypointByTag(viscara.LandingWaypointTag);
        var viscaraOrbit = GetWaypointByTag(viscara.SpaceOrbitWaypointTag);
        var cz220Landing = GetWaypointByTag(cz220.LandingWaypointTag);
        var cz220Orbit = GetWaypointByTag(cz220.SpaceOrbitWaypointTag);
        var tatooineLanding = GetWaypointByTag(tatooine.LandingWaypointTag);
        var tatooineOrbit = GetWaypointByTag(tatooine.SpaceOrbitWaypointTag);
        var stationLanding = GetWaypointByTag("ABANDONED_STATION_DOCK_LANDING");
        var stationOrbit = GetWaypointByTag("Abandoned_Station_Orbit");
        foreach (var waypoint in new[] { velesLanding, viscaraOrbit, cz220Landing, cz220Orbit, tatooineLanding, tatooineOrbit, stationLanding, stationOrbit })
            ctx.Assert(GetIsObjectValid(waypoint), $"waypoint {GetTag(waypoint)} resolves in the packed module");
        ctx.AssertEqual(GetResRef(GetArea(viscaraOrbit)), GetResRef(GetArea(cz220Orbit)), "CZ-220's orbit waypoint is in the Viscara orbit area");
        ctx.AssertEqual(GetResRef(GetArea(viscaraOrbit)), GetResRef(GetArea(stationOrbit)), "the Abandoned Station's orbit is in the Viscara orbit area");

        PropertyLocation At(uint waypoint) => ToPropertyLocation(GetLocation(waypoint));
        PropertyLocation AtRemovedStarport() => new() { InstancePropertyId = Guid.NewGuid().ToString(), X = 3f, Y = 4f };
        WorldProperty Ship(PropertyLocation dock, uint orbit, uint safeDock, uint safeOrbit)
        {
            var positions = new Dictionary<PropertyLocationType, PropertyLocation>
            {
                [PropertyLocationType.SpacePosition] = At(orbit),
                [PropertyLocationType.LastNPCDockPosition] = At(safeDock),
                [PropertyLocationType.LastNPCSpacePosition] = At(safeOrbit),
            };
            if (dock != null)
                positions[PropertyLocationType.DockPosition] = dock;

            return new WorldProperty { CustomName = "Engine Test Starship", PropertyType = PropertyType.Starship, Positions = positions };
        }

        var ships = new (WorldProperty Ship, StarshipDockReturnResult Result, uint Landing, uint Orbit, string Case)[]
        {
            (Ship(At(stationLanding), stationOrbit, tatooineLanding, tatooineOrbit), StarshipDockReturnResult.ReturnedFromShipOnlyLocation,
                velesLanding, viscaraOrbit, "a ship at the Abandoned Station with a Tatooine safe dock returns to Veles"),
            (Ship(AtRemovedStarport(), cz220Orbit, tatooineLanding, tatooineOrbit), StarshipDockReturnResult.ReturnedFromMissingStarport,
                cz220Landing, cz220Orbit, "a ship at a removed starport in CZ-220's orbit with a Tatooine safe dock returns to CZ-220"),
            (Ship(null, cz220Orbit, cz220Landing, cz220Orbit), StarshipDockReturnResult.RestoredMissingDock,
                cz220Landing, cz220Orbit, "a ship without a dock in CZ-220's orbit keeps its CZ-220 safe dock"),
            (Ship(AtRemovedStarport(), viscaraOrbit, cz220Landing, cz220Orbit), StarshipDockReturnResult.ReturnedFromMissingStarport,
                cz220Landing, cz220Orbit, "a ship at a removed starport in Viscara's orbit keeps its CZ-220 safe dock in the same system"),
        };

        var results = new StarshipDockReturnResult[ships.Length];
        var context = ctx.SpawnCreature("nw_bandit001");
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(context, () =>
        {
            for (var index = 0; index < ships.Length; index++)
                results[index] = Property.ReturnStarshipToSafeDock(ships[index].Ship);
        });

        for (var index = 0; index < ships.Length; index++)
        {
            var (ship, result, landing, orbit, description) = ships[index];
            ctx.AssertEqual(result, results[index], $"{description}: result");
            AssertAt(ctx, ship.Positions[PropertyLocationType.DockPosition], landing, $"{description}: dock");
            AssertAt(ctx, ship.Positions[PropertyLocationType.SpacePosition], orbit, $"{description}: orbit");
            AssertAt(ctx, ship.Positions[PropertyLocationType.LastNPCDockPosition], landing, $"{description}: safe dock");
            AssertAt(ctx, ship.Positions[PropertyLocationType.LastNPCSpacePosition], orbit, $"{description}: safe orbit");
            ctx.Assert(string.IsNullOrEmpty(ship.Positions[PropertyLocationType.DockPosition].InstancePropertyId), $"{description}: no player starport");
        }

        ctx.SetResultDetail("Property.ReturnStarshipToSafeDock against the live module areas and waypoints: a ship at the Abandoned Station with a Tatooine safe dock went to Veles and Viscara orbit, a ship at a removed starport in CZ-220's orbit (inside the Viscara orbit area) with a Tatooine safe dock went to CZ-220, a ship without a dock in CZ-220's orbit kept its CZ-220 safe dock, and a ship at a removed starport by Viscara kept its CZ-220 safe dock in the same system.");
    }

    private static WorldProperty CreateShip(Dictionary<PropertyLocationType, PropertyLocation> positions)
    {
        var property = new WorldProperty
        {
            CustomName = "Engine Test Starship",
            PropertyType = PropertyType.Starship,
            Positions = positions
        };
        DB.Set(property);
        return property;
    }

    private static void RunCleanUpData() =>
        typeof(Property).GetMethod("CleanUpData", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

    private static Task Restart(EngineTestContext ctx, PlayerAbilityFixture actor) =>
        ctx.ExecuteInCreatureContextAsync(actor.Creature, RunCleanUpData);

    private static uint FindPlanetDock(uint orbit, PlanetType planetType)
    {
        for (var obj = GetFirstObjectInArea(orbit); GetIsObjectValid(obj); obj = GetNextObjectInArea(orbit))
        {
            if (GetObjectType(obj) == ObjectType.Placeable &&
                GetTag(obj) == "spc_dock" &&
                GetLocalInt(obj, "PLANET_TYPE_ID") == (int)planetType)
            {
                return obj;
            }
        }

        return OBJECT_INVALID;
    }

    private static async Task Dock(EngineTestContext ctx, PlayerAbilityFixture actor, PlayerShip ship, uint dock, string response)
    {
        // Players dock while piloting the ship in space; docking exits space mode and clears the active ship.
        var dbPlayer = DB.Get<Player>(actor.Id);
        dbPlayer.ActiveShipId = ship.Id;
        DB.Set(dbPlayer);
        Gui.ClosePlayerWindow(actor.Creature, GuiWindowType.Conversation);
        await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => ConversationMenu.Start(actor.Creature, dock, DockMenu));
        await ctx.WaitUntilAsync(() => Gui.IsWindowOpen(actor.Creature, GuiWindowType.Conversation), 8f, $"the {GetName(dock)} dock menu opens");
        await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
        {
            var viewModel = Gui.GetPlayerWindow(actor.Creature, GuiWindowType.Conversation).ViewModel;
            var session = (IConversationSession)typeof(ConversationViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(viewModel);
            var choice = session.VisibleChoices.Select((value, index) => (value, index)).Single(x => x.value.Text.Text == response);
            ctx.Assert(session.SelectChoice(choice.index) != ConversationSelectionResult.InvalidChoice, $"the '{response}' dock response executes");
        });
    }

    private static async Task AssertShipWindow(EngineTestContext ctx, PlayerAbilityFixture actor, uint orbit, Location landing, string expectedLocation, bool expectBoardable, string label)
    {
        Gui.ClosePlayerWindow(actor.Creature, GuiWindowType.ShipManagement);
        ShipManagementViewModel model = null;
        await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
        {
            Gui.TogglePlayerWindow(actor.Creature, GuiWindowType.ShipManagement,
                new ShipManagementPayload(PlanetType.Viscara, GetLocation(orbit), landing), OBJECT_INVALID);
            model = (ShipManagementViewModel)Gui.GetPlayerWindow(actor.Creature, GuiWindowType.ShipManagement).ViewModel;
            ctx.AssertEqual(1, model.ShipNames.Count, $"{label}: the Veles ship window lists the ship");
            typeof(ShipManagementViewModel).GetProperty("SelectedShipIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, 0);
            typeof(ShipManagementViewModel).GetMethod("LoadShip", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, null);
        });
        ctx.AssertEqual(expectedLocation, model.ShipLocation, $"{label}: the ship window shows where the ship is docked");
        ctx.AssertEqual(expectBoardable, model.IsBoardShipEnabled, $"{label}: boarding from Veles");
        Gui.ClosePlayerWindow(actor.Creature, GuiWindowType.ShipManagement);
    }

    private static Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> InstallWindows(PlayerAbilityFixture actor)
    {
        var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
            .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        windows.Add(actor.Id, new Dictionary<GuiWindowType, GuiPlayerWindow>());
        foreach (var type in new[] { GuiWindowType.Conversation, GuiWindowType.ShipManagement })
        {
            var template = Gui.GetWindowTemplate(type);
            var window = template.CreatePlayerWindowAction();
            window.ViewModel.Geometry = template.InitialGeometry;
            windows[actor.Id][type] = window;
        }
        return windows;
    }

    private static PropertyLocation ToPropertyLocation(Location location)
    {
        var position = GetPositionFromLocation(location);
        return new PropertyLocation
        {
            AreaResref = GetResRef(GetAreaFromLocation(location)),
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Orientation = GetFacingFromLocation(location)
        };
    }

    private static void AssertAt(EngineTestContext ctx, PropertyLocation actual, uint waypoint, string label) =>
        AssertAt(ctx, actual, GetLocation(waypoint), label);

    private static void AssertAt(EngineTestContext ctx, PropertyLocation actual, Location expected, string label)
    {
        var position = GetPositionFromLocation(expected);
        ctx.Assert(actual != null, $"{label}: the position is recorded");
        ctx.AssertEqual(GetResRef(GetAreaFromLocation(expected)), actual.AreaResref, $"{label}: area");
        ctx.Assert(Math.Abs(actual.X - position.X) < 0.01f && Math.Abs(actual.Y - position.Y) < 0.01f && Math.Abs(actual.Z - position.Z) < 0.01f,
            $"{label}: position ({actual.X:F2}, {actual.Y:F2}, {actual.Z:F2}) matches ({position.X:F2}, {position.Y:F2}, {position.Z:F2})");
    }
}
