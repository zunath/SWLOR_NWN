using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DistributeRPXPDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<DistributeRPXPViewModel> _builder;

        public DistributeRPXPDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<DistributeRPXPViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.DistributeRPXP)
                .SetIsResizable(true)
                .SetIsCollapsible(false)
                .SetInitialGeometry(0, 0, 327f, 250f)
                .SetTitle("Distribute RP XP")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.SkillName)
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.AvailableRPXP)
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.MaxDistributableInfo)
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.Distribution);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Confirm")
                            .BindOnClicked(model => model.OnClickConfirm());

                        row.AddButton()
                            .SetText("Cancel")
                            .BindOnClicked(model => model.OnClickCancel());
                    });
                })

                ;

            return _builder.Build();
        }
    }
}
