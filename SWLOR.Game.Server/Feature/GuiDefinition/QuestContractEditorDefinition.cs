using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class QuestContractEditorDefinition: IGuiWindowDefinition
    {
        // The form content is regenerated and swapped on window resize (NUI cannot bind layout
        // widths), so every event-bearing element needs a stable Id shared between the boot-time
        // registration copy and the runtime-generated copies.
        public const string ContentElement = "QCE_CONTENT";
        private const string ContentDefaultPartial = "QCE_CONTENT_DEFAULT";
        private const string SearchButtonId = "qce_search";
        private const string SearchResultToggleId = "qce_result_toggle";
        private const string AddObjectiveButtonId = "qce_add_obj";
        private const string ObjectiveToggleId = "qce_obj_toggle";
        private const string ObjectiveRemoveButtonId = "qce_obj_remove";
        private const string ApplyQuantityButtonId = "qce_apply_qty";
        private const string RewardRemoveButtonId = "qce_reward_remove";
        private const string AddRewardButtonId = "qce_add_reward";

        public const float DefaultWindowWidth = 580f;

        private readonly GuiWindowBuilder<QuestContractEditorViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.QuestContractEditor)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, DefaultWindowWidth, 680f)
                .SetTitle("Quest Contract Editor")

                // Registered so the form's element events are hooked at boot. The actual layout
                // shown to the player is generated per current window width by the view model.
                .DefinePartialView(ContentDefaultPartial, group =>
                {
                    BuildContent(group, CalculateContentWidth(DefaultWindowWidth));
                })

                .AddColumn(root =>
                {
                    root.AddRow(row =>
                    {
                        row.SetHeight(24f);

                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .SetColor(255, 0, 0);
                    });

                    root.AddRow(row =>
                    {
                        row.AddPartialView(ContentElement);
                    });

                    root.AddRow(row =>
                    {
                        row.SetHeight(44f);

                        row.AddButton()
                            .SetText("Save Draft")
                            .BindOnClicked(model => model.OnClickSaveDetails())
                            .SetHeight(32f)
                            .SetWidth(120f);

                        row.AddButton()
                            .SetText("Publish")
                            .BindOnClicked(model => model.OnClickPublish())
                            .SetHeight(32f)
                            .SetWidth(120f);

                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Close")
                            .BindOnClicked(model => model.OnClickClose())
                            .SetHeight(32f)
                            .SetWidth(120f);
                    });
                });

            return _builder.Build();
        }

        /// <summary>
        /// Converts a window width into the width available to the form content
        /// (window borders and the scrollbar are subtracted).
        /// </summary>
        public static float CalculateContentWidth(float windowWidth)
        {
            var contentWidth = windowWidth - 60f;
            return contentWidth < 460f ? 460f : contentWidth;
        }

        /// <summary>
        /// Builds the editor's form content sized for a specific width, ready to be swapped into
        /// the content element via NuiSetGroupLayout. Called by the view model whenever the window
        /// is resized.
        /// </summary>
        /// <param name="contentWidth">The width the content should occupy.</param>
        /// <returns>The generated layout Json.</returns>
        public static Json BuildContentLayout(float contentWidth)
        {
            var host = new GuiGroup<QuestContractEditorViewModel>();
            BuildContent(host, contentWidth);

            return host.ToJson();
        }

        private static void BuildContent(GuiGroup<QuestContractEditorViewModel> host, float contentWidth)
        {
            host.SetShowBorder(false);
            host.SetScrollbars(NuiScrollbars.None);

            // The scroll group's content stretches to the fixed-width column above it
            // (the CharacterSheet pattern).
            host.AddColumn(outer =>
            {
                outer.SetWidth(contentWidth);

                outer.AddRow(groupRow =>
                {
                    groupRow.AddGroup(scrollGroup =>
                    {
                        scrollGroup.SetShowBorder(false);
                        scrollGroup.SetScrollbars(NuiScrollbars.Auto);

                        scrollGroup.AddColumn(AddFormRows);
                    });
                });
            });
        }

        private static void AddFormRows(GuiColumn<QuestContractEditorViewModel> col)
        {
            col.AddRow(row =>
            {
                row.SetHeight(42f);

                row.AddTextEdit()
                    .SetPlaceholder("Title")
                    .SetMaxLength(QuestContractBoard.MaxTitleLength)
                    .BindValue(model => model.Title)
                    .SetHeight(34f);
            });

            col.AddRow(row =>
            {
                row.SetHeight(88f);

                row.AddTextEdit()
                    .SetPlaceholder("Description")
                    .SetIsMultiline(true)
                    .SetMaxLength(QuestContractBoard.MaxDescriptionLength)
                    .BindValue(model => model.Description)
                    .SetHeight(80f);
            });

            col.AddRow(row =>
            {
                row.SetHeight(40f);

                row.AddLabel()
                    .SetText("Reward Credits")
                    .SetWidth(140f);

                row.AddTextEdit()
                    .SetPlaceholder("Credits")
                    .BindValue(model => model.RewardCreditsText)
                    .SetHeight(32f)
                    .SetWidth(140f);

                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.SetHeight(20f);

                row.AddLabel()
                    .BindText(model => model.EscrowText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.SetHeight(20f);

                row.AddLabel()
                    .BindText(model => model.PostingFeeText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.SetHeight(20f);

                row.AddLabel()
                    .BindText(model => model.TotalCostText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.SetHeight(22f);

                row.AddLabel()
                    .SetText("Search for an item to request:")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.SetHeight(40f);

                row.AddTextEdit()
                    .SetPlaceholder("Item name")
                    .BindValue(model => model.ItemSearchText)
                    .SetHeight(32f);

                row.AddButton()
                    .SetId(SearchButtonId)
                    .SetText("Search")
                    .SetHeight(32f)
                    .SetWidth(90f)
                    .BindOnClicked(model => model.OnClickSearchItems());
            });

            col.AddRow(row =>
            {
                row.SetHeight(176f);

                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetWidth(32f);
                        cell.SetIsVariable(false);

                        cell.AddGroup(group =>
                        {
                            group.AddImage()
                                .BindResref(model => model.SearchResultIconResrefs)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Top);
                        });
                    });

                    template.AddCell(cell =>
                    {
                        cell.AddToggleButton()
                            .SetId(SearchResultToggleId)
                            .BindText(model => model.SearchResultLabels)
                            .BindIsToggled(model => model.SearchResultToggles)
                            .BindOnClicked(model => model.OnClickSelectSearchResult());
                    });
                })
                    .BindRowCount(model => model.SearchResultLabels)
                    .SetRowHeight(32f);
            });

            col.AddRow(row =>
            {
                row.SetHeight(40f);

                row.AddLabel()
                    .SetText($"Quantity (1-{QuestContractBoard.MaxObjectiveQuantity}):")
                    .SetWidth(135f);

                row.AddTextEdit()
                    .SetPlaceholder("Quantity")
                    .BindValue(model => model.NewObjectiveQuantityText)
                    .SetHeight(32f)
                    .SetWidth(80f);

                row.AddButton()
                    .SetId(AddObjectiveButtonId)
                    .SetText("Add as Objective")
                    .BindIsEnabled(model => model.IsAddObjectiveEnabled)
                    .BindOnClicked(model => model.OnClickAddObjective())
                    .SetHeight(32f)
                    .SetWidth(180f);

                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.SetHeight(22f);

                row.AddLabel()
                    .SetText($"Current Objectives (max {QuestContractBoard.MaxObjectives})")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.SetHeight(116f);

                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetWidth(32f);
                        cell.SetIsVariable(false);

                        cell.AddGroup(group =>
                        {
                            group.AddImage()
                                .BindResref(model => model.ObjectiveIconResrefs)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Top);
                        });
                    });

                    template.AddCell(cell =>
                    {
                        cell.AddToggleButton()
                            .SetId(ObjectiveToggleId)
                            .BindText(model => model.ObjectiveLabels)
                            .BindIsToggled(model => model.ObjectiveToggles)
                            .BindOnClicked(model => model.OnClickSelectObjective());
                    });

                    template.AddCell(cell =>
                    {
                        cell.SetWidth(80f);
                        cell.SetIsVariable(false);

                        cell.AddButton()
                            .SetId(ObjectiveRemoveButtonId)
                            .SetText("Remove")
                            .BindOnClicked(model => model.OnClickRemoveObjective());
                    });
                })
                    .BindRowCount(model => model.ObjectiveLabels)
                    .SetRowHeight(32f);
            });

            col.AddRow(row =>
            {
                row.SetHeight(40f);
                row.BindIsVisible(model => model.IsObjectiveDetailVisible);

                row.AddLabel()
                    .SetText("Selected quantity:")
                    .SetWidth(135f);

                row.AddTextEdit()
                    .SetPlaceholder("Quantity")
                    .BindValue(model => model.ObjectiveQuantityText)
                    .SetHeight(32f)
                    .SetWidth(100f);

                row.AddButton()
                    .SetId(ApplyQuantityButtonId)
                    .SetText("Apply")
                    .BindOnClicked(model => model.OnClickApplyObjective())
                    .SetHeight(32f)
                    .SetWidth(100f);

                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.SetHeight(22f);

                row.AddLabel()
                    .SetText($"Reward Items (max {QuestContractBoard.MaxRewardItems})")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.SetHeight(96f);

                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetWidth(40f);
                        cell.SetIsVariable(false);

                        cell.AddGroup(group =>
                        {
                            group.AddImage()
                                .BindResref(model => model.RewardItemIconResrefs)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Top);
                        });
                    });

                    template.AddCell(cell =>
                    {
                        cell.AddText()
                            .BindText(model => model.RewardItemLabels)
                            .SetShowBorder(false)
                            .SetScrollbars(NuiScrollbars.None);
                    });

                    template.AddCell(cell =>
                    {
                        cell.SetWidth(80f);
                        cell.SetIsVariable(false);

                        cell.AddButton()
                            .SetId(RewardRemoveButtonId)
                            .SetText("Remove")
                            .BindOnClicked(model => model.OnClickRemoveRewardItem());
                    });
                })
                    .BindRowCount(model => model.RewardItemLabels)
                    .SetRowHeight(40f);
            });

            col.AddRow(row =>
            {
                row.SetHeight(40f);

                row.AddButton()
                    .SetId(AddRewardButtonId)
                    .SetText("Add Reward Item")
                    .BindIsEnabled(model => model.IsAddRewardItemEnabled)
                    .BindOnClicked(model => model.OnClickAddRewardItem())
                    .SetHeight(32f)
                    .SetWidth(180f);

                row.AddSpacer();
            });
        }
    }
}
