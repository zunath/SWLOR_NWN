using SWLOR.Component.Communication.UI.ViewModel;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.UI.View
{
    public class EmotesDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<EmotesViewModel> _builder;

        public EmotesDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<EmotesViewModel>(_guiService);
        }

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
