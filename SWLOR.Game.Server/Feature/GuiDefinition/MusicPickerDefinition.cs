using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MusicPickerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MusicPickerViewModel> _builder = new();

        private const float CategoryRailWidth = 190f;
        private const float ContentPanelWidth = 540f;
        private const float RowHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.MusicPicker)
                .SetInitialGeometry(0, 0, 790f, 660f)
                .BindTitle(model => model.WindowTitle)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(MusicPickerViewModel.PersonalContentPartial, host => AddContent(host, true))
                .DefinePartialView(MusicPickerViewModel.AreaContentPartial, host => AddContent(host, false));

            window.AddStandardLayout(layout => layout
                .AddLeadingColumn(AddCategories, CategoryRailWidth)
                .SetContentPartialElement(MusicPickerViewModel.ContentElement));

            return _builder.Build();
        }

        private static void AddCategories(GuiColumn<MusicPickerViewModel> column)
        {
            column.AddRow(row => row.AddLabel()
                .SetText("Categories")
                .SetHeight(24f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));

            column.AddRow(row => row.AddList(template => template.AddCell(cell =>
                    cell.AddToggleButton()
                        .BindText(model => model.CategoryNames)
                        .BindIsToggled(model => model.CategorySelected)
                        .BindOnClicked(model => model.OnSelectCategory())))
                .BindRowCount(model => model.CategoryNames)
                .SetRowHeight(RowHeight)
                .SetHeight(480f));
        }

        // List templates own each cell's visibility, so personal-only controls get their own partial.
        private static void AddContent(GuiGroup<MusicPickerViewModel> host, bool isPersonal)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    content.AddRow(r => r.AddLabel()
                        .BindText(model => model.HeaderText)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)
                        .SetHeight(24f));

                    content.AddRow(r =>
                    {
                        r.AddTextEdit()
                            .SetPlaceholder("Search songs...")
                            .BindValue(model => model.SearchText)
                            .SetMaxLength(64)
                            .SetHeight(RowHeight);

                        r.AddButton()
                            .SetText("Clear")
                            .SetWidth(70f)
                            .SetHeight(RowHeight)
                            .BindOnClicked(model => model.OnClearSearch());
                    });

                    content.AddRow(r =>
                    {
                        if (isPersonal)
                        {
                            r.AddCheckBox()
                                .SetText("Show songs not yet collected")
                                .BindIsChecked(model => model.ShowUncollected)
                                .BindIsEnabled(model => model.IsCollectionMode)
                                .SetWidth(300f);
                        }

                        r.AddLabel()
                            .BindText(model => model.ResultText)
                            .SetHorizontalAlign(NuiHorizontalAlign.Right)
                            .SetHeight(24f);
                    });

                    content.AddRow(r => r.AddList(template =>
                    {
                        template.AddCell(cell => cell.AddLabel()
                            .BindText(model => model.SongNames)
                            .BindTooltip(model => model.SongNames)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left));

                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(90f);
                            cell.AddLabel()
                                .BindText(model => model.SongCategories)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });

                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(62f);
                            cell.AddButton()
                                .SetText("Play")
                                .SetWidth(58f)
                                .BindIsEnabled(model => model.PlayEnabled)
                                .BindOnClicked(model => model.OnPlaySong());
                        });

                        if (isPersonal)
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(74f);
                                cell.AddButton()
                                    .SetText("Battle")
                                    .SetTooltip("Use this song as your combat music.")
                                    .SetWidth(70f)
                                    .BindIsEnabled(model => model.PlayEnabled)
                                    .BindOnClicked(model => model.OnSetBattleTheme());
                            });
                        }
                    })
                        .BindRowCount(model => model.SongNames)
                        .SetRowHeight(RowHeight)
                        .SetHeight(300f));

                    content.AddPagination(
                        model => model.PageNumbers,
                        model => model.SelectedPageIndex,
                        model => model.OnPreviousPage(),
                        model => model.OnNextPage());

                    content.AddRow(r =>
                    {
                        r.AddLabel()
                            .BindText(model => model.NowPlayingText)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetHeight(RowHeight);

                        r.AddButton()
                            .SetText("Stop")
                            .SetWidth(80f)
                            .SetHeight(RowHeight)
                            .BindOnClicked(model => model.OnStop());
                    });

                    if (!isPersonal)
                        return;

                    content.AddRow(r =>
                    {
                        r.AddLabel()
                            .BindText(model => model.BattleThemeText)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetHeight(RowHeight);

                        r.AddButton()
                            .SetText("Clear Battle Theme")
                            .SetWidth(150f)
                            .SetHeight(RowHeight)
                            .BindOnClicked(model => model.OnClearBattleTheme());
                    });
                });
            }).SetWidth(ContentPanelWidth)));
        }
    }
}
