using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ChangeDescriptionDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ChangeDescriptionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ChangeDescription)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 326f, 400f)
                .SetTitle("Change Description")

                .AddRow(layout =>
                {
                    layout.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddTextEdit()
                                .SetPlaceholder("Enter your description here.")
                                .BindValue(model => model.Description)
                                .SetIsMultiline(true)
                                .SetMaxLength(5000)
                                .SetHeight(300f);
                        });
                    });

                    layout.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Reset to Original")
                                .BindOnClicked(model => model.OnClickResetToOriginal());
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Save")
                                .BindOnClicked(model => model.OnClickSave());
                        });

                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Cancel")
                                .BindOnClicked(model => model.OnClickCancel());
                        });
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
