using SWLOR.Component.Communication.UI.ViewModel;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.UI.View
{
    public class BugReportDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<BugReportViewModel> _builder;

        public BugReportDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<BugReportViewModel>(_guiService);
        }

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
