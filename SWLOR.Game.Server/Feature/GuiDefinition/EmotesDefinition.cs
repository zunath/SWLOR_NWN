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
            window.SetInitialGeometry(0, 0, 400f, 400f)
                .SetTitle("Emotes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddPartialView("EMOTE_LAYOUT_GROUP");
                    });

                    // Footer comune: Reset + Toggle Layout
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        
                        row.AddToggleButton()
                            .SetText("Grid")
                            .BindOnClicked(model => model.OnSelectLayout(0))
                            .BindIsToggled(model => model.IsHorizontalLayoutToggled)
                            .SetWidth(50f)
                            .SetHeight(20f);

                        row.AddToggleButton()
                            .SetText("Col")
                            .BindOnClicked(model => model.OnSelectLayout(1))
                            .BindIsToggled(model => model.IsVerticalLayoutToggled)
                            .SetWidth(50f)
                            .SetHeight(20f);

                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Reset")
                            .BindOnClicked(model => model.OnClickReset())
                            .SetWidth(100f);
                        row.AddSpacer();
                    });
                });

            window.DefinePartialView("GRID_VIEW", gridColGroup =>
            {
                gridColGroup.AddColumn(gridCol =>
                {
                    // Layout GRID: Header con categorie in griglia 2 righe x 3 colonne
                    gridCol.AddRow(gridHeaderRow1 =>
                    {
                        gridHeaderRow1.AddToggleButton()
                            .BindText(model => model.CategoryAllName)
                            .BindOnClicked(model => model.OnSelectCategory(0))
                            .BindIsToggled(model => model.IsCategoryAllToggled)
                            .SetHeight(32f);

                        gridHeaderRow1.AddToggleButton()
                            .BindText(model => model.CategoryCombatName)
                            .BindOnClicked(model => model.OnSelectCategory(1))
                            .BindIsToggled(model => model.IsCategoryCombatToggled)
                            .SetHeight(32f);

                        gridHeaderRow1.AddToggleButton()
                            .BindText(model => model.CategoryExplorationName)
                            .BindOnClicked(model => model.OnSelectCategory(2))
                            .BindIsToggled(model => model.IsCategoryExplorationToggled)
                            .SetHeight(32f);
                    });

                    gridCol.AddRow(gridHeaderRow2 =>
                    {
                        gridHeaderRow2.AddToggleButton()
                            .BindText(model => model.CategoryTasksName)
                            .BindOnClicked(model => model.OnSelectCategory(3))
                            .BindIsToggled(model => model.IsCategoryTasksToggled)
                            .SetHeight(32f);

                        gridHeaderRow2.AddToggleButton()
                            .BindText(model => model.CategorySocialName)
                            .BindOnClicked(model => model.OnSelectCategory(4))
                            .BindIsToggled(model => model.IsCategorySocialToggled)
                            .SetHeight(32f);

                        gridHeaderRow2.AddToggleButton()
                            .BindText(model => model.CategoryFeelingsName)
                            .BindOnClicked(model => model.OnSelectCategory(5))
                            .BindIsToggled(model => model.IsCategoryFeelingsToggled)
                            .SetHeight(32f);
                    });

                    // Corpo Layout GRID (Scroll singolo largo)
                    gridCol.AddRow(gridBodyRow =>
                    {
                        gridBodyRow.AddList(template =>
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
                });
            });

            window.DefinePartialView("COL_VIEW", colMainGroup =>
            {
                colMainGroup.AddColumn(colMain =>
                {
                    colMain.AddRow(colRow =>
                    {
                        colRow.AddColumn(colLeft =>
                        {
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddToggleButton()
                                    .BindText(model => model.CategoryAllName)
                                    .BindOnClicked(model => model.OnSelectCategory(0))
                                    .BindIsToggled(model => model.IsCategoryAllToggled)
                                    .SetHeight(26f);
                            });
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddToggleButton()
                                    .BindText(model => model.CategoryCombatName)
                                    .BindOnClicked(model => model.OnSelectCategory(1))
                                    .BindIsToggled(model => model.IsCategoryCombatToggled)
                                    .SetHeight(26f);
                            });
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddToggleButton()
                                    .BindText(model => model.CategoryExplorationName)
                                    .BindOnClicked(model => model.OnSelectCategory(2))
                                    .BindIsToggled(model => model.IsCategoryExplorationToggled)
                                    .SetHeight(26f);
                            });
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddToggleButton()
                                    .BindText(model => model.CategoryTasksName)
                                    .BindOnClicked(model => model.OnSelectCategory(3))
                                    .BindIsToggled(model => model.IsCategoryTasksToggled)
                                    .SetHeight(26f);
                            });
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddToggleButton()
                                    .BindText(model => model.CategorySocialName)
                                    .BindOnClicked(model => model.OnSelectCategory(4))
                                    .BindIsToggled(model => model.IsCategorySocialToggled)
                                    .SetHeight(26f);
                            });
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddToggleButton()
                                    .BindText(model => model.CategoryFeelingsName)
                                    .BindOnClicked(model => model.OnSelectCategory(5))
                                    .BindIsToggled(model => model.IsCategoryFeelingsToggled)
                                    .SetHeight(26f);
                            });
                            colLeft.AddRow(leftRow =>
                            {
                                leftRow.AddSpacer();
                            });
                        }).SetWidth(100f);

                        colRow.AddColumn(colRight =>
                        {
                            colRight.AddRow(rightRow =>
                            {
                                rightRow.AddList(template =>
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
                        });
                    });
                });
            });

            return _builder.Build();
        }
    }
}
