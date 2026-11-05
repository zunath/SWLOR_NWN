using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the NUI windows. The logic and state live in <see cref="IGuiService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Gui
    {
        private static IGuiService Instance => FacadeAccessor.Resolve<IGuiService>();

        /// <summary>
        /// Name of the texture used for the GUI elements.
        /// </summary>
        public const string GuiFontName = Domain.UI.GuiService.GuiFontName;

        /// <summary>
        /// Name of the texture used for the GUI text.
        /// </summary>
        public const string TextName = Domain.UI.GuiService.TextName;

        // The following letters represent the character locations of the elements on the GUI spritesheet.
        public const string WindowTopLeft = Domain.UI.GuiService.WindowTopLeft;

        public const string WindowTopMiddle = Domain.UI.GuiService.WindowTopMiddle;

        public const string WindowTopRight = Domain.UI.GuiService.WindowTopRight;

        public const string WindowMiddleLeft = Domain.UI.GuiService.WindowMiddleLeft;

        public const string WindowMiddleRight = Domain.UI.GuiService.WindowMiddleRight;

        public const string WindowMiddleBlank = Domain.UI.GuiService.WindowMiddleBlank;

        public const string WindowBottomLeft = Domain.UI.GuiService.WindowBottomLeft;

        public const string WindowBottomRight = Domain.UI.GuiService.WindowBottomRight;

        public const string WindowBottomMiddle = Domain.UI.GuiService.WindowBottomMiddle;

        public const string Arrow = Domain.UI.GuiService.Arrow;

        public const string BlankWhite = Domain.UI.GuiService.BlankWhite;

        // The following hex codes correspond to colors used on GUI elements.
        // Color tokens won't work on Gui elements.
        public const int ColorTransparent = Domain.UI.GuiService.ColorTransparent;

        public const int ColorWhite = Domain.UI.GuiService.ColorWhite;

        public const int ColorSilver = Domain.UI.GuiService.ColorSilver;

        public const int ColorGray = Domain.UI.GuiService.ColorGray;

        public const int ColorDarkGray = Domain.UI.GuiService.ColorDarkGray;

        public const int ColorBlack = Domain.UI.GuiService.ColorBlack;

        public const int ColorRed = Domain.UI.GuiService.ColorRed;

        public const int ColorMaroon = Domain.UI.GuiService.ColorMaroon;

        public const int ColorOrange = Domain.UI.GuiService.ColorOrange;

        public const int ColorYellow = Domain.UI.GuiService.ColorYellow;

        public const int ColorOlive = Domain.UI.GuiService.ColorOlive;

        public const int ColorLime = Domain.UI.GuiService.ColorLime;

        public const int ColorGreen = Domain.UI.GuiService.ColorGreen;

        public const int ColorAqua = Domain.UI.GuiService.ColorAqua;

        public const int ColorTeal = Domain.UI.GuiService.ColorTeal;

        public const int ColorBlue = Domain.UI.GuiService.ColorBlue;

        public const int ColorNavy = Domain.UI.GuiService.ColorNavy;

        public const int ColorFuschia = Domain.UI.GuiService.ColorFuschia;

        public const int ColorPurple = Domain.UI.GuiService.ColorPurple;

        public const int ColorHealthBar = Domain.UI.GuiService.ColorHealthBar;

        public const int ColorFPBar = Domain.UI.GuiService.ColorFPBar;

        public const int ColorStaminaBar = Domain.UI.GuiService.ColorStaminaBar;

        public const int ColorShieldsBar = Domain.UI.GuiService.ColorShieldsBar;

        public const int ColorHullBar = Domain.UI.GuiService.ColorHullBar;

        public const int ColorCapacitorBar = Domain.UI.GuiService.ColorCapacitorBar;

        /// <summary>
        /// When the module loads, cache all of the GUI windows for later retrieval.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// When a player enters the server, create instances of every window if they have not already been created this session.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void CreatePlayerWindows()
        {
            Instance.CreatePlayerWindows();
        }

        /// <summary>
        /// When a player exits the server, save the geometry positions of any open windows.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void SavePlayerWindowGeometry()
        {
            Instance.SavePlayerWindowGeometry();
        }

        /// <summary>
        /// Registers an element's event information.
        /// </summary>
        /// <param name="elementId">The Id of the element to register.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventAction">The action to run when the event is raised.</param>
        public static void RegisterElementEvent(string elementId, string eventName, GuiMethodDetail eventAction)
        {
            Instance.RegisterElementEvent(elementId, eventName, eventAction);
        }

        /// <summary>
        /// When a NUI event is fired, look for an associated event on the specified element
        /// and execute the cached action.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleNuiEvent)]
        public static void HandleNuiEvents()
        {
            Instance.HandleNuiEvents();
        }

        /// <summary>
        /// When a NUI event is fired, if it was a watch event, update the associated player's view model.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleNuiEvent)]
        public static void HandleNuiWatchEvent()
        {
            Instance.HandleNuiWatchEvent();
        }

        /// <summary>
        /// Builds a window Id based on the type of window provided.
        /// </summary>
        /// <param name="windowType">The type of window.</param>
        /// <returns>A key using the window type.</returns>
        public static string BuildWindowId(GuiWindowType windowType)
        {
            return Instance.BuildWindowId(windowType);
        }

        /// <summary>
        /// Builds a key based on the window Id and element Id.
        /// This is used for event mapping.
        /// </summary>
        /// <param name="windowId">The Id of the Window</param>
        /// <param name="elementId">The Id of the Element</param>
        /// <returns>A key using the window Id and element Id.</returns>
        public static string BuildEventKey(string windowId, string elementId)
        {
            return Instance.BuildEventKey(windowId, elementId);
        }

        /// <summary>
        /// Determines whether a player currently has a window of the specified type open on screen.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of window to look for</param>
        /// <returns>true if the window is open, false otherwise</returns>
        public static bool IsWindowOpen(uint player, GuiWindowType type)
        {
            return Instance.IsWindowOpen(player, type);
        }

        /// <summary>Checks cached window tethers before querying the physical UI target.</summary>
        public static bool HasOpenWindowTetheredTo(uint uiTarget, uint tetherObject)
        {
            return Instance.HasOpenWindowTetheredTo(uiTarget, tetherObject);
        }

        /// <summary>
        /// Shows or hides a specific window for a given player.
        /// </summary>
        /// <param name="player">The player to toggle the window for.</param>
        /// <param name="type">The type of window to toggle.</param>
        /// <param name="payload">An optional payload to pass to the view model.</param>
        /// <param name="tetherObject">The object the window is tethered to. If specified, the window will automatically close if the player moves more than 5 meters away from it.</param>
        /// <param name="uiTarget">Useful for DM possessions. Can override the target of the UI to target the proper NPC a DM has possessed.</param>
        public static void TogglePlayerWindow(uint player, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = OBJECT_INVALID, uint uiTarget = OBJECT_INVALID)
        {
            Instance.TogglePlayerWindow(player, type, payload, tetherObject, uiTarget);
        }

        /// <summary>
        /// Closes an open window and runs its cleanup without ever opening a closed window.
        /// The client window is destroyed before callbacks or persistence can fail.
        /// </summary>
        public static void ClosePlayerWindow(uint player, GuiWindowType type, uint uiTarget = OBJECT_INVALID)
        {
            Instance.ClosePlayerWindow(player, type, uiTarget);
        }

        /// <summary>
        /// Publishes a refresh event. All subscribed view models will be refreshed for this particular player.
        /// This can be useful for pushing updates to a window based on external circumstances.
        /// For example: Refreshing the XP bar when a player gains XP.
        /// If the player does not have the window open on-screen, nothing will happen.
        /// </summary>
        /// <param name="player">The player to refresh.</param>
        /// <param name="payload">The refresh payload.</param>
        /// <param name="uiTarget">Where the windows are shown, when that is not the player, such as a creature a DM possesses.</param>
        public static void PublishRefreshEvent<T>(uint player, T payload, uint uiTarget = OBJECT_INVALID) where T : IGuiRefreshEvent
        {
            Instance.PublishRefreshEvent<T>(player, payload, uiTarget);
        }

        /// <summary>
        /// Refreshes every open character sheet currently displaying the supplied target.
        /// This includes sheets opened by another player, such as DM-inspected creatures.
        /// </summary>
        public static void PublishCharacterSheetRefreshEvent<T>(uint target, T payload) where T : IGuiRefreshEvent
        {
            Instance.PublishCharacterSheetRefreshEvent<T>(target, payload);
        }

        /// <summary>
        /// Skips the default NWN window open events and shows the SWLOR windows instead.
        /// Applies to the Journal, Character Sheet, Player Guide, Item Examine, and Player List panels.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
        public static void ReplaceNWNGuis()
        {
            Instance.ReplaceNWNGuis();
        }

        /// <summary>
        /// Retrieves the window instance of a player's window.
        /// </summary>
        /// <param name="player">The player to retrieve for.</param>
        /// <param name="type">The type of window to retrieve.</param>
        /// <returns>A player's window instance of the specified type.</returns>
        public static GuiPlayerWindow GetPlayerWindow(uint player, GuiWindowType type)
        {
            return Instance.GetPlayerWindow(player, type);
        }

        /// <summary>
        /// Retrieves a window's template by the given type.
        /// </summary>
        /// <param name="type">The type of window to search for.</param>
        /// <returns>A window template.</returns>
        public static GuiConstructedWindow GetWindowTemplate(GuiWindowType type)
        {
            return Instance.GetWindowTemplate(type);
        }

        /// <summary>
        /// When a player enters an area, close all NUI windows except those marked to stay open.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void CloseAllWindows()
        {
            Instance.CloseAllWindows();
        }

        /// <summary>
        /// Force closes a specified window, if open on the player's screen.
        /// This does NOT save the geometry of the window. Typically used for DM-specific functionality.
        /// </summary>
        /// <param name="player">The player whose window will close</param>
        /// <param name="type">The type of window</param>
        /// <param name="uiTarget">The UI target</param>
        public static void CloseWindow(uint player, GuiWindowType type, uint uiTarget)
        {
            Instance.CloseWindow(player, type, uiTarget);
        }

        /// <summary>
        /// Reserves the specified number of Ids for a given system.
        /// </summary>
        /// <param name="systemName">The name of the system. This should be unique across Id sets.</param>
        /// <param name="amount">The number of Ids to reserve.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public static IdReservation ReserveIds(string systemName, int amount)
        {
            return Instance.ReserveIds(systemName, amount);
        }

        /// <summary>
        /// Retrieves a system's Id reservation.
        /// Throws an exception if the system has not yet been registered.
        /// </summary>
        /// <param name="systemName">The system to retrieve by.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public static IdReservation GetSystemReservation(string systemName)
        {
            return Instance.GetSystemReservation(systemName);
        }

        public static void DrawWindow(uint player, int startId, ScreenAnchor anchor, int x, int y, int width, int height, float lifeTime = 10.0f)
        {
            Instance.DrawWindow(player, startId, anchor, x, y, width, height, lifeTime);
        }

        /// <summary>
        /// Gets the modified X coordinate of where to place a string within the center of a window.
        /// </summary>
        /// <param name="text">The text to place in the center.</param>
        /// <param name="windowX">The X position of the window.</param>
        /// <param name="windowWidth">The width of the window.</param>
        /// <returns>The X coordinate to place a string so that it will be in the center of the window.</returns>
        public static int CenterStringInWindow(string text, int windowX, int windowWidth)
        {
            return Instance.CenterStringInWindow(text, windowX, windowWidth);
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void RefreshOnEquip()
        {
            Instance.RefreshOnEquip();
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void RefreshOnUnequip()
        {
            Instance.RefreshOnUnequip();
        }
    }
}
