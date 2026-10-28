using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Planetary Holdings (plan section 26), opened from an NPC starport's Dock Master or a holding site's terminal: the
    /// Holdings tab runs the player's holdings (collect, deliver, upgrade, the Processing Plant, droid staff, upkeep and
    /// events); the Buy tab sells this dock's holding types.
    /// </summary>
    public class PlanetaryHoldingsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PlanetaryHoldingsViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 560f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 32f;
        private const float ButtonWidth = 130f;
        private const float NumberWidth = 64f;
        private const float WideWidth = 110f;
        private const float RowButtonWidth = 76f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.PlanetaryHoldings)
                .SetInitialGeometry(0, 0, 610f, 640f)
                .SetTitle("Planetary Holdings")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(PlanetaryHoldingsViewModel.HoldingsTabPartial, AddHoldingsTab)
                .DefinePartialView(PlanetaryHoldingsViewModel.BuyTabPartial, AddBuyTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("My Holdings")
                        .AddOption("Buy")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(232f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(PlanetaryHoldingsViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<PlanetaryHoldingsViewModel> host, System.Action<GuiColumn<PlanetaryHoldingsViewModel>> content)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content);
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddLabelRow(GuiColumn<PlanetaryHoldingsViewModel> content,
            System.Linq.Expressions.Expression<System.Func<PlanetaryHoldingsViewModel, string>> text,
            System.Linq.Expressions.Expression<System.Func<PlanetaryHoldingsViewModel, bool>> visible = null,
            GuiColor color = null)
        {
            content.AddRow(row =>
            {
                var label = row.AddLabel()
                    .BindText(text)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                if (visible != null)
                    label.BindIsVisible(visible);
                if (color != null)
                    label.SetColor(color);
            });
        }

        private static void AddHoldingsTab(GuiGroup<PlanetaryHoldingsViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddLabelRow(content, model => model.DockName, color: GuiColor.Cyan);
                AddLabelRow(content, model => model.CreditsText);

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Holding")
                        .SetWidth(70f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    row.AddComboBox()
                        .BindOptions(model => model.HoldingOptions)
                        .BindSelectedIndex(model => model.SelectedHoldingIndex)
                        .BindIsEnabled(model => model.HasHolding)
                        .SetWidth(330f)
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Ship")
                        .SetWidth(70f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    row.AddComboBox()
                        .BindOptions(model => model.ShipOptions)
                        .BindSelectedIndex(model => model.SelectedShipIndex)
                        .BindIsEnabled(model => model.HasShip)
                        .SetTooltip("Your ships docked here. Collecting fills, and deliveries come from, the cargo bay of the ship you pick.")
                        .SetWidth(330f)
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });

                AddLabelRow(content, model => model.CargoText);
                AddLabelRow(content, model => model.AwayText, model => model.AwayVisible, GuiColor.Red);
                AddLabelRow(content, model => model.SummaryText, color: GuiColor.Cyan);
                AddLabelRow(content, model => model.OutputText);
                AddLabelRow(content, model => model.StockText);
                AddLabelRow(content, model => model.UpkeepText);
                AddLabelRow(content, model => model.DroidText);
                AddLabelRow(content, model => model.EventText, model => model.EventVisible, new GuiColor(255, 200, 0));

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Collect")
                        .BindIsEnabled(model => model.CanCollect)
                        .SetTooltip("Loads the warehouse into the ship you picked, as much as fits, paying the upkeep owed.")
                        .BindOnClicked(model => model.OnClickCollect())
                        .SetWidth(ButtonWidth)
                        .SetHeight(ButtonHeight);
                    row.AddButton()
                        .SetText("Pay Upkeep")
                        .BindIsEnabled(model => model.CanPayUpkeep)
                        .SetTooltip("Pays the upkeep owed. Unpaid upkeep loses the holding.")
                        .BindOnClicked(model => model.OnClickPayUpkeep())
                        .SetWidth(ButtonWidth)
                        .SetHeight(ButtonHeight);
                    row.AddButton()
                        .SetText("Visit Site")
                        .BindIsEnabled(model => model.CanVisit)
                        .SetTooltip("Walk to the holding's site office.")
                        .BindOnClicked(model => model.OnClickVisit())
                        .SetWidth(ButtonWidth)
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Pay Fine")
                        .BindIsVisible(model => model.FineVisible)
                        .SetTooltip("Pays the customs fine so the inspectors leave the banned stock alone.")
                        .BindOnClicked(model => model.OnClickPayFine())
                        .SetWidth(ButtonWidth)
                        .SetHeight(ButtonHeight);
                    row.AddLabel()
                        .BindText(model => model.FineText)
                        .BindIsVisible(model => model.FineVisible)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddNeedList(content);
                AddUpgradeRows(content);
                AddDroidRow(content);

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Recent events")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
                content.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.LogLines)
                                .BindTooltip(model => model.LogLines)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                    })
                        .BindRowCount(model => model.LogLines)
                        .SetRowHeight(24f);
                    row.SetHeight(110f);
                });

                content.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Abandon")
                        .BindIsEnabled(model => model.HasHolding)
                        .SetTooltip("Give the holding up for part of its price back, less the upkeep owed. Its stock is lost.")
                        .BindOnClicked(model => model.OnClickAbandon())
                        .SetWidth(ButtonWidth)
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });
            });
        }

        private static void AddNeedList(GuiColumn<PlanetaryHoldingsViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel().SetText("Deliveries wanted").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Have").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddLabel().SetText("Aboard").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                row.AddSpacer().SetWidth(RowButtonWidth + 8f);
            });

            content.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.AddLabel()
                            .BindText(model => model.NeedNames)
                            .BindTooltip(model => model.NeedTooltips)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NumberWidth);
                        cell.AddLabel()
                            .BindText(model => model.NeedProgress)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(NumberWidth);
                        cell.AddLabel()
                            .BindText(model => model.NeedAboard)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(RowButtonWidth);
                        cell.AddButton()
                            .SetText("Deliver")
                            .BindIsEnabled(model => model.NeedCanDeliver)
                            .SetTooltip("Unloads as many as the holding still takes from the ship you picked.")
                            .BindOnClicked(model => model.OnClickDeliver());
                    });
                })
                    .BindRowCount(model => model.NeedNames)
                    .SetRowHeight(32f);
                row.SetHeight(140f);
            });
        }

        private static void AddUpgradeRows(GuiColumn<PlanetaryHoldingsViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Upgrade")
                    .BindIsEnabled(model => model.CanUpgrade)
                    .BindTooltip(model => model.UpgradeText)
                    .BindOnClicked(model => model.OnClickUpgrade())
                    .SetWidth(ButtonWidth)
                    .SetHeight(ButtonHeight);
                row.AddLabel()
                    .BindText(model => model.UpgradeText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Build Plant")
                    .BindIsEnabled(model => model.CanBuildPlant)
                    .BindTooltip(model => model.PlantText)
                    .BindOnClicked(model => model.OnClickBuildPlant())
                    .SetWidth(ButtonWidth)
                    .SetHeight(ButtonHeight);
                row.AddLabel()
                    .BindText(model => model.PlantText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row =>
            {
                row.AddCheckBox()
                    .SetText("Refine the output in the Processing Plant")
                    .BindIsChecked(model => model.RefineChecked)
                    .BindIsEnabled(model => model.RefineEnabled)
                    .SetHeight(26f);
                row.AddSpacer();
            });
        }

        private static void AddDroidRow(GuiColumn<PlanetaryHoldingsViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Droid")
                    .SetWidth(70f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddComboBox()
                    .BindOptions(model => model.DroidOptions)
                    .BindSelectedIndex(model => model.SelectedDroidIndex)
                    .BindIsEnabled(model => model.CanAssignDroid)
                    .SetTooltip("Your droids that are not following you or posted elsewhere.")
                    .SetWidth(200f)
                    .SetHeight(ButtonHeight);
                row.AddButton()
                    .SetText("Assign")
                    .BindIsEnabled(model => model.CanAssignDroid)
                    .SetTooltip("Posts the droid as the holding's staff: its Industry raises the output. It stays at its post until recalled.")
                    .BindOnClicked(model => model.OnClickAssignDroid())
                    .SetWidth(RowButtonWidth)
                    .SetHeight(ButtonHeight);
                row.AddButton()
                    .SetText("Recall")
                    .BindIsEnabled(model => model.CanRecallDroid)
                    .SetTooltip("Brings the staff droid back to you.")
                    .BindOnClicked(model => model.OnClickRecallDroid())
                    .SetWidth(RowButtonWidth)
                    .SetHeight(ButtonHeight);
                row.AddSpacer();
            });
        }

        private static void AddBuyTab(GuiGroup<PlanetaryHoldingsViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddLabelRow(content, model => model.DockName, color: GuiColor.Cyan);
                AddLabelRow(content, model => model.CreditsText);
                AddLabelRow(content, model => model.OwnedText);

                content.AddRow(row =>
                {
                    row.AddLabel().SetText("Holding").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                    row.AddLabel().SetText("Makes a day").SetWidth(WideWidth + 40f).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                    row.AddLabel().SetText("Price").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                    row.AddLabel().SetText("Upkeep").SetWidth(NumberWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Right).SetColor(GuiColor.Cyan);
                    row.AddSpacer().SetWidth(RowButtonWidth + 8f);
                });

                content.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.BuyNames)
                                .BindTooltip(model => model.BuyTooltips)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(WideWidth + 40f);
                            cell.AddLabel()
                                .BindText(model => model.BuyOutputs)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(NumberWidth);
                            cell.AddLabel()
                                .BindText(model => model.BuyPrices)
                                .SetHorizontalAlign(NuiHorizontalAlign.Right);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(NumberWidth);
                            cell.AddLabel()
                                .BindText(model => model.BuyUpkeeps)
                                .SetHorizontalAlign(NuiHorizontalAlign.Right);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(RowButtonWidth);
                            cell.AddButton()
                                .SetText("Buy")
                                .BindIsEnabled(model => model.BuyCanBuy)
                                .SetTooltip("Buys the holding. Its output waits in this dock's warehouse for your ship.")
                                .BindOnClicked(model => model.OnClickBuy());
                        });
                    })
                        .BindRowCount(model => model.BuyNames)
                        .SetRowHeight(32f);
                    row.SetHeight(160f);
                });

                content.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.BuyNote)
                        .SetHeight(44f);
                });
            });
        }
    }
}
