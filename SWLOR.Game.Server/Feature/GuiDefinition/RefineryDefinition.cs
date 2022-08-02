using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RefineryDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<RefineryViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Refinery)
                .SetInitialGeometry(0, 0, 800f, 400f)
                .SetTitle("Refinery")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindIsClosable(model => model.IsCloseEnabled)
                .BindOnClosed(model => model.OnWindowClosed())
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetHeight(20f)
                            .BindText(model => model.Instructions)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .BindColor(model => model.InstructionsColor);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Input Items:")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.InputItemNames)
                                        .BindIsToggled(model => model.InputItemToggles)
                                        .BindOnClicked(model => model.OnClickInputItem());
                                });
                        })
                            .BindRowCount(model => model.InputItemNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Add Item")
                            .BindOnClicked(model => model.OnClickAddItem());

                        row.AddButton()
                            .SetText("Remove Items")
                            .BindOnClicked(model => model.OnClickRemoveItems());
                    });


                    col.AddRow(row =>
                    {
                        row.AddImage()
                            .SetResref(RefineryViewModel.PowerCoreIconResref)
                            .SetHeight(64f)
                            .SetWidth(64f);

                        row.AddLabel()
                            .BindText(model => model.RequiredPowerCores);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Refine Items")
                            .BindOnClicked(model => model.OnClickRefine());
                        row.AddSpacer();
                    });

                })
                
                .AddColumn(col =>
                {

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Output Items:")
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.OutputItemNames);
                                });
                            })
                            .BindRowCount(model => model.OutputItemNames);
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
