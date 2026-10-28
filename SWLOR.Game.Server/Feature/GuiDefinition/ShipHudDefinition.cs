using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ShipHudDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipHudViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 380f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 32f;

        private static readonly GuiColor ShieldColor = new(3, 87, 152);
        private static readonly GuiColor HullColor = new(139, 0, 0);

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ShipHud)
                .SetInitialGeometry(10f, 100f, 430f, 470f)
                .SetTitle("Ship HUD")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ShipHudViewModel.ShipTabPartial, AddShipTab)
                .DefinePartialView(ShipHudViewModel.StatsTabPartial, AddStatsTab)
                .DefinePartialView(ShipHudViewModel.ScannerTabPartial, AddScannerTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Ship")
                        .AddOption("Stats")
                        .AddOption("Scanner")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(348f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(ShipHudViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        private static void AddTabShell(
            GuiGroup<ShipHudViewModel> host,
            System.Action<GuiColumn<ShipHudViewModel>> content)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content);
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddShipTab(GuiGroup<ShipHudViewModel> host)
        {
            AddTabShell(host, col =>
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Target")
                        .SetHeight(LabelHeight)
                        .SetWidth(60f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetColor(GuiColor.Cyan);
                    row.AddLabel()
                        .BindText(model => model.TargetName)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                AddTargetBar(col, "Shield", ShieldColor, model => model.TargetShieldProgress, model => model.TargetShieldText);
                AddTargetBar(col, "Hull", HullColor, model => model.TargetHullProgress, model => model.TargetHullText);

                col.AddRow(row =>
                {
                    row.AddButton()
                        .BindText(model => model.AllWeaponsText)
                        .BindIsEnabled(model => model.AllWeaponsEnabled)
                        .SetHeight(ButtonHeight)
                        .SetWidth(240f)
                        .SetTooltip("Switches every toggled weapon on, or off when they are all on. Also on the hotbar.")
                        .BindOnClicked(model => model.OnClickAllWeapons());
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.ModuleSummary)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetColor(GuiColor.Cyan);
                });

                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.ModuleNames)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.ModuleTargets)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(56f);
                            cell.AddProgressBar()
                                .BindValue(model => model.ModuleProgress);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.AddButton()
                                .SetText("Retarget")
                                .SetWidth(76f)
                                .BindIsEnabled(model => model.ModuleCanRetarget)
                                .SetTooltip("Locks this module onto your current target. With nothing selected it locks onto your own ship.")
                                .SetDisabledTooltip("This module always works on your current target.")
                                .BindOnClicked(model => model.OnClickRetarget());
                        });
                    })
                        .BindRowCount(model => model.ModuleNames)
                        .SetRowHeight(32f);
                });

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("No modules are on.")
                        .BindIsVisible(model => model.NoModulesVisible)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });
            });
        }

        private static void AddTargetBar(
            GuiColumn<ShipHudViewModel> col,
            string caption,
            GuiColor color,
            System.Linq.Expressions.Expression<System.Func<ShipHudViewModel, float>> progress,
            System.Linq.Expressions.Expression<System.Func<ShipHudViewModel, string>> text)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText(caption)
                    .SetHeight(LabelHeight)
                    .SetWidth(60f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddProgressBar()
                    .BindValue(progress)
                    .SetColor(color)
                    .SetHeight(LabelHeight);
                row.AddLabel()
                    .BindText(text)
                    .SetHeight(LabelHeight)
                    .SetWidth(100f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right);
            });
        }

        private static void AddScannerTab(GuiGroup<ShipHudViewModel> host)
        {
            AddTabShell(host, col =>
            {
                col.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.ScannerSummary)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetColor(GuiColor.Cyan);
                });

                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.SignatureNames)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.SignatureDetails)
                                .BindTooltip(model => model.SignatureTooltips)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(56f);
                            cell.AddProgressBar()
                                .BindValue(model => model.SignatureProgress);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(52f);
                            cell.AddButton()
                                .SetText("Jump")
                                .SetWidth(52f)
                                .BindIsEnabled(model => model.SignatureCanJump)
                                .SetTooltip("Micro-jumps to the site after a 5 second spool. Taking fire breaks it off.")
                                .SetDisabledTooltip("Your party must scan this signature to 100% first.")
                                .BindOnClicked(model => model.OnClickJump());
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(52f);
                            cell.AddButton()
                                .SetText("Board")
                                .SetWidth(52f)
                                .BindIsEnabled(model => model.SignatureCanBoard)
                                .SetTooltip("Docks a Small or Medium ship with the derelict and boards it (/board). Within 30 m, defenses destroyed.")
                                .SetDisabledTooltip("Only a boardable derelict your party has scanned to 100%.")
                                .BindOnClicked(model => model.OnClickBoard());
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(60f);
                            cell.AddButton()
                                .SetText("Salvage")
                                .SetWidth(60f)
                                .BindIsEnabled(model => model.SignatureCanBoard)
                                .SetTooltip("Sends your posted droids to strip the derelict (/salvage) while your ship holds within 60 m.")
                                .SetDisabledTooltip("Only a boardable derelict your party has scanned to 100%.")
                                .BindOnClicked(model => model.OnClickSalvage());
                        });
                    })
                        .BindRowCount(model => model.SignatureNames)
                        .SetRowHeight(32f);
                });

                col.AddRow(row =>
                {
                    row.AddButton()
                        .BindText(model => model.SweepText)
                        .BindIsEnabled(model => model.SweepEnabled)
                        .SetHeight(ButtonHeight)
                        .SetWidth(240f)
                        .SetTooltip("Pulses your Probe Launcher: lists every ship within its sweep range, cloaked ships included.")
                        .BindOnClicked(model => model.OnClickSweep());
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.ContactLines)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                    })
                        .BindRowCount(model => model.ContactLines)
                        .SetRowHeight(22f);
                });
            });
        }

        private static void AddStatsTab(GuiGroup<ShipHudViewModel> host)
        {
            AddTabShell(host, col =>
            {
                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.StatLabels)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(120f);
                            cell.AddLabel()
                                .BindText(model => model.StatValues)
                                .SetHorizontalAlign(NuiHorizontalAlign.Right);
                        });
                    })
                        .BindRowCount(model => model.StatLabels)
                        .SetRowHeight(22f);
                });
            });
        }
    }
}
