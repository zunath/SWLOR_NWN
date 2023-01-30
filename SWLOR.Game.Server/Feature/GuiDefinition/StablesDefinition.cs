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
                                .BindValue(model => model.Name)
                                .BindIsEnabled(model => model.IsBeastSelected);

                            rowRoot.AddButton()
                                .SetText("Save")
                                .BindOnClicked(model => model.OnClickSaveName())
                                .SetHeight(35f)
                                .SetWidth(40f)
                                .BindIsEnabled(model => model.IsBeastSelected);
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
                                        .SetColor(GuiColor.HPColor)
                                        .SetTooltip("Hit Points - When these hit zero, your beast dies.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("FP")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetColor(GuiColor.FPColor)
                                        .SetTooltip("Force Points - Resource used to activate force abilities. Force sensitive beasts only.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("STM")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetColor(GuiColor.STMColor)
                                        .SetTooltip("Stamina - Resource used to activate non-force abilities.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("SP")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Skill Points - Used to purchase Perks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Level")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .BindTooltip(model => model.XPTooltip);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Might")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Might - Improves damage dealt by melee weapons, carrying capacity, and fortitude saving throws.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Perception")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Perception - Improves damage dealt by ranged and finesse weapons, increases physical accuracy, and reflex saving throws.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Vitality")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Vitality - Improves your max hit points and reduces damage received.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Willpower")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Willpower - Improves your force attack, force defense, max force points, and will saving throws.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Agility")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Agility - Improves ranged accuracy, evasion, and max stamina.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Social")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Social - Improves your XP gain and leadership capabilities.");
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
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Main Hand DMG - Baseline damage ratio before stats and target defenses are taken into account.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Off Hand")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Off Hand DMG - Baseline damage ratio before stats and target defenses are taken into account.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Attack")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Attack - Improves damage dealt by physical attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Accuracy")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Accuracy - Improves your chance to hit.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Evasion")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Evasion - Improves your ability to dodge attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Phys. DEF")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Physical Defense - Reduces the amount of damage taken by physical attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Force DEF")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Force Defense - Reduces the amount of damage taken by force attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Elem. DEF")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Elemental Defenses - Reduces the amount of damage taken by elemental damage. (Order: Fire/Poison/Electrical/Ice)");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Role")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Role - Determines the role of your beast. Available perks vary based on this role.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Sav. Throws")
                                        .SetVerticalAlign(NuiVerticalAlign.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                        .SetTooltip("Saving Throws - Used to resist certain attacks. (Order: Fortitude, Reflex, Will)");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("")
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
                                        .BindText(model => model.Role)
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
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("")
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

                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Learning")
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
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.LearningPurity)
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

                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("")
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
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("")
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
                                    .BindIsToggled(model => model.IsStatsToggled)
                                    .BindIsEnabled(model => model.IsBeastSelected);

                                row.AddToggleButton()
                                    .SetText("Perks")
                                    .SetHeight(32f)
                                    .SetWidth(75f)
                                    .BindOnClicked(model => model.OnClickPerks())
                                    .BindIsToggled(model => model.IsPerksToggled)
                                    .BindIsEnabled(model => model.IsBeastSelected);

                                row.AddToggleButton()
                                    .SetText("Purities")
                                    .SetHeight(32f)
                                    .SetWidth(75f)
                                    .BindOnClicked(model => model.OnClickPurities())
                                    .BindIsToggled(model => model.IsPuritiesToggled)
                                    .BindIsEnabled(model => model.IsBeastSelected);

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
                                    .BindText(model => model.ToggleMakeActiveButtonText)
                                    .BindOnClicked(model => model.OnClickToggleActive())
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsBeastSelected);

                                row.AddButton()
                                    .SetText("Release")
                                    .BindOnClicked(model => model.OnClickReleaseBeast())
                                    .SetHeight(32f)
                                    .BindIsEnabled(model => model.IsBeastSelected);

                                row.AddSpacer();
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
