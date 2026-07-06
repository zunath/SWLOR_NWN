using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class EmotesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<EmotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Emotes);
            window.SetInitialGeometry(0, 0, 340f, 400f)
                .SetTitle("Emotes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)

                .AddColumn(col =>
                {
                    // Riga ricerca
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(30f)
                            .SetWidth(30f)
                            .BindOnClicked(model => model.OnClickClearSearch());
                    });

                    // Riga categorie: quadratini con la lettera iniziale
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("A")
                            .BindOnClicked(model => model.OnSelectCategory(0))
                            .BindIsToggled(model => model.IsCategoryAllToggled)
                            .SetWidth(30f)
                            .SetHeight(30f);

                        row.AddToggleButton()
                            .SetText("C")
                            .BindOnClicked(model => model.OnSelectCategory(1))
                            .BindIsToggled(model => model.IsCategoryCombatToggled)
                            .SetWidth(30f)
                            .SetHeight(30f);

                        row.AddToggleButton()
                            .SetText("E")
                            .BindOnClicked(model => model.OnSelectCategory(2))
                            .BindIsToggled(model => model.IsCategoryExplorationToggled)
                            .SetWidth(30f)
                            .SetHeight(30f);

                        row.AddToggleButton()
                            .SetText("T")
                            .BindOnClicked(model => model.OnSelectCategory(3))
                            .BindIsToggled(model => model.IsCategoryTasksToggled)
                            .SetWidth(30f)
                            .SetHeight(30f);

                        row.AddToggleButton()
                            .SetText("S")
                            .BindOnClicked(model => model.OnSelectCategory(4))
                            .BindIsToggled(model => model.IsCategorySocialToggled)
                            .SetWidth(30f)
                            .SetHeight(30f);

                        row.AddToggleButton()
                            .SetText("F")
                            .BindOnClicked(model => model.OnSelectCategory(5))
                            .BindIsToggled(model => model.IsCategoryFeelingsToggled)
                            .SetWidth(30f)
                            .SetHeight(30f);

                        row.AddSpacer();
                    });

                    // Corpo: lista emote filtrata
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

                    // Footer: Reset sempre visibile
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Reset")
                            .BindOnClicked(model => model.OnClickReset())
                            .SetWidth(100f)
                            .SetHeight(24f);
                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
