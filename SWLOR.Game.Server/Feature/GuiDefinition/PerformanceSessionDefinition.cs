using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The musician's cue window. Opens when a performance on an instrument starts and closes when it ends.</summary>
    public class PerformanceSessionDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PerformanceSessionViewModel> _builder = new();

        private const float ContentPanelWidth = 560f;
        // The set cell is widest so set names such as "Cantina Standards" fit; the four cells keep their combined width.
        private const float NarrowHeaderCellWidth = 116f;
        private const float SongCellWidth = 136f;
        private const float SetCellWidth = 152f;
        private const float HeaderCellHeight = 54f;
        private const float LaneWidth = 88f;
        private const float LaneRowHeight = 30f;
        private const float NoteSize = 28f;
        private const float StrikeSize = 34f;
        private const float RightColumnWidth = 250f;
        private const float QualityCellWidth = 112f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.PerformanceSession)
                .SetInitialGeometry(20f, 60f, 600f, 620f)
                .SetTitle("Session")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(PerformanceSessionViewModel.MainContentPartial, AddContent);

            // No tabs: the standard layout is still the root, and one partial holds the whole body.
            window.AddStandardLayout(layout => layout
                .SetContentPartialElement(PerformanceSessionViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddContent(GuiGroup<PerformanceSessionViewModel> host)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    AddHeader(content);
                    AddBandLine(content);
                    AddLanesAndReadouts(content);
                    AddFooter(content);
                });
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddHeader(GuiColumn<PerformanceSessionViewModel> content)
        {
            content.AddRow(row =>
            {
                AddHeaderCell(row, "INSTRUMENT", NarrowHeaderCellWidth, model => model.InstrumentText);
                AddHeaderCell(row, "SONG", SongCellWidth, model => model.SongText);
                AddHeaderCell(row, "SET", SetCellWidth, model => model.SetText);
                AddHeaderCell(row, "TEMPO", NarrowHeaderCellWidth, model => model.TempoText);
            });
        }

        private static void AddHeaderCell(
            GuiRow<PerformanceSessionViewModel> row,
            string caption,
            float width,
            Expression<Func<PerformanceSessionViewModel, string>> value)
        {
            row.AddGroup(cell =>
            {
                cell.SetShowBorder(true);
                cell.SetScrollbars(NuiScrollbars.None);
                cell.AddColumn(inner =>
                {
                    inner.AddRow(r => r.AddLabel()
                        .SetText(caption)
                        .SetColor(PerformanceWindowColors.Dim)
                        .SetHeight(18f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    inner.AddRow(r => r.AddLabel()
                        .BindText(value)
                        .SetHeight(22f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                });
            })
                .SetWidth(width)
                .SetHeight(HeaderCellHeight);
        }

        private static void AddBandLine(GuiColumn<PerformanceSessionViewModel> content)
        {
            content.AddRow(row => row.AddText()
                .BindText(model => model.BandText)
                .SetShowBorder(true)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(64f));
        }

        private static void AddLanesAndReadouts(GuiColumn<PerformanceSessionViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddColumn(AddLanes).SetWidth(LaneWidth * 3f + 12f);
                row.AddColumn(AddReadouts).SetWidth(RightColumnWidth);
            });
        }

        // Three lanes of upcoming notes dropping toward the strike row; each lane is one list replaced when its notes change.
        private static void AddLanes(GuiColumn<PerformanceSessionViewModel> lanes)
        {
            lanes.AddRow(row =>
            {
                AddLane(row, PerformanceCue.Low, model => model.LowLane);
                AddLane(row, PerformanceCue.Mid, model => model.MidLane);
                AddLane(row, PerformanceCue.High, model => model.HighLane);
            });

            lanes.AddRow(row =>
            {
                AddStrike(row, PerformanceCue.Low, model => model.LowStrike);
                AddStrike(row, PerformanceCue.Mid, model => model.MidStrike);
                AddStrike(row, PerformanceCue.High, model => model.HighStrike);
            });

            lanes.AddRow(row =>
            {
                AddLaneCaption(row, "Low 1");
                AddLaneCaption(row, "Mid 2");
                AddLaneCaption(row, "High 3");
            });
        }

        // An image placed directly in a list cell draws nothing in the client, so each note sits in a borderless group.
        private static void AddLane(
            GuiRow<PerformanceSessionViewModel> row,
            PerformanceCue cue,
            Expression<Func<PerformanceSessionViewModel, GuiBindingList<bool>>> notes)
        {
            row.AddList(template => template.AddCell(cell => cell.AddGroup(group => group.AddImage()
                        .SetResref(PerformanceCueIcons.GetIcon(cue))
                        .BindIsVisible(notes)
                        .SetAspect(NuiAspect.Fit)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetWidth(NoteSize)
                        .SetHeight(NoteSize))
                    .SetShowBorder(false)
                    .SetScrollbars(NuiScrollbars.None)))
                .BindRowCount(notes)
                .SetRowHeight(LaneRowHeight)
                .SetScrollbars(NuiScrollbars.None)
                .SetWidth(LaneWidth)
                .SetHeight(LaneRowHeight * (PerformanceSessionViewModel.UpcomingRows) + 6f);
        }

        private static void AddStrike(
            GuiRow<PerformanceSessionViewModel> row,
            PerformanceCue cue,
            Expression<Func<PerformanceSessionViewModel, bool>> note)
        {
            row.AddGroup(strike =>
            {
                strike.SetShowBorder(true);
                strike.SetScrollbars(NuiScrollbars.None);
                strike.AddColumn(inner => inner.AddRow(r =>
                {
                    r.AddSpacer();
                    r.AddImage()
                        .SetResref(PerformanceCueIcons.GetIcon(cue))
                        .BindIsVisible(note)
                        .SetAspect(NuiAspect.Fit)
                        .SetWidth(StrikeSize)
                        .SetHeight(StrikeSize);
                    r.AddSpacer();
                }));
            })
                .SetWidth(LaneWidth)
                .SetHeight(StrikeSize + 24f);
        }

        private static void AddLaneCaption(GuiRow<PerformanceSessionViewModel> row, string text)
        {
            row.AddLabel()
                .SetText(text)
                .SetColor(PerformanceWindowColors.Dim)
                .SetWidth(LaneWidth)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Center);
        }

        private static void AddReadouts(GuiColumn<PerformanceSessionViewModel> readouts)
        {
            readouts.AddRow(row => row.AddGroup(group =>
            {
                group.SetShowBorder(true);
                group.SetScrollbars(NuiScrollbars.None);
                group.AddColumn(inner =>
                {
                    inner.AddRow(r => r.AddLabel()
                        .SetText("QUALITY (LAST 20 NOTES)")
                        .SetColor(PerformanceWindowColors.Dim)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    inner.AddRow(r =>
                    {
                        AddQualityCell(r, model => model.QualityRoughText, model => model.QualityRoughColor);
                        AddQualityCell(r, model => model.QualitySteadyText, model => model.QualitySteadyColor);
                    });

                    inner.AddRow(r =>
                    {
                        AddQualityCell(r, model => model.QualityGreatText, model => model.QualityGreatColor);
                        AddQualityCell(r, model => model.QualityDazzlingText, model => model.QualityDazzlingColor);
                    });

                    inner.AddRow(r => r.AddLabel()
                        .SetText("STREAK")
                        .SetColor(PerformanceWindowColors.Dim)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    inner.AddRow(r => r.AddLabel()
                        .BindText(model => model.StreakText)
                        .SetHeight(30f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    inner.AddRow(r => r.AddLabel()
                        .BindText(model => model.FlourishText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    inner.AddRow(r => r.AddProgressBar()
                        .BindValue(model => model.FlourishProgress)
                        .SetColor(PerformanceWindowColors.Gold)
                        .SetHeight(16f));

                    inner.AddRow(r => r.AddLabel()
                        .BindText(model => model.AudienceHearsText)
                        .SetHeight(22f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                });
            })
                .SetHeight(LaneRowHeight * PerformanceSessionViewModel.UpcomingRows + StrikeSize + 76f));
        }

        private static void AddQualityCell(
            GuiRow<PerformanceSessionViewModel> row,
            Expression<Func<PerformanceSessionViewModel, string>> text,
            Expression<Func<PerformanceSessionViewModel, GuiColor>> color)
        {
            row.AddLabel()
                .BindText(text)
                .BindColor(color)
                .SetWidth(QualityCellWidth)
                .SetHeight(26f)
                .SetHorizontalAlign(NuiHorizontalAlign.Center);
        }

        private static void AddFooter(GuiColumn<PerformanceSessionViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.InspiringText)
                    .SetWidth(230f)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddLabel()
                    .BindText(model => model.LastResultText)
                    .BindColor(model => model.LastResultColor)
                    .SetHeight(22f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            content.AddRow(row => row.AddLabel()
                .SetText("Press the lane's slot on the strike row")
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(20f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }
    }
}
