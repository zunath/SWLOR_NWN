using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PropertyPermissionsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PropertyPermissionsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.PermissionManagement)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Permissions")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.PropertyName)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Instruction)
                            .BindColor(model => model.InstructionColor)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddTextEdit()
                                    .BindValue(model => model.SearchText)
                                    .SetPlaceholder("Search");

                                row2.AddButton()
                                    .SetText("X")
                                    .SetHeight(35f)
                                    .SetWidth(35f)
                                    .BindOnClicked(model => model.OnClickClearSearch());

                                row2.AddButton()
                                    .SetText("Search")
                                    .SetHeight(35f)
                                    .SetWidth(70f)
                                    .BindOnClicked(model => model.OnClickSearch());
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindOnClicked(model => model.OnSelectPlayer())
                                            .BindIsToggled(model => model.PlayerToggles)
                                            .BindText(model => model.PlayerNames)
                                            .BindTooltip(model => model.PlayerNames);

                                    });
                                })
                                    .BindRowCount(model => model.PlayerNames);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetHeight(20f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                                    .BindText(model => model.PlayerName);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(140f);
                                        cell.AddLabel()
                                            .BindText(model => model.PermissionNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                                            .BindTooltip(model => model.PermissionDescriptions);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(90f);
                                        cell.AddCheckBox()
                                            .BindIsChecked(model => model.PermissionStates)
                                            .SetText("Permission")
                                            .BindIsEnabled(model => model.PermissionEnabled);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(80f);
                                        cell.AddCheckBox()
                                            .BindIsChecked(model => model.PermissionGrantingStates)
                                            .SetText("Grant")
                                            .SetTooltip("Player will be able to grant this permission to others if checked.")
                                            .BindIsEnabled(model => model.GrantPermissionEnabled);
                                    });
                                })
                                    .BindRowCount(model => model.PermissionNames)
                                    .SetWidth(330f);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddCheckBox()
                                    .SetText("Publicly Accessible")
                                    .BindIsChecked(model => model.IsPublic)
                                    .BindIsEnabled(model => model.CanChangePublicSetting)
                                    .SetTooltip("If enabled, all players will be able to enter regardless of permissions.");
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickSaveChanges())
                                    .SetText("Save Changes");

                                row2.AddButton()
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickReset())
                                    .SetText("Reset Changes");
                            });

                        });
                    });
                })

                ;


            return _builder.Build();
        }
    }
}
