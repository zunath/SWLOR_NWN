using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CharacterSheetDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterSheetViewModel> _builder = new();
        private const float IncreaseButtonSize = 14f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CharacterSheet)
                .SetInitialGeometry(0, 0, 800f, 400f)
                .SetTitle("Character Sheet")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Name)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.PortraitResref)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetAspect(NuiAspect.ExactScaled)
                            .SetWidth(128f)
                            .SetHeight(200f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Change Portrait")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickChangePortrait());
                        row.AddSpacer();
                    });
                })

                .AddColumn(col =>
                {

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.CharacterType)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("HP")
                            .SetColor(139, 0, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Hit Points - When these hit zero, you die.");

                        row.AddLabel()
                            .BindText(model => model.HP)
                            .SetColor(139, 0, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("FP")
                            .SetColor(0, 138, 250)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Force Points - Resource used to activate force abilities. Force sensitive characters only.");

                        row.AddLabel()
                            .BindText(model => model.FP)
                            .SetColor(0, 138, 250)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("STM")
                            .SetColor(0, 139, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Stamina - Resource used to activate non-force abilities.");

                        row.AddLabel()
                            .BindText(model => model.STM)
                            .SetColor(0, 139, 0)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Evasion")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Evasion - Improves your ability to dodge attacks.");

                        row.AddLabel()
                            .BindText(model => model.Evasion)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("SP")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Skill Points - Used to purchase Perks.");

                        row.AddLabel()
                            .BindText(model => model.SP)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("AP")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Ability Points - Used to increase your attributes.");

                        row.AddLabel()
                            .BindText(model => model.AP)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Might")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Might - Used to attack and defend in melee combat. Also affects carrying capacity.");

                        row.AddLabel()
                            .BindText(model => model.Might)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .BindIsVisible(model => model.IsMightUpgradeAvailable)
                            .BindOnClicked(model => model.OnClickUpgradeMight());
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Perception")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Perception - Used to attack and defend in ranged combat.");

                        row.AddLabel()
                            .BindText(model => model.Perception)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .BindIsVisible(model => model.IsPerceptionUpgradeAvailable)
                            .BindOnClicked(model => model.OnClickUpgradePerception());
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Vitality")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Vitality - Used to resist damage and increases total HP, FP, and STM.");

                        row.AddLabel()
                            .BindText(model => model.Vitality)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .BindIsVisible(model => model.IsVitalityUpgradeAvailable)
                            .BindOnClicked(model => model.OnClickUpgradeVitality());
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Willpower")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Willpower - Used to attack and defend in mental combat.");

                        row.AddLabel()
                            .BindText(model => model.Willpower)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .BindIsVisible(model => model.IsWillpowerUpgradeAvailable)
                            .BindOnClicked(model => model.OnClickUpgradeWillpower());
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Social")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Social - Used for diplomacy. Increases mission rewards and used in property management.");

                        row.AddLabel()
                            .BindText(model => model.Social)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .BindIsVisible(model => model.IsSocialUpgradeAvailable)
                            .BindOnClicked(model => model.OnClickUpgradeSocial());
                    });
                })

                .AddColumn(col =>
                {

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Race)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Attack")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Attack - Improves damage dealt by physical attacks.");

                        row.AddLabel()
                            .BindText(model => model.Attack)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("F. Attack")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Force Attack - Improves damage dealt by Force attacks.");

                        row.AddLabel()
                            .BindText(model => model.ForceAttack)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Phys. DEF")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Physical Defense - Reduces the amount of damage taken by physical attacks.");

                        row.AddLabel()
                            .BindText(model => model.DefensePhysical)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Force DEF")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Force Defense - Reduces the amount of damage taken by force attacks.");

                        row.AddLabel()
                            .BindText(model => model.DefenseForce)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Elem. DEF")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Elemental Defenses - Reduces the amount of damage taken by elemental damage. (Order: Fire/Poison/Electrical/Ice)");

                        row.AddLabel()
                            .BindText(model => model.DefenseElemental)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Control")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Control - Improves quality of crafted items. Also improves chance to auto-craft items. (Order: Smithery/Engineering/Fabrication/Agriculture)");

                        row.AddLabel()
                            .BindText(model => model.Control)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Craftsmanship")
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetTooltip("Craftsmanship - Improves progress of crafted items. Also improves chance to auto-craft items. (Order: Smithery/Engineering/Fabrication/Agriculture)");

                        row.AddLabel()
                            .BindText(model => model.Craftsmanship)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                })
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddColumn(col2 =>
                            {
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Skills")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickSkills());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Perks")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickPerks());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Quests")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickQuests());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Appearance")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickAppearance());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Recipes")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickRecipes());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Key Items")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickKeyItems());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Achievements")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickAchievements());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Notes")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickNotes());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Open Trash")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickOpenTrash());
                                });
                                col2.AddRow(row2 =>
                                {
                                    row2.AddButton()
                                        .SetText("Settings")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickSettings());
                                });
                            });
                            group.SetScrollbars(NuiScrollbars.Y);
                            group.SetWidth(130f);
                            group.SetShowBorder(false);
                        });
                    });
                })
                
                ;

            return _builder.Build();
        }
    }
}
