using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MarketListingDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MarketListingViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.MarketListing)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Market Listing")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ListCount)
                            .SetHeight(26f);
                    });
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Item Name")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());

                        row.AddButton()
                            .SetText("Add Item")
                            .BindOnClicked(model => model.OnClickAddItem())
                            .BindIsEnabled(model => model.IsAddItemEnabled)
                            .SetHeight(35f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                            {
                                template.AddLabel()
                                    .BindText(model => model.ItemMarkets);

                                template.AddLabel()
                                    .BindText(model => model.ItemNames);

                                template.AddTextEdit()
                                    .BindValue(model => model.ItemPrices);

                                template.AddButton()
                                    .BindText(model => model.ItemListDelistNames)
                                    .BindColor(model => model.ItemListDelistColors)
                                    .BindOnClicked(model => model.OnClickListDelist());

                                template.AddButton()
                                    .SetText("Remove")
                                    .BindOnClicked(model => model.OnClickRemove());
                            })
                            .BindRowCount(model => model.ItemMarkets);
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
