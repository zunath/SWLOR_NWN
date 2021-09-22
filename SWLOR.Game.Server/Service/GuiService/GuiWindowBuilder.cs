using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiWindowBuilder<T>
        where T : IGuiViewModel
    {
        private GuiWindowType _type;
        private GuiWindow<T> _activeWindow;

        public GuiWindow<T> CreateWindow(GuiWindowType type)
        {
            _activeWindow = new GuiWindow<T>();
            _type = type;

            return _activeWindow;
        }

        public void RegisterAllElementEvents()
        {
            var windowId = Gui.BuildWindowId(_type);
            var windowEventKey = Gui.BuildEventKey(windowId, "_window_");

            // Register window events.
            if(_activeWindow.OpenedEventMethodInfo != null)
                Gui.RegisterElementEvent(windowEventKey, "open", _activeWindow.OpenedEventMethodInfo);

            if(_activeWindow.ClosedEventMethodInfo != null)
                Gui.RegisterElementEvent(windowEventKey, "close", _activeWindow.ClosedEventMethodInfo);
            
            // Iterate over every column, every row, and every element to retrieve
            // registered events.
            foreach (var column in _activeWindow.Elements)
            {
                foreach (var row in column.Elements)
                {
                    foreach (var element in row.Elements)
                    {
                        // NWN only fires events for elements with Ids.
                        // Skip any that don't have an Id
                        if (string.IsNullOrWhiteSpace(element.Id))
                            continue;

                        var eventKey = Gui.BuildEventKey(windowId, element.Id);

                        foreach (var (eventName, eventAction) in element.Events)
                        {
                            Gui.RegisterElementEvent(eventKey, eventName, eventAction);
                        }
                    }
                }
            }
        }

        public GuiConstructedWindow Build()
        {
            var json = _activeWindow.Build();
            var windowId = Gui.BuildWindowId(_type);
            RegisterAllElementEvents();

            var constructedWindow = new GuiConstructedWindow(
                _type,
                windowId,
                json,
                _activeWindow.Geometry,
                () =>
            {
                var dataModelInstance = Activator.CreateInstance<T>();
                return new GuiPlayerWindow(dataModelInstance);
            });


            return constructedWindow;
        }
    }
}
