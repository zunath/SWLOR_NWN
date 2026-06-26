using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DMToolsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMToolsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DMTools)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 520f)
                .SetTitle("DM Placeable Tools")
                .BindOnClosed(model => model.OnCloseWindow())

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Instructions)
                            .BindColor(model => model.InstructionColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.PlaceableNames)
                                    .BindOnClicked(model => model.OnSelectPlaceable())
                                    .BindIsToggled(model => model.PlaceableToggles);
                            });
                        })
                        .BindRowCount(model => model.PlaceableNames);

                        row.SetHeight(300f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetWidth(32f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnPreviousPage());

                        row.AddComboBox()
                            .BindOptions(model => model.PageNumbers)
                            .BindSelectedIndex(model => model.SelectedPageIndex);

                        row.AddButton()
                            .SetText(">")
                            .SetWidth(32f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnNextPage());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Refresh")
                            .BindOnClicked(model => model.OnRefreshPlaceables())
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Save Changes")
                            .BindOnClicked(model => model.OnSaveChanges())
                            .BindIsEnabled(model => model.IsPlaceableSelected)
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Delete Placeable")
                            .BindOnClicked(model => model.OnDeletePlaceable())
                            .BindIsEnabled(model => model.IsPlaceableSelected)
                            .SetHeight(35f);
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.SearchText)
                            .SetPlaceholder("Search placeables...");
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Search")
                            .BindOnClicked(model => model.OnSearch())
                            .SetHeight(35f);
                        row.AddButton()
                            .SetText("Clear")
                            .BindOnClicked(model => model.OnClearSearch())
                            .SetHeight(35f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Position")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButtonImage()
                            .SetImageResref("arrow_up")
                            .SetTooltip("Y-Axis +")
                            .BindOnClicked(model => model.OnYAxisUp())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("arrow_down")
                            .SetTooltip("Y-Axis -")
                            .BindOnClicked(model => model.OnYAxisDown())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("arrow_right")
                            .SetTooltip("X-Axis +")
                            .BindOnClicked(model => model.OnXAxisUp())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("arrow_left")
                            .SetTooltip("X-Axis -")
                            .BindOnClicked(model => model.OnXAxisDown())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("arrow_zup")
                            .SetTooltip("Z-Axis +")
                            .BindOnClicked(model => model.OnZAxisUp())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("arrow_zdown")
                            .SetTooltip("Z-Axis -")
                            .BindOnClicked(model => model.OnZAxisDown())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("level")
                            .SetTooltip("Z-Axis Reset")
                            .BindOnClicked(model => model.OnZAxisReset())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Facing")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButtonImage()
                            .SetImageResref("clockwise")
                            .SetTooltip("Clockwise")
                            .BindOnClicked(model => model.OnRotateClockwise())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("counterclockwise")
                            .SetTooltip("Counter-Clockwise")
                            .BindOnClicked(model => model.OnRotateCounterClockwise())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("north")
                            .SetTooltip("North")
                            .BindOnClicked(model => model.OnSetFacingNorth())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("south")
                            .SetTooltip("South")
                            .BindOnClicked(model => model.OnSetFacingSouth())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("east")
                            .SetTooltip("East")
                            .BindOnClicked(model => model.OnSetFacingEast())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddButtonImage()
                            .SetImageResref("west")
                            .SetTooltip("West")
                            .BindOnClicked(model => model.OnSetFacingWest())
                            .SetWidth(32f)
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsPlaceableSelected);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Layout Presets (This Map)")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.LayoutName)
                            .SetPlaceholder("Layout Name")
                            .SetMaxLength(32);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.LayoutNames)
                                    .BindOnClicked(model => model.OnSelectLayout())
                                    .BindIsToggled(model => model.LayoutToggles);
                            });
                        })
                        .BindRowCount(model => model.LayoutNames);

                        row.SetHeight(110f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Save Layout")
                            .BindOnClicked(model => model.OnSaveLayout())
                            .SetHeight(35f);
                        row.AddButton()
                            .SetText("Load Layout")
                            .BindOnClicked(model => model.OnLoadLayout())
                            .BindIsEnabled(model => model.IsLayoutSelected)
                            .SetHeight(35f);
                        row.AddButton()
                            .SetText("Delete")
                            .BindOnClicked(model => model.OnDeleteLayout())
                            .BindIsEnabled(model => model.IsLayoutSelected)
                            .SetHeight(35f);
                    });
                });

            return _builder.Build();
        }
    }
}
