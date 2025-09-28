using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Events.Skill;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Shared.UI.EventHandlers
{
    internal class UIEventHandlers
    {
        private readonly IGuiService _guiService;

        public UIEventHandlers(IGuiService guiService)
        {
            _guiService = guiService;
        }

        /// <summary>
        /// When the module loads, cache all of the GUI windows for later retrieval.
        /// </summary>
        [ScriptHandler<OnSkillDataCached>]
        public void CacheData()
        {
            _guiService.CacheData();
        }

        /// <summary>
        /// When a player enters the server, create instances of every window if they have not already been created this session.
        /// </summary>
        [ScriptHandler<OnPlayerCacheData>]
        public void CreatePlayerWindows()
        {
            _guiService.CreatePlayerWindows();
        }

        /// <summary>
        /// When a player exits the server, save the geometry positions of any open windows.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void SavePlayerWindowGeometry()
        {
            _guiService.SavePlayerWindowGeometry();
        }

        /// <summary>
        /// When a NUI event is fired, look for an associated event on the specified element
        /// and execute the cached action.
        /// </summary>
        [ScriptHandler<OnModuleNuiEvent>]
        public void HandleNuiEvents()
        {
            _guiService.HandleNuiEvents();
        }

        /// <summary>
        /// When a NUI event is fired, if it was a watch event, update the associated player's view model.
        /// </summary>
        [ScriptHandler<OnModuleNuiEvent>]
        public void HandleNuiWatchEvent()
        {
            _guiService.HandleNuiWatchEvent();
        }

        /// <summary>
        /// When a player enters an area, close all NUI windows.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void CloseAllWindows()
        {
            _guiService.CloseAllWindows();
        }

        /// <summary>
        /// When the player enters the server, disable default game windows.
        /// In most cases, these windows are replaced with custom versions.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void DisableWindows()
        {
            _guiService.DisableWindows();
        }
    }
}
