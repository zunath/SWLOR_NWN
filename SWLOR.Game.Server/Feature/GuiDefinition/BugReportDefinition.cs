using SWLOR.Game.Server.Core.Beamdog;
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
                .SetInitialGeometry(0, 0, 476.57895f, 530.2632f)
                .SetTitle("Submit Bug Report")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        //row.AddSpacer();

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
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSubmit())
                            .SetText("Submit Bug Report")
                            .SetIsEnabled(true);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickCancel())
                            .SetText("Cancel")
                            .SetIsEnabled(true);
                    });

                });

            return _builder.Build();
        }
    }
}
