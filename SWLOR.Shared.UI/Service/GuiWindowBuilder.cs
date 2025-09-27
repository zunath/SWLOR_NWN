using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Shared.UI.Service
{
    public class GuiWindowBuilder<T>
        where T : IGuiViewModel
    {
        private readonly IGuiService _guiService;
        private GuiWindowType _type;
        private GuiWindow<T> _activeWindow;

        public GuiWindowBuilder(IGuiService guiService)
        {
            _guiService = guiService;
        }

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
                        var eventKey = _guiService.BuildEventKey(windowId, element.Id);
                        _guiService.RegisterElementEvent(eventKey, eventName, methodDetail);
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
            var windowId = _guiService.BuildWindowId(_type);
            var windowEventKey = _guiService.BuildEventKey(windowId, "_window_");

            // Register window events.
            if(_activeWindow.OpenedEventMethodInfo != null)
                _guiService.RegisterElementEvent(windowEventKey, "open", _activeWindow.OpenedEventMethodInfo);

            if(_activeWindow.ClosedEventMethodInfo != null)
                _guiService.RegisterElementEvent(windowEventKey, "close", _activeWindow.ClosedEventMethodInfo);
            
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
                                        .SetScrollbars(NuiScrollbarType.Auto)
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
            var windowId = _guiService.BuildWindowId(_type);
            RegisterAllElementEvents();

            var constructedWindow = new GuiConstructedWindow(
                _type,
                windowId,
                json,
                _activeWindow.Geometry,
                partialViews,
                () =>
            {
                var dataModelInstance = ServiceContainer.GetService<T>();
                return new GuiPlayerWindow(dataModelInstance);
            });


            return constructedWindow;
        }
    }
}
