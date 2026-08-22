using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    // Debug window rendering every NUI control wrapper and the interesting
    // combinations between them, so wrapper or layout-validator regressions are
    // visible in one place. Opened with /nuigallery (admin; everyone on test).
    //
    // The GALLERY_HAZARD_* / GALLERY_PROBE_* partials fall into two categories
    // (all outcomes verified in-game 2026-07):
    // - CONFIRMED failures: H1 + P1-P4/P6 (fixed-height row containing a same-height
    //   margined widget - button, checkbox, textedit, combo, slider, progress - blanks
    //   the window, rule R2c); H6 (watch on a never-Set property - descriptive R3
    //   exception); P13a (partial applied to a nonexistent element id - client-side
    //   error, no server validation exists).
    // - VERIFIED-WORKING exhibits: W1-W4, P5 (options is margin-free), P7-P12, and
    //   P13b (3-deep nesting renders but the innermost content is dropped by parent
    //   re-applies - do not nest partials more than 2 deep).
    // They are only defined off production. Expected boot warnings on dev/test:
    // EXACTLY SIX [NUI layout warning] lines, all from this window - the R2c
    // regression canaries: GALLERY_HAZARD_BUTTON_ROW, GALLERY_PROBE_ROW_CHECKBOX,
    // GALLERY_PROBE_ROW_TEXTEDIT, GALLERY_PROBE_ROW_COMBO, GALLERY_PROBE_ROW_SLIDER,
    // GALLERY_PROBE_ROW_PROGRESS. Any other warning, anywhere, is a real defect.
    public class DebugNuiGalleryDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DebugNuiGalleryViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float ButtonHeight = 32f;
        private const float LabelHeight = 20f;
        private const float ContentPanelWidth = 560f;

        // Three 28px tabbar rows + margins, same proportions as CharacterSheet's
        // two-row tab panel (76f for two rows).
        private const float TabPanelHeight = 112f;

        private static bool HazardsAreAvailable
        {
            get
            {
                var environment = ApplicationSettings.Get().ServerEnvironment;
                return environment == ServerEnvironmentType.Development ||
                       environment == ServerEnvironmentType.Test;
            }
        }

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DebugNuiGallery)
                .SetInitialGeometry(0, 0, 900f, 560f)
                .SetTitle("NUI Control Gallery")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnOpened(model => model.OnWindowOpened())
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(DebugNuiGalleryViewModel.ButtonsTabPartial, AddButtonsTab)
                .DefinePartialView(DebugNuiGalleryViewModel.TextTabPartial, AddTextTab)
                .DefinePartialView(DebugNuiGalleryViewModel.SelectionTabPartial, AddSelectionTab)
                .DefinePartialView(DebugNuiGalleryViewModel.SlidersTabPartial, AddSlidersTab)
                .DefinePartialView(DebugNuiGalleryViewModel.ListsTabPartial, AddListsTab)
                .DefinePartialView(DebugNuiGalleryViewModel.GroupsTabPartial, AddGroupsTab)
                .DefinePartialView(DebugNuiGalleryViewModel.DrawingTabPartial, AddDrawingTab)
                .DefinePartialView(DebugNuiGalleryViewModel.ChartsTabPartial, AddChartsTab)
                .DefinePartialView(DebugNuiGalleryViewModel.BindingsTabPartial, AddBindingsTab)
                .DefinePartialView(DebugNuiGalleryViewModel.ModalsTabPartial, AddModalsTab)
                .DefinePartialView(DebugNuiGalleryViewModel.HazardsTabPartial, AddHazardsTab);

            if (HazardsAreAvailable)
            {
                DefineHazardPartials(window);
            }

            // Root layout uses GuiStandardLayout - the only root shape proven
            // to track window geometry (see Readmes/NuiLayoutRules.md R5).
            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);

                // Tabbar rows are the one legal fixed-height-row pairing: the
                // toggles widget has no default margin (layout rule R2c exception).
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Buttons")
                        .AddOption("Text & Input")
                        .AddOption("Selection")
                        .AddOption("Sliders")
                        .BindSelectedValue(model => model.RowATabValue)
                        .SetWidth(464f)
                        .SetHeight(TabRowHeight);
                });

                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Lists & Tables")
                        .AddOption("Groups & Layout")
                        .AddOption("Images & Drawing")
                        .AddOption("Charts")
                        .BindSelectedValue(model => model.RowBTabValue)
                        .SetWidth(464f)
                        .SetHeight(TabRowHeight);
                });

                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Bindings")
                        .AddOption("Modals & Events")
                        .AddOption("Hazards")
                        .BindSelectedValue(model => model.RowCTabValue)
                        .SetWidth(348f)
                        .SetHeight(TabRowHeight);
                });

                layout.SetContentPartialElement(DebugNuiGalleryViewModel.TabContentElement);

                // The event log lives in the window root (geometry-bound), so
                // it is always layout-safe and survives tab partial swaps.
                // Variable-width rail (no fixedWidth): under in-game verification.
                // If the content region ever freezes at a constant width again,
                // revert to a fixed width (280f) and record that side rails must
                // be fixed-width in NuiLayoutRules R5.
                layout.AddSideColumn(logCol =>
                {
                    logCol.AddRow(headerRow =>
                    {
                        headerRow.AddLabel()
                            .SetText("Event Log")
                            .SetHeight(24f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        headerRow.AddButton()
                            .SetText("Clear")
                            .SetHeight(24f)
                            .SetWidth(60f)
                            .BindOnClicked(model => model.OnClickClearLog());
                    });

                    logCol.AddRow(listRow =>
                    {
                        listRow.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.EventLog)
                                    .BindTooltip(model => model.EventLog)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });
                        })
                            .BindRowCount(model => model.EventLog)
                            .SetRowHeight(20f);
                    });
                });
            });

            return _builder.Build();
        }

        // Standard tab shell: a fixed-width borderless panel, the same shape
        // CharacterSheet's tab partials use. Vertical/horizontal scrolling is
        // provided by the auto-scroll group hosting the partial in the window
        // root, which keeps every tab compressible (layout rule R2b).
        private static void AddTabShell(
            GuiGroup<DebugNuiGalleryViewModel> host,
            Action<GuiColumn<DebugNuiGalleryViewModel>> content)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content);
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddSectionLabel(GuiColumn<DebugNuiGalleryViewModel> col, string text)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText(text)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetColor(GuiColor.Cyan);
            });
        }

        private static void AddButtonsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Plain button + click counter (no row height set - contrast with hazard H1)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Click me")
                        .SetHeight(ButtonHeight)
                        .SetWidth(140f)
                        .BindOnClicked(model => model.OnClickSimpleButton());
                    row.AddLabel()
                        .BindText(model => model.ClickCountText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddSectionLabel(col, "Image button");
                col.AddRow(row =>
                {
                    row.AddButtonImage()
                        .SetImageResref("arrow_up")
                        .SetHeight(ButtonHeight)
                        .SetWidth(ButtonHeight)
                        .SetTooltip("Image button (arrow_up)")
                        .BindOnClicked(model => model.OnClickImageButton());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Bound-text button (its label is rewritten server-side on click)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .BindText(model => model.DynamicButtonText)
                        .SetHeight(ButtonHeight)
                        .SetWidth(300f)
                        .BindOnClicked(model => model.OnClickBoundTextButton());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Toggle button with bound toggled state");
                col.AddRow(row =>
                {
                    row.AddToggleButton()
                        .SetText("Toggle me")
                        .BindIsToggled(model => model.SampleToggle)
                        .SetHeight(ButtonHeight)
                        .SetWidth(140f)
                        .BindOnClicked(model => model.OnClickSampleToggle());
                    row.AddLabel()
                        .BindText(model => model.SampleToggleText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddSectionLabel(col, "Disabled + encouraged + fully-bound specimen (driven from Bindings tab)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Disabled")
                        .SetIsEnabled(false)
                        .SetDisabledTooltip("Disabled tooltip shows on hover")
                        .SetHeight(ButtonHeight)
                        .SetWidth(120f);
                    row.AddButton()
                        .SetText("Encouraged")
                        .SetIsEncouraged(true)
                        .SetHeight(ButtonHeight)
                        .SetWidth(120f)
                        .BindOnClicked(model => model.OnClickImageButton());
                    row.AddButton()
                        .SetText("Specimen")
                        .BindIsEnabled(model => model.IsSampleEnabled)
                        .BindColor(model => model.SampleColor)
                        .BindTooltip(model => model.SampleTooltip)
                        .SetHeight(ButtonHeight)
                        .SetWidth(120f)
                        .BindOnClicked(model => model.OnClickImageButton());
                });

                AddSectionLabel(col, "Label with mouse down/up events");
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("[ Press and release the mouse on this label ]")
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .BindOnMouseDown(model => model.OnMouseDownLabel())
                        .BindOnMouseUp(model => model.OnMouseUpLabel());
                });
            });
        }

        private static void AddTextTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Label alignment grid (horizontal x vertical)");
                col.AddRow(row =>
                {
                    row.AddLabel().SetText("Left/Top").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left).SetVerticalAlign(NuiVerticalAlign.Top);
                    row.AddLabel().SetText("Center/Top").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center).SetVerticalAlign(NuiVerticalAlign.Top);
                    row.AddLabel().SetText("Right/Top").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Right).SetVerticalAlign(NuiVerticalAlign.Top);
                });
                col.AddRow(row =>
                {
                    row.AddLabel().SetText("Left/Middle").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left).SetVerticalAlign(NuiVerticalAlign.Middle);
                    row.AddLabel().SetText("Center/Middle").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center).SetVerticalAlign(NuiVerticalAlign.Middle);
                    row.AddLabel().SetText("Right/Middle").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Right).SetVerticalAlign(NuiVerticalAlign.Middle);
                });
                col.AddRow(row =>
                {
                    row.AddLabel().SetText("Left/Bottom").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left).SetVerticalAlign(NuiVerticalAlign.Bottom);
                    row.AddLabel().SetText("Center/Bottom").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center).SetVerticalAlign(NuiVerticalAlign.Bottom);
                    row.AddLabel().SetText("Right/Bottom").SetHeight(24f).SetWidth(140f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Right).SetVerticalAlign(NuiVerticalAlign.Bottom);
                });

                AddSectionLabel(col, "Text block with border and scrollbars, bound to a long string");
                col.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.LongText)
                        .SetShowBorder(true)
                        .SetScrollbars(NuiScrollbars.Auto)
                        .SetHeight(100f);
                });

                AddSectionLabel(col, "Single-line text edit (watched) and its live server-side mirror");
                col.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetPlaceholder("Type here - each client sync is logged")
                        .BindValue(model => model.TextEditValue)
                        .SetMaxLength(50)
                        .SetHeight(ButtonHeight);
                });
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.MirrorLabelText)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddSectionLabel(col, "Multiline word-wrap text edit at a fixed height");
                col.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetPlaceholder("Multiline")
                        .BindValue(model => model.MultilineValue)
                        .SetMaxLength(500)
                        .SetIsMultiline(true)
                        .SetHasWordWrap(true)
                        .SetHeight(80f);
                });
            });
        }

        private static void AddSelectionTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Watched checkbox with server-side mirror");
                col.AddRow(row =>
                {
                    row.AddCheckBox()
                        .SetText("Watched checkbox")
                        .BindIsChecked(model => model.IsChecked)
                        .SetHeight(24f)
                        .SetWidth(180f);
                    row.AddLabel()
                        .BindText(model => model.CheckboxMirrorText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddSectionLabel(col, "Combo box with static options (watched index)");
                col.AddRow(row =>
                {
                    row.AddComboBox()
                        .AddOption("Static option 1", 0)
                        .AddOption("Static option 2", 1)
                        .AddOption("Static option 3", 2)
                        .AddOption("Static option 4", 3)
                        .BindSelectedIndex(model => model.StaticComboSelection)
                        .SetHeight(ButtonHeight)
                        .SetWidth(250f);
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Combo box with bound options, replaced wholesale while open");
                col.AddRow(row =>
                {
                    row.AddComboBox()
                        .BindOptions(model => model.DynamicComboOptions)
                        .BindSelectedIndex(model => model.DynamicComboSelection)
                        .SetHeight(ButtonHeight)
                        .SetWidth(250f);
                    row.AddButton()
                        .SetText("Replace Options")
                        .SetHeight(ButtonHeight)
                        .SetWidth(140f)
                        .BindOnClicked(model => model.OnClickReplaceComboOptions());
                });

                AddSectionLabel(col, "Radio options - horizontal and vertical (both watched)");
                col.AddRow(row =>
                {
                    row.AddOptions()
                        .SetDirection(NuiDirection.Horizontal)
                        .AddOption("One")
                        .AddOption("Two")
                        .AddOption("Three")
                        .BindSelectedValue(model => model.OptionsHorizontalValue)
                        .SetHeight(30f)
                        .SetWidth(300f);
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddOptions()
                        .SetDirection(NuiDirection.Vertical)
                        .AddOption("Alpha")
                        .AddOption("Beta")
                        .AddOption("Gamma")
                        .BindSelectedValue(model => model.OptionsVerticalValue)
                        .SetHeight(90f)
                        .SetWidth(200f);
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Color picker (watched) driving the label's bound color");
                col.AddRow(row =>
                {
                    row.AddColorPicker()
                        .BindSelectedColor(model => model.PickedColor)
                        .SetHeight(150f)
                        .SetWidth(250f);
                    row.AddLabel()
                        .SetText("Colored by the picker")
                        .BindColor(model => model.PickedColor)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
            });
        }

        private static void AddSlidersTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Int slider (watched) - its value drives the progress bar below");
                col.AddRow(row =>
                {
                    row.AddSliderInt()
                        .BindValue(model => model.SliderIntValue)
                        .SetMinimum(0)
                        .SetMaximum(10)
                        .SetStepSize(1)
                        .SetHeight(ButtonHeight);
                });

                AddSectionLabel(col, "Progress bar (bound) with server-side mutation buttons");
                col.AddRow(row =>
                {
                    row.AddProgressBar()
                        .BindValue(model => model.ProgressValue)
                        .SetHeight(24f);
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("+10%")
                        .SetHeight(ButtonHeight)
                        .SetWidth(100f)
                        .BindOnClicked(model => model.OnClickProgressAdd());
                    row.AddButton()
                        .SetText("Reset")
                        .SetHeight(ButtonHeight)
                        .SetWidth(100f)
                        .BindOnClicked(model => model.OnClickProgressReset());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Float slider with a bound maximum changed at runtime");
                col.AddRow(row =>
                {
                    row.AddSliderFloat()
                        .BindValue(model => model.SliderFloatValue)
                        .SetMinimum(0f)
                        .BindMaximum(model => model.SliderFloatMax)
                        .SetStepSize(0.05f)
                        .SetHeight(ButtonHeight);
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Change Maximum")
                        .SetHeight(ButtonHeight)
                        .SetWidth(160f)
                        .BindOnClicked(model => model.OnClickRaiseSliderMax());
                    row.AddSpacer();
                });
            });
        }

        private static void AddListsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Mixed-cell list: fixed label / element-sized button / variable label / progress / image");
                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(90f);
                            cell.AddLabel()
                                .BindText(model => model.ListNames)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            // Element-sized fixed cell: no cell width, the inner
                            // button's width sizes it (validator-exempt shape).
                            cell.SetIsVariable(false);
                            cell.AddButton()
                                .SetText("Log")
                                .SetWidth(44f)
                                .BindOnClicked(model => model.OnClickListRowButton());
                        });
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.ListDescriptions)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(80f);
                            cell.AddProgressBar()
                                .BindValue(model => model.ListProgress);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(40f);
                            cell.AddImage()
                                .BindResref(model => model.ListResrefs)
                                .SetAspect(NuiAspect.Fit);
                        });
                    })
                        .BindRowCount(model => model.ListNames)
                        .SetRowHeight(28f)
                        .SetHeight(150f);
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Add Row")
                        .SetHeight(ButtonHeight)
                        .SetWidth(110f)
                        .BindOnClicked(model => model.OnClickListAddRow());
                    row.AddButton()
                        .SetText("Remove Row")
                        .SetHeight(ButtonHeight)
                        .SetWidth(110f)
                        .BindOnClicked(model => model.OnClickListRemoveRow());
                    row.AddButton()
                        .SetText("Replace Lists")
                        .SetHeight(ButtonHeight)
                        .SetWidth(110f)
                        .BindOnClicked(model => model.OnClickListReplace());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Table: fixed(90) / fixed(120) / variable-last columns, with tooltips");
                col.AddTable(table => table
                    .AddColumn("FIXED 90", 90f, model => model.TableColA, model => model.TableTooltips, "Fixed-width column (90)")
                    .AddColumn("FIXED 120", 120f, model => model.TableColB, null, "Fixed-width column (120)")
                    .AddColumn("VARIABLE", 0f, model => model.TableColC, null, "Last column defaults to variable")
                    .SetRowHeight(24f));

                AddSectionLabel(col, "Headerless table with a component (button) column and explicit row count");
                col.AddTable(table => table
                    .SetShowHeader(false)
                    .AddComponentColumn("", 60f, cell =>
                    {
                        cell.AddButton()
                            .SetText("Ping")
                            .SetHeight(22f)
                            .BindOnClicked(model => model.OnClickTable2RowButton());
                    })
                    .AddColumn("NAME", 0f, model => model.Table2Names)
                    .BindRowCount(model => model.Table2Names)
                    .SetRowHeight(28f));
            });
        }

        private static void AddGroupsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Groups nested three deep: border / auto-scroll / borderless");
                col.AddRow(row =>
                {
                    row.AddGroup(outer =>
                    {
                        outer.SetShowBorder(true);
                        outer.SetScrollbars(NuiScrollbars.None);
                        outer.AddColumn(outerCol =>
                        {
                            outerCol.AddRow(r => r.AddLabel().SetText("Outer group (border, no scroll)").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left));
                            outerCol.AddRow(r =>
                            {
                                r.AddGroup(middle =>
                                {
                                    middle.SetShowBorder(true);
                                    middle.SetScrollbars(NuiScrollbars.Auto);
                                    middle.AddColumn(middleCol =>
                                    {
                                        middleCol.AddRow(r2 => r2.AddLabel().SetText("Middle group (auto scroll)").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left));
                                        middleCol.AddRow(r2 =>
                                        {
                                            r2.AddGroup(inner =>
                                            {
                                                inner.SetShowBorder(false);
                                                inner.SetScrollbars(NuiScrollbars.None);
                                                inner.AddColumn(innerCol =>
                                                {
                                                    innerCol.AddRow(r3 => r3.AddLabel().SetText("Inner borderless group").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left));
                                                });
                                            });
                                        });
                                    });
                                });
                            });
                        });
                    })
                        .SetHeight(140f);
                });

                AddSectionLabel(col, "Spacers centering content within a row");
                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddLabel()
                        .SetText("[ Centered by spacers ]")
                        .SetHeight(LabelHeight)
                        .SetWidth(180f);
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Spacer pushing content to the bottom of a fixed-height group");
                col.AddRow(row =>
                {
                    row.AddGroup(group =>
                    {
                        group.SetScrollbars(NuiScrollbars.None);
                        group.AddColumn(groupCol =>
                        {
                            groupCol.AddRow(r => r.AddLabel().SetText("Top of group").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left));
                            groupCol.AddRow(r => r.AddSpacer());
                            groupCol.AddRow(r => r.AddLabel().SetText("Pushed to bottom by a spacer").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left));
                        });
                    })
                        .SetHeight(90f);
                });

                AddSectionLabel(col, "Sibling sizing: fixed width vs aspect ratio vs variable");
                col.AddRow(row =>
                {
                    row.SetHeight(50f);
                    row.AddGroup(group =>
                    {
                        group.AddColumn(c => c.AddRow(r => r.AddLabel().SetText("W=100")));
                    })
                        .SetWidth(100f);
                    row.AddGroup(group =>
                    {
                        group.AddColumn(c => c.AddRow(r => r.AddLabel().SetText("Aspect 2:1")));
                    })
                        .SetAspectRatio(2f);
                    row.AddGroup(group =>
                    {
                        group.AddColumn(c => c.AddRow(r => r.AddLabel().SetText("Variable width")));
                    });
                });

                AddSectionLabel(col, "Identical labels: margin 0 vs default margin vs padding 10");
                col.AddRow(row =>
                {
                    row.AddLabel().SetText("Margin 0").SetHeight(24f).SetMargin(0f).SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                col.AddRow(row =>
                {
                    row.AddLabel().SetText("Default margin").SetHeight(24f).SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                col.AddRow(row =>
                {
                    row.AddLabel().SetText("Padding 10").SetHeight(24f).SetPadding(10f).SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddSectionLabel(col, "Fixed-width group with exactly-fitting fixed children (legal - contrast with hazard H5)");
                col.AddRow(row =>
                {
                    row.AddGroup(group =>
                    {
                        group.AddColumn(c => c.AddRow(r =>
                        {
                            r.AddLabel().SetText("Fixed 90").SetWidth(90f).SetHeight(24f);
                            r.AddLabel().SetText("Fixed 90").SetWidth(90f).SetHeight(24f);
                        }));
                    })
                        .SetWidth(220f)
                        .SetHeight(50f);
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Twenty rows inside an unsized-content scrollable group (the R2b-correct shape)");
                col.AddRow(row =>
                {
                    row.AddGroup(group =>
                    {
                        group.SetScrollbars(NuiScrollbars.Auto);
                        group.AddColumn(groupCol =>
                        {
                            for (var line = 1; line <= 20; line++)
                            {
                                var text = $"Scrollable line {line} of 20";
                                groupCol.AddRow(r => r.AddLabel().SetText(text).SetHeight(24f).SetHorizontalAlign(NuiHorizontalAlign.Left));
                            }
                        });
                    })
                        .SetHeight(150f);
                });
            });
        }

        private static void AddDrawingTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Same image in four aspect modes (hover for the mode name)");
                col.AddRow(row =>
                {
                    row.AddImage().SetResref("arrow_up").SetAspect(NuiAspect.Fit).SetTooltip("NuiAspect.Fit").SetWidth(48f).SetHeight(48f);
                    row.AddImage().SetResref("arrow_up").SetAspect(NuiAspect.Fill).SetTooltip("NuiAspect.Fill").SetWidth(48f).SetHeight(48f);
                    row.AddImage().SetResref("arrow_up").SetAspect(NuiAspect.Exact).SetTooltip("NuiAspect.Exact").SetWidth(48f).SetHeight(48f);
                    row.AddImage().SetResref("arrow_up").SetAspect(NuiAspect.ExactScaled).SetTooltip("NuiAspect.ExactScaled").SetWidth(48f).SetHeight(48f);
                    row.AddImage().SetResref("arrow_up").SetAspect(NuiAspect.Exact).SetRegion(new GuiRectangle(0f, 0f, 16f, 16f)).SetTooltip("SetRegion(0,0,16,16)").SetWidth(48f).SetHeight(48f);
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Image with a bound resref, cycled server-side");
                col.AddRow(row =>
                {
                    row.AddImage()
                        .BindResref(model => model.CycledImageResref)
                        .SetAspect(NuiAspect.Fit)
                        .SetWidth(48f)
                        .SetHeight(48f);
                    row.AddButton()
                        .SetText("Cycle Image")
                        .SetHeight(ButtonHeight)
                        .SetWidth(120f)
                        .BindOnClicked(model => model.OnClickCycleImage());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Static draw list over a label: circle, arc, curve, polyline, text, image item");
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText(".")
                        .SetHeight(100f)
                        .AddDrawList(drawList => drawList
                            .SetIsConstrainedToTargetBounds(true)
                            .AddCircle(circle => circle
                                .SetColor(0, 255, 255)
                                .SetIsFilled(false)
                                .SetLineThickness(2f)
                                .SetBounds(10f, 10f, 40f, 40f))
                            .AddArc(arc => arc
                                .SetColor(255, 0, 0)
                                .SetLineThickness(2f)
                                .SetCenter(90f, 30f)
                                .SetRadius(20f)
                                .SetAMinimum(0f)
                                .SetAMaximum(3.14f))
                            .AddCurve(curve => curve
                                .SetColor(0, 255, 0)
                                .SetLineThickness(2f)
                                .SetA(120f, 10f)
                                .SetB(200f, 50f)
                                .SetCtrl0(140f, 60f)
                                .SetCtrl1(180f, 0f))
                            .AddPolyLine(polyLine => polyLine
                                .SetColor(255, 255, 255)
                                .SetIsFilled(false)
                                .SetLineThickness(2f)
                                .AddPoint(220f, 10f)
                                .AddPoint(260f, 50f)
                                .AddPoint(300f, 10f))
                            .AddText(text => text
                                .SetColor(255, 255, 255)
                                .SetBounds(10f, 60f, 220f, 30f)
                                .SetText("draw list text item"))
                            .AddImage(image => image
                                .SetResref("arrow_up")
                                .SetPosition(250f, 60f, 32f, 32f)
                                .SetAspect(NuiAspect.Exact)
                                .SetDrawTextureRegion(0, 0, 16, 16)));
                });

                AddSectionLabel(col, "Bound draw geometry over an image - Animate steps bounds and color");
                col.AddRow(row =>
                {
                    row.AddImage()
                        .SetResref("arrow_up")
                        .SetAspect(NuiAspect.Exact)
                        .SetWidth(100f)
                        .SetHeight(100f)
                        .AddDrawList(drawList => drawList
                            .SetIsConstrainedToTargetBounds(true)
                            .AddCircle(circle => circle
                                .BindColor(model => model.DrawColor)
                                .SetIsFilled(true)
                                .SetLineThickness(1f)
                                .BindBounds(model => model.DrawCircleBounds)));
                    row.AddButton()
                        .SetText("Animate")
                        .SetHeight(ButtonHeight)
                        .SetWidth(120f)
                        .BindOnClicked(model => model.OnClickAnimateDraw());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Constrained vs unconstrained draw lists (circle drawn past the widget edge)");
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Constrained")
                        .SetWidth(100f)
                        .SetHeight(60f)
                        .AddDrawList(drawList => drawList
                            .SetIsConstrainedToTargetBounds(true)
                            .AddCircle(circle => circle
                                .SetColor(0, 255, 255)
                                .SetIsFilled(true)
                                .SetLineThickness(1f)
                                .SetBounds(70f, 20f, 60f, 60f)));
                    row.AddLabel()
                        .SetText("Unconstrained")
                        .SetWidth(100f)
                        .SetHeight(60f)
                        .AddDrawList(drawList => drawList
                            .SetIsConstrainedToTargetBounds(false)
                            .AddCircle(circle => circle
                                .SetColor(255, 0, 0)
                                .SetIsFilled(true)
                                .SetLineThickness(1f)
                                .SetBounds(70f, 20f, 60f, 60f)));
                    row.AddSpacer();
                });
            });
        }

        private static void AddChartsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Chart with a static line slot and an explicit height");
                col.AddRow(row =>
                {
                    row.AddChart()
                        .AddSlot(slot => slot
                            .SetType(NuiChartType.Lines)
                            .SetLegend("Static line")
                            .SetColor(0, 138, 250)
                            .AddDataPoint(1f)
                            .AddDataPoint(4f)
                            .AddDataPoint(2f)
                            .AddDataPoint(6f)
                            .AddDataPoint(3f))
                        .SetHeight(140f);
                });

                AddSectionLabel(col, "Two slots in one chart: bound bar data + static line overlay");
                col.AddRow(row =>
                {
                    row.AddChart()
                        .AddSlot(slot => slot
                            .SetType(NuiChartType.Columns)
                            .BindLegend(model => model.ChartLegend)
                            .BindColor(model => model.ChartColor)
                            .BindData(model => model.ChartData))
                        .AddSlot(slot => slot
                            .SetType(NuiChartType.Lines)
                            .SetLegend("Static overlay")
                            .SetColor(169, 169, 169)
                            .AddDataPoint(5f)
                            .AddDataPoint(2f)
                            .AddDataPoint(4f)
                            .AddDataPoint(1f)
                            .AddDataPoint(3f))
                        .SetHeight(140f);
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Randomize Data")
                        .SetHeight(ButtonHeight)
                        .SetWidth(140f)
                        .BindOnClicked(model => model.OnClickRandomizeChart());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Unsized chart in a scroll region (documents fill-the-span behavior; may collapse)");
                col.AddRow(row =>
                {
                    row.AddChart()
                        .AddSlot(slot => slot
                            .SetType(NuiChartType.Lines)
                            .SetLegend("Unsized chart")
                            .SetColor(0, 139, 0)
                            .AddDataPoint(2f)
                            .AddDataPoint(5f)
                            .AddDataPoint(1f));
                });
            });
        }

        private static void AddBindingsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Specimen widgets driven by bound IsVisible / IsEnabled / color / tooltip");
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("[ Visibility + color + tooltip specimen ]")
                        .BindIsVisible(model => model.IsSampleVisible)
                        .BindColor(model => model.SampleColor)
                        .BindTooltip(model => model.SampleTooltip)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Enable-bound button")
                        .BindIsEnabled(model => model.IsSampleEnabled)
                        .SetDisabledTooltip("Currently disabled via bound IsEnabled")
                        .SetHeight(ButtonHeight)
                        .SetWidth(180f)
                        .BindOnClicked(model => model.OnClickImageButton());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton().SetText("Toggle Visible").SetHeight(ButtonHeight).SetWidth(120f)
                        .BindOnClicked(model => model.OnClickToggleVisible());
                    row.AddButton().SetText("Toggle Enabled").SetHeight(ButtonHeight).SetWidth(120f)
                        .BindOnClicked(model => model.OnClickToggleEnabled());
                    row.AddButton().SetText("Cycle Color").SetHeight(ButtonHeight).SetWidth(120f)
                        .BindOnClicked(model => model.OnClickCycleColor());
                    row.AddButton().SetText("Cycle Tooltip").SetHeight(ButtonHeight).SetWidth(120f)
                        .BindOnClicked(model => model.OnClickCycleTooltip());
                });

                AddSectionLabel(col, "Watched text edit (assigned before watch per rule R3), plus watch re-issue");
                col.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetPlaceholder("Watched value")
                        .BindValue(model => model.WatchDemoValue)
                        .SetMaxLength(40)
                        .SetHeight(ButtonHeight)
                        .SetWidth(250f);
                    row.AddButton()
                        .SetText("Re-issue Watch")
                        .SetHeight(ButtonHeight)
                        .SetWidth(140f)
                        .BindOnClicked(model => model.OnClickRewatch());
                });

                AddSectionLabel(col, "Window geometry (drag/resize, then log the server-side value)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Log Geometry")
                        .SetHeight(ButtonHeight)
                        .SetWidth(140f)
                        .BindOnClicked(model => model.OnClickLogGeometry());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Bind flood: 20 pushes to one property inside a single event");
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.FloodCounterText)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Flood 20 Updates")
                        .SetHeight(ButtonHeight)
                        .SetWidth(160f)
                        .BindOnClicked(model => model.OnClickBindFlood());
                    row.AddSpacer();
                });
            });
        }

        private static void AddModalsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            AddTabShell(host, col =>
            {
                AddSectionLabel(col, "Stock modal partials (confirm/cancel results are logged)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Show Confirm Modal")
                        .SetHeight(ButtonHeight)
                        .SetWidth(180f)
                        .BindOnClicked(model => model.OnClickShowModal());
                    row.AddButton()
                        .SetText("Show Input Modal")
                        .SetHeight(ButtonHeight)
                        .SetWidth(180f)
                        .BindOnClicked(model => model.OnClickShowInputModal());
                });

                AddSectionLabel(col, "Partial-swap robustness");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Re-apply Current Tab")
                        .SetHeight(ButtonHeight)
                        .SetWidth(180f)
                        .BindOnClicked(model => model.OnClickReapplyTab());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Window open/close events are logged; close and reopen with /nuigallery to verify");
            });
        }

        private static void AddHazardsTab(GuiGroup<DebugNuiGalleryViewModel> host)
        {
            // Uses the standard fixed-width shell; the hazard constructs exercise
            // their constraints inside the slot itself. The slot is the terminal
            // row so the safe state stays legal.
            AddTabShell(host, col =>
            {
                if (!HazardsAreAvailable)
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Hazard partials are disabled on this environment.")
                            .SetHeight(24f);
                    });
                    return;
                }

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Confirmed-failure buttons will blank the window or throw; verified-working buttons render their exhibit.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetColor(GuiColor.Red);
                });
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("If the window blanks out: switch tabs, or close and reopen with /nuigallery.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddSectionLabel(col, "Confirmed failures (verified in-game 2026-07)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("H1: Button-Height Row")
                        .SetTooltip("row.SetHeight(32) + button.SetHeight(32): unsolvable, blanks the window (R2c)")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardButtonRow());
                    row.AddButton()
                        .SetText("H6: Watch Unset Prop")
                        .SetTooltip("WatchOnClient on a never-Set property: descriptive InvalidOperationException (R3); window unaffected and reopenable.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardWatchUnset());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P1: Row+Checkbox (FAILS)")
                        .SetTooltip("row.SetHeight(32)+checkbox.SetHeight(32). CONFIRMED: checkbox has default margins - blanks the window (R2c).")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeRowCheckbox());
                    row.AddButton()
                        .SetText("P2: Row+TextEdit (FAILS)")
                        .SetTooltip("row.SetHeight(32)+textedit.SetHeight(32). CONFIRMED: textedit has default margins - blanks the window (R2c).")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeRowTextEdit());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P3: Row+Combo (FAILS)")
                        .SetTooltip("row.SetHeight(32)+combo.SetHeight(32). CONFIRMED: combo has default margins - blanks the window (R2c).")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeRowCombo());
                    row.AddButton()
                        .SetText("P4: Row+SliderInt (FAILS)")
                        .SetTooltip("row.SetHeight(32)+slider.SetHeight(32). CONFIRMED: slider has default margins - blanks the window (R2c).")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeRowSlider());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P6: Row+Progress (FAILS)")
                        .SetTooltip("row.SetHeight(32)+progress.SetHeight(32). CONFIRMED: progress has default margins - blanks the window (R2c).")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeRowProgress());
                    row.AddButton()
                        .SetText("P13a: Bad Element Id")
                        .SetTooltip("ChangePartialView onto a nonexistent element id. CONFIRMED: client-side 'element id not found' error, no server exception; close/reopen recovers. There is no server-side element-id validation.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeBadElementId());
                    row.AddSpacer();
                });

                AddSectionLabel(col, "Verified working (previously suspected, confirmed OK in-game 2026-07)");
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("W1: Unbounded List (OK)")
                        .SetTooltip("Unbounded list followed by another row in a partial. VERIFIED: renders correctly in-game; kept as a regression exhibit.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardListNonTerminal());
                    row.AddButton()
                        .SetText("W2: Tall Fixed Stack (OK)")
                        .SetTooltip("12 x 120px fixed-height groups with no scroll wrapper. VERIFIED: renders correctly in-game; kept as a regression exhibit.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardTallStack());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("W3: Zero-Width Cell (OK)")
                        .SetTooltip("Fixed list cell with no width anywhere. VERIFIED: renders correctly in-game; kept as a regression exhibit.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardZeroCell());
                    row.AddButton()
                        .SetText("W4: Width Conflict (OK)")
                        .SetTooltip("Two 150px-wide buttons inside a 200px-wide group. VERIFIED: renders correctly in-game; kept as a regression exhibit.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardWidthConflict());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P5: Row+Options (OK)")
                        .SetTooltip("row.SetHeight(32)+options.SetHeight(32). VERIFIED: options is margin-free (like toggles/tabbar) - renders correctly.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeRowOptions());
                    row.AddButton()
                        .SetText("P7: Narrow Group (OK)")
                        .SetTooltip("100f-wide fixed group containing a 150f-wide fixed button. VERIFIED: renders correctly.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeColWidth());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P8: Aspect+Dims (OK)")
                        .SetTooltip("Image with aspect Exact plus explicit width AND height; second image with invalid resref. VERIFIED: renders correctly.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeAspect());
                    row.AddButton()
                        .SetText("P9: Zero Dims (OK)")
                        .SetTooltip("Button with SetWidth(0)/SetHeight(0). VERIFIED: renders correctly.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeZeroDim());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P10: Negative Dims (OK)")
                        .SetTooltip("Button with SetWidth(-10)/SetHeight(-10). VERIFIED: renders correctly.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeNegDim());
                    row.AddButton()
                        .SetText("P11: List Mismatch (OK)")
                        .SetTooltip("One list's cells bound to 5-row and 2-row lists. VERIFIED: renders without failure. Check Lists & Tables tab; recover via Replace Lists.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeListMismatch());
                    row.AddSpacer();
                });
                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("P12: Empty Data (OK)")
                        .SetTooltip("Empty chart data + empty combo options. VERIFIED: renders without failure. Check Charts/Selection tabs; recover via Randomize Data / Replace Options.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeEmptyData());
                    row.AddButton()
                        .SetText("P13b: Nested x3 (OK*)")
                        .SetTooltip("Partial inside a partial inside a partial (3 deep). VERIFIED with caveat: renders, but the innermost content is dropped by the parent's re-apply after a moment - do not nest partials more than 2 deep in real windows.")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickProbeNestedPartial());
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Reset Hazard Slot")
                        .SetHeight(ButtonHeight)
                        .SetWidth(200f)
                        .BindOnClicked(model => model.OnClickHazardReset());
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    var slot = row.AddPartialView(DebugNuiGalleryViewModel.HazardSlotElement);
                    slot.AddColumn(slotCol =>
                    {
                        slotCol.AddRow(r =>
                        {
                            r.AddLabel()
                                .SetText("No hazard loaded.")
                                .SetHeight(24f);
                        });
                    });
                });
            });
        }

        private static void DefineHazardPartials(GuiWindow<DebugNuiGalleryViewModel> window)
        {
            window.DefinePartialView(DebugNuiGalleryViewModel.HazardSafePartial, safe =>
            {
                safe.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Hazard slot reset. No hazard loaded.")
                            .SetHeight(24f);
                    });
                });
            });

            // H1 - the confirmed R2c failure: a row whose explicit height equals its
            // button's height cannot satisfy row_height >= child_height + margins.
            window.DefinePartialView(DebugNuiGalleryViewModel.HazardButtonRowPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(32f);
                        row.AddButton()
                            .SetText("Unsolvable")
                            .SetHeight(32f);
                    });
                });
            });

            // H2 - unbounded list that is not the terminal content of its column (R2).
            // Previously suspected to fail, but verified rendering correctly in-game 2026-07.
            window.DefinePartialView(DebugNuiGalleryViewModel.HazardListNonTerminalPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.ListNames)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });
                        })
                            .BindRowCount(model => model.ListNames);
                    });
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Row after the unbounded list")
                            .SetHeight(LabelHeight);
                    });
                });
            });

            // H3 - fixed heights stacking past the host viewport with no scroll wrapper (R2b).
            // Previously suspected to fail, but verified rendering correctly in-game 2026-07.
            window.DefinePartialView(DebugNuiGalleryViewModel.HazardTallStackPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    for (var block = 1; block <= 12; block++)
                    {
                        var text = $"Fixed 120px block {block} of 12";
                        col.AddRow(row =>
                        {
                            row.AddGroup(group =>
                            {
                                group.SetScrollbars(NuiScrollbars.None);
                                group.AddColumn(c => c.AddRow(r => r.AddLabel().SetText(text)));
                            })
                                .SetHeight(120f);
                        });
                    }
                });
            });

            // H4 - fixed template cell with no width on the cell or its element.
            // Previously suspected to fail, but verified rendering correctly in-game 2026-07.
            window.DefinePartialView(DebugNuiGalleryViewModel.HazardZeroCellPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.AddLabel()
                                    .BindText(model => model.ListNames);
                            });
                        })
                            .BindRowCount(model => model.ListNames)
                            .SetHeight(120f);
                    });
                });
            });

            // H5 - fixed child widths that cannot fit inside a fixed parent width.
            // Previously suspected to fail, but verified rendering correctly in-game 2026-07.
            window.DefinePartialView(DebugNuiGalleryViewModel.HazardWidthConflictPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddColumn(c => c.AddRow(r =>
                            {
                                r.AddButton().SetText("150 wide A").SetWidth(150f);
                                r.AddButton().SetText("150 wide B").SetWidth(150f);
                            }));
                        })
                            .SetWidth(200f)
                            .SetHeight(60f);
                    });
                });
            });

            // Probes - generalizing R2c margin detection across widget families.
            // Each probe uses a fixed row (32px) with SetHeight(32f) on its widget.
            // Outcome unknown per family; a confirmed BLANK promotes that family
            // into the validator's button-family list.
            DefineFixedRowProbe(window, DebugNuiGalleryViewModel.ProbeRowCheckboxPartial, 32f, row =>
            {
                row.AddCheckBox()
                    .SetText("Probe")
                    .BindIsChecked(model => model.IsChecked)
                    .SetHeight(32f)
                    .SetWidth(150f);
            });

            DefineFixedRowProbe(window, DebugNuiGalleryViewModel.ProbeRowTextEditPartial, 32f, row =>
            {
                row.AddTextEdit()
                    .SetPlaceholder("Probe")
                    .BindValue(model => model.TextEditValue)
                    .SetMaxLength(50)
                    .SetHeight(32f);
            });

            DefineFixedRowProbe(window, DebugNuiGalleryViewModel.ProbeRowComboPartial, 32f, row =>
            {
                row.AddComboBox()
                    .AddOption("Probe A", 0)
                    .AddOption("Probe B", 1)
                    .BindSelectedIndex(model => model.StaticComboSelection)
                    .SetHeight(32f)
                    .SetWidth(150f);
            });

            DefineFixedRowProbe(window, DebugNuiGalleryViewModel.ProbeRowSliderPartial, 32f, row =>
            {
                row.AddSliderInt()
                    .BindValue(model => model.SliderIntValue)
                    .SetMinimum(0)
                    .SetMaximum(10)
                    .SetStepSize(1)
                    .SetHeight(32f);
            });

            DefineFixedRowProbe(window, DebugNuiGalleryViewModel.ProbeRowOptionsPartial, 32f, row =>
            {
                row.AddOptions()
                    .SetDirection(NuiDirection.Horizontal)
                    .AddOption("One")
                    .AddOption("Two")
                    .BindSelectedValue(model => model.OptionsHorizontalValue)
                    .SetHeight(32f)
                    .SetWidth(200f);
            });

            DefineFixedRowProbe(window, DebugNuiGalleryViewModel.ProbeRowProgressPartial, 32f, row =>
            {
                row.AddProgressBar()
                    .BindValue(model => model.ProgressValue)
                    .SetHeight(32f);
            });

            // Structural probes - outcome unknown, verified in-game via the probe buttons.
            // P7: column-axis width conflict.
            window.DefinePartialView(DebugNuiGalleryViewModel.ProbeColWidthPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddColumn(inner => inner.AddRow(r => r.AddButton().SetText("150 wide").SetWidth(150f).SetHeight(32f)));
                        })
                            .SetWidth(100f)
                            .SetHeight(60f);
                    });
                });
            });

            // P8: aspect + both dimensions on one image, plus an invalid resref image.
            window.DefinePartialView(DebugNuiGalleryViewModel.ProbeAspectPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddImage()
                            .SetResref("arrow_up")
                            .SetAspect(NuiAspect.Exact)
                            .SetWidth(64f)
                            .SetHeight(200f)
                            .SetTooltip("Aspect Exact + width 64 + height 200");
                    });
                    col.AddRow(row =>
                    {
                        row.AddLabel().SetText("Below: image with invalid resref 'zz_no_such_img'").SetHeight(20f);
                    });
                    col.AddRow(row =>
                    {
                        row.AddImage()
                            .SetResref("zz_no_such_img")
                            .SetAspect(NuiAspect.Fit)
                            .SetWidth(48f)
                            .SetHeight(48f);
                    });
                });
            });

            // P9 / P10: zero and negative dimensions.
            window.DefinePartialView(DebugNuiGalleryViewModel.ProbeZeroDimPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row => row.AddButton().SetText("Zero-dim").SetWidth(0f).SetHeight(0f));
                });
            });
            window.DefinePartialView(DebugNuiGalleryViewModel.ProbeNegDimPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row => row.AddButton().SetText("Neg-dim").SetWidth(-10f).SetHeight(-10f));
                });
            });

            // P13b: 3-deep partial nesting host (a partial containing another partial slot).
            window.DefinePartialView(DebugNuiGalleryViewModel.ProbeNestedHostPartial, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel().SetText("Nested host loaded; inner slot below.").SetHeight(20f);
                    });
                    col.AddRow(row =>
                    {
                        var slot = row.AddPartialView(DebugNuiGalleryViewModel.ProbeNestedSlotElement);
                        slot.AddColumn(slotCol =>
                        {
                            slotCol.AddRow(r => r.AddLabel().SetText("Inner slot default content.").SetHeight(20f));
                        });
                    });
                });
            });
        }

        private static void DefineFixedRowProbe(
            GuiWindow<DebugNuiGalleryViewModel> window,
            string partialName,
            float rowHeight,
            Action<GuiRow<DebugNuiGalleryViewModel>> addWidget)
        {
            window.DefinePartialView(partialName, hazard =>
            {
                hazard.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(rowHeight);
                        addWidget(row);
                    });
                });
            });
        }
    }
}
