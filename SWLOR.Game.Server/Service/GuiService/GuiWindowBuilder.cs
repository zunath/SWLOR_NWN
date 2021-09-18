using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiWindowBuilder
    {
        private readonly Dictionary<GuiWindowType, GuiWindow> _windows = new Dictionary<GuiWindowType, GuiWindow>();
        private GuiWindow _activeWindow;

        public GuiWindow CreateWindow(GuiWindowType type)
        {
            _activeWindow = new GuiWindow();
            _windows[type] = _activeWindow;

            return _activeWindow;
        }

        public Dictionary<GuiWindowType, GuiWindow> Build()
        {

            return _windows;
        }
    }
}
