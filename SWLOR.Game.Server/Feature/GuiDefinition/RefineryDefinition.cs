using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RefineryDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CharacterSheetViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Refinery)
                .SetInitialGeometry(0, 0, 800f, 400f)
                .SetTitle("Refinery")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col =>
                {

                });

            return _builder.Build();
        }
    }
}
