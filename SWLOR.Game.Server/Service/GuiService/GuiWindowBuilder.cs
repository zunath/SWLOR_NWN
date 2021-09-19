using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiWindowBuilder<T>
        where T : IGuiDataModel
    {
        private GuiWindowType _type;
        private GuiWindow<T> _activeWindow;

        public GuiWindow<T> CreateWindow(GuiWindowType type)
        {
            _activeWindow = new GuiWindow<T>();
            _type = type;

            return _activeWindow;
        }

        public Dictionary<string, Dictionary<string, GuiEventDelegate>> GetAllElementEvents()
        {
            var windowId = Gui.BuildWindowId(_type);
            var events = new Dictionary<string, Dictionary<string, GuiEventDelegate>>();
            var windowEventKey = Gui.BuildEventKey(windowId, "_window_");

            // Register window events.
            events[windowEventKey] = new Dictionary<string, GuiEventDelegate>();
            if (_activeWindow.ClosedEvent != null)
                events[windowEventKey]["open"] = _activeWindow.OpenedEvent;
            if (_activeWindow.OpenedEvent != null)
                events[windowEventKey]["close"] = _activeWindow.ClosedEvent;

            // Iterate over every column, every row, and every element to retrieve
            // registered events.
            foreach (var column in _activeWindow.Columns)
            {
                foreach (var row in column.Rows)
                {
                    foreach (var element in row.Elements)
                    {
                        // NWN only fires events for elements with Ids.
                        // Skip any that don't have an Id
                        if (string.IsNullOrWhiteSpace(element.Id))
                            continue;

                        var eventKey = Gui.BuildEventKey(windowId, element.Id);

                        if (!events.ContainsKey(eventKey))
                            events[eventKey] = new Dictionary<string, GuiEventDelegate>();

                        foreach (var (eventName, eventAction) in element.Events)
                        {
                            events[eventKey][eventName] = eventAction;
                        }
                    }
                }
            }

            return events;
        }

        public GuiConstructedWindow Build()
        {
            var json = _activeWindow.Build();
            var windowId = Gui.BuildWindowId(_type);
            var events = GetAllElementEvents();

            var constructedWindow = new GuiConstructedWindow(
                _type,
                windowId,
                json,
                events,
                (player) =>
            {
                var dataModelInstance = Activator.CreateInstance<T>();
                return new GuiPlayerWindow(dataModelInstance);
            });


            return constructedWindow;
        }
    }
}
