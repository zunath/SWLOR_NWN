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

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());

                        row.AddButton()
                            .SetText("Add New Creature")
                            .SetHeight(35f)
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
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .SetWidth(120f)
                                            .BindText(model => model.CreatureNames);

                                        cell.SetWidth(120f);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddButton()
                                            .SetText("Create")
                                            .SetWidth(40f)
                                            .BindOnClicked(model => model.OnCreateCreature());

                                        cell.SetWidth(40f);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddButton()
                                            .SetText("Delete")
                                            .SetWidth(40f)
                                            .BindOnClicked(model => model.OnDeleteCreature());

                                        cell.SetWidth(40f);
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
