using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CreatureManagerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CreatureManagerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CreatureManager)
                .SetInitialGeometry(0, 0, 638f, 638f)
                .SetTitle("Creature Manager")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        // This window sizes to its content, so these widths keep the rows and list window-wide.
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .SetHeight(35f)
                            .SetWidth(435f)
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .SetWidth(100f)
                            .BindOnClicked(model => model.OnClickSearch());
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .BindText(model => model.FavoritesFilterText)
                            .SetHeight(35f)
                            .SetWidth(290f)
                            .BindOnClicked(model => model.OnClickToggleFavoritesFilter());

                        row.AddButton()
                            .SetText("Add New Creature")
                            .SetHeight(35f)
                            .SetWidth(290f)
                            .BindOnClicked(model => model.OnClickAddNew());
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(colCreatures =>
                        {
                            colCreatures.SetHeight(450f);
                            colCreatures.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    // The name takes whatever the fixed-width buttons leave.
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .BindText(model => model.CreatureNames);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(65f);
                                        cell.AddButton()
                                            .SetText("Create")
                                            .BindIsEnabled(model => model.CreatureExists)
                                            .BindOnClicked(model => model.OnCreateCreature());
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(65f);
                                        cell.AddButton()
                                            .SetText("Delete")
                                            .BindIsEnabled(model => model.CreatureExists)
                                            .BindOnClicked(model => model.OnDeleteCreature());
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(90f);
                                        cell.AddButton()
                                            .BindText(model => model.FavoriteTexts)
                                            .BindOnClicked(model => model.OnToggleFavorite());
                                    });
                                })
                                    .BindRowCount(model => model.CreatureNames);
                            });

                            colCreatures.AddPagination(
                                model => model.PageNumbers,
                                model => model.SelectedPageIndex,
                                model => model.OnClickPreviousPage(),
                                model => model.OnClickNextPage());

                        });
                    });
                });

            return _builder.Build();
        }
    }
}
