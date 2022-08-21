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

                        row.AddButton()
                            .SetText("TEST")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickTest());
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

                        });

                        row.AddColumn(colAreas =>
                        {
                            colAreas.SetHeight(450f);
                            colAreas.SetWidth(325f);
                            colAreas.AddRow(row =>
                            {
                                row.SetHeight(300f);

                                row.AddList(template =>
                                {
                                    template.SetHeight(300f);

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
                                
                                /*
                                row.AddTextEdit()
                                    .SetIsMultiline(true)
                                    .BindValue(model => model.AreaObjectList)
                                    .SetHeight(450f)
                                    .SetWidth(300f)
                                    .SetIsEnabled(false);
                                */
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
