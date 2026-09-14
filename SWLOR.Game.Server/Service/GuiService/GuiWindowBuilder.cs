using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
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
            var authoredElements = _activeWindow.Elements.ToList();

            _activeWindow
                .DefineStandardMainPartial(authoredElements)
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
                })
                .DefinePartialView("%%WINDOW_INPUT_MODAL%%", group =>
                {
                    // Like %%WINDOW_MODAL%%, but with a multiline text box the player
                    // can type into. The typed text flows back to the server through
                    // the always-watched ModalInputText bind (see GuiViewModelBase.Bind).
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
                                        .SetScrollbars(NuiScrollbars.None)
                                        .SetShowBorder(false)
                                        .SetHeight(64f);
                                });

                                // Unsized scrollable wrapper so the fixed-height editor
                                // can never make the layout's required height exceed the
                                // window viewport (layout rule R2b).
                                col.AddRow(row =>
                                {
                                    row.AddGroup(editorGroup =>
                                    {
                                        editorGroup.SetShowBorder(false);
                                        editorGroup.SetScrollbars(NuiScrollbars.Auto);
                                        editorGroup.AddColumn(editorCol =>
                                        {
                                            editorCol.AddRow(editorRow =>
                                            {
                                                editorRow.AddTextEdit()
                                                    .SetIsMultiline(true)
                                                    .SetMaxLength(4000)
                                                    .BindValue(model => model.ModalInputText)
                                                    .SetHeight(450f);
                                            });
                                        });
                                    });
                                });

                                col.AddRow(row =>
                                {
                                    row.AddSpacer();
                                    row.AddButton()
                                        .BindText(model => model.ModalConfirmButtonText)
                                        .BindOnClicked(model => model.OnInputModalConfirmClick())
                                        .SetHeight(35f);

                                    row.AddButton()
                                        .BindText(model => model.ModalCancelButtonText)
                                        .BindOnClicked(model => model.OnInputModalCancelClick())
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

            var windowId = Gui.BuildWindowId(_type);

            // Surface layout shapes confirmed to fail NUI's client-side constraint
            // solver, with a widget path - the client error itself carries no context.
            // Every warning is a real defect; see GuiLayoutValidator and Readmes/NuiLayoutRules.md.
            var layoutFindings = GuiLayoutValidator.Validate(windowId, _activeWindow.PartialViews);

            if (GuiLayoutValidator.IsValidationOnlyBuild)
            {
                return new GuiConstructedWindow(
                    _type,
                    windowId,
                    default,
                    _activeWindow.Geometry,
                    new Dictionary<string, Json>(),
                    layoutFindings,
                    () =>
                    {
                        var dataModelInstance = Activator.CreateInstance<T>();
                        return new GuiPlayerWindow(dataModelInstance);
                    });
            }

            foreach (var finding in layoutFindings)
            {
                Log.WriteStructured(LogGroup.Server, "[NUI layout warning] {Finding}", finding);
            }

            var partialViews = new Dictionary<string, Json>();
            foreach (var (key, partial) in _activeWindow.PartialViews)
            {
                partialViews[key] = partial.ToJson();
            }

            var json = _activeWindow.Build();

            // Dump the exact wire JSON for layout debugging (see Readmes/NuiLayoutRules.md,
            // "Diagnosing a client-side layout error"). Lands in the Server log group.
            var environment = ApplicationSettings.Get().ServerEnvironment;
            if (environment == ServerEnvironmentType.Development ||
                environment == ServerEnvironmentType.Test ||
                Environment.GetEnvironmentVariable("SWLOR_NUI_DUMP_JSON") == "1")
            {
                Log.WriteStructured(
                    LogGroup.Server,
                    "[NUI JSON] window={WindowId} root={RootJson}",
                    windowId,
                    JsonDump(json));
                foreach (var (partialName, partialJson) in partialViews)
                {
                    Log.WriteStructured(
                        LogGroup.Server,
                        "[NUI JSON] window={WindowId} partial={PartialName} json={PartialJson}",
                        windowId,
                        partialName,
                        JsonDump(partialJson));
                }
            }

            RegisterAllElementEvents();

            var constructedWindow = new GuiConstructedWindow(
                _type,
                windowId,
                json,
                _activeWindow.Geometry,
                partialViews,
                layoutFindings,
                () =>
            {
                var dataModelInstance = Activator.CreateInstance<T>();
                return new GuiPlayerWindow(dataModelInstance);
            });


            return constructedWindow;
        }
    }
}
