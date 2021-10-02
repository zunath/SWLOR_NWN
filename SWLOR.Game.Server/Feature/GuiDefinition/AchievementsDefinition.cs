using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AchievementsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AchievementsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Achievements);

            return _builder.Build();
        }
    }
}
