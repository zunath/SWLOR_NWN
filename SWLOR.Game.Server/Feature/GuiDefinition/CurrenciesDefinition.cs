using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CurrenciesDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<CurrenciesViewModel> _builder;

        public CurrenciesDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<CurrenciesViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Currencies)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Currencies")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Currency Name")
                            .SetHeight(32f);
                    });
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.CurrencyNames);
                            });
                        })
                            .BindRowCount(model => model.CurrencyNames);
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Amount")
                            .SetHeight(32f);
                    });
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.CurrencyValues);
                            });
                        })
                            .BindRowCount(model => model.CurrencyValues);
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
