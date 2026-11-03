using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PazaakDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PazaakViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float ContentPanelWidth = 500f;
        private const float HalfWidth = 230f;
        private const float TableCardSize = 44f;
        private const float GridWidth = 156f;
        private const float LightSize = 20f;
        private const float LightColumnWidth = 30f;
        private const float TotalBoxWidth = 44f;
        private const float TotalBoxHeight = 32f;
        private const float CardButtonSize = 48f;
        private const float TurnButtonWidth = 100f;
        private const float ActionButtonHeight = 32f;
        private const float NameWidth = 150f;
        private const float ListCardSize = 36f;

        private static readonly Expression<Func<PazaakViewModel, string>>[] OpponentSlots =
        {
            m => m.OpponentSlot0, m => m.OpponentSlot1, m => m.OpponentSlot2,
            m => m.OpponentSlot3, m => m.OpponentSlot4, m => m.OpponentSlot5,
            m => m.OpponentSlot6, m => m.OpponentSlot7, m => m.OpponentSlot8,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] PlayerSlots =
        {
            m => m.PlayerSlot0, m => m.PlayerSlot1, m => m.PlayerSlot2,
            m => m.PlayerSlot3, m => m.PlayerSlot4, m => m.PlayerSlot5,
            m => m.PlayerSlot6, m => m.PlayerSlot7, m => m.PlayerSlot8,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] OpponentSlotImages =
        {
            m => m.OpponentSlot0Image, m => m.OpponentSlot1Image, m => m.OpponentSlot2Image, m => m.OpponentSlot3Image, m => m.OpponentSlot4Image, m => m.OpponentSlot5Image, m => m.OpponentSlot6Image, m => m.OpponentSlot7Image, m => m.OpponentSlot8Image,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] PlayerSlotImages =
        {
            m => m.PlayerSlot0Image, m => m.PlayerSlot1Image, m => m.PlayerSlot2Image, m => m.PlayerSlot3Image, m => m.PlayerSlot4Image, m => m.PlayerSlot5Image, m => m.PlayerSlot6Image, m => m.PlayerSlot7Image, m => m.PlayerSlot8Image,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] HandImages =
        {
            m => m.Hand0Image, m => m.Hand1Image, m => m.Hand2Image, m => m.Hand3Image,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] DeckSlotImages =
        {
            m => m.DeckSlot0Image, m => m.DeckSlot1Image, m => m.DeckSlot2Image, m => m.DeckSlot3Image, m => m.DeckSlot4Image, m => m.DeckSlot5Image, m => m.DeckSlot6Image, m => m.DeckSlot7Image, m => m.DeckSlot8Image, m => m.DeckSlot9Image,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] HandTooltips =
        {
            m => m.Hand0Tooltip, m => m.Hand1Tooltip, m => m.Hand2Tooltip, m => m.Hand3Tooltip,
        };

        private static readonly Expression<Func<PazaakViewModel, bool>>[] HandEnabled =
        {
            m => m.IsHand0Enabled, m => m.IsHand1Enabled, m => m.IsHand2Enabled, m => m.IsHand3Enabled,
        };

        private static readonly Expression<Func<PazaakViewModel, Action>>[] HandClicks =
        {
            m => m.OnClickHand0(), m => m.OnClickHand1(), m => m.OnClickHand2(), m => m.OnClickHand3(),
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] PlayerSetLights =
        {
            m => m.PlayerSetLight0, m => m.PlayerSetLight1, m => m.PlayerSetLight2,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] OpponentSetLights =
        {
            m => m.OpponentSetLight0, m => m.OpponentSetLight1, m => m.OpponentSetLight2,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] OpponentHandImages =
        {
            m => m.OpponentHand0Image, m => m.OpponentHand1Image, m => m.OpponentHand2Image, m => m.OpponentHand3Image,
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] HandSigns =
        {
            m => m.HandSign0, m => m.HandSign1, m => m.HandSign2, m => m.HandSign3,
        };

        private static readonly Expression<Func<PazaakViewModel, bool>>[] HandSignEnabled =
        {
            m => m.IsHandSign0Enabled, m => m.IsHandSign1Enabled, m => m.IsHandSign2Enabled, m => m.IsHandSign3Enabled,
        };

        private static readonly Expression<Func<PazaakViewModel, Action>>[] HandFlipClicks =
        {
            m => m.OnClickFlipHand0(), m => m.OnClickFlipHand1(), m => m.OnClickFlipHand2(), m => m.OnClickFlipHand3(),
        };

        private static readonly Expression<Func<PazaakViewModel, string>>[] DeckSlots =
        {
            m => m.DeckSlot0, m => m.DeckSlot1, m => m.DeckSlot2, m => m.DeckSlot3, m => m.DeckSlot4,
            m => m.DeckSlot5, m => m.DeckSlot6, m => m.DeckSlot7, m => m.DeckSlot8, m => m.DeckSlot9,
        };

        private static readonly Expression<Func<PazaakViewModel, Action>>[] DeckClicks =
        {
            m => m.OnClickDeckSlot0(), m => m.OnClickDeckSlot1(), m => m.OnClickDeckSlot2(),
            m => m.OnClickDeckSlot3(), m => m.OnClickDeckSlot4(), m => m.OnClickDeckSlot5(),
            m => m.OnClickDeckSlot6(), m => m.OnClickDeckSlot7(), m => m.OnClickDeckSlot8(),
            m => m.OnClickDeckSlot9(),
        };

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Pazaak)
                .SetInitialGeometry(0, 0, 540f, 660f)
                .SetTitle("Pazaak")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(PazaakViewModel.TablePartial, AddTableTab)
                .DefinePartialView(PazaakViewModel.SideDeckPartial, AddSideDeckTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Table")
                        .AddOption("Side Deck")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(232f)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(PazaakViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        private static void AddTabPanel(GuiGroup<PazaakViewModel> host, Action<GuiColumn<PazaakViewModel>> buildContent)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(buildContent);
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddCenteredLabel(GuiColumn<PazaakViewModel> col, Expression<Func<PazaakViewModel, string>> text)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(text)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center);
            });
        }

        private static void AddLight(GuiRow<PazaakViewModel> row, Expression<Func<PazaakViewModel, string>> image)
        {
            row.AddImage()
                .BindResref(image)
                .SetAspect(NuiAspect.Fit)
                .SetWidth(LightSize)
                .SetHeight(LightSize);
        }

        private static void AddTotalBox(GuiRow<PazaakViewModel> row, Expression<Func<PazaakViewModel, string>> total)
        {
            row.AddGroup(box =>
            {
                box.SetShowBorder(true);
                box.SetScrollbars(NuiScrollbars.None);
                box.AddLabel()
                    .BindText(total)
                    .SetHorizontalAlign(NuiHorizontalAlign.Center)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
            })
                .SetWidth(TotalBoxWidth)
                .SetHeight(TotalBoxHeight);
        }

        // Set-won lights stand in a column on the outer edge of each player's grid.
        private static void AddSetLights(GuiRow<PazaakViewModel> row, Expression<Func<PazaakViewModel, string>>[] lights)
        {
            row.AddColumn(col =>
            {
                foreach (var light in lights)
                {
                    col.AddRow(lightRow => AddLight(lightRow, light));
                }
            })
                .SetWidth(LightColumnWidth);
        }

        // A player's nine table slots as a 3x3 grid, filled left to right and top to bottom.
        private static void AddGrid(
            GuiRow<PazaakViewModel> row,
            Expression<Func<PazaakViewModel, string>>[] images,
            Expression<Func<PazaakViewModel, string>>[] labels)
        {
            row.AddColumn(col =>
            {
                for (var gridRow = 0; gridRow < 3; gridRow++)
                {
                    var first = gridRow * 3;
                    col.AddRow(cards =>
                    {
                        for (var index = first; index < first + 3; index++)
                        {
                            cards.AddImage()
                                .BindResref(images[index])
                                .BindTooltip(labels[index])
                                .SetAspect(NuiAspect.Exact)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle)
                                .SetWidth(TableCardSize)
                                .SetHeight(TableCardSize);
                        }
                    });
                }
            })
                .SetWidth(GridWidth);
        }

        private static void AddSectionTitle(GuiRow<PazaakViewModel> row, string text)
        {
            row.AddLabel()
                .SetText(text)
                .SetHeight(20f)
                .SetWidth(HalfWidth)
                .SetHorizontalAlign(NuiHorizontalAlign.Center);
        }

        // The familiar Pazaak table: each player's name, turn light, and total across the top, the two
        // 3x3 grids side by side with set-won lights on the outer edges, then the player's hand with a
        // flip button under each +/- card, the opponent's face-down hand, and the turn buttons.
        private static void AddTableTab(GuiGroup<PazaakViewModel> host)
        {
            AddTabPanel(host, col =>
            {
                col.AddRow(row =>
                {
                    AddLight(row, model => model.PlayerTurnLight);
                    row.AddLabel()
                        .BindText(model => model.PlayerTitle)
                        .SetWidth(NameWidth)
                        .SetHeight(TotalBoxHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left);
                    row.AddSpacer();
                    AddTotalBox(row, model => model.PlayerTotal);
                    AddTotalBox(row, model => model.OpponentTotal);
                    row.AddSpacer();
                    row.AddLabel()
                        .BindText(model => model.OpponentTitle)
                        .SetWidth(NameWidth)
                        .SetHeight(TotalBoxHeight)
                        .SetHorizontalAlign(NuiHorizontalAlign.Right);
                    AddLight(row, model => model.OpponentTurnLight);
                });

                col.AddRow(row =>
                {
                    AddSetLights(row, PlayerSetLights);
                    AddGrid(row, PlayerSlotImages, PlayerSlots);
                    row.AddSpacer();
                    AddGrid(row, OpponentSlotImages, OpponentSlots);
                    AddSetLights(row, OpponentSetLights);
                });

                col.AddRow(row =>
                {
                    AddSectionTitle(row, "Player Hand");
                    row.AddSpacer();
                    AddSectionTitle(row, "Opponent Hand");
                });

                col.AddRow(row =>
                {
                    for (var index = 0; index < HandImages.Length; index++)
                    {
                        row.AddButtonImage()
                            .BindImageResref(HandImages[index])
                            .BindTooltip(HandTooltips[index])
                            .BindIsEnabled(HandEnabled[index])
                            .BindOnClicked(HandClicks[index])
                            .SetWidth(CardButtonSize)
                            .SetHeight(CardButtonSize);
                    }
                    row.AddSpacer();
                    foreach (var image in OpponentHandImages)
                    {
                        row.AddImage()
                            .BindResref(image)
                            .SetAspect(NuiAspect.Exact)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetWidth(CardButtonSize)
                            .SetHeight(CardButtonSize);
                    }
                });

                col.AddRow(row =>
                {
                    for (var index = 0; index < HandSigns.Length; index++)
                    {
                        row.AddButton()
                            .BindText(HandSigns[index])
                            .BindIsEnabled(HandSignEnabled[index])
                            .BindOnClicked(HandFlipClicks[index])
                            .SetTooltip("Flip the sign this card is played with.")
                            .SetWidth(CardButtonSize)
                            .SetHeight(ActionButtonHeight);
                    }
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("End Turn")
                        .SetTooltip("Pass the turn. You draw another card on your next turn.")
                        .BindIsEnabled(model => model.IsTurnActionEnabled)
                        .BindOnClicked(model => model.OnClickEndTurn())
                        .SetWidth(TurnButtonWidth)
                        .SetHeight(ActionButtonHeight);
                    row.AddButton()
                        .SetText("Stand")
                        .SetTooltip("Keep your total for the rest of this set.")
                        .BindIsEnabled(model => model.IsTurnActionEnabled)
                        .BindOnClicked(model => model.OnClickStand())
                        .SetWidth(TurnButtonWidth)
                        .SetHeight(ActionButtonHeight);
                });

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Flip Hand Card")
                        .SetHeight(20f)
                        .SetWidth(HalfWidth)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Forfeit Game")
                        .BindIsEnabled(model => model.IsForfeitEnabled)
                        .BindOnClicked(model => model.OnClickForfeit())
                        .SetWidth(TurnButtonWidth * 2f + 8f)
                        .SetHeight(ActionButtonHeight);
                });

                AddCenteredLabel(col, model => model.StatusText);
                AddCenteredLabel(col, model => model.LastSetText);

                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsChallengeVisible);
                    row.AddLabel()
                        .BindText(model => model.ChallengeText)
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                col.AddRow(row =>
                {
                    row.BindIsVisible(model => model.IsChallengeVisible);
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Challenge a Player")
                        .SetTooltip("Click a player within 10 meters to challenge them.")
                        .BindOnClicked(model => model.OnClickChallenge())
                        .SetWidth(180f);
                    row.AddSpacer();
                });
            });
        }

        private static void AddSideDeckTab(GuiGroup<PazaakViewModel> host)
        {
            AddTabPanel(host, col =>
            {
                AddCenteredLabel(col, model => model.DeckHeader);

                for (var deckRow = 0; deckRow < 2; deckRow++)
                {
                    var first = deckRow * 5;
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        for (var index = first; index < first + 5; index++)
                        {
                            row.AddButtonImage()
                                .BindImageResref(DeckSlotImages[index])
                                .BindTooltip(DeckSlots[index])
                                .BindOnClicked(DeckClicks[index])
                                .SetWidth(CardButtonSize)
                                .SetHeight(CardButtonSize);
                        }
                        row.AddSpacer();
                    });
                }

                col.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText("Click a card to add or remove it.")
                        .SetHeight(20f)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center);
                });

                col.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(ListCardSize + 12f);
                            cell.AddButtonImage()
                                .BindImageResref(model => model.CardImages)
                                .BindTooltip(model => model.CardNames)
                                .BindIsEnabled(model => model.CardAddEnabled)
                                .BindOnClicked(model => model.OnClickAddCard())
                                .SetWidth(ListCardSize + 4f)
                                .SetHeight(ListCardSize + 4f);
                        });
                        template.AddCell(cell =>
                        {
                            cell.SetIsVariable(false);
                            cell.SetWidth(40f);
                            cell.AddLabel()
                                .BindText(model => model.CardOwnedCounts)
                                .SetTooltip("Copies in your side deck / copies you own.")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });
                        template.AddCell(cell =>
                        {
                            cell.AddLabel()
                                .BindText(model => model.CardDescriptions)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left);
                        });
                    })
                        .BindRowCount(model => model.CardNames)
                        .SetRowHeight(ListCardSize + 8f)
                        .SetHeight(190f);
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Save Deck")
                        .BindIsEnabled(model => model.IsSaveEnabled)
                        .BindOnClicked(model => model.OnClickSaveDeck())
                        .SetWidth(110f);
                    row.AddButton()
                        .SetText("Default Deck")
                        .SetTooltip("Replace your side deck with +1 to +5 and -1 to -5.")
                        .BindOnClicked(model => model.OnClickDefaultDeck())
                        .SetWidth(110f);
                    row.AddSpacer();
                });

                AddCenteredLabel(col, model => model.DeckStatusText);
            });
        }
    }
}
