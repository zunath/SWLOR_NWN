using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PerksDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PerksViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Perks);

            return _builder.Build();
        }
    }
}
