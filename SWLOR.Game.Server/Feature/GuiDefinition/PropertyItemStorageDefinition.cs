using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PropertyItemStorageDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PropertyItemStorageViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PropertyItemStorage)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Property Item Storage")

                ;
            return _builder.Build();
        }
    }
}
