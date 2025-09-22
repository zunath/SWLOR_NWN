using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExamineItemDefinition: IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<ExamineItemViewModel> _builder;

        public ExamineItemDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<ExamineItemViewModel>(_guiService);
        }
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ExamineItem)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 385f, 379f)
                .BindTitle(model => model.WindowTitle)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddLabel()
                                .SetText("Description")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        })
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.Description)
                            .SetHeight(160f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddLabel()
                                .SetText("Item Properties")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        })
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.ItemProperties)
                            .SetHeight(105f);
                    });
                });
                ;

            return _builder.Build();
        }
    }
}
