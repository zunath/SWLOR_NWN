using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PlayerStatusPortraitSpaceDefinition : IGuiWindowDefinition
    {
        internal const float WidgetWidth = 72f;
        internal const float WidgetHeight = 82f;
        private const float BarHeight = 18f;
        private const float RowHeight = 23f;

        private readonly GuiWindowBuilder<PlayerStatusPortraitSpaceViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PlayerStatusPortraitSpace)
                .SetInitialGeometry(0, 0, WidgetWidth, WidgetHeight)
                .SetTitle(null)
                .SetIsClosable(false)
                .SetIsResizable(false)
                .SetIsCollapsible(false)
                .SetIsTransparent(true)
                .SetShowBorder(false)
                .SetAcceptsInput(false)
                .AddColumn(col =>
                {
                    // Shield / Hull / Capacitor, matching the docked SH/HL/CAP order.
                    // Set each row's height directly: AddRow returns the column, so chaining
                    // SetHeight after it would squeeze all three bars into one row's height.
                    col.AddRow(row =>
                    {
                        row.SetHeight(RowHeight);
                        row.AddProgressBar()
                            .BindValue(model => model.ShieldProgress)
                            .BindColor(model => model.ShieldColor)
                            .SetHeight(BarHeight)
                            .SetMargin(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.SetIsConstrainedToTargetBounds(true);
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.ShieldValue);
                                    text.SetBounds(4f, 0f, 48f, BarHeight);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    });

                    col.AddRow(row =>
                    {
                        row.SetHeight(RowHeight);
                        row.AddProgressBar()
                            .BindValue(model => model.HullProgress)
                            .BindColor(model => model.HullColor)
                            .SetHeight(BarHeight)
                            .SetMargin(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.SetIsConstrainedToTargetBounds(true);
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.HullValue);
                                    text.SetBounds(4f, 0f, 48f, BarHeight);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    });

                    col.AddRow(row =>
                    {
                        row.SetHeight(RowHeight);
                        row.AddProgressBar()
                            .BindValue(model => model.CapacitorProgress)
                            .BindColor(model => model.CapacitorColor)
                            .SetHeight(BarHeight)
                            .SetMargin(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.SetIsConstrainedToTargetBounds(true);
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.CapacitorValue);
                                    text.SetBounds(4f, 0f, 48f, BarHeight);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    });
                });

            return _builder.Build();
        }
    }
}
