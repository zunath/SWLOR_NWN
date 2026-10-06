using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public sealed class ShipCockpitDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ShipCockpitViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            var window=_builder.CreateWindow(GuiWindowType.ShipCockpit).SetInitialGeometry(30f,40f,700f,640f)
                .SetTitle("Ship Operations").SetIsResizable(true).SetIsCollapsible(true)
                .DefinePartialView(ShipCockpitViewModel.MainContentPartial,AddContent);
            window.AddStandardLayout(layout=>layout.SetContentPartialElement(ShipCockpitViewModel.ContentElement));
            return _builder.Build();
        }
        private static void AddContent(GuiGroup<ShipCockpitViewModel> host)=>host.AddColumn(root=>root.AddRow(row=>row.AddGroup(panel=>
        {
            panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            panel.AddColumn(content=>
            {
                content.AddRow(r=>r.AddText().BindText(m=>m.Summary).SetWidth(626f).SetHeight(45f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                content.AddRow(r=>
                {
                    r.AddButton().BindText(m=>m.Bank1Text).SetWidth(120f).SetHeight(32f).BindOnClicked(m=>m.OnSelectBank(1));
                    r.AddButton().SetText("Fire Bank 1").SetWidth(110f).SetHeight(32f).BindIsEnabled(m=>m.InFlight).BindOnClicked(m=>m.OnFireBank(1));
                    r.AddButton().BindText(m=>m.Bank2Text).SetWidth(120f).SetHeight(32f).BindOnClicked(m=>m.OnSelectBank(2));
                    r.AddButton().SetText("Fire Bank 2").SetWidth(110f).SetHeight(32f).BindIsEnabled(m=>m.InFlight).BindOnClicked(m=>m.OnFireBank(2));
                    r.AddButton().SetText("Refresh").SetWidth(100f).SetHeight(32f).BindOnClicked(m=>m.OnRefresh());
                });
                content.AddRow(r=>r.AddLabel().BindText(m=>m.PreparedSummary).SetWidth(626f).SetHeight(24f));
                content.AddRow(r=>r.AddList(template=>
                {
                    template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(38f);cell.AddImage().BindResref(m=>m.PerkIcons).SetWidth(32f).SetHeight(32f);});
                    template.AddCell(cell=>cell.AddLabel().BindText(m=>m.PerkRows).BindTooltip(m=>m.PerkDescriptions).SetHorizontalAlign(NuiHorizontalAlign.Left));
                    template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(90f);cell.AddButton().BindText(m=>m.PerkUseText).BindIsEnabled(m=>m.CanUsePerk).SetHeight(32f).BindOnClicked(m=>m.OnUsePerk());});
                    template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(90f);cell.AddButton().BindText(m=>m.PerkPrepareText).BindIsEnabled(m=>m.Docked).SetHeight(32f).BindOnClicked(m=>m.OnPreparePerk());});
                }).BindRowCount(m=>m.PerkRows).SetWidth(626f).SetRowHeight(36f).SetHeight(180f));
                content.AddRow(r=>r.AddButton().BindText(m=>m.ConstituentText).SetWidth(626f).SetHeight(32f).BindOnClicked(m=>m.OnSelectConstituent()));
                content.AddRow(r=>r.AddList(template=>
                {
                    template.AddCell(cell=>cell.AddLabel().BindText(m=>m.ModuleRows).BindTooltip(m=>m.ModuleDescriptions).SetHorizontalAlign(NuiHorizontalAlign.Left));
                    template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(80f);cell.AddButton().BindText(m=>m.ModuleBank1).BindIsEnabled(m=>m.Docked).SetHeight(32f).BindOnClicked(m=>m.OnAssignBank(1));});
                    template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(80f);cell.AddButton().BindText(m=>m.ModuleBank2).BindIsEnabled(m=>m.Docked).SetHeight(32f).BindOnClicked(m=>m.OnAssignBank(2));});
                    template.AddCell(cell=>{cell.SetIsVariable(false);cell.SetWidth(75f);cell.AddButton().SetText("Select tool").SetHeight(32f).BindOnClicked(m=>m.OnSelectTool());});
                }).BindRowCount(m=>m.ModuleRows).SetWidth(626f).SetRowHeight(36f).SetHeight(108f));
                content.AddRow(r=>r.AddText().BindText(m=>m.EffectsText).SetWidth(626f).SetHeight(58f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
                content.AddRow(r=>r.AddText().BindText(m=>m.StatusText).SetWidth(626f).SetHeight(44f).SetShowBorder(false).SetScrollbars(NuiScrollbars.Auto));
            });
        }).SetWidth(650f)));
    }
}
