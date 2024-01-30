using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class TargetDescriptionDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TargetDescriptionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.TargetDescription)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 652f, 400f)
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
                                .SetText("Save")
                                .BindOnClicked(model => model.OnClickSave());
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
