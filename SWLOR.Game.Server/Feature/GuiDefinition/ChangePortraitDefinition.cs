using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ChangePortraitDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ChangePortraitViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ChangePortrait);

            return _builder.Build();
        }
    }
}