using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public sealed class ShipCargoDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipCargoViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ShipCargo).SetInitialGeometry(20f, 60f, 650f, 440f)
                .SetTitle("Ship Cargo").SetIsResizable(true).SetIsCollapsible(true)
                .DefinePartialView(ShipCargoViewModel.MainContentPartial, AddContent);
            window.AddStandardLayout(layout => layout.SetContentPartialElement(ShipCargoViewModel.ContentElement));
            return _builder.Build();
        }
        private static void AddContent(GuiGroup<ShipCargoViewModel> host) => host.AddColumn(root => root.AddRow(row => row.AddGroup(panel =>
        {
            panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            panel.AddColumn(content =>
            {
                content.AddRow(r => r.AddLabel().BindText(m => m.Summary).SetWidth(576f).SetHeight(28f));
                content.AddRow(r => r.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel().BindText(m => m.CargoRows).SetHorizontalAlign(NuiHorizontalAlign.Left));
                    template.AddCell(cell => { cell.SetIsVariable(false); cell.SetWidth(120f); cell.AddButton().BindText(m => m.WithdrawLabels).BindIsEnabled(m => m.WithdrawEnabled).BindOnClicked(m => m.OnWithdraw()).SetHeight(32f); });
                }).BindRowCount(m => m.CargoRows).SetWidth(576f).SetRowHeight(36f).SetHeight(230f));
                content.AddRow(r => r.AddText().BindText(m => m.StatusText).SetWidth(576f).SetHeight(50f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                content.AddRow(r =>
                {
                    r.AddButton().SetText("Load stack").SetWidth(130f).SetHeight(32f).BindOnClicked(m => m.OnLoad());
                    r.AddButton().SetText("Retry transfer").SetWidth(140f).SetHeight(32f).BindOnClicked(m => m.OnRecover());
                    r.AddButton().SetText("Refresh").SetWidth(100f).SetHeight(32f).BindOnClicked(m => m.OnRefresh());
                });
            });
        }).SetWidth(600f)));
    }
}
