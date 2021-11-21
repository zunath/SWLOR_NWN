using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageStructuresDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageStructuresViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ManageStructures)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Structures")
                .BindOnClosed(model => model.OnCloseWindow())

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.StructureNames)
                                    .BindOnClicked(model => model.OnSelectStructure())
                                    .BindIsToggled(model => model.StructureToggles);
                            });
                        })
                            .BindRowCount(model => model.StructureNames);
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
                            .BindOnClicked(model => model.OnManageProperty())
                            .BindIsEnabled(model => model.IsManagePropertyEnabled)
                            .SetText("Manage Property")
                            .SetHeight(35f);

                        row.AddButton()
                            .BindOnClicked(model => model.OnOpenStorage())
                            .BindIsEnabled(model => model.IsOpenStorageEnabled)
                            .SetText("Open Storage")
                            .SetHeight(35f);
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.StructureName)
                            .BindIsEnabled(model => model.IsStructureNameEnabled)
                            .SetPlaceholder("Structure Name")
                            .SetMaxLength(32);
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
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("arrow_down")
                            .SetTooltip("Y-Axis -")
                            .BindOnClicked(model => model.OnYAxisDown())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("arrow_right")
                            .SetTooltip("X-Axis +")
                            .BindOnClicked(model => model.OnXAxisUp())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("arrow_left")
                            .SetTooltip("X-Axis -")
                            .BindOnClicked(model => model.OnXAxisDown())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("arrow_zup")
                            .SetTooltip("Z-Axis +")
                            .BindOnClicked(model => model.OnZAxisUp())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("arrow_zdown")
                            .SetTooltip("Z-Axis -")
                            .BindOnClicked(model => model.OnZAxisDown())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("level")
                            .SetTooltip("Z-Axis Reset")
                            .BindOnClicked(model => model.OnZAxisReset())
                            .SetWidth(32f)
                            .SetHeight(32f);

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
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("counterclockwise")
                            .SetTooltip("Counter-Clockwise")
                            .BindOnClicked(model => model.OnRotateCounterClockwise())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("north")
                            .SetTooltip("North")
                            .BindOnClicked(model => model.OnSetFacingNorth())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("south")
                            .SetTooltip("South")
                            .BindOnClicked(model => model.OnSetFacingSouth())
                            .SetWidth(32f)
                            .SetHeight(32f);
                        
                        row.AddButtonImage()
                            .SetImageResref("east")
                            .SetTooltip("East")
                            .BindOnClicked(model => model.OnSetFacingEast())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddButtonImage()
                            .SetImageResref("west")
                            .SetTooltip("West")
                            .BindOnClicked(model => model.OnSetFacingWest())
                            .SetWidth(32f)
                            .SetHeight(32f);

                        row.AddSpacer();
                    });
                    
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Save Changes")
                            .BindOnClicked(model => model.OnSaveChanges())
                            .BindIsEnabled(model => model.IsStructureNameEnabled)
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Discard Changes")
                            .BindOnClicked(model => model.OnDiscardChanges())
                            .BindIsEnabled(model => model.IsStructureNameEnabled)
                            .SetHeight(35f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Retrieve Structure")
                            .BindOnClicked(model => model.OnRetrieveStructure())
                            .BindIsEnabled(model => model.IsRetrieveStructureEnabled)
                            .SetHeight(35f);
                        row.AddSpacer();
                    });
                });

                ;


            return _builder.Build();
        }
    }
}
