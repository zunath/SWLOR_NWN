using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ElectionDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ElectionViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.Election)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Election")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.Instructions)
                            .SetHeight(100f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindIsToggled(model => model.CandidateToggles)
                                    .BindText(model => model.CandidateNames)
                                    .BindIsEnabled(model => model.CandidateEnables)
                                    .BindOnClicked(model => model.SelectCandidate());
                            });
                        })
                            .BindRowCount(model => model.CandidateNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .BindText(model => model.MainActionButtonText)
                            .BindOnClicked(model => model.MainAction())
                            .BindColor(model => model.MainActionButtonColor);

                        row.AddSpacer();
                    });
                })
                ;



            return _builder.Build();
        }
    }
}
