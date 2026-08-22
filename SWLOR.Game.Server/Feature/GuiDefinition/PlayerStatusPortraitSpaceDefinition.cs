using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PlayerStatusPortraitSpaceDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PlayerStatusPortraitSpaceViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PlayerStatusPortraitSpace)
                .SetInitialGeometry(0, 0, 72f, 74f)
                .SetTitle(null)
                .SetIsClosable(false)
                .SetIsResizable(false)
                .SetIsCollapsible(false)
                .SetIsTransparent(true)
                .SetShowBorder(false)
                .SetAcceptsInput(false)
                .AddColumn(col =>
                {
                    // Shield / Hull / Capacitor, matching the docked SH/HL/CAP order. Rows carry
                    // explicit heights (~8px over the bar height, the project's safe ratio) so all
                    // three rows always fit; a too-short window clips the bottom bar.
                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.ShieldProgress)
                            .BindColor(model => model.ShieldColor)
                            .SetHeight(15f)
                            .SetPadding(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.ShieldValue);
                                    text.SetBounds(15f, -1f, 110f, 50f);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    })
                        .SetHeight(23f);

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.HullProgress)
                            .BindColor(model => model.HullColor)
                            .SetHeight(15f)
                            .SetPadding(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.HullValue);
                                    text.SetBounds(15f, -1f, 110f, 50f);
                                    text.SetColor(255, 255, 255);
                                });
                            });
                    })
                        .SetHeight(23f);

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.CapacitorProgress)
                            .BindColor(model => model.CapacitorColor)
                            .SetHeight(15f)
                            .SetPadding(0f)
                            .AddDrawList(drawList =>
                            {
                                drawList.AddText(text =>
                                {
                                    text.BindText(model => model.CapacitorValue);
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
