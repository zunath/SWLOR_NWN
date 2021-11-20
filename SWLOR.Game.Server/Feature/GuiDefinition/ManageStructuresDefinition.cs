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
                            .SetPlaceholder("Structure Name")
                            .SetMaxLength(32);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Position / Facing")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("X")
                            .BindValue(model => model.XPosition);

                        row.AddTextEdit()
                            .SetPlaceholder("Y")
                            .BindValue(model => model.YPosition);

                        row.AddTextEdit()
                            .SetPlaceholder("Z")
                            .BindValue(model => model.XPosition);

                        row.AddTextEdit()
                            .SetPlaceholder("Facing")
                            .BindValue(model => model.Facing);
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
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Discard Changes")
                            .BindOnClicked(model => model.OnDiscardChanges())
                            .SetHeight(35f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Retrieve Structure")
                            .BindOnClicked(model => model.OnRetrieveStructure())
                            .BindIsEnabled(model => model.IsRetrieveStructureEnabled)
                            .SetHeight(35f);
                    });
                });

                ;


            return _builder.Build();
        }
    }
}
