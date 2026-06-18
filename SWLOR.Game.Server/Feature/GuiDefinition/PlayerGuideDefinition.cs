using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PlayerGuideDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PlayerGuideViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.PlayerGuide)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 980f, 640f)
                .SetTitle("Player Guide")
                .AddColumn(shell =>
                {
                    shell.AddRow(root =>
                    {
                        root.AddColumn(left =>
                        {
                            left.SetWidth(250f);

                            left.AddRow(row =>
                            {
                                row.AddLabel()
                                    .SetText("BROWSE")
                                    .SetHeight(18f)
                                    .SetColor(142, 153, 148)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Top);
                            });

                            left.AddRow(row =>
                            {
                                row.AddTextEdit()
                                    .SetPlaceholder("Search topics")
                                    .BindValue(model => model.SearchText)
                                    .SetHeight(34f);

                                row.AddButton()
                                    .SetText("X")
                                    .SetWidth(34f)
                                    .SetHeight(34f)
                                    .BindOnClicked(model => model.OnClickClearSearch())
                                    .SetTooltip("Clear search");

                                row.AddButton()
                                    .SetText("Search")
                                    .SetWidth(64f)
                                    .SetHeight(34f)
                                    .BindOnClicked(model => model.OnClickSearch());
                            });

                            left.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.TopicButtonTexts)
                                            .BindIsToggled(model => model.TopicSelections)
                                            .BindTooltip(model => model.TopicTooltips)
                                            .BindOnClicked(model => model.OnClickTopic());
                                    });
                                })
                                    .SetRowHeight(42f)
                                    .SetScrollbars(NuiScrollbars.Auto)
                                    .BindRowCount(model => model.TopicButtonTexts);
                            });
                        });

                        root.AddColumn(main =>
                        {
                            main.AddRow(heroRow =>
                            {
                                heroRow.AddGroup(hero =>
                                {
                                    hero.SetScrollbars(NuiScrollbars.None);
                                    hero.AddColumn(heroColumn =>
                                    {
                                        heroColumn.AddRow(row =>
                                        {
                                            row.AddLabel()
                                                .BindText(model => model.SelectedTopicCategory)
                                                .SetHeight(18f)
                                                .SetColor(221, 181, 93)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                                .SetVerticalAlign(NuiVerticalAlign.Top);
                                        });

                                        heroColumn.AddRow(row =>
                                        {
                                            row.AddLabel()
                                                .BindText(model => model.SelectedTopicName)
                                                .SetHeight(30f)
                                                .SetColor(237, 241, 236)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                                        });

                                        heroColumn.AddRow(row =>
                                        {
                                            row.AddText()
                                                .BindText(model => model.SelectedTopicSummary)
                                                .SetShowBorder(false)
                                                .SetScrollbars(NuiScrollbars.None)
                                                .SetHeight(54f);
                                        });
                                    });
                                })
                                    .SetHeight(118f);
                            });

                            main.AddRow(row =>
                            {
                                row.AddText()
                                    .BindText(model => model.SelectedArticleBody)
                                    .SetScrollbars(NuiScrollbars.Auto);
                            });
                        });

                        root.AddColumn(context =>
                        {
                            context.SetWidth(250f);

                            context.AddRow(commonRow =>
                            {
                                commonRow.AddGroup(common =>
                                {
                                    common.SetScrollbars(NuiScrollbars.Auto);
                                    common.AddColumn(col =>
                                    {
                                        col.AddRow(row =>
                                        {
                                            row.AddLabel()
                                                .SetText("COMMON QUESTIONS")
                                                .SetHeight(18f)
                                                .SetColor(142, 153, 148)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                                .SetVerticalAlign(NuiVerticalAlign.Top);
                                        });

                                        col.AddRow(row =>
                                        {
                                            row.AddText()
                                                .BindText(model => model.QuestionSummaryText)
                                                .SetScrollbars(NuiScrollbars.Auto);
                                        });
                                    });
                                })
                                    .SetHeight(270f);
                            });

                            context.AddRow(relatedRow =>
                            {
                                relatedRow.AddGroup(related =>
                                {
                                    related.SetScrollbars(NuiScrollbars.Auto);
                                    related.AddColumn(col =>
                                    {
                                        col.AddRow(row =>
                                        {
                                            row.AddLabel()
                                                .SetText("RELATED TOPICS")
                                                .SetHeight(18f)
                                                .SetColor(142, 153, 148)
                                                .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                                .SetVerticalAlign(NuiVerticalAlign.Top);
                                        });

                                        col.AddRow(row =>
                                        {
                                            row.AddList(template =>
                                            {
                                                template.AddCell(cell =>
                                                {
                                                    cell.AddButton()
                                                        .BindText(model => model.RelatedTopicTexts)
                                                        .BindTooltip(model => model.RelatedTopicTooltips)
                                                        .BindOnClicked(model => model.OnClickRelatedTopic());
                                                });
                                            })
                                                .SetRowHeight(40f)
                                                .SetScrollbars(NuiScrollbars.Auto)
                                                .BindRowCount(model => model.RelatedTopicTexts);
                                        });
                                    });
                                });
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
