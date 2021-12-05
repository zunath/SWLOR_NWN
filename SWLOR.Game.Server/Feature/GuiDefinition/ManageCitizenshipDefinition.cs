using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ManageCitizenshipDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ManageCitizenshipViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ManageCitizenship)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Manage Citizenship")
                
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
