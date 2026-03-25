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
                .SetInitialGeometry(0, 0, 350f, 350f)
                .SetTitle("Dice Bag")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.DiceCountText)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("-")
                            .SetWidth(35f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickRemoveDie());

                        row.AddButton()
                            .SetText("+")
                            .SetWidth(35f)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickAddDie());
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Skill")
                            .SetWidth(45f)
                            .SetHeight(26f)
                            .BindIsVisible(model => model.IsSkillSelectionVisible);

                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedSkillId)
                            .BindOptions(model => model.Skills)
                            .SetWidth(250f)
                            .BindIsVisible(model => model.IsSkillSelectionVisible);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d2").BindOnClicked(model => model.OnClickRollD2());
                        row.AddButton().SetText("d4").BindOnClicked(model => model.OnClickRollD4());
                        row.AddButton().SetText("d6").BindOnClicked(model => model.OnClickRollD6());
                        row.AddButton().SetText("d8").BindOnClicked(model => model.OnClickRollD8());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton().SetText("d10").BindOnClicked(model => model.OnClickRollD10());
                        row.AddButton().SetText("d20").BindOnClicked(model => model.OnClickRollD20());
                        row.AddButton().SetText("d100").BindOnClicked(model => model.OnClickRollD100());
                    });
                });

            return _builder.Build();
        }
    }
}
