using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class AbandonedStationDockMasterEngineTests
{
    [EngineTest("Abandoned Station dock master opens its shop and boards a docked ship", Category = "DockMasterBoarding", TimeoutSeconds = 90f)]
    public static async Task ShopAndBoarding(EngineTestContext ctx)
    {
        await VisitStation(ctx);
        await VisitStation(ctx);
        ctx.SetResultDetail("Two fresh station visits passed native NPC dialogue, stocked shop, ship listing, actual property loading and boarding, and mutual nonhostility with every spawned station enemy. Both visits removed their station spawns and reset the spawn queues. Headless boarding omits the network client handshake and rendered UI.");
    }

    private static async Task VisitStation(EngineTestContext ctx)
    {
        var station = Area.GetAreaByResref("zomb_abanstation");
        ctx.Assert(GetIsObjectValid(station), "the packed production module contains the station");
        var landing = GetWaypointByTag("ABANDONED_STATION_DOCK_LANDING");
        var orbit = GetWaypointByTag("Abandoned_Station_Orbit");
        ctx.Assert(GetIsObjectValid(landing) && GetIsObjectValid(orbit), "both authored starport waypoints resolve in the packed module");
        ctx.AssertEqual(station, GetArea(landing), "the landing waypoint is in the dock master's area");

        var stationContext = new EngineTestContext(ctx.TestName, station, GetLocation(landing));
        PlayerAbilityFixture actor = null;
        WorldProperty property = null;
        PlayerShip ship = null;
        var interior = OBJECT_INVALID;
        var ownsStationSpawns = false;
        Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> windows = null;
        try
        {
            actor = await PlayerAbilityFixture.CreateAsync(stationContext);
            windows = InstallWindows(actor);
            var active = (IDictionary)SpawnState("_activeSpawnsByArea");
            var queued = (IDictionary)SpawnState("_queuedSpawnsByArea");
            ctx.Assert(!active.Contains(station) || ((IList)active[station]).Count == 0, "each visit starts without active station spawns");
            ctx.Assert(!queued.Contains(station) || ((IList)queued[station]).Count == 0, "each visit starts without queued station spawns");
            ownsStationSpawns = true;
            // The live spawn service serializes placed creatures at boot and recreates
            // them on player entry. The headless fixture does not raise that native event.
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(station).AsNWSArea().m_oidLastEntered = actor.Creature;
            ScriptExecutionProvider.Current.ExecuteInScriptContext(Spawn.SpawnArea, station);
            await ctx.WaitUntilAsync(() => GetIsObjectValid(GetObjectByTag("aban_dockmaster")), 10f, "player entry spawns the authored dock master");
            var dockMaster = GetObjectByTag("aban_dockmaster");
            ctx.AssertEqual(station, GetArea(dockMaster), "the dock master spawns on the station");
            ctx.AssertEqual(landing, GetWaypointByTag(GetLocalString(dockMaster, "STARPORT_LANDING_WAYPOINT")), "the live NPC retains its landing waypoint");
            ctx.AssertEqual(orbit, GetWaypointByTag(GetLocalString(dockMaster, "STARPORT_TELEPORT_WAYPOINT")), "the live NPC retains its orbit waypoint");
            await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
            {
                KeyItem.GiveKeyItem(actor.Creature, KeyItemType.CZ220ShuttlePass);
                property = Property.CreateStarship(actor.Creature, PropertyLayoutType.LightFreighter1,
                    Planet.GetPlanetType(station), GetLocation(orbit), GetLocation(landing));
            });
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

            // Inspect actual dungeon spawns without depending on a random table selecting a Rakghoul.
            var enemyCount = 0;
            for (var enemy = GetFirstObjectInArea(station); GetIsObjectValid(enemy); enemy = GetNextObjectInArea(station))
            {
                if (GetObjectType(enemy) != ObjectType.Creature ||
                    !GetResRef(enemy).StartsWith("abs_", StringComparison.Ordinal)) continue;
                enemyCount++;
                ctx.Assert(!GetIsEnemy(dockMaster, enemy) && !GetIsEnemy(enemy, dockMaster), "every live station enemy and dock master are mutually nonhostile");
            }
            ctx.Assert(enemyCount > 0, "player entry produces station enemies for the native faction check");

            var store = GetNearestObjectByTag("dockhand_store", dockMaster);
            ctx.Assert(GetIsObjectValid(store), "the authored dockhand store exists");
            ctx.AssertEqual(station, GetArea(store), "the nearest store is local to the station");
            ctx.Assert(GetIsObjectValid(GetFirstItemInInventory(store)), "the store has native inventory");
            await ChooseMenuResponse(ctx, actor, dockMaster, "View Shop");
            var client = GetClient(actor);
            ctx.AssertEqual(store, client.m_pStoreGUI.m_oidOpen, "the actual menu response opens the station's native store");
            client.m_pStoreGUI.CloseStore(client);

            await ChooseMenuResponse(ctx, actor, dockMaster, "Manage Ships");
            ctx.Assert(Gui.IsWindowOpen(actor.Creature, GuiWindowType.ShipManagement), "the actual menu response opens ship management");
            var model = (ShipManagementViewModel)Gui.GetPlayerWindow(actor.Creature, GuiWindowType.ShipManagement).ViewModel;
            ctx.AssertEqual(dockMaster, model.TetherObject, "the ship window is tethered to the authored dock master");
            ctx.AssertEqual(1, model.ShipNames.Count, "the docked ship is listed");
            typeof(ShipManagementViewModel).GetProperty("SelectedShipIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, 0);
            typeof(ShipManagementViewModel).GetMethod("LoadShip", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, null);
            ctx.Assert(model.IsBoardShipEnabled, "the existing menu enables boarding at the ship's recorded dock");

            await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickBoardShip());
            await ctx.WaitUntilAsync(() => Property.TryGetLoadedInstance(property.Id, out _), 30f, "the real property loader prepares the ship interior");
            ctx.Assert(Property.TryGetLoadedInstance(property.Id, out var instance), "the ship interior is enterable");
            interior = instance.Area;
            ctx.Assert(!GetIsInCombat(actor.Creature) && !Enmity.HasEnmity(actor.Creature), "boarding starts out of combat");

            // A headless fixture cannot acknowledge client area loading. Let the real queued
            // jump execute with its client unregistered, restoring native PC identity afterward.
            // This proves server movement, not rendered UI or a network client's load handshake.
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetGameObject(actor.Creature).AsNWSCreature();
            var playerFlag = creature.m_bPlayerCharacter;
            await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
            {
                model.OnClickBoardShip()();
                server.GetPlayerList().Remove(client);
                creature.m_bPlayerCharacter = 0;
            });
            try
            {
                await ctx.WaitUntilAsync(() => GetArea(actor.Creature) == interior, 8f, "the actual boarding action moves the character into its ship");
            }
            finally
            {
                creature.m_bPlayerCharacter = playerFlag;
                server.GetPlayerList().Add(client);
            }
            ctx.AssertEqual(property.Id, Property.GetPropertyId(GetArea(actor.Creature)), "boarding enters the correct owned ship");
            ctx.Assert(!Gui.IsWindowOpen(actor.Creature, GuiWindowType.ShipManagement), "successful boarding closes the management window");
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
            try
            {
                if (ownsStationSpawns) ResetStationSpawns(ctx, station);
            }
            finally
            {
                stationContext.Cleanup();
            }
            await ctx.WaitFrameAsync();
            if (ship != null) DB.Delete<PlayerShip>(ship.Id);
            if (property != null) Property.DeleteProperty(property);
            if (GetIsObjectValid(interior)) DestroyArea(interior);
        }
    }

    private static object SpawnState(string name) =>
        typeof(Spawn).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

    private static void ResetStationSpawns(EngineTestContext ctx, uint station)
    {
        var queuedByArea = (IDictionary)SpawnState("_queuedSpawnsByArea");
        var queue = (IList)SpawnState("_queuedSpawns");
        if (queuedByArea.Contains(station))
            foreach (var pending in ((IList)queuedByArea[station]).Cast<object>().ToArray())
                queue.Remove(pending);
        queuedByArea.Remove(station);

        // Exercise the normal area's despawn cleanup immediately, including prop cleanup
        // and resource timers. Only this test-owned area's deadline is advanced.
        var deadlines = (Dictionary<uint, DateTime>)SpawnState("_queuedAreaDespawns");
        deadlines[station] = DateTime.MinValue;
        typeof(Spawn).GetMethod("ProcessDespawnAreas", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        var active = (IDictionary)SpawnState("_activeSpawnsByArea");
        ctx.Assert(!active.Contains(station) || ((IList)active[station]).Count == 0, "station cleanup removes every active spawn");
        ctx.Assert(!queuedByArea.Contains(station) && !deadlines.ContainsKey(station), "station cleanup resets its spawn and despawn queues");
        active.Remove(station);
    }

    private static CNWSPlayer GetClient(PlayerAbilityFixture actor) =>
        (CNWSPlayer)typeof(PlayerAbilityFixture).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(actor);

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

    private static async Task ChooseMenuResponse(EngineTestContext ctx, PlayerAbilityFixture actor, uint dockMaster, string response)
    {
        Gui.ClosePlayerWindow(actor.Creature, GuiWindowType.Conversation);
        await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => ActionStartConversation(dockMaster, string.Empty, true, false));
        await ctx.WaitUntilAsync(() => Gui.IsWindowOpen(actor.Creature, GuiWindowType.Conversation), 8f, "the NPC's native dialogue event opens its authored conversation");
        await ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
        {
            var viewModel = Gui.GetPlayerWindow(actor.Creature, GuiWindowType.Conversation).ViewModel;
            var session = (IConversationSession)typeof(ConversationViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(viewModel);
            var choice = session.VisibleChoices.Select((value, index) => (value, index)).Single(x => x.value.Text.Text == response);
            ctx.Assert(session.SelectChoice(choice.index) != ConversationSelectionResult.InvalidChoice, "the actual menu response executes");
        });
    }
}
