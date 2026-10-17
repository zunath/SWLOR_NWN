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

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class AbandonedStationDockMasterEngineTests
{
    [EngineTest("Abandoned Station dock master opens its shop and boards a docked ship", Category = "DockMasterBoarding", TimeoutSeconds = 90f)]
    public static async Task ShopAndBoarding(EngineTestContext ctx)
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
        Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> windows = null;
        try
        {
            actor = await PlayerAbilityFixture.CreateAsync(stationContext);
            windows = InstallWindows(actor);
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

            // Inspect an actual dungeon spawn, preserving its authored faction.
            var enemy = GetObjectByTag("abs_rakghoul");
            ctx.Assert(GetIsObjectValid(enemy), "the production station enemy spawns on player entry");
            ctx.AssertEqual(station, GetArea(enemy), "the live enemy is on the station");
            ctx.Assert(!GetIsEnemy(dockMaster, enemy) && !GetIsEnemy(enemy, dockMaster), "the live station enemy and dock master are mutually nonhostile");

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
            ctx.SetResultDetail("Packed station NPC and both waypoints loaded; live Rakghoul faction is nonhostile both ways; authored menu opened its stocked native shop and ship management; the real property loader and boarding action entered the owned ship. Headless area transfer omits the network client handshake and rendered UI.");
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
            stationContext.Cleanup();
            await ctx.WaitFrameAsync();
            if (ship != null) DB.Delete<PlayerShip>(ship.Id);
            if (property != null) Property.DeleteProperty(property);
            if (GetIsObjectValid(interior)) DestroyArea(interior);
        }
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
