using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A city's public log, newest entry first. Anyone can read it, visitors included.
    /// </summary>
    public class CityGazetteDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CityGazetteViewModel> _builder = new();

        private const float ContentPanelWidth = 700f;
        private const float LabelHeight = 20f;
        private const float DateWidth = 84f;
        private const float OfficialWidth = 150f;
        private const float EntryRowHeight = 72f;
        private const float EntryTextHeight = 64f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CityGazette)
                .SetInitialGeometry(0, 0, 760f, 560f)
                .SetTitle("City Gazette")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(CityGazetteViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(CityGazetteViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<CityGazetteViewModel> host)
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
                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.HeaderText)
                                    .SetColor(GuiColor.Cyan)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.EmptyText)
                                    .BindIsVisible(model => model.IsEmpty)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel().SetText("Date").SetWidth(DateWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                                line.AddLabel().SetText("Official").SetWidth(OfficialWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                                line.AddLabel().SetText("Entry").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(DateWidth);
                                        cell.AddLabel()
                                            .BindText(model => model.RowDates)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Top);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetIsVariable(false);
                                        cell.SetWidth(OfficialWidth);
                                        cell.AddText()
                                            .BindText(model => model.RowOfficials)
                                            .SetShowBorder(false)
                                            .SetScrollbars(NuiScrollbars.None)
                                            .SetHeight(EntryTextHeight);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.AddText()
                                            .BindText(model => model.RowTexts)
                                            .BindTooltip(model => model.RowTexts)
                                            .SetShowBorder(false)
                                            .SetScrollbars(NuiScrollbars.None)
                                            .SetHeight(EntryTextHeight);
                                    });
                                })
                                    .BindRowCount(model => model.RowTexts)
                                    .SetRowHeight(EntryRowHeight);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }
    }
}
