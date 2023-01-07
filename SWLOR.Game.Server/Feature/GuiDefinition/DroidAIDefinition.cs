using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class DroidAIDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DroidAIViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DroidAI)
                .BindOnClosed(model => model.CloseWindow())
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 480f, 540f)
                .BindTitle(model => model.DroidName)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Available Perks")
                            .SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.AvailablePerkNames)
                                    .BindIsToggled(model => model.AvailablePerkSelections);
                            });
                        })
                        .BindRowCount(model => model.AvailablePerkNames)
                        .SetWidth(300f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Upload Instruction")
                            .BindOnClicked(model => model.AddInstructionDisk());
                        row.AddSpacer();
                    });
                })
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("AI Slots")
                                    .SetHeight(32f);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.AISlots)
                                    .SetHeight(32f)
                                    .BindColor(model => model.AISlotsColor);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText(">>")
                                    .SetHeight(32f)
                                    .SetWidth(64f)
                                    .BindOnClicked(model => model.AddToActivePerks());
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("<<")
                                    .SetHeight(32f)
                                    .SetWidth(64f)
                                    .BindOnClicked(model => model.RemoveFromActivePerks());
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                            });
                        });
                    });
                })
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Active Perks")
                            .SetHeight(32f);
                    });
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.ActivePerkNames)
                                    .BindIsToggled(model => model.ActivePerkSelections);
                            });
                        })
                        .BindRowCount(model => model.ActivePerkNames);
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
