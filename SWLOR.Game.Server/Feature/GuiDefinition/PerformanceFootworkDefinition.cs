using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The dancer's cue window. Opens when a dance starts and closes when it ends.</summary>
    public class PerformanceFootworkDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PerformanceFootworkViewModel> _builder = new();

        private const float ContentPanelWidth = 520f;
        private const float HeaderCellWidth = 124f;
        private const float HeaderCellHeight = 54f;
        private const float NowTileWidth = 104f;
        private const float NowImageSize = 84f;
        private const float TileWidth = 68f;
        private const float TileImageSize = 56f;
        private const float QualityCellWidth = 118f;

        private static readonly Expression<Func<PerformanceFootworkViewModel, string>>[] CueImages =
        {
            model => model.Cue0Image, model => model.Cue1Image, model => model.Cue2Image,
            model => model.Cue3Image, model => model.Cue4Image, model => model.Cue5Image
        };

        private static readonly Expression<Func<PerformanceFootworkViewModel, string>>[] CueSlots =
        {
            model => model.Cue0Slot, model => model.Cue1Slot, model => model.Cue2Slot,
            model => model.Cue3Slot, model => model.Cue4Slot, model => model.Cue5Slot
        };

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.PerformanceFootwork)
                .SetInitialGeometry(20f, 40f, 580f, 630f)
                .SetTitle("Footwork")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(PerformanceFootworkViewModel.MainContentPartial, AddContent);

            // No tabs: the standard layout is still the root, and one partial holds the whole body.
            window.AddStandardLayout(layout => layout
                .SetContentPartialElement(PerformanceFootworkViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddContent(GuiGroup<PerformanceFootworkViewModel> host)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    AddHeader(content);
                    AddCueTrack(content);
                    AddBeatBar(content);
                    AddReadouts(content);
                    AddFooter(content);
                });
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddHeader(GuiColumn<PerformanceFootworkViewModel> content)
        {
            content.AddRow(row =>
            {
                AddHeaderCell(row, "ROUTINE", model => model.RoutineText);
                AddHeaderCell(row, "TEMPO", model => model.TempoText);
                AddHeaderCell(row, "VENUE", model => model.VenueText);
                AddHeaderCell(row, "FOLLOWERS", model => model.FollowingText);
            });
        }

        private static void AddHeaderCell(
            GuiRow<PerformanceFootworkViewModel> row,
            string caption,
            Expression<Func<PerformanceFootworkViewModel, string>> value)
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
                .SetWidth(HeaderCellWidth)
                .SetHeight(HeaderCellHeight);
        }

        // The NOW cue in its own bordered tile, then the next five. Each tile shows the icon of the hot bar button to press
        // and the slot number it sits in.
        private static void AddCueTrack(GuiColumn<PerformanceFootworkViewModel> content)
        {
            content.AddRow(row => row.AddGroup(track =>
            {
                track.SetShowBorder(true);
                track.SetScrollbars(NuiScrollbars.None);
                track.AddColumn(inner => inner.AddRow(r =>
                {
                    r.AddGroup(now =>
                    {
                        now.SetShowBorder(true);
                        now.SetScrollbars(NuiScrollbars.None);
                        now.AddColumn(tile =>
                        {
                            tile.AddRow(t => t.AddLabel()
                                .SetText("NOW")
                                .SetColor(PerformanceWindowColors.Gold)
                                .SetHeight(18f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center));
                            AddTileBody(tile, 0, NowImageSize, PerformanceWindowColors.Gold);
                        });
                    })
                        .SetWidth(NowTileWidth)
                        .SetHeight(NowImageSize + 80f);

                    for (var index = 1; index < PerformanceFootworkViewModel.CueCount; index++)
                    {
                        var tileIndex = index;
                        r.AddColumn(tile => AddTileBody(tile, tileIndex, TileImageSize, PerformanceWindowColors.Dim))
                            .SetWidth(TileWidth);
                    }

                    r.AddSpacer();
                }));
            })
                .SetHeight(NowImageSize + 104f));
        }

        private static void AddTileBody(
            GuiColumn<PerformanceFootworkViewModel> tile,
            int index,
            float imageSize,
            GuiColor slotColor)
        {
            tile.AddRow(t => t.AddImage()
                .BindResref(CueImages[index])
                .SetAspect(NuiAspect.Fit)
                .SetWidth(imageSize)
                .SetHeight(imageSize));
            tile.AddRow(t => t.AddLabel()
                .BindText(CueSlots[index])
                .SetColor(slotColor)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Center));
        }

        private static void AddBeatBar(GuiColumn<PerformanceFootworkViewModel> content)
        {
            // No row height: a row holding a progress bar derives its height from the bar (layout rule R2c).
            content.AddRow(row => row.AddProgressBar()
                .BindValue(model => model.BeatProgress)
                .SetColor(PerformanceWindowColors.Gold)
                .SetHeight(16f));
            content.AddRow(row => row.AddLabel()
                .SetText("Time left on this beat")
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(20f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static void AddReadouts(GuiColumn<PerformanceFootworkViewModel> content)
        {
            content.AddRow(row => row.AddGroup(readouts =>
            {
                readouts.SetShowBorder(true);
                readouts.SetScrollbars(NuiScrollbars.None);
                readouts.AddColumn(inner =>
                {
                    inner.AddRow(r => r.AddLabel()
                        .SetText("QUALITY (LAST 20 CUES)")
                        .SetColor(PerformanceWindowColors.Dim)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));

                    inner.AddRow(r =>
                    {
                        AddQualityCell(r, model => model.QualityRoughText, model => model.QualityRoughColor);
                        AddQualityCell(r, model => model.QualitySteadyText, model => model.QualitySteadyColor);
                        AddQualityCell(r, model => model.QualityGreatText, model => model.QualityGreatColor);
                        AddQualityCell(r, model => model.QualityDazzlingText, model => model.QualityDazzlingColor);
                    });

                    inner.AddRow(r =>
                    {
                        r.AddColumn(streak =>
                        {
                            streak.AddRow(s => s.AddLabel()
                                .SetText("STREAK")
                                .SetColor(PerformanceWindowColors.Dim)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));
                            streak.AddRow(s => s.AddLabel()
                                .BindText(model => model.StreakText)
                                .SetHeight(30f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));
                        })
                            .SetWidth(110f);

                        r.AddColumn(flourish =>
                        {
                            flourish.AddRow(f => f.AddLabel()
                                .BindText(model => model.FlourishText)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));
                            flourish.AddRow(f => f.AddProgressBar()
                                .BindValue(model => model.FlourishProgress)
                                .SetColor(PerformanceWindowColors.Gold)
                                .SetHeight(16f));
                        });
                    });
                });
            })
                .SetHeight(150f));
        }

        private static void AddQualityCell(
            GuiRow<PerformanceFootworkViewModel> row,
            Expression<Func<PerformanceFootworkViewModel, string>> text,
            Expression<Func<PerformanceFootworkViewModel, GuiColor>> color)
        {
            row.AddLabel()
                .BindText(text)
                .BindColor(color)
                .SetWidth(QualityCellWidth)
                .SetHeight(26f)
                .SetHorizontalAlign(NuiHorizontalAlign.Center);
        }

        private static void AddFooter(GuiColumn<PerformanceFootworkViewModel> content)
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
                .SetText("Press the hotbar slot shown on the NOW cue")
                .SetColor(PerformanceWindowColors.Dim)
                .SetHeight(20f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }
    }
}
