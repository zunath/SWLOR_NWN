using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Gui
    {
        private static readonly Dictionary<GuiWindowType, GuiConstructedWindow> _windowTemplates = new();
        private static readonly Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> _playerWindows = new();
        private static readonly Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> _playerModals = new();
        private static readonly Dictionary<string, Dictionary<string, MethodInfo>> _elementEvents = new();
        private static readonly Dictionary<string, GuiWindowType> _windowTypesByKey = new();

        /// <summary>
        /// When the module loads, cache all of the GUI windows for later retrieval.
        /// </summary>
        [NWNEventHandler("swlor_skl_cache")]
        public static void CacheData()
        {
            LoadWindowTemplates();
        }
        
        /// <summary>
        /// Loads all of the window definitions, constructs them, and caches the data into memory.
        /// </summary>
        private static void LoadWindowTemplates()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IGuiWindowDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IGuiWindowDefinition)Activator.CreateInstance(type);
                var constructedWindow = instance.BuildWindow();

                // Safety check to ensure we don't try to build the same type of window more than once.
                if (_windowTemplates.ContainsKey(constructedWindow.Type))
                {
                    throw new Exception($"GUI Window type '{constructedWindow.Type}' has been defined more than once.");
                }

                // Register the window template into the cache.
                _windowTemplates[constructedWindow.Type] = constructedWindow;
                _windowTypesByKey[BuildWindowId(constructedWindow.Type)] = constructedWindow.Type;
                _windowTypesByKey[BuildWindowId(constructedWindow.Type) + "_MODAL"] = constructedWindow.Type;
            }

            Console.WriteLine($"Loaded {_windowTemplates.Count} GUI window templates.");
        }

        /// <summary>
        /// When a player enters the server, create instances of every window if they have not already been created this session.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void CreatePlayerWindows()
        {
            var player = GetEnteringObject();
            var playerId = GetObjectUUID(player);

            if (_playerWindows.ContainsKey(playerId))
                return;
            
            _playerWindows[playerId] = new Dictionary<GuiWindowType, GuiPlayerWindow>();
            _playerModals[playerId] = new Dictionary<GuiWindowType, GuiPlayerWindow>();

            foreach (var (type, window) in _windowTemplates)
            {
                // Add the window
                var playerWindow = window.CreatePlayerWindowAction();
                playerWindow.ViewModel.Geometry = window.InitialGeometry;
                _playerWindows[playerId][type] = playerWindow;

                // All windows also get a separate modal window added to the cache.
                var modalWindow = _windowTemplates[GuiWindowType.Modal].CreatePlayerWindowAction();
                modalWindow.ViewModel.Geometry = window.InitialGeometry;
                _playerModals[playerId][type] = modalWindow;
            }
        }

        /// <summary>
        /// Registers an element's event information.
        /// </summary>
        /// <param name="elementId">The Id of the element to register.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventAction">The action to run when the event is raised.</param>
        public static void RegisterElementEvent(string elementId, string eventName, MethodInfo eventAction)
        {
            if (!_elementEvents.ContainsKey(elementId))
                _elementEvents[elementId] = new Dictionary<string, MethodInfo>();

            _elementEvents[elementId][eventName] = eventAction;
        }

        /// <summary>
        /// When a NUI event is fired, look for an associated event on the specified element
        /// and execute the cached action.
        /// </summary>
        [NWNEventHandler("mod_nui_event")]
        public static void HandleNuiEvents()
        {
            var player = NuiGetEventPlayer();
            var playerId = GetObjectUUID(player);
            var windowToken = NuiGetEventWindow();
            var windowId = NuiGetWindowId(player, windowToken);
            var parentWindowType = GuiWindowType.Invalid;

            var isModal = windowId.EndsWith("_MODAL");
            if (isModal)
            {
                var parentWindowId = windowId;
                parentWindowType = _windowTypesByKey[parentWindowId];
                windowId = BuildWindowId(GuiWindowType.Modal);
            }

            var eventType = NuiGetEventType();
            var elementId = NuiGetEventElement();
            var eventKey = BuildEventKey(windowId, elementId);

            if (!_elementEvents.ContainsKey(eventKey))
                return;

            var eventGroup = _elementEvents[eventKey];

            if (!eventGroup.ContainsKey(eventType))
                return;

            var windowType = _windowTypesByKey[windowId];
            var playerWindow = isModal 
                ? _playerModals[playerId][parentWindowType] 
                : _playerWindows[playerId][windowType];
            var viewModel = playerWindow.ViewModel;

            // Player moved more than 5 meters away from the tether.
            // Automatically close the window.
            if (GetIsObjectValid(viewModel.TetherObject))
            {
                if (GetDistanceBetween(player, viewModel.TetherObject) > 5f)
                {
                    TogglePlayerWindow(player, windowType);
                    SendMessageToPC(player, ColorToken.Red($"You have moved too far away from the {GetName(viewModel.TetherObject)}."));
                    return;
                }
            }

            // Note: This section has the possibility of being slow.
            // If it is, look into building the methods and caching them at the time of window creation.
            var methodInfo = eventGroup[eventType];
            var method = viewModel.GetType().GetMethod(methodInfo.Name);
            var action = method?.Invoke(playerWindow.ViewModel, null);
            ((Action)action)?.Invoke();
        }

        /// <summary>
        /// When a NUI event is fired, if it was a watch event, update the associated player's view model.
        /// </summary>
        [NWNEventHandler("mod_nui_event")]
        public static void HandleNuiWatchEvent()
        {
            var player = NuiGetEventPlayer();
            var playerId = GetObjectUUID(player);
            var windowToken = NuiGetEventWindow();
            var windowId = NuiGetWindowId(player, windowToken);
            var eventType = NuiGetEventType();
            var propertyName = NuiGetEventElement();

            if (eventType != "watch")
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
        public static string BuildWindowId(GuiWindowType windowType)
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
        public static string BuildEventKey(string windowId, string elementId)
        {
            return windowId + "_" + elementId;
        }

        /// <summary>
        /// Determines whether a player currently has a window of the specified type open on screen.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of window to look for</param>
        /// <returns>true if the window is open, false otherwise</returns>
        public static bool IsWindowOpen(uint player, GuiWindowType type)
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
        public static void TogglePlayerWindow(
            uint player, 
            GuiWindowType type, 
            GuiPayloadBase payload = null, 
            uint tetherObject = OBJECT_INVALID)
        {
            var playerId = GetObjectUUID(player);
            var template = _windowTemplates[type];
            var playerWindow = _playerWindows[playerId][type];
            var windowId = BuildWindowId(type);

            // If the window is closed, open it.
            if (NuiFindWindow(player, windowId) == 0)
            {
                //Console.WriteLine(JsonDump(template.Window));

                playerWindow.WindowToken = NuiCreate(player, template.Window, template.WindowId);
                playerWindow.ViewModel.Bind(player, playerWindow.WindowToken, template.InitialGeometry, type, payload, tetherObject);
            }
            // Otherwise the window must already be open. Close it.
            else
            {
                NuiDestroy(player, playerWindow.WindowToken);

                // Also destroy the modal, if it's open.
                var modalWindowId = windowId + "_MODAL";
                if (NuiFindWindow(player, modalWindowId) != 0)
                {
                    NuiDestroy(player, _playerModals[playerId][type].WindowToken);
                }
            }
        }

        /// <summary>
        /// Shows the modal associated with the specified parent window type for the player.
        /// If the modal is already open, nothing will happen.
        /// </summary>
        /// <param name="player">The player who will be shown the modal.</param>
        /// <param name="parentType">The parent window type.</param>
        public static void ShowModal(uint player, GuiWindowType parentType)
        {
            var modalWindowId = BuildWindowId(parentType) + "_MODAL";

            // If the modal is already open, don't do anything else.
            if (NuiFindWindow(player, modalWindowId) != 0)
                return;

            var playerId = GetObjectUUID(player);
            var playerModal = _playerModals[playerId][parentType];
            var template = _windowTemplates[GuiWindowType.Modal];
            var parentWindow = _playerWindows[playerId][parentType];
            playerModal.WindowToken = NuiCreate(player, template.Window, modalWindowId);
            playerModal.ViewModel.Bind(player, playerModal.WindowToken, parentWindow.ViewModel.Geometry, parentType, null, parentWindow.ViewModel.TetherObject);
        }

        /// <summary>
        /// Closes the modal associated with the specified parent window type for the player.
        /// If the modal isn't open, nothing will happen.
        /// </summary>
        /// <param name="player">The player to close the modal for.</param>
        /// <param name="parentType">The parent window type.</param>
        public static void CloseModal(uint player, GuiWindowType parentType)
        {
            var modalWindowId = BuildWindowId(parentType) + "_MODAL";

            // If the modal is not open, don't do anything else.
            if (NuiFindWindow(player, modalWindowId) == 0)
                return;

            var playerId = GetObjectUUID(player);
            NuiDestroy(player, _playerModals[playerId][parentType].WindowToken);
        }

        /// <summary>
        /// Skips the character sheet panel open event and shows the SWLOR character sheet instead.
        /// </summary>
        [NWNEventHandler("mod_gui_event")]
        public static void CharacterSheetGui()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            if (type != GuiEventType.DisabledPanelAttemptOpen) return;

            var panelType = (GuiPanel)GetLastGuiEventInteger();
            if (panelType != GuiPanel.CharacterSheet)
                return;

            TogglePlayerWindow(player, GuiWindowType.CharacterSheet);
        }

        /// <summary>
        /// Retrieves the modal instance of a player's window.
        /// </summary>
        /// <param name="player">The player to retrieve for.</param>
        /// <param name="type">The type of window to retrieve.</param>
        /// <returns>A player's window instance of the specified type.</returns>
        public static GuiPlayerWindow GetPlayerModal(uint player, GuiWindowType type)
        {
            var playerId = GetObjectUUID(player);
            return _playerModals[playerId][type];
        }

        /// <summary>
        /// Retrieves the window instance of a player's window.
        /// </summary>
        /// <param name="player">The player to retrieve for.</param>
        /// <param name="type">The type of window to retrieve.</param>
        /// <returns>A player's window instance of the specified type.</returns>
        public static GuiPlayerWindow GetPlayerWindow(uint player, GuiWindowType type)
        {
            var playerId = GetObjectUUID(player);
            return _playerWindows[playerId][type];
        }

        public class IdReservation
        {
            public int Count { get; set; }
            public int StartId { get; set; }
            public int EndId { get; set; }
        }

        /// <summary>
        /// Name of the texture used for the GUI elements.
        /// </summary>
        public const string GuiFontName = "fnt_es_gui";

        /// <summary>
        /// Name of the texture used for the GUI text.
        /// </summary>
        public const string TextName = "fnt_es_text";

        // The following letters represent the character locations of the elements on the GUI spritesheet.
        public const string WindowTopLeft = "a";
        public const string WindowTopMiddle = "b";
        public const string WindowTopRight = "c";
        public const string WindowMiddleLeft = "d";
        public const string WindowMiddleRight = "f";
        public const string WindowMiddleBlank = "i";
        public const string WindowBottomLeft = "h";
        public const string WindowBottomRight = "g";
        public const string WindowBottomMiddle = "e";
        public const string Arrow = "j";
        public const string BlankWhite = "k";

        // The following hex codes correspond to colors used on GUI elements.
        // Color tokens won't work on Gui elements.
        public static int ColorTransparent = Convert.ToInt32("0xFFFFFF00", 16);
        public static int ColorWhite = Convert.ToInt32("0xFFFFFFFF", 16);
        public static int ColorSilver = Convert.ToInt32("0xC0C0C0FF", 16);
        public static int ColorGray = Convert.ToInt32("0x808080FF", 16);
        public static int ColorDarkGray = Convert.ToInt32("0x303030FF", 16);
        public static int ColorBlack = Convert.ToInt32("0x000000FF", 16);
        public static int ColorRed = Convert.ToInt32("0xFF0000FF", 16);
        public static int ColorMaroon = Convert.ToInt32("0x800000FF", 16);
        public static int ColorOrange = Convert.ToInt32("0xFFA500FF", 16);
        public static int ColorYellow = Convert.ToInt32("0xFFFF00FF", 16);
        public static int ColorOlive = Convert.ToInt32("0x808000FF", 16);
        public static int ColorLime = Convert.ToInt32("0x00FF00FF", 16);
        public static int ColorGreen = Convert.ToInt32("0x008000FF", 16);
        public static int ColorAqua = Convert.ToInt32("0x00FFFFFF", 16);
        public static int ColorTeal = Convert.ToInt32("0x008080FF", 16);
        public static int ColorBlue = Convert.ToInt32("0x0000FFFF", 16);
        public static int ColorNavy = Convert.ToInt32("0x000080FF", 16);
        public static int ColorFuschia = Convert.ToInt32("0xFF00FFFF", 16);
        public static int ColorPurple = Convert.ToInt32("0x800080FF", 16);

        public static int ColorHealthBar = Convert.ToInt32("0x8B0000FF", 16);
        public static int ColorFPBar = Convert.ToInt32("0x00008BFF", 16);
        public static int ColorStaminaBar = Convert.ToInt32("0x008B00FF", 16);

        public static int ColorShieldsBar = Convert.ToInt32("0x00AAE4FF", 16);
        public static int ColorHullBar = Convert.ToInt32("0x8B0000FF", 16);
        public static int ColorCapacitorBar = Convert.ToInt32("0xFFA500FF", 16);

        private static int ReservedIdCount;

        private static readonly Dictionary<string, IdReservation> _systemReservedIdCount = new Dictionary<string, IdReservation>();

        /// <summary>
        /// Reserves the specified number of Ids for a given system.
        /// </summary>
        /// <param name="systemName">The name of the system. This should be unique across Id sets.</param>
        /// <param name="amount">The number of Ids to reserve.</param>
        /// <returns>An object containing Id reservation details.</returns>
        public static IdReservation ReserveIds(string systemName, int amount)
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
        public static IdReservation GetSystemReservation(string systemName)
        {
            return _systemReservedIdCount[systemName];
        }

        public static void DrawWindow(uint player, int startId, ScreenAnchor anchor, int x, int y, int width, int height, float lifeTime = 10.0f)
        {
            var top = WindowTopLeft;
            var middle = WindowMiddleLeft;
            var bottom = WindowBottomLeft;

            for (var i = 0; i < width; i++)
            {
                top += WindowTopMiddle;
                middle += WindowMiddleBlank;
                bottom += WindowBottomMiddle;
            }

            top += WindowTopRight;
            middle += WindowMiddleRight;
            bottom += WindowBottomRight;

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

        private static void Draw(uint player, string message, int x, int y, ScreenAnchor anchor, int id, float lifeTime = 10.0f)
        {
            PostString(player, message, x, y, anchor, lifeTime, ColorWhite, ColorWhite, id, GuiFontName);
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
            return (windowX + (windowWidth / 2)) - ((text.Length + 2) / 2);
        }

    }
}
