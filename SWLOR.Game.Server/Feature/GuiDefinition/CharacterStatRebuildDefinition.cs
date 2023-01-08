using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CharacterStatRebuildDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterStatRebuildViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.StatRebuild)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Stat Rebuild")

                .AddColumn(col =>
                {
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
                            .SetWidth(32f);

                        row.AddLabel()
                            .BindText(model => model.Might)
                            .SetTooltip("Might - Improves damage dealt by melee weapons and increases carrying capacity.")
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("+")
                            .BindOnClicked(model => model.OnClickAddMight())
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("-")
                            .BindOnClicked(model => model.OnClickSubtractPerception())
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddLabel()
                            .BindText(model => model.Perception)
                            .SetTooltip("Perception - Improves damage dealt by ranged and finesse weapons and increases physical accuracy.")
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("+")
                            .BindOnClicked(model => model.OnClickAddPerception())
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("-")
                            .BindOnClicked(model => model.OnClickSubtractVitality())
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddLabel()
                            .BindText(model => model.Vitality)
                            .SetTooltip("Vitality - Improves your max hit points and reduces damage received.")
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("+")
                            .BindOnClicked(model => model.OnClickAddVitality())
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("-")
                            .BindOnClicked(model => model.OnClickSubtractWillpower())
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddLabel()
                            .BindText(model => model.Willpower)
                            .SetTooltip("Willpower - Improves your force attack, force defense, and max force points.")
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("+")
                            .BindOnClicked(model => model.OnClickAddWillpower())
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("-")
                            .BindOnClicked(model => model.OnClickSubtractAgility())
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddLabel()
                            .BindText(model => model.Agility)
                            .SetTooltip("Agility - Improves ranged accuracy, evasion, and max stamina.")
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("+")
                            .BindOnClicked(model => model.OnClickAddAgility())
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("-")
                            .BindOnClicked(model => model.OnClickSubtractSocial())
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddLabel()
                            .BindText(model => model.Social)
                            .SetTooltip("Social - Improves your XP gain and leadership capabilities.")
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("+")
                            .BindOnClicked(model => model.OnClickAddSocial())
                            .SetHeight(32f)
                            .SetWidth(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Save Changes")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickSave());
                        row.AddSpacer();
                    });

                });

            return _builder.Build();
        }
    }
}
