using SWLOR.Component.Player.UI.ViewModel;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Player.UI.View
{
    public class ChangeDescriptionDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<ChangeDescriptionViewModel> _builder;

        public ChangeDescriptionDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<ChangeDescriptionViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ChangeDescription)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
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
