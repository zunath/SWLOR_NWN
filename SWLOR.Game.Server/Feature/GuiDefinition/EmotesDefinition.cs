using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class EmotesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<EmotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Emotes)
                .SetInitialGeometry(0, 0, 400f, 400f)
                .SetTitle("Emotes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .BindText(model => model.CategoryAllName)
                            .BindOnClicked(model => model.OnSelectCategory(0))
                            .BindIsToggled(model => model.IsCategoryAllToggled)
                            .SetHeight(32f);

                        row.AddToggleButton()
                            .BindText(model => model.CategoryCombatName)
                            .BindOnClicked(model => model.OnSelectCategory(1))
                            .BindIsToggled(model => model.IsCategoryCombatToggled)
                            .SetHeight(32f);

                        row.AddToggleButton()
                            .BindText(model => model.CategoryExplorationName)
                            .BindOnClicked(model => model.OnSelectCategory(2))
                            .BindIsToggled(model => model.IsCategoryExplorationToggled)
                            .SetHeight(32f);

                        row.AddToggleButton()
                            .BindText(model => model.CategoryTasksName)
                            .BindOnClicked(model => model.OnSelectCategory(3))
                            .BindIsToggled(model => model.IsCategoryTasksToggled)
                            .SetHeight(32f);

                        row.AddToggleButton()
                            .BindText(model => model.CategorySocialName)
                            .BindOnClicked(model => model.OnSelectCategory(4))
                            .BindIsToggled(model => model.IsCategorySocialToggled)
                            .SetHeight(32f);

                        row.AddToggleButton()
                            .BindText(model => model.CategoryFeelingsName)
                            .BindOnClicked(model => model.OnSelectCategory(5))
                            .BindIsToggled(model => model.IsCategoryFeelingsToggled)
                            .SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.EmoteNames)
                                    .BindTooltip(model => model.EmoteDescriptions)
                                    .BindOnClicked(model => model.OnSelectEmote());
                            });
                        })
                        .BindRowCount(model => model.EmoteNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Reset")
                            .BindOnClicked(model => model.OnClickReset())
                            .SetWidth(100f);
                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
