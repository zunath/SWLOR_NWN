using System.Collections.Generic;
using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for NUI (New User Interface)
        private readonly Dictionary<uint, Dictionary<int, NuiWindowData>> _playerWindows = new();
        private readonly Dictionary<uint, Dictionary<string, Json>> _windowBinds = new();
        private readonly Dictionary<uint, Dictionary<string, bool>> _bindWatches = new();
        private readonly Dictionary<uint, Dictionary<int, Json>> _userData = new();
        private uint _eventPlayer = OBJECT_INVALID;
        private int _eventWindow = 0;
        private string _eventType = "";
        private string _eventElement = "";
        private int _eventArrayIndex = 0;
        private Json _eventPayload = new Json(0);
        private int _nextWindowId = 1;

        private class NuiWindowData
        {
            public string WindowId { get; set; } = "";
            public string ResRef { get; set; } = "";
            public Json NuiData { get; set; } = new Json(0);
            public int Token { get; set; } = 0;
        }

        public int NuiCreateFromResRef(uint oPlayer, string sResRef, string sWindowId = "") 
        {
            var token = _nextWindowId++;
            var windowData = new NuiWindowData 
            { 
                WindowId = sWindowId, 
                ResRef = sResRef, 
                Token = token 
            };
            
            if (!_playerWindows.ContainsKey(oPlayer))
                _playerWindows[oPlayer] = new Dictionary<int, NuiWindowData>();
            _playerWindows[oPlayer][token] = windowData;
            
            return token;
        }

        public int NuiCreate(uint oPlayer, Json jNui, string sWindowId = "") 
        {
            var token = _nextWindowId++;
            var windowData = new NuiWindowData 
            { 
                WindowId = sWindowId, 
                NuiData = jNui, 
                Token = token 
            };
            
            if (!_playerWindows.ContainsKey(oPlayer))
                _playerWindows[oPlayer] = new Dictionary<int, NuiWindowData>();
            _playerWindows[oPlayer][token] = windowData;
            
            return token;
        }

        public int NuiFindWindow(uint oPlayer, string sId) 
        {
            if (!_playerWindows.ContainsKey(oPlayer))
                return 0;
            
            foreach (var window in _playerWindows[oPlayer].Values)
            {
                if (window.WindowId == sId)
                    return window.Token;
            }
            return 0;
        }

        public void NuiDestroy(uint oPlayer, int nUiToken) 
        {
            if (_playerWindows.ContainsKey(oPlayer))
                _playerWindows[oPlayer].Remove(nUiToken);
        }

        public uint NuiGetEventPlayer() => _eventPlayer;

        public int NuiGetEventWindow() => _eventWindow;

        public string NuiGetEventType() => _eventType;

        public string NuiGetEventElement() => _eventElement;

        public int NuiGetEventArrayIndex() => _eventArrayIndex;

        public string NuiGetWindowId(uint oPlayer, int nUiToken) 
        {
            return _playerWindows.GetValueOrDefault(oPlayer, new Dictionary<int, NuiWindowData>())
                .GetValueOrDefault(nUiToken, new NuiWindowData()).WindowId;
        }

        public Json NuiGetBind(uint oPlayer, int nUiToken, string sBindName) 
        {
            var playerBinds = _windowBinds.GetValueOrDefault(oPlayer, new Dictionary<string, Json>());
            return playerBinds.GetValueOrDefault(sBindName, new Json(0));
        }

        public void NuiSetBind(uint oPlayer, int nUiToken, string sBindName, Json jValue) 
        {
            if (!_windowBinds.ContainsKey(oPlayer))
                _windowBinds[oPlayer] = new Dictionary<string, Json>();
            _windowBinds[oPlayer][sBindName] = jValue;
        }

        public void NuiSetGroupLayout(uint oPlayer, int nUiToken, string sElement, Json jNui) 
        {
            // Mock implementation - would update the UI layout
        }

        public int NuiSetBindWatch(uint oPlayer, int nUiToken, string sBind, bool bWatch) 
        {
            if (!_bindWatches.ContainsKey(oPlayer))
                _bindWatches[oPlayer] = new Dictionary<string, bool>();
            _bindWatches[oPlayer][sBind] = bWatch;
            return 1; // Success
        }

        public int NuiGetNthWindow(uint oPlayer, int nNth = 0) 
        {
            if (!_playerWindows.ContainsKey(oPlayer))
                return 0;
            
            var windows = _playerWindows[oPlayer].Values.ToList();
            if (nNth >= 0 && nNth < windows.Count)
                return windows[nNth].Token;
            return 0;
        }

        public string NuiGetNthBind(uint oPlayer, int nToken, bool bWatched, int nNth = 0) 
        {
            var binds = _windowBinds.GetValueOrDefault(oPlayer, new Dictionary<string, Json>());
            var watchedBinds = new List<string>();
            
            if (bWatched)
            {
                var watches = _bindWatches.GetValueOrDefault(oPlayer, new Dictionary<string, bool>());
                foreach (var bind in binds.Keys)
                {
                    if (watches.GetValueOrDefault(bind, false))
                        watchedBinds.Add(bind);
                }
            }
            else
            {
                watchedBinds.AddRange(binds.Keys);
            }
            
            if (nNth >= 0 && nNth < watchedBinds.Count)
                return watchedBinds[nNth];
            return "";
        }

        public Json NuiGetEventPayload() => _eventPayload;

        public Json NuiGetUserData(uint oPlayer, int nToken) => 
            _userData.GetValueOrDefault(oPlayer, new Dictionary<int, Json>())
                .GetValueOrDefault(nToken, new Json(0));

        public void NuiSetUserData(uint oPlayer, int nToken, Json jUserData) 
        {
            if (!_userData.ContainsKey(oPlayer))
                _userData[oPlayer] = new Dictionary<int, Json>();
            _userData[oPlayer][nToken] = jUserData;
        }

        // NUI methods
        public void OpenInventory(uint oPlayer, uint oTarget) 
        { 
            // Mock implementation - no-op for testing
        }
        
        public void PopUpDeathGUIPanel(uint oPlayer, bool bUseRespawnButton = true) 
        { 
            // Mock implementation - no-op for testing
        }
        
        public void PopUpGUIPanel(uint oPlayer, int nGUIPanel) 
        { 
            // Mock implementation - no-op for testing
        }

        // Helper methods for testing


    }
}
