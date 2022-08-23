using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AreaManagerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AreaManagerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AreaManager)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 700f, 600f)
                .SetTitle("Area Notes")

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
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(colAreas =>
                        {
                            colAreas.SetHeight(450f);
                            colAreas.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.AreaNames)
                                            .BindIsToggled(model => model.AreaToggled)
                                            .BindOnClicked(model => model.OnSelectArea());
                                    });
                                })
                                    .BindRowCount(model => model.AreaNames);
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddSpacer();

                                row.AddButton()
                                    .SetText("Resave All Objects")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickResaveAllObjects());

                                row.AddSpacer();
                            });
                        });

                        row.AddColumn(colAreas =>
                        {
                            colAreas.SetHeight(450f);
                            colAreas.SetWidth(325f);
                            colAreas.AddRow(row =>
                            {
                                row.SetHeight(450f);

                                row.AddList(template =>
                                {
                                    template.SetHeight(450f);

                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.AreaObjectList)
                                            .BindIsToggled(model => model.AreaObjectToggled)
                                            .BindOnClicked(model => model.OnSelectAreaObject())
                                            .SetWidth(300f);
                                    });
                                })
                                .BindRowCount(model => model.AreaObjectList);
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddButton()
                                    .SetText("Delete Object")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickDeleteObject());

                                row.AddSpacer();

                                row.AddButton()
                                    .SetText("Reset Area")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickResetArea());
                            });
                        });
                    });
                });

            return _builder.Build();
        }       
    }
}
