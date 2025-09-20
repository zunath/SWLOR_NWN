using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MarketListingDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MarketListingViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.MarketListing)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .BindTitle(model => model.WindowTitle)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Item Name")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClear());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Add Item")
                            .BindOnClicked(model => model.OnClickAddItem())
                            .BindIsEnabled(model => model.IsAddItemEnabled)
                            .SetHeight(35f);

                        row.AddButton()
                            .BindText(model => model.ShopTill)
                            .BindIsEnabled(model => model.IsShopTillEnabled)
                            .BindOnClicked(model => model.OnClickShopTill())
                            .SetHeight(35f);

                        row.AddLabel()
                            .BindText(model => model.ListCount)
                            .SetHeight(35f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetWidth(40f);
                                cell.SetIsVariable(false);

                                cell.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.ItemIconResrefs)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .BindTooltip(model => model.ItemNames);
                                });
                            });

                            template.AddCell(cell =>
                            {
                                cell.AddText()
                                    .BindText(model => model.ItemNames)
                                    .BindTooltip(model => model.ItemNames);
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(120f);

                                cell.AddButton()
                                    .BindText(model => model.ItemPriceNames)
                                    .BindOnClicked(model => model.OnClickChangePrice());
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(50f);

                                cell.AddCheckBox()
                                    .BindIsChecked(model => model.ItemListed)
                                    .BindIsEnabled(model => model.ListingCheckboxEnabled)
                                    .SetText("For Sale");
                            });

                            template.AddCell(cell =>
                            {
                                cell.SetWidth(50f);

                                cell.AddButton()
                                    .SetText("Remove")
                                    .BindOnClicked(model => model.OnClickRemove());
                            });
                        })
                            .BindRowCount(model => model.ItemNames)
                            .SetRowHeight(40f);
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
