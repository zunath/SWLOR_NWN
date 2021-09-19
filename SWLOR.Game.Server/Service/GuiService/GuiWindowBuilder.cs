using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiWindowBuilder<T>
        where T: IGuiDataModel
    {
        private GuiWindowType _type;
        private GuiWindow<T> _activeWindow;

        public GuiWindow<T> CreateWindow(GuiWindowType type)
        {
            _activeWindow = new GuiWindow<T>();
            _type = type;

            return _activeWindow;
        }

        public (GuiWindowType, GuiWindow<T>) Build()
        {
            return (_type, _activeWindow);
        }
    }
}
