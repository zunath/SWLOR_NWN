using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DistributeRPXPDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DistributeRPXPViewModel> _builder = new();

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
