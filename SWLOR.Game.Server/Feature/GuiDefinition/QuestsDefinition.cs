using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class QuestsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<QuestsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Quests);

            return _builder.Build();
        }
    }
}
