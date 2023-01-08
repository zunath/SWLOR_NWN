using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DebugEnmityDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DebugEnmityViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DebugEnmity)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Enmity Debugger")
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.EnmityDetails)
                                    .SetHeight(32f);
                            });
                        })
                            .BindRowCount(model => model.EnmityDetails);
                    });

                })
                ;

            return _builder.Build();
        }
    }
}
