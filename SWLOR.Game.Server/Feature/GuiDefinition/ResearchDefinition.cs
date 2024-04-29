using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ResearchDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ResearchViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Recipes)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 600f)
                .SetTitle("Research Terminal")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {

                    });
                });


            return _builder.Build();
        }
    }
}
