using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.MasteryService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Player-facing Masteries window. Three toggle-button tabs (My Masteries / Catalog /
    /// My Requests) plus a fourth Request Form view reachable only from the Catalog tab.
    /// Each tab's content is registered as a named partial view and swapped into a single
    /// stable placeholder element at runtime (MasteriesViewModel.ContentPartialElement) -
    /// the same pattern CharacterSheetDefinition uses for its tabbed detail area - rather
    /// than four BindIsVisible-toggled rows, which would leave three empty flex rows each
    /// reserving a quarter of the window's height even while hidden.
    /// </summary>
    public class MasteriesDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MasteriesViewModel> _builder = new();
        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 40f;
        private const float ContentPanelWidth = 560f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Masteries)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 820f, 580f)
                .SetTitle("Masteries")

                .DefinePartialView(MasteriesViewModel.MyMasteriesPartial, AddMyMasteriesTab)
                .DefinePartialView(MasteriesViewModel.CatalogPartial, AddCatalogTab)
                .DefinePartialView(MasteriesViewModel.RequestFormPartial, AddRequestFormTab)
                .DefinePartialView(MasteriesViewModel.MyRequestsPartial, AddMyRequestsTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("My Masteries")
                        .AddOption("Catalog")
                        .AddOption("My Requests")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetHeight(TabRowHeight)
                        .SetWidth(480f);
                });
                layout.SetContentPartialElement(MasteriesViewModel.ContentPartialElement);
            });

            return _builder.Build();
        }

        private static void AddTabShell(
            GuiGroup<MasteriesViewModel> host,
            Action<GuiColumn<MasteriesViewModel>> content)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content);
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        // ---------------------------------------------------------------
        // My Masteries
        // ---------------------------------------------------------------

        private static void AddMyMasteriesTab(GuiGroup<MasteriesViewModel> group)
        {
            AddTabShell(group, col =>
            {
                col.AddRow(row =>
                {
                    row.SetHeight(70f);
                    row.AddText()
                        .BindText(model => model.TotalsText)
                        .SetShowBorder(false);
                });

                col.AddRow(row =>
                {
                    row.SetHeight(60f);
                    row.AddText()
                        .BindText(model => model.TrainingText)
                        .SetShowBorder(false);
                });

                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsTrainingVisible);

                    row.AddProgressBar()
                        .BindValue(model => model.TrainingProgress)
                        .SetHeight(16f);
                });

                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddText()
                                .BindText(model => model.OwnedMasteryLabels)
                                .SetShowBorder(false)
                                .SetScrollbars(NuiScrollbars.None);
                        });
                    })
                        .BindRowCount(model => model.OwnedMasteryLabels)
                        .SetRowHeight(28f);
                });
            });
        }

        // ---------------------------------------------------------------
        // Catalog
        // ---------------------------------------------------------------

        private static void AddCatalogTab(GuiGroup<MasteriesViewModel> group)
        {
            AddTabShell(group, col =>
            {
                col.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetPlaceholder("Search")
                        .BindValue(model => model.SearchText)
                        .SetHeight(32f);

                    var categoryCombo = row.AddComboBox()
                        .BindSelectedIndex(model => model.SelectedCategoryId)
                        .SetHeight(32f)
                        .SetWidth(220f)
                        .AddOption("All Categories", -1);

                    foreach (MasteryCategoryType category in Enum.GetValues(typeof(MasteryCategoryType)))
                    {
                        categoryCombo.AddOption(
                            category.GetAttribute<MasteryCategoryType, MasteryCategoryAttribute>().Label,
                            (int)category);
                    }

                    row.AddButton()
                        .SetText("Request Unlisted Mastery")
                        .SetHeight(32f)
                        .SetWidth(190f)
                        .BindOnClicked(model => model.OnClickRequestUnlisted());
                });

                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddText()
                                .BindText(model => model.CatalogLabels)
                                .SetShowBorder(false)
                                .SetScrollbars(NuiScrollbars.None);
                        });

                        template.AddCell(cell =>
                        {
                            cell.SetWidth(90f);
                            cell.SetIsVariable(false);

                            cell.AddButton()
                                .SetText("Request")
                                .BindIsEnabled(model => model.CatalogRequestEnabled)
                                .BindDisabledTooltip(model => model.CatalogRequestTooltips)
                                .BindOnClicked(model => model.OnClickRequestCatalogRow());
                        });
                    })
                        .BindRowCount(model => model.CatalogLabels)
                        .SetRowHeight(48f);
                });

                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("<< Prev")
                        .SetHeight(32f)
                        .SetWidth(90f)
                        .BindIsEnabled(model => model.IsCatalogPrevEnabled)
                        .BindOnClicked(model => model.OnClickCatalogPrevPage());

                    row.AddLabel()
                        .BindText(model => model.CatalogPageText);

                    row.AddButton()
                        .SetText("Next >>")
                        .SetHeight(32f)
                        .SetWidth(90f)
                        .BindIsEnabled(model => model.IsCatalogNextEnabled)
                        .BindOnClicked(model => model.OnClickCatalogNextPage());
                });
            });
        }

        // ---------------------------------------------------------------
        // Request form
        // ---------------------------------------------------------------

        private static void AddRequestFormTab(GuiGroup<MasteriesViewModel> group)
        {
            AddTabShell(group, col =>
            {
                col.AddRow(row =>
                {
                    row.SetHeight(30f);

                    row.AddLabel()
                        .BindText(model => model.RequestMasteryLabel)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);

                    row.AddLabel()
                        .BindText(model => model.RequestTierLabel)
                        .SetWidth(90f);
                });

                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsCustomFieldsVisible);

                    row.AddTextEdit()
                        .SetPlaceholder("Unlisted Mastery Name")
                        .SetMaxLength(MasteriesViewModel.MaxCustomNameLength)
                        .SetHeight(32f)
                        .BindValue(model => model.CustomName);
                });

                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsCustomFieldsVisible);

                    row.AddTextEdit()
                        .SetIsMultiline(true)
                        .SetPlaceholder("Briefly describe this mastery")
                        .SetMaxLength(MasteriesViewModel.MaxCustomDescriptionLength)
                        .SetHeight(70f)
                        .BindValue(model => model.CustomDescription);
                });

                col.AddRow(row =>
                {
                    row.SetHeight(20f);
                    row.AddLabel()
                        .SetText("RP Justification")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                col.AddRow(row =>
                {
                    row.AddTextEdit()
                        .SetIsMultiline(true)
                        .SetMaxLength(MasteriesViewModel.MaxJustificationLength)
                        .SetHeight(120f)
                        .BindValue(model => model.Justification);
                });

                col.AddRow(row =>
                {
                    row.SetHeight(20f);
                    row.AddLabel()
                        .SetText("Eligibility")
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                });

                col.AddRow(row =>
                {
                    row.AddText()
                        .BindText(model => model.EligibilityText)
                        .SetShowBorder(false);
                });

                col.AddRow(row =>
                {
                    row.SetHeight(24f);
                    row.AddLabel()
                        .BindText(model => model.FormStatusText)
                        .SetColor(255, 0, 0);
                });

                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("Submit Request")
                        .SetHeight(32f)
                        .SetWidth(150f)
                        .BindIsEnabled(model => model.IsSubmitEnabled)
                        .BindOnClicked(model => model.OnClickSubmitRequest());

                    row.AddButton()
                        .SetText("Back to Catalog")
                        .SetHeight(32f)
                        .SetWidth(150f)
                        .BindOnClicked(model => model.OnClickBackToCatalog());
                });
            });
        }

        // ---------------------------------------------------------------
        // My Requests
        // ---------------------------------------------------------------

        private static void AddMyRequestsTab(GuiGroup<MasteriesViewModel> group)
        {
            AddTabShell(group, col =>
            {
                col.AddRow(row =>
                {
                    row.AddColumn(left =>
                    {
                        left.AddRow(r =>
                        {
                            r.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.RequestLabels)
                                        .BindIsToggled(model => model.RequestToggles)
                                        .BindColor(model => model.RequestColors)
                                        .BindOnClicked(model => model.OnClickSelectRequest());
                                });
                            })
                                .BindRowCount(model => model.RequestLabels)
                                .SetRowHeight(40f);
                        });
                    })
                        .SetWidth(260f);

                    row.AddColumn(right =>
                    {
                        right.AddRow(r =>
                        {
                            r.SetHeight(90f);
                            r.AddText()
                                .BindText(model => model.RequestDetailText)
                                .SetShowBorder(false);
                        });

                        right.AddRow(r =>
                        {
                            r.AddText()
                                .BindText(model => model.CommentsText)
                                .SetShowBorder(false);
                        });

                        right.AddRow(r =>
                        {
                            r.AddTextEdit()
                                .SetIsMultiline(true)
                                .SetPlaceholder("Write a reply...")
                                .SetMaxLength(MasteriesViewModel.MaxReplyLength)
                                .SetHeight(60f)
                                .BindValue(model => model.ReplyText);
                        });

                        right.AddRow(r =>
                        {
                            r.AddButton()
                                .SetText("Send Reply")
                                .SetHeight(32f)
                                .SetWidth(120f)
                                .BindIsEnabled(model => model.IsReplyEnabled)
                                .BindOnClicked(model => model.OnClickSendReply());

                            r.AddButton()
                                .SetText("Cancel Request")
                                .SetHeight(32f)
                                .SetWidth(140f)
                                .BindIsEnabled(model => model.IsCancelEnabled)
                                .BindOnClicked(model => model.OnClickCancelRequest());
                        });
                    });
                });
            });
        }
    }
}
