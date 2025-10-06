using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Shared.UI.EventHandlers
{
    internal class UIEventHandlers
    {
        private readonly IGuiService _guiService;

        public UIEventHandlers(
            IGuiService guiService,
            IEventAggregator eventAggregator)
        {
            _guiService = guiService;

            // Subscribe to events
            eventAggregator.Subscribe<OnPlayerCacheData>(e => CreatePlayerWindows());
            eventAggregator.Subscribe<OnModuleExit>(e => SavePlayerWindowGeometry());
            eventAggregator.Subscribe<OnModuleNuiEvent>(e => HandleNuiEvents());
            eventAggregator.Subscribe<OnModuleNuiEvent>(e => HandleNuiWatchEvent());
            eventAggregator.Subscribe<OnAreaEnter>(e => CloseAllWindows());
            eventAggregator.Subscribe<OnModuleEnter>(e => DisableWindows());
        }

        /// <summary>
        /// When a player enters the server, create instances of every window if they have not already been created this session.
        /// </summary>
        public void CreatePlayerWindows()
        {
            _guiService.CreatePlayerWindows();
        }

        /// <summary>
        /// When a player exits the server, save the geometry positions of any open windows.
        /// </summary>
        public void SavePlayerWindowGeometry()
        {
            _guiService.SavePlayerWindowGeometry();
        }

        /// <summary>
        /// When a NUI event is fired, look for an associated event on the specified element
        /// and execute the cached action.
        /// </summary>
        public void HandleNuiEvents()
        {
            _guiService.HandleNuiEvents();
        }

        /// <summary>
        /// When a NUI event is fired, if it was a watch event, update the associated player's view model.
        /// </summary>
        public void HandleNuiWatchEvent()
        {
            _guiService.HandleNuiWatchEvent();
        }

        /// <summary>
        /// When a player enters an area, close all NUI windows.
        /// </summary>
        public void CloseAllWindows()
        {
            _guiService.CloseAllWindows();
        }

        /// <summary>
        /// When the player enters the server, disable default game windows.
        /// In most cases, these windows are replaced with custom versions.
        /// </summary>
        public void DisableWindows()
        {
            _guiService.DisableWindows();
        }
    }
}
