using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class BugReportDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<BugReportViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.BugReport)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 829f, 453f)
                .SetTitle("Submit Bug Report")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Please enter as much information as possible regarding the encountered bug.")
                            .SetIsVisible(true)
                            .SetWidth(800f);

                        row.SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetIsMultiline(true)
                            .SetMaxLength(BugReportViewModel.MaxBugReportLength)
                            .BindValue(model => model.BugReportText)
                            .SetIsEnabled(true)
                            .SetHeight(300f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSubmit())
                            .SetHeight(35f)
                            .SetText("Submit Bug Report")
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
