using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PropertyItemStorageDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PropertyItemStorageViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PropertyItemStorage)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Property Item Storage")

                .AddColumn(colHeader =>
                {
                    colHeader.AddRow(rowHeader =>
                    {
                        rowHeader.AddLabel()
                            .SetHeight(20f)
                            .BindText(model => model.Instructions)
                            .BindColor(model => model.InstructionsColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    colHeader.AddRow(rowHeader =>
                    {
                        rowHeader.AddLabel()
                            .SetHeight(20f)
                            .BindText(model => model.ItemCount)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    colHeader.AddRow(rowHeader =>
                    {
                        rowHeader.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.CategoryNames)
                                            .BindIsToggled(model => model.CategoryToggles)
                                            .BindOnClicked(model => model.OnSelectCategory())
                                            .BindIsEnabled(model => model.CategoryEnables);
                                    });
                                })
                                    .BindRowCount(model => model.CategoryNames);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("Add Category")
                                    .BindOnClicked(model => model.OnAddCategory())
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.CanAddCategory);

                                row.AddButton()
                                    .SetText("Delete Category")
                                    .BindOnClicked(model => model.OnDeleteCategory())
                                    .BindIsEnabled(model => model.IsCategorySelected)
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.CanDeleteCategory);
                            });
                            col.AddRow(row =>
                            {
                                row.AddSpacer();
                                row.AddButton()
                                    .SetText("Permissions")
                                    .BindOnClicked(model => model.OnEditPermissions())
                                    .BindIsEnabled(model => model.CanEditPermissions)
                                    .SetHeight(35f);
                                row.AddSpacer();
                            });
                        });

                        rowHeader.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddTextEdit()
                                    .BindValue(model => model.CategoryName)
                                    .SetPlaceholder("Name")
                                    .SetMaxLength(32)
                                    .SetWidth(300f)
                                    .BindIsEnabled(model => model.CanEditCategory);

                                row.AddButton()
                                    .SetText("Save")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnSaveName())
                                    .BindIsEnabled(model => model.CanEditCategory);
                            });

                            col.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindIsToggled(model => model.ItemToggles)
                                            .BindTooltip(model => model.ItemNames)
                                            .BindText(model => model.ItemNames)
                                            .BindOnClicked(model => model.OnSelectItem());
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(40f);

                                        cell.AddButton()
                                            .SetHeight(40f)
                                            .SetWidth(40f)
                                            .BindOnClicked(model => model.OnExamineItem())
                                            .SetText("?");
                                    });
                                })
                                    .BindRowCount(model => model.ItemNames);
                            });

                            col.AddRow(row =>
                            {
                                row.AddSpacer();
                                row.AddButton()
                                    .SetText("Store Item")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnStoreItem())
                                    .BindIsEnabled(model => model.IsCategorySelected);

                                row.AddButton()
                                    .SetText("Retrieve Item")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnRetrieveItem())
                                    .BindIsEnabled(model => model.IsItemSelected);

                                row.AddSpacer();
                            });
                        });
                    });
                })

                ;
            return _builder.Build();
        }
    }
}
