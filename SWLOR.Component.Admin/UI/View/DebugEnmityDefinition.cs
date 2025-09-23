using SWLOR.Component.Admin.UI.ViewModel;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Admin.UI.View
{
    public class DebugEnmityDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<DebugEnmityViewModel> _builder;

        public DebugEnmityDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<DebugEnmityViewModel>(_guiService);
        }

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
