using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RecipesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<RecipesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Recipes);

            return _builder.Build();
        }
    }
}
