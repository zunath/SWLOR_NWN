using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class MusicPickerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MusicPickerViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.MusicPicker)
                .SetInitialGeometry(0, 0, 450f, 500f)
                .SetTitle("Music Picker")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col =>
                {
                    col.SetHeight(500f);
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.SearchText)
                            .SetPlaceholder("Search");

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .SetWidth(60f)
                            .BindOnClicked(model => model.OnClickSearch());
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.SongNames)
                                    .BindTooltip(model => model.SongNames)
                                    .BindOnClicked(model => model.OnSelectSong());
                            });
                        })
                        .BindRowCount(model => model.SongNames);
                    });
                });

            return _builder.Build();
        }
    }
}
