using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The "HP Tracker" scene window (opened with /hptracker). A header row (HP value + Add-by-target),
    /// then one list row per tracked creature in range: the creature name (fills the row), a colored HP
    /// bar, "cur/max", a Locate button (glow only the viewer sees), and per-row -/+/x buttons. Structure
    /// mirrors the proven Bank/MarketListing windows (resizable; a variable-width name label fills the row;
    /// every action is a fixed-width button so nothing collapses). Never touches real combat HP. See
    /// <see cref="ViewModel.HpTrackerViewModel"/>.
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
                .SetInitialGeometry(0, 0, 480f, 360f)

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

                        // A flex spacer advises the column to fill the window width (NUI sizes content
                        // bottom-up; without a flex element the whole inner area shrinks to its content
                        // width, collapsing the variable name cell and clipping the right-hand buttons).
                        row.AddSpacer();
                    });

                    // The scene: one row per tracked creature in range.
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            // A narrow fixed spacer cell pads the name off the window border (a label's own
                            // margin isn't honored inside a list cell).
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(6f);
                                cell.AddLabel()
                                    .SetText("");
                            });
                            // Name — variable-width label fills the row (proven Bank pattern) so it shows
                            // the full name and stretches the window's inner width.
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Names)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(110f);
                                cell.AddProgressBar()
                                    .BindValue(model => model.HpProgresses)
                                    .BindColor(model => model.HpColors);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(46f);
                                cell.AddLabel()
                                    .BindText(model => model.HpTexts)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                            // Locate — glow the creature in the world for the clicker only (click again to
                            // clear). A view action, so it stays enabled regardless of manage permission.
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(36f);
                                cell.AddButton()
                                    .SetText("Loc")
                                    .SetTooltip("Locate: glow this creature in the world (only you see it).")
                                    .BindOnClicked(model => model.OnClickName());
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
                            .BindRowCount(model => model.Names)
                            .SetRowHeight(30f);
                    });
                });

            return _builder.Build();
        }
    }
}
