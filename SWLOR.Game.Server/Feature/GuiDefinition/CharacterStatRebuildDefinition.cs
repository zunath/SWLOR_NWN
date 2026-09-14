using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AttributeService;
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
                            .SetTooltip("Might - " + AttributeDescription.MightSummary)
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
                            .SetTooltip("Perception - " + AttributeDescription.PerceptionSummary)
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
                            .SetTooltip("Vitality - " + AttributeDescription.VitalitySummary)
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
                            .SetTooltip("Willpower - " + AttributeDescription.WillpowerSummary)
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
                            .SetTooltip("Agility - " + AttributeDescription.AgilitySummary)
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
                            .SetTooltip("Social - " + AttributeDescription.SocialSummary)
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
