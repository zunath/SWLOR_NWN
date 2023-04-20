using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MusicManagerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MusicManagerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.MusicManager)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 475f, 650f)
                .SetTitle("Music Manager")

                // Main Search - Top Window Row
                .AddColumn(colSearch =>
                {
                    colSearch.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search for Music")
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

                    // Main Window Container - Second Window Row
                    colSearch.AddRow(row =>
                    {
                        row.AddColumn(colMusic =>
                        {
                            colMusic.SetHeight(500f);
                            colMusic.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.MusicList)
                                            .BindIsToggled(model => model.MusicToggled)
                                            .BindOnClicked(model => model.OnSelectMusic());
                                    });
                                })
                                .BindRowCount(model => model.MusicList);
                            });

                            colMusic.AddRow(rowEditor =>
                            {
                                rowEditor.AddSpacer();
                                rowEditor.AddButton()
                                    .SetText("<")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickPreviousPage());

                                rowEditor.AddComboBox()
                                    .BindOptions(model => model.PageNumbers)
                                    .BindSelectedIndex(model => model.SelectedPageIndex);

                                rowEditor.AddButton()
                                    .SetText(">")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickNextPage());

                                rowEditor.AddSpacer();
                            });

                            colMusic.AddRow(row =>
                            {
                                row.AddSpacer();

                                row.AddButton()
                                    .SetText("Play Music")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsMusicSelected)
                                    .BindOnClicked(model => model.OnClickPlayMusic());

                                row.AddButton()
                                    .SetText("Pause Music")
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClicKStopMusic());

                                row.AddSpacer();
                            });
                        });
                    });
                });
        return _builder.Build();
        }
    }
}