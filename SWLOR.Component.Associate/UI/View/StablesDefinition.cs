using SWLOR.Component.Associate.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Associate.UI.View
{
    internal class StablesDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<StablesViewModel> _builder;

        public StablesDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<StablesViewModel>(_guiService);
        }

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
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetColor(GuiColor.HPColor)
                                        .SetTooltip("Hit Points - When these hit zero, your beast dies.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("FP")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetColor(GuiColor.FPColor)
                                        .SetTooltip("Force Points - Resource used to activate force abilities. Force sensitive beasts only.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("STM")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetColor(GuiColor.STMColor)
                                        .SetTooltip("Stamina - Resource used to activate non-force abilities.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("SP")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Skill Points - Used to purchase Perks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Level")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .BindTooltip(model => model.XPTooltip);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Might")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Might - Improves damage dealt by melee weapons, carrying capacity, and fortitude saving throws.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Perception")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Perception - Improves damage dealt by ranged and finesse weapons, increases physical accuracy, and reflex saving throws.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Vitality")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Vitality - Improves your max hit points and reduces damage received.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Willpower")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Willpower - Improves your force attack, force defense, max force points, and will saving throws.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Agility")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Agility - Improves ranged accuracy, evasion, and max stamina.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Social")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
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
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.FP)
                                        .SetColor(GuiColor.FPColor)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.STM)
                                        .SetColor(GuiColor.STMColor)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.SP)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Level)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Might)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Perception)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Vitality)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Willpower)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Agility)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Social)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                            });
                            rowRoot.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Main Hand")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Main Hand DMG - Baseline damage ratio before stats and target defenses are taken into account.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Off Hand")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Off Hand DMG - Baseline damage ratio before stats and target defenses are taken into account.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Attack")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Attack - Improves damage dealt by physical attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Accuracy")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Accuracy - Improves your chance to hit.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Evasion")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Evasion - Improves your ability to dodge attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Phys. DEF")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Physical Defense - Reduces the amount of damage taken by physical attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Force DEF")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Force Defense - Reduces the amount of damage taken by force attacks.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Elem. DEF")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Elemental Defenses - Reduces the amount of damage taken by elemental damage. (Order: Fire/Poison/Electrical/Ice)");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Role")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Role - Determines the role of your beast. Available perks vary based on this role.");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Sav. Throws")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                                        .SetTooltip("Saving Throws - Used to resist certain attacks. (Order: Fortitude, Reflex, Will)");
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                            });
                            rowRoot.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.MainHand)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.OffHand)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Attack)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Accuracy)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Evasion)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.PhysicalDefense)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ForceDefense)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ElementalDefense)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.Role)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.SavingThrows)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
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
                                .SetVerticalAlign(NuiVerticalAlignType.Top)
                                .SetHorizontalAlign(NuiHorizontalAlignType.Left);
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
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Accuracy")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Evasion")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Phys. Defense")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Force Defense")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Fire Defense")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Learning")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                            });
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.AttackPurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.AccuracyPurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.EvasionPurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.PhysicalDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ForceDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.FireDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.LearningPurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                            });
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Ice Defense")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Pois. Defense")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Elec. Defense")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Fortitude")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Reflex")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("Will")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });

                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .SetText("XP Penalty")
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                            });
                            rootRow.AddColumn(col =>
                            {
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.IceDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.PoisonDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ElectricalDefensePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.FortitudePurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.ReflexPurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.WillPurity)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                                });
                                col.AddRow(row =>
                                {
                                    row.AddLabel()
                                        .BindText(model => model.XPPenalty)
                                        .SetVerticalAlign(NuiVerticalAlignType.Top)
                                        .SetHorizontalAlign(NuiHorizontalAlignType.Left);
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
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
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
