using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AreaManagerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AreaManagerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AreaManager)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 975f, 650f)
                .SetTitle("Area Manager")
                .BindOnClosed(model => model.OnWindowClose())

                .AddColumn(colSearch =>
                {
                    // Main Search - Top Window Row
                    colSearch.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());
                    });

                    // Main Window Container - Second Window Row
                    colSearch.AddRow(row =>
                    {
                        row.AddColumn(colAreas =>
                        {
                            colAreas.SetHeight(500f);
                            colAreas.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.AreaNames)
                                            .BindIsToggled(model => model.AreaToggled)
                                            .BindOnClicked(model => model.OnSelectArea());
                                    });
                                })
                                    .BindRowCount(model => model.AreaNames);
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddSpacer();

                                row.AddButton()
                                    .SetText("Reset Template")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsResetAreaEnabled)
                                    .BindOnClicked(model => model.OnClickResetArea());

                                row.AddButton()
                                    .SetText("Create Template")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.CreateNewAreaTemplate());

                                row.AddSpacer();
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddSpacer();

                                row.AddButton()
                                    .SetText("Goto Template")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickGoToArea());

                                row.AddButton()
                                    .SetText("Delete Template")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickDeleteAreaTemplate());

                                row.AddSpacer();
                            });
                        });

                        row.AddColumn(colObjects =>
                        {
                            colObjects.SetHeight(450f);
                            colObjects.SetWidth(325f);
                            colObjects.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.SetHeight(450f);

                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.AreaObjectList)
                                            .BindIsToggled(model => model.AreaObjectToggled)
                                            .BindOnClicked(model => model.OnSelectAreaObject());
                                    });
                                })
                                .BindRowCount(model => model.AreaObjectList);
                            });

                            colObjects.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("Delete Object")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsDeleteObjectEnabled)
                                    .BindOnClicked(model => model.OnClickDeleteObject());

                                row.AddButton()
                                    .SetText("Goto Object")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .BindOnClicked(model => model.OnClickGoToObject());
                            });
                        });

                        row.AddColumn(colEditor =>
                        {
                            colEditor.SetHeight(500f);
                            colEditor.SetWidth(325f);

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddTextEdit()
                                    .BindValue(model => model.SelectedAreaObjectName)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .SetPlaceholder("Object Name")
                                    .SetMaxLength(100);
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddLabel()
                                    .SetText("Position")
                                    .SetHeight(20f);
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddSpacer();

                                rowEditor.AddButtonImage()
                                    .SetImageResref("arrow_up")
                                    .SetTooltip("Y-Axis +")
                                    .BindOnClicked(model => model.OnYAxisUp())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("arrow_down")
                                    .SetTooltip("Y-Axis -")
                                    .BindOnClicked(model => model.OnYAxisDown())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("arrow_right")
                                    .SetTooltip("X-Axis +")
                                    .BindOnClicked(model => model.OnXAxisUp())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("arrow_left")
                                    .SetTooltip("X-Axis -")
                                    .BindOnClicked(model => model.OnXAxisDown())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("arrow_zup")
                                    .SetTooltip("Z-Axis +")
                                    .BindOnClicked(model => model.OnZAxisUp())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("arrow_zdown")
                                    .SetTooltip("Z-Axis -")
                                    .BindOnClicked(model => model.OnZAxisDown())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("level")
                                    .SetTooltip("Z-Axis Reset")
                                    .BindOnClicked(model => model.OnZAxisReset())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddSpacer();
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddLabel()
                                    .SetText("Facing")
                                    .SetHeight(20f);
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddSpacer();

                                rowEditor.AddButtonImage()
                                    .SetImageResref("clockwise")
                                    .SetTooltip("Clockwise")
                                    .BindOnClicked(model => model.OnRotateClockwise())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("counterclockwise")
                                    .SetTooltip("Counter-Clockwise")
                                    .BindOnClicked(model => model.OnRotateCounterClockwise())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("north")
                                    .SetTooltip("North")
                                    .BindOnClicked(model => model.OnSetFacingNorth())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("south")
                                    .SetTooltip("South")
                                    .BindOnClicked(model => model.OnSetFacingSouth())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("east")
                                    .SetTooltip("East")
                                    .BindOnClicked(model => model.OnSetFacingEast())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddButtonImage()
                                    .SetImageResref("west")
                                    .SetTooltip("West")
                                    .BindOnClicked(model => model.OnSetFacingWest())
                                    .SetWidth(32f)
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                rowEditor.AddSpacer();
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddLabel()
                                    .SetText("Appearance")
                                    .SetHeight(20f);
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddTextEdit()
                                    .SetPlaceholder("Search")
                                    .SetMaxLength(100)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .BindValue(model => model.SearchAppearanceText);

                                rowEditor.AddButton()
                                    .SetText("X")
                                    .SetHeight(35f)
                                    .SetWidth(35f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                .BindOnClicked(model => model.OnClickClearAppearanceSearch());

                                rowEditor.AddButton()
                                    .SetText("Search")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .BindOnClicked(model => model.OnClickAppearanceSearch());
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                colEditor.AddRow(row =>
                                {
                                    row.SetHeight(200f);

                                    row.AddList(template =>
                                    {
                                        template.BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature);

                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindText(model => model.ObjectAppearanceList)
                                                .BindIsToggled(model => model.ObjectAppearanceToggled)
                                                .BindOnClicked(model => model.OnSelectObjectAppearance());
                                        });
                                    })
                                    .BindRowCount(model => model.ObjectAppearanceList);
                                });
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddSpacer();
                                rowEditor.AddButton()
                                    .SetText("<")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .BindOnClicked(model => model.OnPreviousPageAppearance());

                                rowEditor.AddComboBox()
                                    .BindOptions(model => model.PageNumbersAppearances)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .BindSelectedIndex(model => model.SelectedPageIndexAppearances);

                                rowEditor.AddButton()
                                    .SetText(">")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .BindOnClicked(model => model.OnNextPageAppearance());

                                rowEditor.AddSpacer();
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddSpacer();
                            });

                            colEditor.AddRow(rowEditor =>
                            {
                                rowEditor.AddButton()
                                    .SetText("Save Changes")
                                    .BindOnClicked(model => model.OnSaveObjectChanges())
                                    .BindIsEnabled(model => model.IsSelectedObjectPlaceableOrCreature)
                                    .SetHeight(35f);

                                rowEditor.AddButton()
                                    .SetText("Resave All Objects")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsResaveAllObjectsEnabled)
                                    .BindOnClicked(model => model.OnClickResaveAllObjects());
                            });
                        });
                    });
                });

            return _builder.Build();
        }       
    }
}
