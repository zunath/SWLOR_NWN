using System;
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

        public GuiConstructedWindow Build()
        {
            var json = _activeWindow.Build();
            var windowId = $"GUI_WINDOW_{_type}";

            return new GuiConstructedWindow(_type, windowId, json, () =>
            {
                var dataModelInstance = Activator.CreateInstance<T>();
                return new GuiPlayerWindow<T>(dataModelInstance);
            });
        }
    }
}
