using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Shared.UI.Contracts
{
    public interface IGuiService
    {
        /// <summary>
        /// When the module loads, cache all of the GUI windows for later retrieval.
        /// </summary>
        void CacheData();

        /// <summary>
        /// When a player enters the server, create instances of every window if they have not already been created this session.
        /// </summary>
        void CreatePlayerWindows();

        /// <summary>
        /// When a player exits the server, save the geometry positions of any open windows.
        /// </summary>
        void SavePlayerWindowGeometry();

        /// <summary>
        /// Registers an element's event information.
        /// </summary>
        /// <param name="elementId">The Id of the element to register.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventAction">The action to run when the event is raised.</param>
        void RegisterElementEvent(string elementId, string eventName, GuiMethodDetail eventAction);

        /// <summary>
        /// When a NUI event is fired, look for an associated event on the specified element
        /// and execute the cached action.
        /// </summary>
        void HandleNuiEvents();

        /// <summary>
        /// When a NUI event is fired, if it was a watch event, update the associated player's view model.
        /// </summary>
        void HandleNuiWatchEvent();

        /// <summary>
        /// Builds a window Id based on the type of window provided.
        /// </summary>
        /// <param name="windowType">The type of window.</param>
        /// <returns>A key using the window type.</returns>
        string BuildWindowId(GuiWindowType windowType);

        /// <summary>
        /// Builds a key based on the window Id and element Id.
        /// This is used for event mapping.
        /// </summary>
        /// <param name="windowId">The Id of the Window</param>
        /// <param name="elementId">The Id of the Element</param>
        /// <returns>A key using the window Id and element Id.</returns>
        string BuildEventKey(string windowId, string elementId);

        /// <summary>
        /// Determines whether a player currently has a window of the specified type open on screen.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of window to look for</param>
        /// <returns>true if the window is open, false otherwise</returns>
        bool IsWindowOpen(uint player, GuiWindowType type);

        /// <summary>
        /// Shows or hides a specific window for a given player.
        /// </summary>
        /// <param name="player">The player to toggle the window for.</param>
        /// <param name="type">The type of window to toggle.</param>
        /// <param name="payload">An optional payload to pass to the view model.</param>
        /// <param name="tetherObject">The object the window is tethered to. If specified, the window will automatically close if the player moves more than 5 meters away from it.</param>
        /// <param name="uiTarget">Useful for DM possessions. Can override the target of the UI to target the proper NPC a DM has possessed.</param>
        void TogglePlayerWindow(
            uint player,
            GuiWindowType type,
            GuiPayloadBase payload = null,
            uint tetherObject = OBJECT_INVALID,
            uint uiTarget = OBJECT_INVALID);

        /// <summary>
        /// Publishes a refresh event. All subscribed view models will be refreshed for this particular player.
        /// This can be useful for pushing updates to a window based on external circumstances.
        /// For example: Refreshing the XP bar when a player gains XP.
        /// If the player does not have the window open on-screen, nothing will happen.
        /// </summary>
        /// <param name="player">The player to refresh.</param>
        /// <param name="payload">The refresh payload.</param>
        void PublishRefreshEvent<T>(uint player, T payload)
            where T : IGuiRefreshEvent;

        /// <summary>
        /// Skips the default NWN window open events and shows the SWLOR windows instead.
        /// Applies to the Journal and Character Sheet.
        /// </summary>
        void ReplaceNWNGuis();

        /// <summary>
        /// Retrieves the window instance of a player's window.
        /// </summary>
        /// <param name="player">The player to retrieve for.</param>
        /// <param name="type">The type of window to retrieve.</param>
        /// <returns>A player's window instance of the specified type.</returns>
        GuiPlayerWindow GetPlayerWindow(uint player, GuiWindowType type);

        /// <summary>
        /// Retrieves a window's template by the given type.
        /// </summary>
        /// <param name="type">The type of window to search for.</param>
        /// <returns>A window template.</returns>
        GuiConstructedWindow GetWindowTemplate(GuiWindowType type);

        /// <summary>
        /// When a player enters an area, close all NUI windows.
        /// </summary>
        void CloseAllWindows();

        /// <summary>
        /// Force closes a specified window, if open on the player's screen.
        /// This does NOT save the geometry of the window. Typically used for DM-specific functionality.
        /// </summary>
        /// <param name="player">The player whose window will close</param>
        /// <param name="type">The type of window</param>
        /// <param name="uiTarget">The UI target</param>
        void CloseWindow(uint player, GuiWindowType type, uint uiTarget);

        /// <summary>
        /// Reserves the specified number of Ids for a given system.
        /// </summary>
        /// <param name="systemName">The name of the system. This should be unique across Id sets.</param>
        /// <param name="amount">The number of Ids to reserve.</param>
        /// <returns>An object containing Id reservation details.</returns>
        IdReservation ReserveIds(string systemName, int amount);

        /// <summary>
        /// Retrieves a system's Id reservation.
        /// Throws an exception if the system has not yet been registered.
        /// </summary>
        /// <param name="systemName">The system to retrieve by.</param>
        /// <returns>An object containing Id reservation details.</returns>
        IdReservation GetSystemReservation(string systemName);

        void DrawWindow(uint player, int startId, ScreenAnchor anchor, int x, int y, int width, int height, float lifeTime = 10.0f);

        /// <summary>
        /// Gets the modified X coordinate of where to place a string within the center of a window.
        /// </summary>
        /// <param name="text">The text to place in the center.</param>
        /// <param name="windowX">The X position of the window.</param>
        /// <param name="windowWidth">The width of the window.</param>
        /// <returns>The X coordinate to place a string so that it will be in the center of the window.</returns>
        int CenterStringInWindow(string text, int windowX, int windowWidth);

        void RefreshOnEquip();
        void RefreshOnUnequip();
    }
}