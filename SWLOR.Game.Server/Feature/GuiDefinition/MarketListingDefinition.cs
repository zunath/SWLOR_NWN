using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MarketListingDefinition : IGuiWindowDefinition
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
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClear());

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
                            template.AddImage()
                                //.BindResref(model => model.ItemIconResrefs)
                                .SetResref("iwswss");

                            template.AddLabel()
                                .BindText(model => model.ItemMarkets)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);

                            template.AddLabel()
                                .BindText(model => model.ItemNames)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);

                            template.AddTextEdit()
                                .BindValue(model => model.ItemPrices);

                            template.AddCheckBox()
                                .BindIsChecked(model => model.ItemListed)
                                .SetText("For Sale");

                            template.AddButton()
                                .SetText("Remove")
                                .BindOnClicked(model => model.OnClickRemove());
                        })
                            .BindRowCount(model => model.ItemMarkets);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Save Changes")
                            .BindOnClicked(model => model.OnClickSaveChanges());

                        row.AddSpacer();
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
