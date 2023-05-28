using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CharacterFullRebuildDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterFullRebuildViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CharacterMigration)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Character Rebuild")

                .AddRow(wrapperRow =>
                {
                    wrapperRow.AddGroup(wrapper =>
                    {
                        wrapper.SetShowBorder(false);

                        wrapper.AddColumn(col =>
                        {
                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("Reset Character")
                                    .SetColor(255, 0, 0)
                                    .SetHeight(32f)
                                    .SetWidth(564f)
                                    .BindOnClicked(model => model.OnClickResetEverything());
                            });

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("Character Type:")
                                    .SetHeight(20f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddSpacer();
                                row.AddOptions()
                                    .AddOption("Standard")
                                    .AddOption("Force Sensitive")
                                    .SetDirection(NuiDirection.Horizontal)
                                    .BindSelectedValue(model => model.CharacterType)
                                    .BindIsEnabled(model => model.CanDistribute);
                                row.AddSpacer();
                            });

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .BindText(model => model.RemainingAbilityPoints)
                                    .SetHeight(32f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickSubtractMight())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);

                                row.AddLabel()
                                    .BindText(model => model.Might)
                                    .SetTooltip(
                                        "Might - Improves damage dealt by melee weapons and increases carrying capacity.")
                                    .SetHeight(32f)
                                    .SetWidth(500f);

                                row.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickAddMight())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickSubtractPerception())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);

                                row.AddLabel()
                                    .BindText(model => model.Perception)
                                    .SetTooltip(
                                        "Perception - Improves damage dealt by ranged and finesse weapons and increases physical accuracy.")
                                    .SetHeight(32f)
                                    .SetWidth(500f);

                                row.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickAddPerception())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickSubtractVitality())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);

                                row.AddLabel()
                                    .BindText(model => model.Vitality)
                                    .SetTooltip("Vitality - Improves your max hit points and reduces damage received.")
                                    .SetHeight(32f)
                                    .SetWidth(500f);

                                row.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickAddVitality())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickSubtractWillpower())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);

                                row.AddLabel()
                                    .BindText(model => model.Willpower)
                                    .SetTooltip(
                                        "Willpower - Improves your force attack, force defense, and max force points.")
                                    .SetHeight(32f)
                                    .SetWidth(500f);

                                row.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickAddWillpower())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickSubtractAgility())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);

                                row.AddLabel()
                                    .BindText(model => model.Agility)
                                    .SetTooltip("Agility - Improves ranged accuracy, evasion, and max stamina.")
                                    .SetHeight(32f)
                                    .SetWidth(500f);

                                row.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickAddAgility())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);
                            });

                            col.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickSubtractSocial())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);

                                row.AddLabel()
                                    .BindText(model => model.Social)
                                    .SetTooltip("Social - Improves your XP gain and leadership capabilities.")
                                    .SetHeight(32f)
                                    .SetWidth(500f);

                                row.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickAddSocial())
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.CanDistribute);
                            });

                            col.AddRow(row =>
                            {
                                row.AddLabel()
                                    .BindText(model => model.RemainingSkillPoints)
                                    .SetHeight(20f);
                            });

                            col.AddRow(row =>
                            {
                                row.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.SetIsVariable(false);
                                            cell.AddButton()
                                                .SetText("-10")
                                                .BindOnClicked(model => model.OnClickSubtractTenSkillPoints())
                                                .SetWidth(32f)
                                                .SetHeight(32f)
                                                .BindIsEnabled(model => model.CanDistribute);
                                        });
                                        template.AddCell(cell =>
                                        {
                                            cell.SetIsVariable(false);
                                            cell.AddButton()
                                                .SetText("-1")
                                                .BindOnClicked(model => model.OnClickSubtractOneSkillPoint())
                                                .SetWidth(32f)
                                                .SetHeight(32f)
                                                .BindIsEnabled(model => model.CanDistribute);
                                        });

                                        template.AddCell(cell =>
                                        {
                                            cell.SetIsVariable(false);
                                            cell.SetWidth(300f);

                                            cell.AddLabel()
                                                .BindText(model => model.SkillNames)
                                                .BindTooltip(model => model.SkillTooltips)
                                                .SetHeight(32f);
                                        });

                                        template.AddCell(cell =>
                                        {
                                            cell.SetIsVariable(false);
                                            cell.AddButton()
                                                .SetText("+1")
                                                .BindOnClicked(model => model.OnClickAddOneSkillPoint())
                                                .SetWidth(32f)
                                                .SetHeight(32f)
                                                .BindIsEnabled(model => model.CanDistribute);
                                        });

                                        template.AddCell(cell =>
                                        {
                                            cell.SetIsVariable(false);
                                            cell.AddButton()
                                                .SetText("+10")
                                                .BindOnClicked(model => model.OnClickAddTenSkillPoints())
                                                .SetWidth(32f)
                                                .SetHeight(32f)
                                                .BindIsEnabled(model => model.CanDistribute);
                                        });
                                    })
                                    .BindRowCount(model => model.SkillNames);
                            });

                            col.AddRow(row =>
                            {
                                row.AddSpacer();
                                row.AddButton()
                                    .SetText("Save Changes")
                                    .SetHeight(32f)
                                    .BindOnClicked(model => model.OnClickSave())
                                    .BindIsEnabled(model => model.CanDistribute);
                                row.AddSpacer();
                            });
                        });
                    }); ;
                });

            return _builder.Build();
        }
    }
}
