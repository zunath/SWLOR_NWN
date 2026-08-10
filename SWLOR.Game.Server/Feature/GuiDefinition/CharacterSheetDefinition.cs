using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CharacterSheetDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterSheetViewModel> _builder = new();
        private const float IncreaseButtonSize = 18f;
        private const float RailWidth = 165f;
        private const float ActionsWidth = 136f;
        private const float StatRowHeight = 22f;
        private const float TabPairWidth = 236f;
        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 76f;
        private const float AttributePanelWidth = 250f;
        private const float CombatPanelWidth = 250f;
        private const float StatsPanelWidth = 460f;
        private const float ResistancePanelWidth = 430f;
        private const float CraftingPanelWidth = 430f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CharacterSheet)
                .SetInitialGeometry(0, 0, 800f, 460f)
                .SetTitle("Character Sheet")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(CharacterSheetViewModel.AttributesTabPartial, AddAttributesTab)
                .DefinePartialView(CharacterSheetViewModel.StatsTabPartial, AddStatsTab)
                .DefinePartialView(CharacterSheetViewModel.ResistancesTabPartial, AddResistancesTab)
                .DefinePartialView(CharacterSheetViewModel.CraftingTabPartial, AddCraftingTab)
                .AddColumn(root =>
                {
                    root.AddRow(mainRow =>
                    {
                        mainRow.AddColumn(AddIdentityRail)
                            .SetWidth(RailWidth);

                        mainRow.AddColumn(AddTabbedDetailArea);

                        mainRow.AddColumn(AddActionsRail)
                            .SetWidth(ActionsWidth)
                            .BindIsVisible(model => model.IsPlayerMode);
                    });
                });

            return _builder.Build();
        }

        private static void AddIdentityRail(GuiColumn<CharacterSheetViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.Name)
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.Race)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .BindIsVisible(model => model.IsPlayerMode);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.CharacterType)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .BindIsVisible(model => model.IsPlayerMode);
            });

            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddImage()
                    .BindResref(model => model.PortraitResref)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetAspect(NuiAspect.ExactScaled)
                    .SetWidth(120f)
                    .SetHeight(170f);
                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.AddSpacer();
                row.AddButton()
                    .SetText("Customize")
                    .SetHeight(34f)
                    .SetWidth(140f)
                    .BindOnClicked(model => model.OnClickChangePortrait());
                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.AddGroup(group =>
                {
                    group.SetShowBorder(false);
                    group.SetScrollbars(NuiScrollbars.Auto);
                    group.AddColumn(resourceCol =>
                    {
                        AddBoundValueRow(resourceCol, "HP", model => model.HP, "Health. At 0, you die.", null, 36f, GuiColor.HPColor);
                        AddBoundValueRow(resourceCol, "FP", model => model.FP, "Force ability resource.", null, 36f, GuiColor.FPColor);
                        AddBoundValueRow(resourceCol, "STM", model => model.STM, "Non-Force ability resource.", null, 36f, GuiColor.STMColor);
                        AddBoundValueRow(resourceCol, "Ranks", model => model.SkillRanks, $"Skill ranks contributing to the {Skill.SkillCap}-rank limit.", null, 36f, null, model => model.ShowSkillRanks);
                        AddBoundValueRow(resourceCol, "SP", model => model.SP, "Perk purchase points.", null, 36f, null, model => model.ShowSP);
                        AddBoundValueRow(resourceCol, model => model.APOrLevelLabel, model => model.APOrLevel, null, model => model.APOrLevelTooltip, 36f, null, model => model.ShowAPOrLevel);
                    });
                });
            });
        }

        private static void AddTabbedDetailArea(GuiColumn<CharacterSheetViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddGroup(group =>
                {
                    group.SetShowBorder(false);
                    group.SetScrollbars(NuiScrollbars.Auto);
                    group.AddColumn(tabColumn =>
                    {
                        tabColumn.AddRow(tabRow =>
                        {
                            tabRow.SetHeight(TabRowHeight);
                            tabRow.AddToggles()
                                .AddOption("Attributes")
                                .AddOption("Stats")
                                .BindSelectedValue(model => model.TopTabId)
                                .SetWidth(TabPairWidth)
                                .SetHeight(TabRowHeight);
                        });

                        tabColumn.AddRow(tabRow =>
                        {
                            tabRow.SetHeight(TabRowHeight);
                            tabRow.AddToggles()
                                .AddOption("Resistances")
                                .AddOption("Crafting")
                                .BindSelectedValue(model => model.BottomTabId)
                                .SetWidth(TabPairWidth)
                                .SetHeight(TabRowHeight);
                        });
                    });
                })
                    .SetHeight(TabPanelHeight);
            });

            col.AddRow(row =>
            {
                row.AddGroup(group =>
                {
                    group.SetShowBorder(false);
                    group.SetScrollbars(NuiScrollbars.Auto);
                    group.AddColumn(contentCol =>
                    {
                        contentCol.AddRow(contentRow =>
                        {
                            contentRow.AddPartialView(CharacterSheetViewModel.TabContentPartialElement);
                        });
                    });
                });
            });
        }

        private static void AddAttributesTab(GuiGroup<CharacterSheetViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(attributeGroup =>
                    {
                        attributeGroup.SetScrollbars(NuiScrollbars.None);
                        attributeGroup.AddColumn(attributeCol =>
                        {
                            AddSectionHeader(attributeCol, "Attributes");
                            AddAttributeRow(attributeCol, "Might", model => model.Might, "Melee damage, STM, and carry weight.", model => model.IsMightUpgradeAvailable, model => model.OnClickUpgradeMight());
                            AddAttributeRow(attributeCol, "Perception", model => model.Perception, "Melee accuracy, ranged damage, and crit support.", model => model.IsPerceptionUpgradeAvailable, model => model.OnClickUpgradePerception());
                            AddAttributeRow(attributeCol, "Vitality", model => model.Vitality, "Max HP, HP regen, and physical toughness.", model => model.IsVitalityUpgradeAvailable, model => model.OnClickUpgradeVitality());
                            AddAttributeRow(attributeCol, "Willpower", model => model.Willpower, "Force attack, defense, and FP.", model => model.IsWillpowerUpgradeAvailable, model => model.OnClickUpgradeWillpower());
                            AddAttributeRow(attributeCol, "Agility", model => model.Agility, "Ranged accuracy and evasion.", model => model.IsAgilityUpgradeAvailable, model => model.OnClickUpgradeAgility());
                            AddAttributeRow(attributeCol, "Social", model => model.Social, "XP gain and leadership.", model => model.IsSocialUpgradeAvailable, model => model.OnClickUpgradeSocial());
                        });
                    })
                        .SetWidth(AttributePanelWidth);

                    row.AddGroup(combatGroup =>
                    {
                        combatGroup.SetScrollbars(NuiScrollbars.None);
                        combatGroup.AddColumn(combatCol =>
                        {
                            AddSectionHeader(combatCol, "Combat");
                            AddBoundValueRow(combatCol, "Main Hand", model => model.MainHandDMG, "Estimated main-hand weapon damage.", model => model.MainHandTooltip, 94f);
                            AddBoundValueRow(combatCol, "Off Hand", model => model.OffHandDMG, "Estimated off-hand weapon damage.", model => model.OffHandTooltip, 94f);
                            AddBoundValueRow(combatCol, "Atk Delay", model => model.AttackDelay, "Estimated time between auto attacks.", model => model.AttackDelayTooltip, 94f);
                            AddBoundValueRow(combatCol, "Attack", model => model.Attack, "Physical damage bonus.", null, 94f);
                    AddBoundValueRow(combatCol, "Force Attack", model => model.ForceAttack, "Force damage bonus.", null, 94f);
                            AddBoundValueRow(combatCol, "Accuracy", model => model.Accuracy, "Chance to hit.", null, 94f);
                            AddBoundValueRow(combatCol, "Evasion", model => model.Evasion, "Chance to dodge.", null, 94f);
                            AddBoundValueRow(combatCol, "Physical DEF", model => model.PhysicalDefense, "Defense against physical attacks.", null, 94f);
                            AddBoundValueRow(combatCol, "Force DEF", model => model.ForceDefense, "Defense against Force attacks.", null, 94f);
                        });
                    })
                        .SetWidth(CombatPanelWidth);
                });
            });
        }

        private static void AddStatsTab(GuiGroup<CharacterSheetViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(tableRow =>
                {
                    tableRow.AddGroup(table =>
                    {
                        table.SetScrollbars(NuiScrollbars.None);
                        table.AddColumn(tableCol =>
                        {
                            tableCol.AddRow(row =>
                            {
                                AddTableHeader(row, "STAT", 190f, "Character stat.");
                                AddTableHeader(row, "VALUE", 0f, "Current value.");
                            });

                            tableCol.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(190f);
                                        cell.AddLabel()
                                            .BindText(model => model.StatNames)
                                            .BindTooltip(model => model.StatTooltips)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.StatValues)
                                            .BindTooltip(model => model.StatTooltips)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                })
                                    .BindRowCount(model => model.StatNames)
                                    .SetRowHeight(24f)
                                    .SetShowBorders(false)
                                    .SetScrollbars(NuiScrollbars.Y);
                            });
                        });
                    })
                        .SetWidth(StatsPanelWidth);
                });
            });
        }

        private static void AddResistancesTab(GuiGroup<CharacterSheetViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(tableRow =>
                {
                    tableRow.AddGroup(table =>
                    {
                        table.SetScrollbars(NuiScrollbars.None);
                        table.AddColumn(tableCol =>
                        {
                            tableCol.AddRow(row =>
                            {
                                AddTableHeader(row, "TYPE", 90f, "Resistance family.");
                                AddTableHeader(row, "SCORE", 55f, "Higher reduces impact.");
                                AddTableHeader(row, "DAMAGE", 90f, "Damage received.");
                                AddTableHeader(row, "STATUS", 0f, "Status duration.");
                            });

                            tableCol.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(90f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResistanceNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(55f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResistanceScores)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(90f);
                                        cell.AddLabel()
                                            .BindText(model => model.ResistanceDamageTaken)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.ResistanceStatusDurations)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                })
                                    .BindRowCount(model => model.ResistanceNames)
                                    .SetRowHeight(24f)
                                    .SetShowBorders(false)
                                    .SetScrollbars(NuiScrollbars.Y);
                            });
                        });
                    })
                        .SetWidth(ResistancePanelWidth);
                });
            });
        }

        private static void AddCraftingTab(GuiGroup<CharacterSheetViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(tableRow =>
                {
                    tableRow.AddGroup(table =>
                    {
                        table.SetScrollbars(NuiScrollbars.None);
                        table.AddColumn(tableCol =>
                        {
                            tableCol.AddRow(row =>
                            {
                                AddTableHeader(row, "CRAFT", 135f, "Crafting skill.");
                                AddTableHeader(row, "CONTROL", 82f, "Craft quality and auto-craft chance.");
                                AddTableHeader(row, "CRAFTSMANSHIP", 0f, "Craft progress and auto-craft chance.");
                            });

                            tableCol.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(135f);
                                        cell.AddLabel()
                                            .BindText(model => model.CraftNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(82f);
                                        cell.AddLabel()
                                            .BindText(model => model.CraftControls)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });

                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.CraftCraftsmanship)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                                    });
                                })
                                    .BindRowCount(model => model.CraftNames)
                                    .SetRowHeight(28f)
                                    .SetShowBorders(false)
                                    .SetScrollbars(NuiScrollbars.Y);
                            });

                        });
                    })
                        .SetWidth(CraftingPanelWidth);
                });
            });
        }

        private static void AddActionsRail(GuiColumn<CharacterSheetViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddGroup(group =>
                {
                    group.SetScrollbars(NuiScrollbars.Y);
                    group.SetShowBorder(false);
                    group.AddColumn(actions =>
                    {
                        AddActionButton(actions, "Skills", model => model.OnClickSkills());
                        AddActionButton(actions, "Perks", model => model.OnClickPerks());
                        AddActionButton(actions, "Techniques", model => model.OnClickTechniques(), model => model.IsTechniquesEnabled);
                        AddActionButton(actions, "Recipes", model => model.OnClickRecipes());
                        AddActionButton(actions, "Quests", model => model.OnClickQuests());
                        AddActionButton(actions, "Open Trash", model => model.OnClickOpenTrash());
                        AddActionButton(actions, "Currencies", model => model.OnClickCurrencies());
                        AddActionButton(actions, "Achievements", model => model.OnClickAchievements());
                        AddActionButton(actions, "Notes", model => model.OnClickNotes());
                        AddActionButton(actions, "Appearance", model => model.OnClickAppearance());
                        AddActionButton(actions, "Disguises", model => model.OnClickDisguises());
                        AddActionButton(actions, "Settings", model => model.OnClickSettings());
                        AddActionButton(actions, "HoloCom", model => model.OnClickHoloCom(), model => model.IsHolocomEnabled);
                        AddActionButton(actions, "Key Items", model => model.OnClickKeyItems());
                        AddActionButton(actions, "Guide", model => model.OnClickGuide());
                    });
                })
                    .SetWidth(128f);
            });
        }

        private static void AddSectionHeader(GuiColumn<CharacterSheetViewModel> col, string text)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText(text)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddActionButton(
            GuiColumn<CharacterSheetViewModel> col,
            string text,
            Expression<Func<CharacterSheetViewModel, Action>> clickExpression,
            Expression<Func<CharacterSheetViewModel, bool>> enabledExpression = null)
        {
            col.AddRow(row =>
            {
                var button = row.AddButton()
                    .SetText(text)
                    .SetHeight(32f)
                    .SetWidth(104f)
                    .BindOnClicked(clickExpression);

                if (enabledExpression != null)
                {
                    button.BindIsEnabled(enabledExpression);
                }

                row.AddSpacer();
            });
        }

        private static void AddTableHeader(
            GuiRow<CharacterSheetViewModel> row,
            string text,
            float width,
            string tooltip = null)
        {
            var label = row.AddLabel()
                .SetText(text)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left);

            if (width > 0f)
            {
                label.SetWidth(width);
            }

            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                label.SetTooltip(tooltip);
            }
        }

        private static void AddAttributeRow(
            GuiColumn<CharacterSheetViewModel> col,
            string label,
            Expression<Func<CharacterSheetViewModel, int>> valueExpression,
            string tooltip,
            Expression<Func<CharacterSheetViewModel, bool>> upgradeVisibleExpression,
            Expression<Func<CharacterSheetViewModel, Action>> clickExpression)
        {
            const string attributeCapTooltip = " AP upgrades stop at 26. A racial bonus may raise one attribute to 27; that extra point remains part of combat formulas, while direct-effect scaling reaches its designed cap at 26.";

            col.AddRow(row =>
            {
                row.SetHeight(StatRowHeight);

                row.AddLabel()
                    .SetText(label)
                    .SetWidth(112f)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetTooltip(tooltip + attributeCapTooltip);

                row.AddLabel()
                    .BindText(valueExpression)
                    .SetWidth(42f)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                row.AddButton()
                    .SetWidth(IncreaseButtonSize)
                    .SetHeight(IncreaseButtonSize)
                    .SetText("+")
                    .BindIsVisible(upgradeVisibleExpression)
                    .BindOnClicked(clickExpression);
            });
        }

        private static void AddBoundValueRow<TValue>(
            GuiColumn<CharacterSheetViewModel> col,
            string label,
            Expression<Func<CharacterSheetViewModel, TValue>> valueExpression,
            string labelTooltip = null,
            Expression<Func<CharacterSheetViewModel, string>> valueTooltipExpression = null,
            float labelWidth = 72f,
            GuiColor color = null,
            Expression<Func<CharacterSheetViewModel, bool>> visibleExpression = null)
        {
            col.AddRow(row =>
            {
                row.SetHeight(StatRowHeight);

                var labelElement = row.AddLabel()
                    .SetText(label)
                    .SetWidth(labelWidth)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                if (!string.IsNullOrWhiteSpace(labelTooltip))
                {
                    labelElement.SetTooltip(labelTooltip);
                }

                if (color != null)
                {
                    labelElement.SetColor(color);
                }

                var value = row.AddLabel()
                    .BindText(valueExpression)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                if (!string.IsNullOrWhiteSpace(labelTooltip))
                {
                    value.SetTooltip(labelTooltip);
                }

                if (valueTooltipExpression != null)
                {
                    value.BindTooltip(valueTooltipExpression);
                }

                if (color != null)
                {
                    value.SetColor(color);
                }

                if (visibleExpression != null)
                {
                    row.BindIsVisible(visibleExpression);
                }
            });
        }

        private static void AddBoundValueRow<TLabel, TValue>(
            GuiColumn<CharacterSheetViewModel> col,
            Expression<Func<CharacterSheetViewModel, TLabel>> labelExpression,
            Expression<Func<CharacterSheetViewModel, TValue>> valueExpression,
            string labelTooltip = null,
            Expression<Func<CharacterSheetViewModel, string>> valueTooltipExpression = null,
            float labelWidth = 72f,
            GuiColor color = null,
            Expression<Func<CharacterSheetViewModel, bool>> visibleExpression = null)
        {
            col.AddRow(row =>
            {
                row.SetHeight(StatRowHeight);

                var label = row.AddLabel()
                    .BindText(labelExpression)
                    .SetWidth(labelWidth)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                if (!string.IsNullOrWhiteSpace(labelTooltip))
                {
                    label.SetTooltip(labelTooltip);
                }

                if (color != null)
                {
                    label.SetColor(color);
                }

                var value = row.AddLabel()
                    .BindText(valueExpression)
                    .SetVerticalAlign(NuiVerticalAlign.Top)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);

                if (!string.IsNullOrWhiteSpace(labelTooltip))
                {
                    value.SetTooltip(labelTooltip);
                }

                if (valueTooltipExpression != null)
                {
                    label.BindTooltip(valueTooltipExpression);
                    value.BindTooltip(valueTooltipExpression);
                }

                if (color != null)
                {
                    value.SetColor(color);
                }

                if (visibleExpression != null)
                {
                    row.BindIsVisible(visibleExpression);
                }
            });
        }
    }
}
