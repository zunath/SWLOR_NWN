using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RenameTargetDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<RenameTargetViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.RenameItem)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(200f, 200f, 500f, 300f)
                .SetTitle("Rename Target")

                .DefinePartialView(RenameTargetViewModel.ItemEditorPartialName, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .SetIsMultiline(false)
                                .SetMaxLength(63)
                                .SetPlaceholder("New Name")
                                .BindValue(model => model.NewName)
                                .SetIsEnabled(true)
                                .SetHeight(20f);
                        });
                    });
                })

                .DefinePartialView(RenameTargetViewModel.PlayerEditorPartialName, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .SetIsMultiline(false)
                                .SetMaxLength(63)
                                .SetPlaceholder("First Name")
                                .BindValue(model => model.NewFirstName)
                                .SetIsEnabled(true)
                                .SetHeight(20f);
                        });
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .SetIsMultiline(false)
                                .SetMaxLength(63)
                                .SetPlaceholder("Last Name")
                                .BindValue(model => model.NewLastName)
                                .SetIsEnabled(true)
                                .SetHeight(20f);
                        });
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Header)
                            .SetColor(255, 0, 0)
                            .SetIsVisible(true)
                            .SetWidth(300f)
                            .SetHeight(20f);

                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("Original Name: ")
                                    .SetIsVisible(true)
                                    .SetWidth(100f)
                                    .SetHeight(20f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("Current Name: ")
                                    .SetIsVisible(true)
                                    .SetWidth(100f)
                                    .SetHeight(20f);
                            });

                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.OriginalName)
                                    .SetColor(0, 255, 0)
                                    .SetIsVisible(true)
                                    .SetWidth(200f)
                                    .SetHeight(20f);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.CurrentName)
                                    .SetColor(0, 255, 0)
                                    .SetIsVisible(true)
                                    .SetWidth(200f)
                                    .SetHeight(20f);
                            });

                        });
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView(RenameTargetViewModel.EditorPartialId);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSubmit())
                            .SetHeight(35f)
                            .SetText("Change Name")
                            .SetIsEnabled(true);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickReset())
                            .SetHeight(35f)
                            .SetText("Reset Name")
                            .SetIsEnabled(true);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickCancel())
                            .SetHeight(35f)
                            .SetText("Cancel")
                            .SetIsEnabled(true);

                        row.AddSpacer();

                    });
                });
        
            return _builder.Build();
        }
    }
}
