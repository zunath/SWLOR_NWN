using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class KeyItemsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<KeyItemsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.KeyItems)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 760f, 420f)
                .SetTitle("Key Items")
                .AddColumn(shell =>
                {
                    shell.AddRow(root =>
                    {
                        root.AddColumn(browser =>
                        {
                            browser.AddRow(row =>
                            {
                                row.AddSpacer();
                                var comboBox = row.AddComboBox()
                                    .BindSelectedIndex(model => model.SelectedCategoryId)
                                    .SetWidth(260f);

                                comboBox.AddOption("<All Types>", 0);
                                foreach (var (type, detail) in KeyItem.GetActiveCategories())
                                {
                                    comboBox.AddOption(detail.Name, (int)type);
                                }

                                row.AddSpacer();
                            });

                            browser.AddRow(row =>
                            {
                                row.AddSpacer()
                                    .SetWidth(40f);

                                row.AddLabel()
                                    .SetText("Key Item")
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                    .SetVerticalAlign(NuiVerticalAlign.Top);

                                row.SetHeight(20f);
                            });

                            browser.AddRow(row =>
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
                                                .BindResref(model => model.Icons)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                                .SetAspect(NuiAspect.Fit);
                                        })
                                            .SetScrollbars(NuiScrollbars.None);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.Names)
                                            .BindIsToggled(model => model.Selections)
                                            .BindOnClicked(model => model.OnSelectKeyItem());
                                    });
                                })
                                    .SetRowHeight(40f)
                                    .BindRowCount(model => model.Names);
                            });

                            browser.AddPagination(
                                model => model.PageNumbers,
                                model => model.SelectedPageIndex,
                                model => model.OnClickPreviousPage(),
                                model => model.OnClickNextPage());
                        });

                        root.AddColumn(details =>
                        {
                            details.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("DETAILS")
                                    .SetHeight(18f)
                                    .SetColor(221, 181, 93)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Top);
                            });

                            details.AddRow(header =>
                            {
                                header.SetHeight(72f);

                                header.AddGroup(group =>
                                {
                                    group.AddImage()
                                        .BindResref(model => model.SelectedIcon)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                                        .SetAspect(NuiAspect.Fit);
                                })
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetWidth(64f)
                                    .SetHeight(64f);

                                header.AddColumn(metadata =>
                                {
                                    metadata.AddRow(row =>
                                    {
                                        row.AddText()
                                            .BindText(model => model.SelectedName)
                                            .SetShowBorder(false)
                                            .SetScrollbars(NuiScrollbars.None)
                                            .SetHeight(42f);
                                    });

                                    metadata.AddRow(row =>
                                    {
                                        row.AddLabel()
                                            .BindText(model => model.SelectedType)
                                            .SetColor(142, 153, 148)
                                            .SetHeight(20f)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Top);
                                    });
                                });
                            });

                            details.AddRow(row =>
                            {
                                row.AddText()
                                    .BindText(model => model.SelectedDescription)
                                    .SetShowBorder(true)
                                    .SetScrollbars(NuiScrollbars.Auto);
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
