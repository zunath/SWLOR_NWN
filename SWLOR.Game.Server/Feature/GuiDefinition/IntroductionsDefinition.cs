using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class IntroductionsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<IntroductionsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Introductions)
                .SetInitialGeometry(0, 0, 610f, 440f)
                .SetTitle("Introductions")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(IntroductionsViewModel.MainContentPartial, AddContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(IntroductionsViewModel.ContentElement));
            return _builder.Build();
        }

        private static void AddContent(GuiGroup<IntroductionsViewModel> host)
        {
            host.AddColumn(root => root.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(content =>
                {
                    content.AddRow(row => row.AddText()
                        .SetText("These names may be aliases. Remember saves a private label for you; Dismiss changes nothing. Introductions expire after 10 minutes.")
                        .SetHeight(58f)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto));
                    content.AddRow(row => row.AddList(template =>
                    {
                        template.AddCell(cell => cell.AddText()
                            .BindText(model => model.OfferDescriptions)
                            .SetShowBorder(false)
                            .SetScrollbars(NuiScrollbars.Auto));
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(100f);
                            cell.AddButton()
                                .BindText(model => model.RememberLabels)
                                .BindOnClicked(model => model.OnClickRemember());
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(80f);
                            cell.AddButton()
                                .BindText(model => model.DismissLabels)
                                .BindOnClicked(model => model.OnClickDismiss());
                        });
                    })
                        .BindRowCount(model => model.OfferDescriptions)
                        .SetRowHeight(100f)
                        .SetHeight(230f));
                    content.AddRow(row => row.AddText()
                        .BindText(model => model.StatusText)
                        .SetHeight(44f)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto));
                    content.AddRow(row => row.AddButton()
                        .SetText("Refresh")
                        .SetHeight(32f)
                        .SetWidth(100f)
                        .BindOnClicked(model => model.OnClickRefresh()));
                });
            }).SetWidth(560f)));
        }
    }
}
