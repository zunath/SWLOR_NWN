using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ModalDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ModalViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Modal)
                .BindOnClosed(model => model.OnWindowClose())
                .SetIsResizable(false)
                .SetIsClosable(false)
                .SetIsCollapsed(false)
                .SetTitle(null)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(modal => modal.PromptText);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindText(model => model.ConfirmButtonText)
                            .BindOnClicked(model => model.OnConfirmClick());

                        row.AddButton()
                            .BindText(model => model.CancelButtonText)
                            .BindOnClicked(model => model.OnCancelClick());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
