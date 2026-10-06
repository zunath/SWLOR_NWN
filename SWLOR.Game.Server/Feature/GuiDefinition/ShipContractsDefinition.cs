using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public sealed class ShipContractsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipContractsViewModel> _builder=new();
        public GuiConstructedWindow BuildWindow()
        {
            var window=_builder.CreateWindow(GuiWindowType.ShipContracts).SetTitle("Space Contracts").SetInitialGeometry(30f,40f,700f,620f).SetIsResizable(true).SetIsCollapsible(true)
                .DefinePartialView(ShipContractsViewModel.MainContentPartial,host=>host.AddColumn(root=>root.AddRow(row=>row.AddGroup(panel=>
                {
                    panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
                    panel.AddColumn(content=>
                    {
                        content.AddRow(r=>r.AddText().BindText(m=>m.Summary).SetWidth(626f).SetHeight(130f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                        content.AddRow(r=>r.AddList(template=>
                        {
                            template.AddCell(cell=>cell.AddLabel().BindText(m=>m.Jobs).BindTooltip(m=>m.Descriptions).SetHorizontalAlign(NuiHorizontalAlign.Left));
                            template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(90f);cell.AddButton().SetText("Accept").BindIsEnabled(m=>m.Docked).SetHeight(32f).BindOnClicked(m=>m.OnAccept());});
                        }).BindRowCount(m=>m.Jobs).SetRowHeight(36f).SetHeight(264f).SetWidth(626f));
                        content.AddRow(r=>
                        {
                            r.AddButton().SetText("Join party").SetWidth(110f).SetHeight(32f).BindIsEnabled(m=>m.Docked).BindOnClicked(m=>m.OnJoin());
                            r.AddButton().SetText("Set course").SetWidth(110f).SetHeight(32f).BindOnClicked(m=>m.OnNavigate());
                            r.AddButton().SetText("Complete").SetWidth(110f).SetHeight(32f).BindIsEnabled(m=>m.Docked).BindOnClicked(m=>m.OnFinish());
                            r.AddButton().SetText("Cancel").SetWidth(110f).SetHeight(32f).BindOnClicked(m=>m.OnCancel());
                            r.AddButton().SetText("Refresh").SetWidth(110f).SetHeight(32f).BindOnClicked(m=>m.OnRefresh());
                        });
                        content.AddRow(r=>
                        {
                            r.AddButton().SetText("Select objective").SetWidth(180f).SetHeight(32f).BindOnClicked(m=>m.OnObjective());
                            r.AddButton().SetText("Board target").SetWidth(180f).SetHeight(32f).BindOnClicked(m=>m.OnBoard());
                            r.AddButton().SetText("Return to ship").SetWidth(180f).SetHeight(32f).BindOnClicked(m=>m.OnReturn());
                        });
                        content.AddRow(r=>r.AddText().BindText(m=>m.Message).SetWidth(626f).SetHeight(44f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                    });
                }).SetWidth(650f))));
            window.AddStandardLayout(layout=>layout.SetContentPartialElement(ShipContractsViewModel.ContentElement));return _builder.Build();
        }
    }
}
