using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class StablesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<StablesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Stables)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Stables")

                .DefinePartialView(StablesViewModel.PartialViewStats, group =>
                {
                    group.AddColumn(root =>
                    {
                        root.AddRow(rowRoot =>
                        {
                            rowRoot.AddTextEdit()
                                .SetPlaceholder("Name")
                                .SetMaxLength(30)
                                .BindValue(model => model.Name);

                            rowRoot.AddButton()
                                .SetText("Save")
                                .BindOnClicked(model => model.OnClickSaveName())
                                .SetHeight(35f)
                                .SetWidth(40f);
                        });

                        root.AddRow(rowRoot =>
                        {

                            rowRoot.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("HP")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetColor(GuiColor.HPColor);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("FP")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetColor(GuiColor.FPColor);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("STM")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetColor(GuiColor.STMColor);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("SP")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Level")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Might")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Perception")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Vitality")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Willpower")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Agility")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Social")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                            rowRoot.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.HP)
                                        .SetColor(GuiColor.HPColor)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.FP)
                                        .SetColor(GuiColor.FPColor)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.STM)
                                        .SetColor(GuiColor.STMColor)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.SP)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Level)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Might)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Perception)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Vitality)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Willpower)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Agility)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Social)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                            rowRoot.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Main Hand")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Off Hand")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Attack")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Accuracy")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Evasion")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Phys. DEF")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Force DEF")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Elem. DEF")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Control")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Craftsmanship")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Sav. Throws")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                            rowRoot.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.MainHand)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.OffHand)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Attack)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Accuracy)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Evasion)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.PhysicalDefense)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ForceDefense)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ElementalDefense)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Control)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Craftsmanship)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.SavingThrows)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                        });
                    });
                })

                .DefinePartialView(StablesViewModel.PartialViewPerks, group =>
                {
                    group.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.PerkNames)
                                .SetVerticalAlign(NuiVerticalAlign.Top)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                    })
                        .BindRowCount(model => model.PerkNames);
                })

                .DefinePartialView(StablesViewModel.PartialViewPurities, group =>
                {
                    group.AddColumn(root =>
                    {
                        root.AddRow(rootRow =>
                        {
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Attack")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Accuracy")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Evasion")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Phys. Defense")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Force Defense")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Fire Defense")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.AttackPurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.AccuracyPurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.EvasionPurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.PhysicalDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ForceDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.FireDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Ice Defense")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Pois. Defense")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Elec. Defense")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Fortitude")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Reflex")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Will")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.IceDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.PoisonDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ElectricalDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.FortitudePurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ReflexPurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.WillPurity)
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                });
                            });
                        });
                    });
                })
                
                .AddColumn(rootCol =>
                {
                    rootCol.AddRow(rootRow =>
                    {
                        rootRow.AddLabel()
                            .SetHeight(30f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .BindText(model => model.Instructions)
                            .BindColor(model => model.InstructionsColor);
                    });

                    rootCol.AddRow(root =>
                    {
                        root.AddColumn(col =>
                        {
                            col.SetWidth(250f);

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .BindText(model => model.BeastCount)
                                    .SetHeight(32f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindText(model => model.BeastNames)
                                                .BindOnClicked(model => model.OnClickBeast())
                                                .BindIsToggled(model => model.BeastToggles)
                                                .BindColor(model => model.BeastNameColors);
                                        });
                                    })
                                    .BindRowCount(model => model.BeastNames);
                            });
                        });

                        root.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddSpacer();

                                row.AddToggleButton()
                                    .SetText("Stats")
                                    .SetHeight(32f)
                                    .SetWidth(75f)
                                    .BindOnClicked(model => model.OnClickStats())
                                    .BindIsToggled(model => model.IsStatsToggled);

                                row.AddToggleButton()
                                    .SetText("Perks")
                                    .SetHeight(32f)
                                    .SetWidth(75f)
                                    .BindOnClicked(model => model.OnClickPerks())
                                    .BindIsToggled(model => model.IsPerksToggled);

                                row.AddToggleButton()
                                    .SetText("Purities")
                                    .SetHeight(32f)
                                    .SetWidth(75f)
                                    .BindOnClicked(model => model.OnClickPurities())
                                    .BindIsToggled(model => model.IsPuritiesToggled);

                                row.AddSpacer();
                            });

                            col.AddRow(row =>
                            {
                                row.AddPartialView(StablesViewModel.BeastDetailsPartial);
                            });

                            col.AddRow(row =>
                            {
                                row.AddSpacer();

                                row.AddButton()
                                    .SetText("Make Active")
                                    .BindOnClicked(model => model.OnClickMakeActive())
                                    .SetHeight(32f);

                                row.AddButton()
                                    .SetText("Release")
                                    .BindOnClicked(model => model.OnClickReleaseBeast())
                                    .SetHeight(32f);

                                row.AddSpacer();
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
