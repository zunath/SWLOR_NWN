using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public sealed class ShipSupplyDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipSupplyViewModel> _builder=new();
        public GuiConstructedWindow BuildWindow()
        {
            var window=_builder.CreateWindow(GuiWindowType.ShipSupply).SetTitle("Dock Supplies and Commissions").SetInitialGeometry(30f,40f,700f,730f).SetIsResizable(true).SetIsCollapsible(true)
                .DefinePartialView(ShipSupplyViewModel.MainContentPartial,host=>host.AddColumn(root=>root.AddRow(row=>row.AddGroup(panel=>
                {
                    panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
                    panel.AddColumn(content=>
                    {
                        content.AddRow(r=>r.AddText().BindText(m=>m.Summary).SetWidth(626f).SetHeight(100f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                        content.AddRow(r=>r.AddList(template=>
                        {
                            template.AddCell(cell=>cell.AddLabel().BindText(m=>m.Stock).SetHorizontalAlign(NuiHorizontalAlign.Left));
                            template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(90f);cell.AddButton().SetText("Buy").SetHeight(28f).BindOnClicked(m=>m.OnBuy());});
                        }).BindRowCount(m=>m.Stock).SetRowHeight(32f).SetHeight(256f).SetWidth(626f));
                        content.AddRow(r=>r.AddList(template=>
                        {
                            template.AddCell(cell=>cell.AddLabel().BindText(m=>m.Ore).SetHorizontalAlign(NuiHorizontalAlign.Left));
                            template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(90f);cell.AddButton().SetText("Commission").SetHeight(24f).BindOnClicked(m=>m.OnSellOre());});
                        }).BindRowCount(m=>m.Ore).SetRowHeight(28f).SetHeight(168f).SetWidth(626f));
                        content.AddRow(r=>
                        {
                            r.AddButton().SetText("Starter escort").SetWidth(155f).SetHeight(32f).BindOnClicked(m=>m.OnStarterEscort());
                            r.AddButton().SetText("Starter prospector").SetWidth(165f).SetHeight(32f).BindOnClicked(m=>m.OnStarterIndustry());
                            r.AddButton().SetText("Recover transaction").SetWidth(170f).SetHeight(32f).BindOnClicked(m=>m.OnRecover());
                            r.AddButton().SetText("Refresh").SetWidth(95f).SetHeight(32f).BindOnClicked(m=>m.OnRefresh());
                        });
                        content.AddRow(r=>r.AddButton().SetText("Claim verified materials").SetWidth(250f).SetHeight(28f).BindOnClicked(m=>m.OnMaterials()));
                        content.AddRow(r=>r.AddText().BindText(m=>m.Message).SetWidth(626f).SetHeight(40f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                    });
                }).SetWidth(650f))));
            window.AddStandardLayout(layout=>layout.SetContentPartialElement(ShipSupplyViewModel.ContentElement));return _builder.Build();
        }
    }
}
