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

        /// <summary>
        /// Registers events found on the list of elements provided.
        /// </summary>
        /// <param name="elements">The elements to register.</param>
        /// <param name="windowId">The window to register under.</param>
        private void RegisterElementEvents(List<IGuiWidget> elements, string windowId)
        {
            foreach (var element in elements)
            {
                foreach (var (eventName, eventAction) in element.Events)
                {
                    // NWN only fires events for elements with Ids.
                    // Skip any that don't have an Id
                    if (!string.IsNullOrWhiteSpace(element.Id))
                    {
                        var eventKey = Gui.BuildEventKey(windowId, element.Id);
                        Gui.RegisterElementEvent(eventKey, eventName, eventAction);
                    }
                }

                RegisterElementEvents(element.Elements, windowId);
            }
        }

        /// <summary>
        /// Registers all events on all elements for a given window.
        /// </summary>
        public void RegisterAllElementEvents()
        {
            var windowId = Gui.BuildWindowId(_type);
            var windowEventKey = Gui.BuildEventKey(windowId, "_window_");

            // Register window events.
            if(_activeWindow.OpenedEventMethodInfo != null)
                Gui.RegisterElementEvent(windowEventKey, "open", _activeWindow.OpenedEventMethodInfo);

            if(_activeWindow.ClosedEventMethodInfo != null)
                Gui.RegisterElementEvent(windowEventKey, "close", _activeWindow.ClosedEventMethodInfo);
            
            // Recurse over all elements in the window, looking for and registering any events
            RegisterElementEvents(_activeWindow.Elements, windowId);
        }

        /// <summary>
        /// Builds the window and registers all associated events.
        /// </summary>
        /// <returns>A constructed window.</returns>
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
