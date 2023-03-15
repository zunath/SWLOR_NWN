using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AreaManagerNamingDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AreaManagerNamingViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AreaManagerNaming)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 829f, 453f)
                .SetTitle("Template Area Naming")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText($"Please type in your new template area's name. Remember, it should not be longer than {AreaManagerNamingViewModel.MaxTemplateAreaNewNameLength} characters!)")
                            .SetIsVisible(true)
                            .SetWidth(800f);

                        row.SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetIsMultiline(true)
                            .SetMaxLength(AreaManagerNamingViewModel.MaxTemplateAreaNewNameLength)
                            .BindValue(model => model.TemplateAreaNewName)
                            .SetIsEnabled(true)
                            .SetHeight(300f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSubmit())
                            .SetHeight(35f)
                            .SetText("Set Name")
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
