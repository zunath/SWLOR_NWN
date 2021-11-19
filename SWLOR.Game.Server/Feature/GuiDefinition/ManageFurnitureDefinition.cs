using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageFurnitureDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageFurnitureViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ManageFurniture)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Furniture")

                ;


            return _builder.Build();
        }
    }
}
