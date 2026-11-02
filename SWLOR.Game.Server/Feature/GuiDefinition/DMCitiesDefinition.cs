using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The DM city window: every city with its people, Edicts, Gazette, and story request, a planet filter, and the planetary
    /// council summons. Opened by DMs with the /cities command.
    /// </summary>
    public class DMCitiesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMCitiesViewModel> _builder = new();

        private const float ContentPanelWidth = 520f;
        private const float SideRailWidth = 400f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 32f;
        private const float CityRowHeight = 30f;
        private const float DetailsHeight = 250f;
        private const float RequestHeight = 160f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMCities)
                .SetInitialGeometry(80, 0, 960f, 640f)
                .SetTitle("Cities")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMCitiesViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMCitiesViewModel.MainContentElement);
                layout.AddSideColumn(AddDetailsRail, SideRailWidth);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMCitiesViewModel> host)
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
                            content.AddRow(status =>
                            {
                                status.AddLabel()
                                    .BindText(model => model.StatusText)
                                    .BindColor(model => model.StatusColor)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(LabelHeight);
                            });

                            content.AddRow(filters =>
                            {
                                filters.AddComboBox()
                                    .BindOptions(model => model.PlanetOptions)
                                    .BindSelectedIndex(model => model.SelectedPlanetIndex)
                                    .SetTooltip("Show the cities on one planet's council. Stations join their planet's council.")
                                    .SetWidth(280f)
                                    .SetHeight(ButtonHeight);

                                filters.AddButton()
                                    .SetText("Refresh")
                                    .SetTooltip("Read every city again.")
                                    .SetWidth(100f)
                                    .SetHeight(ButtonHeight)
                                    .BindOnClicked(model => model.OnClickRefresh());

                                filters.AddSpacer();
                            });

                            content.AddRow(summon =>
                            {
                                summon.AddTextEdit()
                                    .SetPlaceholder("Council: add details for the mayors and officers (optional)")
                                    .SetTooltip("Added to the summons. Pick a planet in the filter, then call its council.")
                                    .BindValue(model => model.SummonMessage)
                                    .SetMaxLength(PlanetaryCouncilRules.MessageMaxLength)
                                    .SetHeight(ButtonHeight);

                                summon.AddButton()
                                    .SetText("Summon Council")
                                    .SetTooltip("Messages the planet's mayors and officers now and leaves a notice for their next login. The Gazettes post it.")
                                    .SetDisabledTooltip("Pick a planet in the filter first.")
                                    .SetWidth(140f)
                                    .SetHeight(ButtonHeight)
                                    .BindIsEnabled(model => model.CanSummon)
                                    .BindOnClicked(model => model.OnClickSummon());
                            });

                            content.AddRow(summary =>
                            {
                                summary.AddLabel()
                                    .BindText(model => model.ListSummary)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(LabelHeight);
                            });

                            content.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindText(model => model.CityLabels)
                                                .BindTooltip(model => model.CityLabels)
                                                .BindIsToggled(model => model.CityToggles)
                                                .BindOnClicked(model => model.OnSelectCity());
                                        });
                                    })
                                    .BindRowCount(model => model.CityLabels)
                                    .SetRowHeight(CityRowHeight);
                            });
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddDetailsRail(GuiColumn<DMCitiesViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("CITY")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(LabelHeight);
            });

            // Text boxes rather than labels so DMs can select and copy what they read.
            col.AddRow(row =>
            {
                row.AddTextEdit()
                    .BindValue(model => model.DetailsText)
                    .SetIsMultiline(true)
                    .SetHasWordWrap(true)
                    .SetMaxLength(4000)
                    .SetHeight(DetailsHeight);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("STORY REQUEST")
                    .SetColor(221, 181, 93)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetHeight(LabelHeight);
            });

            col.AddRow(row =>
            {
                row.AddTextEdit()
                    .BindValue(model => model.RequestText)
                    .SetIsMultiline(true)
                    .SetHasWordWrap(true)
                    .SetMaxLength(1200)
                    .SetHeight(RequestHeight);
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Take")
                    .SetTooltip("Mark the request taken: a DM will run it.")
                    .SetDisabledTooltip("Pick a city with an open story request.")
                    .BindIsEnabled(model => model.CanTake)
                    .BindOnClicked(model => model.OnClickTake())
                    .SetHeight(ButtonHeight);

                row.AddButton()
                    .SetText("Done")
                    .SetTooltip("Mark the request done.")
                    .SetDisabledTooltip("Pick a city with an open or taken story request.")
                    .BindIsEnabled(model => model.CanFinish)
                    .BindOnClicked(model => model.OnClickDone())
                    .SetHeight(ButtonHeight);
            });

            col.AddRow(row => row.AddSpacer());
        }
    }
}
