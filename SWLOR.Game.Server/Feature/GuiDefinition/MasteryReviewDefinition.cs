using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.MasteryService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// DM/Admin staff window opened via the /masteryreview chat command
    /// (Feature/ChatCommandDefinition/MasteryChatCommand.cs). Two toggle-button tabs:
    /// the review queue itself, and a catalog-management screen folded in per
    /// MASTERY_SPEC.md's guidance to keep catalog editing "smallest viable" rather than a
    /// separate window/GuiWindowType.
    /// </summary>
    public class MasteryReviewDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<MasteryReviewViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.MasteryReview)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 900f, 620f)
                .SetTitle("Mastery Review Queue")

                .DefinePartialView(MasteryReviewViewModel.ReviewQueuePartial, AddReviewQueueTab)
                .DefinePartialView(MasteryReviewViewModel.CatalogManagePartial, AddCatalogManageTab)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(40f);
                        row.AddSpacer();

                        row.AddToggleButton()
                            .SetText("Review Queue")
                            .BindIsToggled(model => model.IsReviewQueueTabToggled)
                            .BindOnClicked(model => model.OnClickReviewQueueTab())
                            .SetHeight(32f)
                            .SetWidth(160f);

                        row.AddToggleButton()
                            .SetText("Catalog")
                            .BindIsToggled(model => model.IsCatalogTabToggled)
                            .BindOnClicked(model => model.OnClickCatalogManageTab())
                            .SetHeight(32f)
                            .SetWidth(160f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView(MasteryReviewViewModel.ContentPartialElement);
                    });
                });

            return _builder.Build();
        }

        // ---------------------------------------------------------------
        // Review queue
        // ---------------------------------------------------------------

        private static void AddReviewQueueTab(GuiGroup<MasteryReviewViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddColumn(left =>
                    {
                        left.SetWidth(300f);

                        left.AddRow(r =>
                        {
                            r.SetHeight(34f);

                            r.AddTextEdit()
                                .SetPlaceholder("Search player...")
                                .BindValue(model => model.SearchText);

                            r.AddComboBox()
                                .BindSelectedIndex(model => model.SelectedStatusFilterId)
                                .SetWidth(150f)
                                .AddOption("Pending", 0)
                                .AddOption("In Review", 1)
                                .AddOption("Recently Decided", 2);
                        });

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

                        left.AddRow(r =>
                        {
                            r.SetHeight(32f);

                            r.AddButton()
                                .SetText("<< Prev")
                                .SetWidth(90f)
                                .BindIsEnabled(model => model.IsPrevEnabled)
                                .BindOnClicked(model => model.OnClickPrevPage());

                            r.AddLabel()
                                .BindText(model => model.PageText);

                            r.AddButton()
                                .SetText("Next >>")
                                .SetWidth(90f)
                                .BindIsEnabled(model => model.IsNextEnabled)
                                .BindOnClicked(model => model.OnClickNextPage());
                        });
                    });

                    row.AddColumn(right =>
                    {
                        right.AddRow(r =>
                        {
                            r.SetHeight(24f);
                            r.AddLabel()
                                .BindText(model => model.DetailHeaderText);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(60f);
                            r.AddText()
                                .BindText(model => model.JustificationText)
                                .SetShowBorder(false);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(20f);
                            r.AddLabel()
                                .SetText("Rules Check");
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(80f);
                            r.AddText()
                                .BindText(model => model.RulesCheckText)
                                .SetShowBorder(false);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(20f);
                            r.AddLabel()
                                .SetText("Player Profile");
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(60f);
                            r.AddText()
                                .BindText(model => model.PlayerProfileText)
                                .SetShowBorder(false);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(60f);
                            r.AddText()
                                .BindText(model => model.CommentsText)
                                .SetShowBorder(false);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(50f);
                            r.AddTextEdit()
                                .SetIsMultiline(true)
                                .SetPlaceholder("Reply / staff comment...")
                                .SetMaxLength(500)
                                .BindValue(model => model.ReplyText);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(32f);

                            r.AddCheckBox()
                                .SetText("Use Quick Slot")
                                .BindIsChecked(model => model.UseQuickSlot)
                                .BindIsEnabled(model => model.IsQuickSlotCheckboxEnabled);

                            r.AddCheckBox()
                                .SetText("Instant Grant")
                                .BindIsChecked(model => model.IsInstantGrant);

                            r.AddLabel()
                                .BindText(model => model.DecisionDurationText);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(32f);
                            r.AddTextEdit()
                                .SetPlaceholder("Feedback to player (required to deny)")
                                .SetMaxLength(1000)
                                .BindValue(model => model.FeedbackText);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(32f);
                            r.BindIsVisible(model => model.IsOverrideReasonVisible);

                            r.AddTextEdit()
                                .SetPlaceholder("Override reason (required - a rule check failed)")
                                .SetMaxLength(500)
                                .BindValue(model => model.OverrideReasonText);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(20f);
                            r.AddLabel()
                                .BindText(model => model.StatusMessageText)
                                .SetColor(255, 220, 0);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(36f);

                            r.AddButton()
                                .SetText("Approve & Queue")
                                .BindIsEnabled(model => model.IsApproveEnabled)
                                .BindOnClicked(model => model.OnClickApprove());

                            r.AddButton()
                                .SetText("Deny")
                                .BindIsEnabled(model => model.IsRequestSelected)
                                .BindOnClicked(model => model.OnClickDeny());

                            r.AddButton()
                                .SetText("Comment Only")
                                .BindIsEnabled(model => model.IsRequestSelected)
                                .BindOnClicked(model => model.OnClickCommentOnly());

                            r.AddButton()
                                .SetText("Open Full Profile")
                                .BindIsEnabled(model => model.IsOpenProfileEnabled)
                                .BindDisabledTooltip(model => model.OpenProfileDisabledTooltip)
                                .BindOnClicked(model => model.OnClickOpenFullProfile());
                        });
                    });
                });
            });
        }

        // ---------------------------------------------------------------
        // Catalog management
        // ---------------------------------------------------------------

        private static void AddCatalogManageTab(GuiGroup<MasteryReviewViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddColumn(left =>
                    {
                        left.SetWidth(320f);

                        left.AddRow(r =>
                        {
                            r.SetHeight(34f);

                            r.AddTextEdit()
                                .SetPlaceholder("Search")
                                .BindValue(model => model.CatalogSearchText);

                            var categoryCombo = r.AddComboBox()
                                .BindSelectedIndex(model => model.CatalogSelectedCategoryId)
                                .SetWidth(200f)
                                .AddOption("All Categories", -1);

                            foreach (MasteryCategoryType category in Enum.GetValues(typeof(MasteryCategoryType)))
                            {
                                categoryCombo.AddOption(
                                    category.GetAttribute<MasteryCategoryType, MasteryCategoryAttribute>().Label,
                                    (int)category);
                            }
                        });

                        left.AddRow(r =>
                        {
                            r.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.CatalogManageLabels)
                                        .BindIsToggled(model => model.CatalogManageToggles)
                                        .BindOnClicked(model => model.OnClickSelectCatalogRow());
                                });
                            })
                                .BindRowCount(model => model.CatalogManageLabels)
                                .SetRowHeight(28f);
                        });

                        left.AddRow(r =>
                        {
                            r.SetHeight(32f);

                            r.AddButton()
                                .SetText("<< Prev")
                                .SetWidth(90f)
                                .BindIsEnabled(model => model.IsCatalogManagePrevEnabled)
                                .BindOnClicked(model => model.OnClickCatalogPrevPage());

                            r.AddLabel()
                                .BindText(model => model.CatalogManagePageText);

                            r.AddButton()
                                .SetText("Next >>")
                                .SetWidth(90f)
                                .BindIsEnabled(model => model.IsCatalogManageNextEnabled)
                                .BindOnClicked(model => model.OnClickCatalogNextPage());
                        });

                        left.AddRow(r =>
                        {
                            r.SetHeight(32f);
                            r.AddButton()
                                .SetText("New Mastery")
                                .BindOnClicked(model => model.OnClickNewMasteryEntry());
                        });
                    });

                    row.AddColumn(right =>
                    {
                        right.AddRow(r =>
                        {
                            r.SetHeight(32f);
                            r.AddTextEdit()
                                .SetPlaceholder("Name")
                                .SetMaxLength(100)
                                .BindIsEnabled(model => model.IsCatalogEntrySelected)
                                .BindValue(model => model.CatalogEditName);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(32f);

                            var categoryCombo = r.AddComboBox()
                                .BindSelectedIndex(model => model.CatalogEditCategoryId)
                                .BindIsEnabled(model => model.IsCatalogEntrySelected);

                            foreach (MasteryCategoryType category in Enum.GetValues(typeof(MasteryCategoryType)))
                            {
                                categoryCombo.AddOption(
                                    category.GetAttribute<MasteryCategoryType, MasteryCategoryAttribute>().Label,
                                    (int)category);
                            }

                            var rarityCombo = r.AddComboBox()
                                .BindSelectedIndex(model => model.CatalogEditRarityId)
                                .BindIsEnabled(model => model.IsCatalogEntrySelected)
                                .SetWidth(130f)
                                .AddOption("Standard", (int)MasteryRarityType.Standard)
                                .AddOption("Rare", (int)MasteryRarityType.Rare)
                                .AddOption("Off Limit", (int)MasteryRarityType.OffLimit);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(32f);

                            r.AddComboBox()
                                .BindOptions(model => model.SkillOptions)
                                .BindSelectedIndex(model => model.CatalogEditSkillId)
                                .BindIsEnabled(model => model.IsCatalogEntrySelected);

                            r.AddCheckBox()
                                .SetText("Active")
                                .BindIsChecked(model => model.CatalogEditIsActive)
                                .BindIsEnabled(model => model.IsCatalogEntrySelected);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(140f);
                            r.AddTextEdit()
                                .SetIsMultiline(true)
                                .SetPlaceholder("Description")
                                .SetMaxLength(1000)
                                .BindIsEnabled(model => model.IsCatalogEntrySelected)
                                .BindValue(model => model.CatalogEditDescription);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(20f);
                            r.AddLabel()
                                .BindText(model => model.CatalogStatusText)
                                .SetColor(255, 220, 0);
                        });

                        right.AddRow(r =>
                        {
                            r.SetHeight(36f);
                            r.AddButton()
                                .SetText("Save")
                                .BindIsEnabled(model => model.IsCatalogEntrySelected)
                                .BindOnClicked(model => model.OnClickSaveCatalogEntry());
                        });
                    });
                });
            });
        }
    }
}
