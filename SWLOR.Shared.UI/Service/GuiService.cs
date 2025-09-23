using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Entity;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Shared.UI.Service
{
    public class GuiService: IGuiService
    {
        private readonly IDatabaseService _db;
        private readonly Dictionary<GuiWindowType, GuiConstructedWindow> _windowTemplates = new();
        private readonly Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> _playerWindows = new();
        private readonly Dictionary<string, Dictionary<string, GuiMethodDetail>> _elementEvents = new();
        private readonly Dictionary<string, GuiWindowType> _windowTypesByKey = new();
        private readonly Dictionary<Type, List<GuiWindowType>> _windowTypesByRefreshEvent = new();

        public GuiService(IDatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// When the module loads, cache all of the GUI windows for later retrieval.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorSkillCache)]
        public void CacheData()
        {
            LoadWindowTemplates();
            LoadRefreshableViewModels();
        }

        /// <summary>
        /// Loads all of the window definitions, constructs them, and caches the data into memory.
        /// </summary>
        private void LoadWindowTemplates()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IGuiWindowDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IGuiWindowDefinition)ServiceContainer.ServiceProvider.GetRequiredService(type);
                var constructedWindow = instance.BuildWindow();

                // Safety check to ensure we don't try to build the same type of window more than once.
                if (_windowTemplates.ContainsKey(constructedWindow.Type))
                {
                    throw new Exception($"GUI Window type '{constructedWindow.Type}' has been defined more than once.");
                }

                // Register the window template into the cache.
                _windowTemplates[constructedWindow.Type] = constructedWindow;
                _windowTypesByKey[BuildWindowId(constructedWindow.Type)] = constructedWindow.Type;
            }

            Console.WriteLine($"Loaded {_windowTemplates.Count} GUI window templates.");
        }

        /// <summary>
        /// After the window definitions have been cached, we need to associate refresh events to each window type.
        /// This will let us individually refresh specific player view models when needed by external events.
        /// </summary>
        private void LoadRefreshableViewModels()
        {
            foreach (var (windowType, window) in _windowTemplates)
            {
                var tempWindow = window.CreatePlayerWindowAction();
                var vm = tempWindow.ViewModel;
                var vmType = vm.GetType();

                var refreshables = vmType
                    .GetInterfaces()
                    .Where(x => x.IsGenericType &&
                                x.GetGenericTypeDefinition() == typeof(IGuiRefreshable<>));

                foreach (var refreshable in refreshables)
                {
                    var eventType = refreshable.GenericTypeArguments[0];

                    if (!_windowTypesByRefreshEvent.ContainsKey(eventType))
                        _windowTypesByRefreshEvent[eventType] = new List<GuiWindowType>();

                    _windowTypesByRefreshEvent[eventType].Add(windowType);
                }
            }
        }

        /// <summary>
        /// When a player enters the server, create instances of every window if they have not already been created this session.
        /// </summary>
        [ScriptHandler<OnPlayerCacheData>]
        public void CreatePlayerWindows()
        {
            var player = GetEnteringObject();
            var playerId = GetObjectUUID(player);

            if (_playerWindows.ContainsKey(playerId))
                return;

            var dbPlayer = _db.Get<PlayerWindowGeometry>(playerId) ?? new PlayerWindowGeometry(playerId);
            _playerWindows[playerId] = new Dictionary<GuiWindowType, GuiPlayerWindow>();

            foreach (var (type, window) in _windowTemplates)
            {
                var defaultGeometry = window.InitialGeometry;
                var playerGeometry = dbPlayer.WindowGeometries.ContainsKey(type)
                    ? dbPlayer.WindowGeometries[type]
                    : defaultGeometry;
                var resizable = JsonObjectGet(window.Window, "resizable");

                // If the window cannot be resized and there isn't a bind on it, 
                // the default width and height are used.
                var forceResize = JsonGetInt(resizable) != 1 &&
                                  string.IsNullOrWhiteSpace(JsonGetString(JsonObjectGet(resizable, "bind")));
                if (forceResize)
                {
                    playerGeometry.Width = defaultGeometry.Width;
                    playerGeometry.Height = defaultGeometry.Height;
                }

                // Add the window
                var playerWindow = window.CreatePlayerWindowAction();
                playerWindow.ViewModel.Geometry = playerGeometry;
                _playerWindows[playerId][type] = playerWindow;
            }
        }

        /// <summary>
        /// When a player exits the server, save the geometry positions of any open windows.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void SavePlayerWindowGeometry()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<PlayerWindowGeometry>(playerId) ?? new PlayerWindowGeometry(playerId);

            foreach (var (type, _) in _windowTemplates)
            {
                if (IsWindowOpen(player, type))
                {
                    var window = GetPlayerWindow(player, type);
                    dbPlayer.WindowGeometries[type] = window.ViewModel.Geometry;
                }
            }

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Registers an element's event information.
        /// </summary>
        /// <param name="elementId">The Id of the element to register.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventAction">The action to run when the event is raised.</param>
        public void RegisterElementEvent(string elementId, string eventName, GuiMethodDetail eventAction)
        {
            if (!_elementEvents.ContainsKey(elementId))
                _elementEvents[elementId] = new Dictionary<string, GuiMethodDetail>();

            _elementEvents[elementId][eventName] = eventAction;
        }

        private void SaveWindowGeometry(string playerId, GuiWindowType windowType, GuiRectangle geometry)
        {
            var dbPlayer = _db.Get<PlayerWindowGeometry>(playerId) ?? new PlayerWindowGeometry(playerId);

            dbPlayer.WindowGeometries[windowType] = geometry;

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// When a NUI event is fired, look for an associated event on the specified element
        /// and execute the cached action.
        /// </summary>
        [ScriptHandler<OnModuleNuiEvent>]
        public void HandleNuiEvents()
        {
            var player = NuiGetEventPlayer();
            var uiTarget = player;

            if (GetIsDMPossessed(player))
                player = GetMaster(player);

            var playerId = GetObjectUUID(player);
            var windowToken = NuiGetEventWindow();
            var windowId = NuiGetWindowId(uiTarget, windowToken);
            var eventType = NuiGetEventType();
            var elementId = NuiGetEventElement();
            var eventKey = BuildEventKey(windowId, elementId);
            if (string.IsNullOrWhiteSpace(windowId))
                return;

            var windowType = _windowTypesByKey[windowId];
            var playerWindow = _playerWindows[playerId][windowType];
            var viewModel = playerWindow.ViewModel;

            if (!_elementEvents.ContainsKey(eventKey))
            {
                if (eventType == "close")
                {
                    SaveWindowGeometry(playerId, windowType, viewModel.Geometry);
                }
                return;
            }

            var eventGroup = _elementEvents[eventKey];

            if (!eventGroup.ContainsKey(eventType))
                return;

            // Player moved more than 5 meters away from the tether.
            // Automatically close the window.
            if (GetIsObjectValid(viewModel.TetherObject))
            {
                if (GetDistanceBetween(uiTarget, viewModel.TetherObject) > 5f)
                {
                    TogglePlayerWindow(uiTarget, windowType);
                    SendMessageToPC(uiTarget, ColorToken.Red($"You have moved too far away from the {GetName(viewModel.TetherObject)}."));
                    return;
                }
            }

            // Note: This section has the possibility of being slow.
            // If it is, look into building the methods and caching them at the time of window creation.
            var methodInfo = eventGroup[eventType];
            var method = viewModel.GetType().GetMethod(methodInfo.Method.Name);
            var args = methodInfo.Arguments.Select(s => s.Value);
            var action = method?.Invoke(playerWindow.ViewModel, args.ToArray());
            ((Action)action)?.Invoke();

            // If the window was closed, save its geometry 
            if (eventType == "close")
            {
                SaveWindowGeometry(playerId, windowType, viewModel.Geometry);
            }
        }

        /// <summary>
        /// When a NUI event is fired, if it was a watch event, update the associated player's view model.
        /// </summary>
        [ScriptHandler<OnModuleNuiEvent>]
        public void HandleNuiWatchEvent()
        {
            var player = NuiGetEventPlayer();
            var playerId = GetObjectUUID(player);
            var windowToken = NuiGetEventWindow();
            var windowId = NuiGetWindowId(player, windowToken);
            var eventType = NuiGetEventType();
            var propertyName = NuiGetEventElement();

            if (eventType != "watch")
                return;

            if (string.IsNullOrWhiteSpace(windowId))
                return;

            if (!_playerWindows.ContainsKey(playerId))
                return;

            var playerWindows = _playerWindows[playerId];
            var windowType = _windowTypesByKey[windowId];
            var playerWindow = playerWindows[windowType];

            playerWindow.ViewModel.UpdatePropertyFromClient(propertyName);
        }

        /// <summary>
        /// Builds a window Id based on the type of window provided.
        /// </summary>
        /// <param name="windowType">The type of window.</param>
        /// <returns>A key using the window type.</returns>
        public string BuildWindowId(GuiWindowType windowType)
        {
            return $"GUI_WINDOW_{windowType}";
        }

        /// <summary>
        /// Builds a key based on the window Id and element Id.
        /// This is used for event mapping.
        /// </summary>
        /// <param name="windowId">The Id of the Window</param>
        /// <param name="elementId">The Id of the Element</param>
        /// <returns>A key using the window Id and element Id.</returns>
        public string BuildEventKey(string windowId, string elementId)
        {
            return windowId + "_" + elementId;
        }

        /// <summary>
        /// Determines whether a player currently has a window of the specified type open on screen.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of window to look for</param>
        /// <returns>true if the window is open, false otherwise</returns>
        public bool IsWindowOpen(uint player, GuiWindowType type)
        {
            var windowId = BuildWindowId(type);
            return NuiFindWindow(player, windowId) != 0;
        }

        /// <summary>
        /// Shows or hides a specific window for a given player.
        /// </summary>
        /// <param name="player">The player to toggle the window for.</param>
        /// <param name="type">The type of window to toggle.</param>
        /// <param name="payload">An optional payload to pass to the view model.</param>
        /// <param name="tetherObject">The object the window is tethered to. If specified, the window will automatically close if the player moves more than 5 meters away from it.</param>
        /// <param name="uiTarget">Useful for DM possessions. Can override the target of the UI to target the proper NPC a DM has possessed.</param>
        public void TogglePlayerWindow(
            uint player,
            GuiWindowType type,
            IGuiPayload payload = null,
            uint tetherObject = OBJECT_INVALID,
            uint uiTarget = OBJECT_INVALID)
        {
            if (!GetIsPC(player) && uiTarget == OBJECT_INVALID)
                return;

            if (uiTarget == OBJECT_INVALID)
                uiTarget = player;

            var playerId = GetObjectUUID(player);
            var template = _windowTemplates[type];
            var playerWindow = _playerWindows[playerId][type];
            var windowId = BuildWindowId(type);

            // If the window is closed, open it.
            if (NuiFindWindow(player, windowId) == 0)
            {
                //Console.WriteLine(JsonDump(template.Window));

                playerWindow.WindowToken = NuiCreate(uiTarget, template.Window, template.WindowId);
                playerWindow.ViewModel.Bind(uiTarget, playerWindow.WindowToken, template.InitialGeometry, type, payload, tetherObject);
            }
            // Otherwise the window must already be open. Close it.
            else
            {
                SaveWindowGeometry(playerId, type, playerWindow.ViewModel.Geometry);
                NuiDestroy(player, playerWindow.WindowToken);
                
                // Call OnWindowClosed to ensure proper cleanup (like returning items to player)
                playerWindow.ViewModel.OnWindowClosed()?.Invoke();
            }
        }

        /// <summary>
        /// Publishes a refresh event. All subscribed view models will be refreshed for this particular player.
        /// This can be useful for pushing updates to a window based on external circumstances.
        /// For example: Refreshing the XP bar when a player gains XP.
        /// If the player does not have the window open on-screen, nothing will happen.
        /// </summary>
        /// <param name="player">The player to refresh.</param>
        /// <param name="payload">The refresh payload.</param>
        public void PublishRefreshEvent<T>(uint player, T payload)
            where T : IGuiRefreshEvent
        {
            if (!GetIsPC(player))
                return;

            foreach (var windowType in _windowTypesByRefreshEvent[typeof(T)])
            {
                var playerId = GetObjectUUID(player);
                var playerWindow = _playerWindows[playerId][windowType];
                var windowId = BuildWindowId(windowType);

                if(NuiFindWindow(player, windowId) != 0)
                    ((IGuiRefreshable<T>)playerWindow.ViewModel).Refresh(payload);
            }
        }

        /// <summary>
        /// Retrieves the window instance of a player's window.
        /// </summary>
        /// <param name="player">The player to retrieve for.</param>
        /// <param name="type">The type of window to retrieve.</param>
        /// <returns>A player's window instance of the specified type.</returns>
        public GuiPlayerWindow GetPlayerWindow(uint player, GuiWindowType type)
        {
            var playerId = GetObjectUUID(player);
            return _playerWindows[playerId][type];
        }

        /// <summary>
        /// Retrieves a window's template by the given type.
        /// </summary>
        /// <param name="type">The type of window to search for.</param>
        /// <returns>A window template.</returns>
        public GuiConstructedWindow GetWindowTemplate(GuiWindowType type)
        {
            return _windowTemplates[type];
        }

        /// <summary>
        /// When a player enters an area, close all NUI windows.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void CloseAllWindows()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            foreach (var (type, _) in _windowTemplates)
            {
                if (IsWindowOpen(player, type))
                    TogglePlayerWindow(player, type);
            }
        }

        /// <summary>
        /// Force closes a specified window, if open on the player's screen.
        /// This does NOT save the geometry of the window. Typically used for DM-specific functionality.
        /// </summary>
        /// <param name="player">The player whose window will close</param>
        /// <param name="type">The type of window</param>
        /// <param name="uiTarget">The UI target</param>
        public void CloseWindow(uint player, GuiWindowType type, uint uiTarget)
        {
            if (uiTarget == OBJECT_INVALID)
                uiTarget = player;

            if (IsWindowOpen(player, type))
            {
                var playerId = GetObjectUUID(uiTarget);
                var playerWindow = _playerWindows[playerId][type];

                NuiDestroy(player, playerWindow.WindowToken);
            }
        }

        private int ReservedIdCount;

        private readonly Dictionary<string, IdReservation> _systemReservedIdCount = new();

        /// <summary>
        /// Reserves the specified number of Ids for a given system.
        /// </summary>
        /// <param name="systemName">The name of the system. This should be unique across Id sets.</param>
        /// <param name="amount">The number of Ids to reserve.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public IdReservation ReserveIds(string systemName, int amount)
        {
            // Ids haven't been reserved yet. Do that now.
            if (!_systemReservedIdCount.ContainsKey(systemName))
            {
                _systemReservedIdCount[systemName] = new IdReservation();
                _systemReservedIdCount[systemName].Count = amount;
                _systemReservedIdCount[systemName].StartId = ReservedIdCount;
                _systemReservedIdCount[systemName].EndId = ReservedIdCount + amount - 1;

                ReservedIdCount += amount;
            }

            return _systemReservedIdCount[systemName];
        }

        /// <summary>
        /// Retrieves a system's Id reservation.
        /// Throws an exception if the system has not yet been registered.
        /// </summary>
        /// <param name="systemName">The system to retrieve by.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public IdReservation GetSystemReservation(string systemName)
        {
            return _systemReservedIdCount[systemName];
        }

        public void DrawWindow(uint player, int startId, ScreenAnchor anchor, int x, int y, int width, int height, float lifeTime = 10.0f)
        {
            var top = GuiTextTexture.WindowTopLeft;
            var middle = GuiTextTexture.WindowMiddleLeft;
            var bottom = GuiTextTexture.WindowBottomLeft;

            for (var i = 0; i < width; i++)
            {
                top += GuiTextTexture.WindowTopMiddle;
                middle += GuiTextTexture.WindowMiddleBlank;
                bottom += GuiTextTexture.WindowBottomMiddle;
            }

            top += GuiTextTexture.WindowTopRight;
            middle += GuiTextTexture.WindowMiddleRight;
            bottom += GuiTextTexture.WindowBottomRight;

            if (anchor == ScreenAnchor.BottomRight)
            {
                Draw(player, bottom, x, y, anchor, startId++, lifeTime);
            }
            else
            {
                Draw(player, top, x, y, anchor, startId++, lifeTime);
            }


            for (var i = 0; i < height; i++)
            {
                Draw(player, middle, x, ++y, anchor, startId++, lifeTime);
            }

            if (anchor == ScreenAnchor.BottomRight)
            {
                Draw(player, top, x, ++y, anchor, startId, lifeTime);
            }
            else
            {
                Draw(player, bottom, x, ++y, anchor, startId, lifeTime);
            }

        }

        private void Draw(uint player, string message, int x, int y, ScreenAnchor anchor, int id, float lifeTime = 10.0f)
        {
            PostString(player, message, x, y, anchor, lifeTime, GuiStandardColor.ColorWhite, GuiStandardColor.ColorWhite, id, GuiTextTexture.GuiFontName);
        }

        /// <summary>
        /// Gets the modified X coordinate of where to place a string within the center of a window.
        /// </summary>
        /// <param name="text">The text to place in the center.</param>
        /// <param name="windowX">The X position of the window.</param>
        /// <param name="windowWidth">The width of the window.</param>
        /// <returns>The X coordinate to place a string so that it will be in the center of the window.</returns>
        public int CenterStringInWindow(string text, int windowX, int windowWidth)
        {
            return (windowX + (windowWidth / 2)) - ((text.Length + 2) / 2);
        }

        /// <summary>
        /// When the player enters the server, disable default game windows.
        /// In most cases, these windows are replaced with custom versions.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void DisableWindows()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            // Spell Book - Completely unused
            SetGuiPanelDisabled(player, GuiPanel.SpellBook, true);

            // Character Sheet - A NUI replacement is used
            SetGuiPanelDisabled(player, GuiPanel.CharacterSheet, true);

            // Journal - A NUI replacement is used
            SetGuiPanelDisabled(player, GuiPanel.Journal, true);

            // Compass - Space is used by HP/FP/STM bars.
            SetGuiPanelDisabled(player, GuiPanel.Compass, true);
        }
    }
}
