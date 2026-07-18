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

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Masteries)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 820f, 580f)
                .SetTitle("Masteries")

                .DefinePartialView(MasteriesViewModel.MyMasteriesPartial, AddMyMasteriesTab)
                .DefinePartialView(MasteriesViewModel.CatalogPartial, AddCatalogTab)
                .DefinePartialView(MasteriesViewModel.RequestFormPartial, AddRequestFormTab)
                .DefinePartialView(MasteriesViewModel.MyRequestsPartial, AddMyRequestsTab)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(40f);
                        row.AddSpacer();

                        row.AddToggleButton()
                            .SetText("My Masteries")
                            .BindIsToggled(model => model.IsMyMasteriesTabToggled)
                            .BindOnClicked(model => model.OnClickMyMasteriesTab())
                            .SetHeight(32f)
                            .SetWidth(160f);

                        row.AddToggleButton()
                            .SetText("Catalog")
                            .BindIsToggled(model => model.IsCatalogTabToggled)
                            .BindOnClicked(model => model.OnClickCatalogTab())
                            .SetHeight(32f)
                            .SetWidth(160f);

                        row.AddToggleButton()
                            .SetText("My Requests")
                            .BindIsToggled(model => model.IsMyRequestsTabToggled)
                            .BindOnClicked(model => model.OnClickMyRequestsTab())
                            .SetHeight(32f)
                            .SetWidth(160f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView(MasteriesViewModel.ContentPartialElement);
                    });
                });

            return _builder.Build();
        }

        // ---------------------------------------------------------------
        // My Masteries
        // ---------------------------------------------------------------

        private static void AddMyMasteriesTab(GuiGroup<MasteriesViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
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
                    row.SetHeight(16f);
                    row.BindIsVisible(model => model.IsTrainingVisible);

                    row.AddProgressBar()
                        .BindValue(model => model.TrainingProgress);
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
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.SetHeight(34f);

                    row.AddTextEdit()
                        .SetPlaceholder("Search")
                        .BindValue(model => model.SearchText);

                    var categoryCombo = row.AddComboBox()
                        .BindSelectedIndex(model => model.SelectedCategoryId)
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
                    row.SetHeight(32f);

                    row.AddButton()
                        .SetText("<< Prev")
                        .SetWidth(90f)
                        .BindIsEnabled(model => model.IsCatalogPrevEnabled)
                        .BindOnClicked(model => model.OnClickCatalogPrevPage());

                    row.AddLabel()
                        .BindText(model => model.CatalogPageText);

                    row.AddButton()
                        .SetText("Next >>")
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
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
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
                    row.SetHeight(30f);
                    row.BindIsVisible(model => model.IsCustomFieldsVisible);

                    row.AddTextEdit()
                        .SetPlaceholder("Unlisted Mastery Name")
                        .SetMaxLength(MasteriesViewModel.MaxCustomNameLength)
                        .BindValue(model => model.CustomName);
                });

                col.AddRow(row =>
                {
                    row.SetHeight(70f);
                    row.BindIsVisible(model => model.IsCustomFieldsVisible);

                    row.AddTextEdit()
                        .SetIsMultiline(true)
                        .SetPlaceholder("Briefly describe this mastery")
                        .SetMaxLength(MasteriesViewModel.MaxCustomDescriptionLength)
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
                    row.SetHeight(120f);
                    row.AddTextEdit()
                        .SetIsMultiline(true)
                        .SetMaxLength(MasteriesViewModel.MaxJustificationLength)
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
                    row.SetHeight(40f);

                    row.AddButton()
                        .SetText("Submit Request")
                        .SetWidth(150f)
                        .BindIsEnabled(model => model.IsSubmitEnabled)
                        .BindOnClicked(model => model.OnClickSubmitRequest());

                    row.AddButton()
                        .SetText("Back to Catalog")
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
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
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
                            r.SetHeight(60f);
                            r.AddTextEdit()
                                .SetIsMultiline(true)
                                .SetPlaceholder("Write a reply...")
                                .SetMaxLength(MasteriesViewModel.MaxReplyLength)
                                .BindValue(model => model.ReplyText);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(40f);

                            r.AddButton()
                                .SetText("Send Reply")
                                .SetWidth(120f)
                                .BindIsEnabled(model => model.IsReplyEnabled)
                                .BindOnClicked(model => model.OnClickSendReply());

                            r.AddButton()
                                .SetText("Cancel Request")
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
