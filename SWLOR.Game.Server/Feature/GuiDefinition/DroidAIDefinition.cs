using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class DroidAIDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DroidAIViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DroidAI)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 480f, 540f)
                .SetTitle("Droid AI")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {

                    });
                })
                ;

            return _builder.Build();
        }
    }
}
