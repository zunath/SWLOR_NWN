using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// A city's City Projects: the facility upgrades and Home Ground it is building, what each still needs, and who gave what. Anyone,
    /// visitors included, can donate the listed materials they carry; an official with the power to start projects can cancel one.
    /// </summary>
    public class CityProjectsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CityProjectsViewModel> _builder = new();

        private const float ContentPanelWidth = 500f;
        private const float LabelHeight = 20f;
        private const float MessageHeight = 48f;
        private const float DetailHeight = 150f;
        private const float RowHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CityProjects)
                .SetInitialGeometry(0, 0, 560f, 640f)
                .SetTitle("City Projects")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(CityProjectsViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout => layout.SetContentPartialElement(CityProjectsViewModel.MainContentElement));

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<CityProjectsViewModel> host)
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
                                line.AddText()
                                    .BindText(model => model.MessageText)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(MessageHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.ProjectsHeaderText)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetColor(GuiColor.Cyan);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.ProjectNames)
                                            .BindIsToggled(model => model.ProjectToggles)
                                            .BindOnClicked(model => model.OnClickProject());
                                    });
                                })
                                    .BindRowCount(model => model.ProjectNames)
                                    .SetRowHeight(RowHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddText()
                                    .BindText(model => model.DetailText)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.Auto)
                                    .SetHeight(DetailHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddLabel()
                                    .BindText(model => model.MaterialsHeaderText)
                                    .SetHeight(LabelHeight)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetColor(GuiColor.Cyan);
                            });

                            content.AddRow(line =>
                            {
                                line.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.MaterialNames)
                                            .BindIsToggled(model => model.MaterialToggles)
                                            .BindOnClicked(model => model.OnClickMaterial());
                                    });
                                })
                                    .BindRowCount(model => model.MaterialNames)
                                    .SetRowHeight(RowHeight);
                            });

                            content.AddRow(line =>
                            {
                                line.AddButton()
                                    .SetText("Donate")
                                    .BindIsEnabled(model => model.CanDonate)
                                    .BindDisabledTooltip(model => model.DonateTooltip)
                                    .BindOnClicked(model => model.OnClickDonate());
                                line.AddButton()
                                    .SetText("Donate All")
                                    .BindIsEnabled(model => model.CanDonateAll)
                                    .BindDisabledTooltip(model => model.DonateAllTooltip)
                                    .BindOnClicked(model => model.OnClickDonateAll());
                                line.AddButton()
                                    .SetText("Cancel Project")
                                    .BindIsEnabled(model => model.CanCancel)
                                    .BindDisabledTooltip(model => model.CancelTooltip)
                                    .BindOnClicked(model => model.OnClickCancel());
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }
    }
}
