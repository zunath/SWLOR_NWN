using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DiceDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DiceViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Dice)
                .SetInitialGeometry(0, 0, 190f, 430f)
                .SetTitle("Dice Bag")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("DICE")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.DiceCountText)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("-")
                            .SetWidth(28f)
                            .SetHeight(18f)
                            .BindOnClicked(model => model.OnClickRemoveDie());

                        row.AddButton()
                            .SetText("+")
                            .SetWidth(28f)
                            .SetHeight(18f)
                            .BindOnClicked(model => model.OnClickAddDie());

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsSkillSelectionVisible);
                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("Skill")
                            .SetWidth(35f)
                            .SetHeight(26f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsSkillSelectionVisible);
                        row.AddSpacer();

                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedSkillId)
                            .BindOptions(model => model.Skills)
                            .SetWidth(120f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d2").SetHeight(18f).BindOnClicked(model => model.OnClickRollD2());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d4").SetHeight(18f).BindOnClicked(model => model.OnClickRollD4());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d6").SetHeight(18f).BindOnClicked(model => model.OnClickRollD6());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d8").SetHeight(18f).BindOnClicked(model => model.OnClickRollD8());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d10").SetHeight(18f).BindOnClicked(model => model.OnClickRollD10());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d20").SetHeight(18f).BindOnClicked(model => model.OnClickRollD20());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d100").SetHeight(18f).BindOnClicked(model => model.OnClickRollD100());
                    });
                });

            return _builder.Build();
        }
    }
}
