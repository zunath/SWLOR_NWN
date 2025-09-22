using SWLOR.Component.Communication.UI.ViewModel;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.UI.View
{
    public class TargetDescriptionDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<TargetDescriptionViewModel> _builder;

        public TargetDescriptionDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<TargetDescriptionViewModel>(_guiService);
        }

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
