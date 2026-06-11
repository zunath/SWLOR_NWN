using System;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PazaakDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PazaakViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Pazaak)
                .SetInitialGeometry(0, 0, 720f, 520f)
                .SetTitle("Pazaak")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(PazaakViewModel.DeckPartial, AddDeckPartial)
                .DefinePartialView(PazaakViewModel.MatchPartial, AddMatchPartial)
                .DefinePartialView(PazaakViewModel.TablePartial, AddTablePartial)
                .DefinePartialView(PazaakViewModel.LeaderboardPartial, AddLeaderboardPartial)
                .AddColumn(root =>
                {
                    root.AddRow(row =>
                    {
                        row.SetHeight(32f);
                        row.AddOptions()
                            .AddOption("Deck")
                            .AddOption("Match")
                            .AddOption("Table")
                            .AddOption("Leaderboard")
                            .BindSelectedValue(model => model.SelectedTabId);
                    });

                    root.AddRow(row =>
                    {
                        row.AddPartialView(PazaakViewModel.ContentPartialElement);
                    });
                });

            return _builder.Build();
        }

        private static void AddDeckPartial(GuiGroup<PazaakViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(root =>
            {
                root.AddRow(row =>
                {
                    row.AddLabel()
                        .BindText(model => model.DeckStatus)
                        .SetHeight(24f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                root.AddRow(row =>
                {
                    row.AddGroup(collection =>
                    {
                        collection.SetScrollbars(NuiScrollbars.Y);
                        collection.AddColumn(col =>
                        {
                            col.AddRow(header =>
                            {
                                AddHeader(header, string.Empty, 54f);
                                AddHeader(header, "Collection", 140f);
                                AddHeader(header, "Owned", 52f);
                                AddHeader(header, "Deck", 44f);
                                AddHeader(header, string.Empty, 64f);
                            });

                            col.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(54f);
                                        cell.SetIsVariable(false);
                                        AddCardImage(cell, model => model.CollectionIconResrefs, model => model.CollectionNames);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(140f);
                                        cell.SetIsVariable(false);
                                        cell.AddLabel()
                                            .BindText(model => model.CollectionNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(52f);
                                        cell.SetIsVariable(false);
                                        cell.AddLabel()
                                            .BindText(model => model.CollectionOwned)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(44f);
                                        cell.SetIsVariable(false);
                                        cell.AddLabel()
                                            .BindText(model => model.CollectionInDeck)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(64f);
                                        cell.SetIsVariable(false);
                                        cell.AddButton()
                                            .SetText("Add")
                                            .SetHeight(34f)
                                            .BindOnClicked(model => model.OnClickAddCollectionCard());
                                    });
                                })
                                    .BindRowCount(model => model.CollectionNames)
                                    .SetRowHeight(78f);
                            });
                        });
                    })
                        .SetWidth(390f);

                    row.AddGroup(deck =>
                    {
                        deck.SetScrollbars(NuiScrollbars.Y);
                        deck.AddColumn(col =>
                        {
                            col.AddRow(header =>
                            {
                                AddHeader(header, string.Empty, 54f);
                                AddHeader(header, "Active Side Deck", 142f);
                                AddHeader(header, string.Empty, 80f);
                            });

                            col.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(54f);
                                        cell.SetIsVariable(false);
                                        AddCardImage(cell, model => model.DeckIconResrefs, model => model.DeckNames);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(142f);
                                        cell.SetIsVariable(false);
                                        cell.AddLabel()
                                            .BindText(model => model.DeckNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(80f);
                                        cell.SetIsVariable(false);
                                        cell.AddButton()
                                            .SetText("Remove")
                                            .SetHeight(34f)
                                            .BindOnClicked(model => model.OnClickRemoveDeckCard());
                                    });
                                })
                                    .BindRowCount(model => model.DeckNames)
                                    .SetRowHeight(78f);
                            });
                        });
                    });
                });

                root.AddRow(row =>
                {
                    row.SetHeight(34f);
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Save Deck")
                        .SetWidth(110f)
                        .BindOnClicked(model => model.OnClickSaveDeck());
                    row.AddButton()
                        .SetText("Reset")
                        .SetWidth(80f)
                        .BindOnClicked(model => model.OnClickResetDeck());
                    row.AddSpacer();
                });
            });
        }

        private static void AddMatchPartial(GuiGroup<PazaakViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(root =>
            {
                root.AddRow(row =>
                {
                    row.SetHeight(26f);
                    row.AddLabel()
                        .BindText(model => model.MatchStatus)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                root.AddRow(row =>
                {
                    row.SetHeight(24f);
                    row.AddLabel()
                        .BindText(model => model.MatchScore)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                root.AddRow(row =>
                {
                    row.SetHeight(24f);
                    row.AddLabel().BindText(model => model.YourTotal);
                    row.AddLabel().BindText(model => model.ActiveTurnText);
                    row.AddLabel().BindText(model => model.OpponentTotal);
                });

                root.AddRow(row =>
                {
                    row.SetHeight(188f);
                    AddBoard(row, "Your Board", model => model.YourBoardCards, model => model.YourBoardCardIconResrefs);
                    AddBoard(row, "Opponent Board", model => model.OpponentBoardCards, model => model.OpponentBoardCardIconResrefs);
                });

                root.AddRow(row =>
                {
                    row.AddGroup(hand =>
                    {
                        hand.SetScrollbars(NuiScrollbars.Y);
                        hand.AddColumn(col =>
                        {
                            col.AddRow(header =>
                            {
                                AddHeader(header, string.Empty, 54f);
                                AddHeader(header, "Side Hand", 206f);
                                AddHeader(header, string.Empty, 70f);
                            });

                            col.AddRow(listRow =>
                            {
                                listRow.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(54f);
                                        cell.SetIsVariable(false);
                                        AddCardImage(cell, model => model.SideHandIconResrefs, model => model.SideHandNames);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(206f);
                                        cell.SetIsVariable(false);
                                        cell.AddLabel()
                                            .BindText(model => model.SideHandNames)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                    template.AddCell(cell =>
                                    {
                                        cell.SetWidth(70f);
                                        cell.SetIsVariable(false);
                                        cell.AddButton()
                                            .SetText("Play")
                                            .SetHeight(34f)
                                            .BindOnClicked(model => model.OnClickPlaySideCard());
                                    });
                                })
                                    .BindRowCount(model => model.SideHandNames)
                                    .SetRowHeight(78f);
                            });
                        });
                    });

                    row.AddColumn(actions =>
                    {
                        actions.AddRow(valueRow =>
                        {
                            valueRow.SetHeight(32f);
                            valueRow.AddLabel()
                                .SetText("Value")
                                .SetWidth(60f);
                            valueRow.AddTextEdit()
                                .BindValue(model => model.SelectedSideValue)
                                .SetMaxLength(4)
                                .SetWidth(80f);
                        });

                        actions.AddRow(buttonRow =>
                        {
                            buttonRow.SetHeight(34f);
                            buttonRow.AddButton()
                                .SetText("End Turn")
                                .BindOnClicked(model => model.OnClickEndTurn());
                        });
                        actions.AddRow(buttonRow =>
                        {
                            buttonRow.SetHeight(34f);
                            buttonRow.AddButton()
                                .SetText("Stand")
                                .BindOnClicked(model => model.OnClickStand());
                        });
                        actions.AddRow(buttonRow =>
                        {
                            buttonRow.SetHeight(34f);
                            buttonRow.AddButton()
                                .SetText("Forfeit")
                                .BindOnClicked(model => model.OnClickForfeit());
                        });
                    })
                        .SetWidth(180f);
                });
            });
        }

        private static void AddTablePartial(GuiGroup<PazaakViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(root =>
            {
                root.AddRow(row =>
                {
                    row.SetHeight(28f);
                    row.AddLabel()
                        .BindText(model => model.TableStatus)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                root.AddRow(row =>
                {
                    row.SetHeight(28f);
                    row.AddLabel()
                        .BindText(model => model.NpcStatus)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                root.AddRow(row =>
                {
                    row.SetHeight(32f);
                    row.AddSpacer();
                    row.AddLabel().SetText("Wager").SetWidth(60f);
                    row.AddTextEdit()
                        .BindValue(model => model.WagerText)
                        .SetMaxLength(8)
                        .SetWidth(110f);
                    row.AddCheckBox()
                        .SetText("Rated")
                        .BindIsChecked(model => model.IsRated)
                        .SetWidth(100f);
                    row.AddLabel().SetText("Timer").SetWidth(55f);
                    row.AddTextEdit()
                        .BindValue(model => model.TurnTimerText)
                        .SetMaxLength(4)
                        .SetWidth(80f);
                    row.AddSpacer();
                });

                root.AddRow(row =>
                {
                    row.SetHeight(36f);
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Start NPC")
                        .SetWidth(120f)
                        .BindOnClicked(model => model.OnClickStartNpc());
                    row.AddButton()
                        .SetText("Host Table")
                        .SetWidth(120f)
                        .BindIsEnabled(model => model.IsTableAvailable)
                        .BindOnClicked(model => model.OnClickHostTable());
                    row.AddButton()
                        .SetText("Join Table")
                        .SetWidth(120f)
                        .BindIsEnabled(model => model.IsTableAvailable)
                        .BindOnClicked(model => model.OnClickJoinTable());
                    row.AddButton()
                        .SetText("Cancel")
                        .SetWidth(90f)
                        .BindIsEnabled(model => model.IsTableAvailable)
                        .BindOnClicked(model => model.OnClickCancelTable());
                    row.AddSpacer();
                });
            });
        }

        private static void AddLeaderboardPartial(GuiGroup<PazaakViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.Y);
            group.AddColumn(root =>
            {
                root.AddRow(header =>
                {
                    AddHeader(header, "#", 50f);
                    AddHeader(header, "Player", 360f);
                    AddHeader(header, "Rating", 120f);
                });

                root.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetWidth(50f);
                            cell.SetIsVariable(false);
                            cell.AddLabel()
                                .BindText(model => model.LeaderboardRanks)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetWidth(360f);
                            cell.SetIsVariable(false);
                            cell.AddLabel()
                                .BindText(model => model.LeaderboardNames)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetWidth(120f);
                            cell.SetIsVariable(false);
                            cell.AddLabel()
                                .BindText(model => model.LeaderboardRatings)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });
                    })
                        .BindRowCount(model => model.LeaderboardRanks)
                        .SetRowHeight(28f);
                });
            });
        }

        private static void AddBoard(
            GuiRow<PazaakViewModel> row,
            string title,
            System.Linq.Expressions.Expression<Func<PazaakViewModel, GuiBindingList<string>>> cardsExpression,
            System.Linq.Expressions.Expression<Func<PazaakViewModel, GuiBindingList<string>>> iconResrefsExpression)
        {
            row.AddGroup(group =>
            {
                group.SetScrollbars(NuiScrollbars.Y);
                group.AddColumn(col =>
                {
                    col.AddRow(header =>
                    {
                        header.SetHeight(24f);
                        header.AddLabel()
                            .SetText(title)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                    });
                    col.AddRow(listRow =>
                    {
                        listRow.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.SetWidth(54f);
                                cell.SetIsVariable(false);
                                AddCardImage(cell, iconResrefsExpression, cardsExpression);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(cardsExpression)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                        })
                            .BindRowCount(cardsExpression)
                            .SetRowHeight(78f);
                    });
                });
            });
        }

        private static void AddCardImage(
            GuiTemplateCell<PazaakViewModel> cell,
            System.Linq.Expressions.Expression<Func<PazaakViewModel, GuiBindingList<string>>> iconResrefsExpression,
            System.Linq.Expressions.Expression<Func<PazaakViewModel, GuiBindingList<string>>> tooltipExpression)
        {
            cell.AddGroup(group =>
            {
                group.SetScrollbars(NuiScrollbars.None);
                group.AddImage()
                    .BindResref(iconResrefsExpression)
                    .SetAspect(NuiAspect.ExactScaled)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle)
                    .SetWidth(48f)
                    .SetHeight(72f)
                    .BindTooltip(tooltipExpression);
            });
        }

        private static void AddHeader(GuiRow<PazaakViewModel> row, string text, float width)
        {
            row.AddLabel()
                .SetText(text)
                .SetWidth(width)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left);
        }
    }
}
