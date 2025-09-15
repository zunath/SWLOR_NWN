using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

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
                foreach (var (eventName, methodDetail) in element.Events)
                {
                    // NWN only fires events for elements with Ids.
                    // Skip any that don't have an Id
                    if (!string.IsNullOrWhiteSpace(element.Id))
                    {
                        var eventKey = Gui.BuildEventKey(windowId, element.Id);
                        Gui.RegisterElementEvent(eventKey, eventName, methodDetail);
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
            RegisterElementEvents(_activeWindow.PartialViews.Values.ToList(), windowId);
            RegisterElementEvents(_activeWindow.Elements, windowId);
        }

        /// <summary>
        /// Builds the window and registers all associated events.
        /// </summary>
        /// <returns>A constructed window.</returns>
        public GuiConstructedWindow Build()
        {
            _activeWindow
                .DefinePartialView("%%WINDOW_MAIN%%", group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.Elements.AddRange(_activeWindow.Elements.ToList());
                        });
                    });
                })
                .DefinePartialView("%%WINDOW_MODAL%%", group =>
                {
                    group.AddColumn(mainCol =>
                    {
                        mainCol.AddRow(mainRow =>
                        {
                            mainRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddText()
                                        .BindText(model => model.ModalPromptText)
                                        .SetScrollbars(NuiScrollbars.Auto)
                                        .SetShowBorder(false);
                                });


                                col.AddRow(row =>
                                {
                                    row.AddSpacer();
                                    row.AddButton()
                                        .BindText(model => model.ModalConfirmButtonText)
                                        .BindOnClicked(model => model.OnModalConfirmClick())
                                        .SetHeight(35f);

                                    row.AddButton()
                                        .BindText(model => model.ModalCancelButtonText)
                                        .BindOnClicked(model => model.OnModalCancelClick())
                                        .SetHeight(35f);
                                    row.AddSpacer();
                                });
                            });
                        });
                    });
                });

            _activeWindow.Elements.Clear();

            _activeWindow
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddPartialView("%%WINDOW_MAIN_PARTIAL%%");
                    });
                });

            var partialViews = new Dictionary<string, Json>();
            foreach (var (key, partial) in _activeWindow.PartialViews)
            {
                partialViews[key] = partial.ToJson();
            }

            var json = _activeWindow.Build();
            var windowId = Gui.BuildWindowId(_type);
            RegisterAllElementEvents();

            var constructedWindow = new GuiConstructedWindow(
                _type,
                windowId,
                json,
                _activeWindow.Geometry,
                partialViews,
                () =>
            {
                var dataModelInstance = Activator.CreateInstance<T>();
                return new GuiPlayerWindow(dataModelInstance);
            });


            return constructedWindow;
        }
    }
}
