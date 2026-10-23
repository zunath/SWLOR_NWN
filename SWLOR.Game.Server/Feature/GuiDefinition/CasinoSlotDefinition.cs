using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CasinoSlotDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CasinoSlotViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CasinoSlot)
                .SetInitialGeometry(80f, 25f, 620f, 660f)
                .SetTitle("Smuggler's Moon Casino")
                .SetIsResizable(true)
                .SetIsCollapsible(false)
                .SetIsClosable(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(CasinoSlotViewModel.BodyPartial, AddBody);
            window.AddStandardLayout(layout => layout.SetContentPartialElement(CasinoSlotViewModel.BodyElement));
            return _builder.Build();
        }

        private static void AddBody(GuiGroup<CasinoSlotViewModel> host)
        {
            host.AddColumn(column => column.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    content.AddRow(r => r.AddLabel().BindText(m => m.Headline)
                        .BindColor(m => m.ResultColor).SetHeight(30f));
                    content.AddRow(r => r.AddLabel().BindText(m => m.CreditsText).SetHeight(24f));
                    content.AddRow(reels =>
                    {
                        reels.AddSpacer();
                        reels.AddImage().BindResref(m => m.LeftReel).SetAspect(NuiAspect.Fit).SetWidth(152f).SetHeight(152f);
                        reels.AddImage().BindResref(m => m.MiddleReel).SetAspect(NuiAspect.Fit).SetWidth(152f).SetHeight(152f);
                        reels.AddImage().BindResref(m => m.RightReel).SetAspect(NuiAspect.Fit).SetWidth(152f).SetHeight(152f);
                        reels.AddSpacer();
                    });
                    content.AddRow(r => r.AddText().BindText(m => m.ResultText).SetShowBorder(false).SetHeight(48f));
                    content.AddRow(details =>
                    {
                        details.AddImage().BindResref(m => m.HostImage).SetAspect(NuiAspect.Fit).SetWidth(160f).SetHeight(160f);
                        details.AddText().BindText(m => m.DailyText).SetShowBorder(false).SetWidth(376f).SetHeight(160f);
                    });
                    content.AddRow(buttons =>
                    {
                        buttons.AddButton().SetText("SPIN - 100 CREDITS").BindIsEnabled(m => m.CanSpin)
                            .BindOnClicked(m => m.OnClickSpin()).SetHeight(36f).SetWidth(260f);
                        buttons.AddButton().SetText("Refresh").BindOnClicked(m => m.OnClickRefresh()).SetHeight(36f);
                        buttons.AddButton().SetText("Leave").BindOnClicked(m => m.OnClickLeave()).SetHeight(36f);
                    });
                    content.AddRow(r => r.AddText().SetText(
                        "Payout odds: 70%: 0 | 20%: 100 | 8%: 500 | 1.9%: 1,000 | 0.1%: 10,000\n" +
                        "Payouts include your stake. Average return: 89%. Three seconds between spins.\n" +
                        "Big wins of 1,000 credits or more earn a Hutt celebration!")
                        .SetShowBorder(false).SetHeight(74f));
                });
            }).SetWidth(560f)));
        }
    }
}
