using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Spends a language token to master a language of the player's choice.
    /// </summary>
    public class LanguageTokenDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 320f;

        private readonly GuiWindowBuilder<LanguageTokenViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.LanguageToken)
                .SetInitialGeometry(0, 0, 360f, 500f)
                .SetTitle("Language Token")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(LanguageTokenViewModel.MainPartial, AddBody);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(LanguageTokenViewModel.ContentElement);
            });

            return _builder.Build();
        }

        private static void AddBody(GuiGroup<LanguageTokenViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            content.AddRow(r => r.AddLabel()
                                .SetText("Choose a language to master.")
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(24f));

                            content.AddRow(r => r.AddLabel()
                                .BindText(model => model.TokenCountText)
                                .SetColor(255, 236, 155)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                .SetHeight(24f));

                            content.AddRow(r =>
                            {
                                r.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddButton()
                                            .BindText(model => model.LanguageNames)
                                            .BindTooltip(model => model.LanguageDescriptions)
                                            .BindOnClicked(model => model.OnClickLanguage());
                                    });
                                })
                                    .SetRowHeight(36f)
                                    .SetScrollbars(NuiScrollbars.Y)
                                    .BindRowCount(model => model.LanguageNames)
                                    .SetHeight(360f);
                            });
                        });
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }
    }
}
