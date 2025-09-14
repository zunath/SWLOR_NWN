using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageBansDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageBansViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ManageBans)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Bans")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .BindColor(model => model.StatusColor)
                            .SetHeight(20f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.CDKeys)
                                    .BindIsToggled(model => model.UserToggles)
                                    .BindOnClicked(model => model.OnSelectUser());
                            });
                        })
                            .BindRowCount(model => model.CDKeys);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("New User")
                            .BindOnClicked(model => model.OnClickNewUser());

                        row.AddButton()
                            .SetText("Delete User")
                            .BindOnClicked(model => model.OnClickDeleteUser());
                    });
                })

                .AddColumn(col =>
                {
                    col.SetHeight(300f);

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.ActiveUserCDKey)
                            .BindIsEnabled(model => model.IsUserSelected)
                            .SetPlaceholder("CD Key (8 chars)")
                            .SetMaxLength(8);

                        row.SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.ActiveBanReason)
                            .BindIsEnabled(model => model.IsUserSelected)
                            .SetPlaceholder("Ban Reason");

                        row.SetHeight(32f);
                    });
                    
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSave())
                            .SetText("Save")
                            .BindIsEnabled(model => model.IsUserSelected);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickDiscardChanges())
                            .SetText("Discard Changes")
                            .BindIsEnabled(model => model.IsUserSelected);
                    });
                });

            return _builder.Build();
        }
    }
}
