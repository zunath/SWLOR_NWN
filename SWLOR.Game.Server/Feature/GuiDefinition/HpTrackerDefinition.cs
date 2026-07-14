using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The "HP Tracker" scene window (opened with /hptracker). Lists creatures near the viewer that have a
    /// tracker: name, a colored HP progress bar, "cur/max", and per-row -/+/x buttons; plus a top HP-value
    /// field and an Add-by-target button. See <see cref="ViewModel.HpTrackerViewModel"/>.
    /// </summary>
    public class HpTrackerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HpTrackerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.HpTracker)
                .SetTitle("HP Tracker")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 430f, 320f)
                .AddColumn(col =>
                {
                    // Top controls: HP value + Add-by-target.
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("HP:")
                            .SetWidth(28f)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);

                        row.AddTextEdit()
                            .SetPlaceholder("10")
                            .SetWidth(60f)
                            .BindValue(model => model.AddHpText);

                        row.AddButton()
                            .SetText("Add (target)")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickAdd());

                        row.SetHeight(34f);
                    });

                    // Column headers.
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Name")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        row.AddLabel()
                            .SetText("HP")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddLabel()
                            .SetText("")
                            .SetWidth(132f);
                        row.SetHeight(18f);
                    });

                    // The scene: one row per tracked creature in range.
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Names)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddProgressBar()
                                    .BindValue(model => model.HpProgresses)
                                    .BindColor(model => model.HpColors);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(48f);
                                cell.AddLabel()
                                    .BindText(model => model.HpTexts)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(26f);
                                cell.AddButton()
                                    .SetText("-")
                                    .BindOnClicked(model => model.OnClickDecrease())
                                    .BindIsEnabled(model => model.CanManage);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(26f);
                                cell.AddButton()
                                    .SetText("+")
                                    .BindOnClicked(model => model.OnClickIncrease())
                                    .BindIsEnabled(model => model.CanManage);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(26f);
                                cell.AddButton()
                                    .SetText("x")
                                    .BindOnClicked(model => model.OnClickRemove())
                                    .BindIsEnabled(model => model.CanManage);
                            });
                        })
                            .BindRowCount(model => model.Names);
                    });
                });

            return _builder.Build();
        }
    }
}
