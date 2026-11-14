using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// City Affairs: the mayor and officers decide the city's open situations, and the mayor appoints officers. Opened from
    /// the Citizenship and Manage City windows at City Hall.
    /// </summary>
    public partial class CityAffairsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CityAffairsViewModel> _builder = new();

        // Fits a 1280x720 client with the title bar visible. The tab row is as wide as its four tabs: the client draws each tab
        // about 148 pixels wide whatever width the row asks for, so a narrower window clips the last tab and scrolls the bar.
        private const float WindowWidth = 720f;
        private const float WindowHeight = 680f;
        private const float TabBarWidth = 600f;

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 560f;
        private const float LabelHeight = 20f;
        private const float ButtonHeight = 32f;
        private const float SituationRowHeight = 30f;
        private const float SituationListHeight = 96f;
        private const float DescriptionHeight = 72f;
        private const float ChoiceRowHeight = 64f;
        private const float ChoiceListHeight = 204f;
        private const float ChoiceButtonWidth = 150f;
        private const float ChoiceInfoHeight = 56f;
        private const float OfficerRowHeight = 28f;
        private const float OfficerListHeight = 150f;
        private const float OfficerTitleWidth = 190f;
        private const float OfficerDateWidth = 90f;
        private const float ComboWidth = 360f;
        private const float StoryNoteHeight = 72f;
        private const float StoryCurrentHeight = 120f;
        private const float StoryEditorHeight = 140f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CityAffairs)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("City Affairs")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(CityAffairsViewModel.SituationsTabPartial, AddSituationsTab)
                .DefinePartialView(CityAffairsViewModel.OfficersTabPartial, AddOfficersTab)
                .DefinePartialView(CityAffairsViewModel.StoryTabPartial, AddStoryTab)
                .DefinePartialView(CityAffairsViewModel.BountiesTabPartial, AddBountiesTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Situations")
                        .AddOption("Officers")
                        .AddOption("Story Request")
                        .AddOption("Bounties")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(TabBarWidth)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(CityAffairsViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        // The panel scrolls itself: a group advises no height, so the layout's scrolling host never sees a tall tab
        // overflow and would clip its lower rows without a scrollbar.
        private static void AddPanel(GuiGroup<CityAffairsViewModel> host, System.Action<GuiColumn<CityAffairsViewModel>> content)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.Auto);
                panel.AddColumn(content);
            }).SetWidth(ContentPanelWidth)));
        }

        private static void AddInstructions(GuiColumn<CityAffairsViewModel> content)
        {
            content.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.Instructions)
                    .BindColor(model => model.InstructionsColor)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddSituationsTab(GuiGroup<CityAffairsViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddInstructions(content);

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.StatusText)
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText($"Open situations (up to {SituationRules.MaxOpenSituations}). Undecided ones take their ignored outcome after {SituationRules.ExpiryHours} hours.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.SituationNames)
                                    .BindIsToggled(model => model.SituationToggles)
                                    .BindOnClicked(model => model.SelectSituation());
                            });
                        })
                        .BindRowCount(model => model.SituationNames)
                        .SetRowHeight(SituationRowHeight);
                    row.SetHeight(SituationListHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsSituationListEmpty);
                    row.AddLabel()
                        .SetText("No situations are open. New ones arrive when the server restarts.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsSituationSelected);
                    row.AddLabel()
                        .BindText(model => model.SelectedTitle)
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsSituationSelected);
                    row.AddLabel()
                        .BindText(model => model.SelectedTimeLeft)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsSituationSelected);
                    row.AddText()
                        .BindText(model => model.SelectedText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(DescriptionHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsSituationSelected);
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(ChoiceButtonWidth);
                                cell.AddButton()
                                    .BindText(model => model.ChoiceTexts)
                                    .BindIsEnabled(model => model.ChoiceEnabled)
                                    .BindOnClicked(model => model.OnClickChoice());
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddText()
                                    .BindText(model => model.ChoiceInfos)
                                    .BindTooltip(model => model.ChoiceInfos)
                                    .SetShowBorder(false)
                                    .SetScrollbars(NuiScrollbars.None)
                                    .SetHeight(ChoiceInfoHeight);
                            });
                        })
                        .BindRowCount(model => model.ChoiceTexts)
                        .SetRowHeight(ChoiceRowHeight);
                    row.SetHeight(ChoiceListHeight);
                });
            });
        }

        private static void AddStoryTab(GuiGroup<CityAffairsViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddInstructions(content);

                content.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.StoryStatusText)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.None)
                        .SetHeight(StoryNoteHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasCurrentStory);
                    row.AddLabel()
                        .SetText("Current request")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.HasCurrentStory);
                    row.AddText()
                        .BindText(model => model.CurrentStoryText)
                        .SetScrollbars(NuiScrollbars.Auto)
                        .SetHeight(StoryCurrentHeight);
                });

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText($"New request (up to {StoryRequestRules.MaxLength} characters)")
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetPlaceholder("An invitation for a plot, or a hint about ruins near the city.")
                        .SetMaxLength(StoryRequestRules.MaxLength)
                        .SetIsMultiline(true)
                        .BindValue(model => model.StoryInput)
                        .SetHeight(StoryEditorHeight);
                });

                content.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Post Request")
                        .BindIsEnabled(model => model.CanPostStory)
                        .SetTooltip("Sends the request to the DMs. A new request replaces the old one.")
                        .BindOnClicked(model => model.OnClickPostStory())
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });
            });
        }

        private static void AddOfficersTab(GuiGroup<CityAffairsViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddInstructions(content);

                content.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.OfficerSummary)
                        .SetColor(GuiColor.Cyan)
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.AddLabel().SetText("Officer").SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                    row.AddLabel().SetText("Title").SetWidth(OfficerTitleWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                    row.AddLabel().SetText("Appointed").SetWidth(OfficerDateWidth).SetHeight(LabelHeight).SetHorizontalAlign(NuiHorizontalAlign.Left).SetColor(GuiColor.Cyan);
                });

                content.AddRow(row =>
                {
                    row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.OfficerNames)
                                    .BindIsToggled(model => model.OfficerToggles)
                                    .BindOnClicked(model => model.SelectOfficer());
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(OfficerTitleWidth);
                                cell.AddLabel()
                                    .BindText(model => model.OfficerTitles)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });
                            template.AddCell(cell =>
                            {
                                cell.SetIsVariable(false);
                                cell.SetWidth(OfficerDateWidth);
                                cell.AddLabel()
                                    .BindText(model => model.OfficerDates)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                            });
                        })
                        .BindRowCount(model => model.OfficerNames)
                        .SetRowHeight(OfficerRowHeight);
                    row.SetHeight(OfficerListHeight);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayor);
                    row.AddLabel()
                        .SetText("Appoint a citizen, or pick an officer above to change their title or remove them.")
                        .SetHeight(LabelHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayor);
                    row.AddComboBox()
                        .BindOptions(model => model.CitizenOptions)
                        .BindSelectedIndex(model => model.SelectedCitizenIndex)
                        .BindIsEnabled(model => model.CanAppoint)
                        .SetTooltip("Citizens who are not officers yet.")
                        .SetWidth(ComboWidth)
                        .SetHeight(ButtonHeight);
                    row.AddSpacer();
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayor);
                    row.AddTextEdit()
                        .BindValue(model => model.TitleInput)
                        .SetPlaceholder("Title, such as Minister of Trade")
                        .SetMaxLength(OfficerRules.TitleMaxLength);
                });

                content.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsMayor);
                    row.AddButton()
                        .SetText("Appoint")
                        .BindIsEnabled(model => model.CanAppoint)
                        .SetTooltip("Appoints the citizen above with the title you typed. Officers decide City Affairs situations and earn Leadership XP.")
                        .BindOnClicked(model => model.OnClickAppoint())
                        .SetHeight(ButtonHeight);
                    row.AddButton()
                        .SetText("Set Title")
                        .BindIsEnabled(model => model.HasSelectedOfficer)
                        .SetTooltip("Gives the officer picked in the list the title you typed.")
                        .BindOnClicked(model => model.OnClickSetTitle())
                        .SetHeight(ButtonHeight);
                    row.AddButton()
                        .SetText("Remove")
                        .BindIsEnabled(model => model.HasSelectedOfficer)
                        .SetTooltip("Ends the picked officer's appointment.")
                        .BindOnClicked(model => model.OnClickRemoveOfficer())
                        .SetHeight(ButtonHeight);
                });
            });
        }
    }
}
