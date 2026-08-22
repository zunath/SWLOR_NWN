using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PlayerStatusPortraitDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PlayerStatusPortraitViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PlayerStatusPortrait)
                .SetInitialGeometry(0, 0, 72f, 52f)
                .SetTitle(null)
                .SetIsClosable(false)
                .SetIsResizable(false)
                .SetIsCollapsible(false)
                .SetIsTransparent(true)
                .SetShowBorder(false)
                .SetAcceptsInput(false)
                .AddColumn(col =>
                {
                    // Stamina (green) on top, FP (blue) below, matching the docked HP/STM/FP order.
                    // Rows carry explicit heights (~8px over the bar height, the project's safe
                    // ratio) so both rows always fit; a too-short window clips the second bar.
                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.StaminaProgress)
                            .BindColor(model => model.StaminaColor)
                            .SetHeight(15f)
                            .SetPadding(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.StaminaValue);
                                    text.SetBounds(15f, -1f, 110f, 50f);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    })
                        .SetHeight(23f);

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.FPProgress)
                            .BindColor(model => model.FPColor)
                            .SetHeight(15f)
                            .SetPadding(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.FPValue);
                                    text.SetBounds(15f, -1f, 110f, 50f);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    })
                        .SetHeight(23f);
                });

            return _builder.Build();
        }
    }
}
