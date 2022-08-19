using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class EmotesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<EmotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Emotes)
                .SetInitialGeometry(0, 0, 150f, 300f)
                .SetTitle("Emotes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)

                .AddColumn(col =>
                {
                    col.SetHeight(300f);
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.EmoteNames)
                                    .BindTooltip(model => model.EmoteDescriptions)
                                    .BindOnClicked(model => model.OnSelectEmote());
                            });
                        })
                            .BindRowCount(model => model.EmoteNames);
                    });
                });

            return _builder.Build();
        }
    }
}
