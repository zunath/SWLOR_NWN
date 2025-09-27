using SWLOR.Component.Character.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.View
{
    public class CharacterSheetDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<CharacterSheetViewModel> _builder;
        private const float IncreaseButtonSize = 14f;

        public CharacterSheetDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<CharacterSheetViewModel>(_guiService);
        }

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
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Center)
                            .SetAspect(NuiAspectType.ExactScaled)
                            .SetWidth(128f)
                            .SetHeight(200f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Customize")
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

                        row.BindIsVisible(model => model.IsPlayerMode);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("HP")
                            .SetColor(GuiColor.HPColor)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Hit Points - When these hit zero, you die.");

                        row.AddLabel()
                            .BindText(model => model.HP)
                            .SetColor(GuiColor.HPColor)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetColor(GuiColor.FPColor)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Force Points - Resource used to activate force abilities. Force sensitive characters only.");

                        row.AddLabel()
                            .BindText(model => model.FP)
                            .SetColor(GuiColor.FPColor)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetColor(GuiColor.STMColor)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Stamina - Resource used to activate non-force abilities.");

                        row.AddLabel()
                            .BindText(model => model.STM)
                            .SetColor(GuiColor.STMColor)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Skill Points - Used to purchase Perks.");

                        row.AddLabel()
                            .BindText(model => model.SP)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);
                        
                        row.BindIsVisible(model => model.ShowSP);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.APOrLevelLabel)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .BindTooltip(model => model.APOrLevelTooltip);

                        row.AddLabel()
                            .BindText(model => model.APOrLevel)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .SetIsVisible(false);

                            row.BindIsVisible(model => model.ShowAPOrLevel);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Might")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Might - Improves damage dealt by melee weapons, carrying capacity, and fortitude saving throws.");

                        row.AddLabel()
                            .BindText(model => model.Might)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Perception - Improves damage dealt by ranged and finesse weapons, increases physical accuracy, and reflex saving throws.");

                        row.AddLabel()
                            .BindText(model => model.Perception)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Vitality - Improves your max hit points and reduces damage received.");

                        row.AddLabel()
                            .BindText(model => model.Vitality)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Willpower - Improves your force attack, force defense, max force points, and will saving throws.");

                        row.AddLabel()
                            .BindText(model => model.Willpower)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetText("Agility")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Agility - Improves ranged accuracy, evasion, and max stamina.");

                        row.AddLabel()
                            .BindText(model => model.Agility)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

                        row.AddButton()
                            .SetWidth(IncreaseButtonSize)
                            .SetHeight(IncreaseButtonSize)
                            .SetText("+")
                            .BindIsVisible(model => model.IsAgilityUpgradeAvailable)
                            .BindOnClicked(model => model.OnClickUpgradeAgility());
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Social")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Social - Improves your XP gain and leadership capabilities.");

                        row.AddLabel()
                            .BindText(model => model.Social)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);

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
                            .SetText("Main Hand")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Main Hand DMG - Baseline damage ratio before stats and target defenses are taken into account.");

                        row.AddLabel()
                            .BindText(model => model.MainHandDMG)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .BindTooltip(model => model.MainHandTooltip);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Off Hand")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Off Hand DMG - Baseline damage ratio before stats and target defenses are taken into account.");

                        row.AddLabel()
                            .BindText(model => model.OffHandDMG)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .BindTooltip(model => model.OffHandTooltip);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Attack")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Attack - Improves damage dealt by physical attacks.");

                        row.AddLabel()
                            .BindText(model => model.Attack)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Accuracy")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Accuracy - Improves your chance to hit.");

                        row.AddLabel()
                            .BindText(model => model.Accuracy)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Evasion")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Evasion - Improves your ability to dodge attacks.");

                        row.AddLabel()
                            .BindText(model => model.Evasion)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Phys. DEF")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Physical Defense - Reduces the amount of damage taken by physical attacks.");

                        row.AddLabel()
                            .BindText(model => model.DefensePhysical)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Force DEF")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Force Defense - Reduces the amount of damage taken by force attacks.");

                        row.AddLabel()
                            .BindText(model => model.DefenseForce)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Elem. DEF")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Elemental Defenses - Reduces the amount of damage taken by elemental damage. (Order: Fire/Poison/Electrical/Ice)");

                        row.AddLabel()
                            .BindText(model => model.DefenseElemental)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Control")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Control - Improves quality of crafted items. Also improves chance to auto-craft items. (Order: Smithery/Engineering/Fabrication/Agriculture)");

                        row.AddLabel()
                            .BindText(model => model.Control)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Craftsmanship")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Craftsmanship - Improves progress of crafted items. Also improves chance to auto-craft items. (Order: Smithery/Engineering/Fabrication/Agriculture)");

                        row.AddLabel()
                            .BindText(model => model.Craftsmanship)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Sav. Throws")
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left)
                            .SetTooltip("Saving Throws - Used to resist certain attacks. (Order: Fortitude, Reflex, Will)");

                        row.AddLabel()
                            .BindText(model => model.SavingThrows)
                            .SetVerticalAlign(NuiVerticalAlignType.Top)
                            .SetHorizontalAlign(NuiHorizontalAlignType.Left);
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
                                        .SetText("HoloCom")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickHoloCom())
                                        .BindIsEnabled(model => model.IsHolocomEnabled);
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
                                        .SetText("Currencies")
                                        .SetHeight(32f)
                                        .SetWidth(100f)
                                        .BindOnClicked(model => model.OnClickCurrencies());
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
                            group.SetScrollbars(NuiScrollbarType.Y);
                            group.SetWidth(130f);
                            group.SetShowBorder(false);
                        });
                    });

                    col.BindIsVisible(model => model.IsPlayerMode);
                })
                
                
                ;

            return _builder.Build();
        }
    }
}
