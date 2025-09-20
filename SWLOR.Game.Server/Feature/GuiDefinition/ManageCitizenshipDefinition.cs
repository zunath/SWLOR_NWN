using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageCitizenshipDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageCitizenshipViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ManageCitizenship)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Citizenship")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.CityDetails)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                        })
                            .BindRowCount(model => model.CityDetails);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .BindText(model => model.RegisterRevokeButtonName)
                            .BindOnClicked(model => model.RegisterRevoke())
                            .BindColor(model => model.RegisterRevokeButtonColor)
                            .SetHeight(35f);

                        row.AddButton()
                            .BindText(model => model.PayTaxesButtonName)
                            .BindIsEnabled(model => model.IsPayTaxesEnabled)
                            .BindOnClicked(model => model.PayTaxes())
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Election")
                            .BindIsEnabled(model => model.IsElectionActive)
                            .BindOnClicked(model => model.OpenElectionMenu())
                            .SetHeight(35f);
                    });
                });

            return _builder.Build();
        }
    }
}
