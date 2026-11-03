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
        private const float ContentPanelWidth = 400f;
        private const float TableCardSize = 36f;
        private const float CardButtonSize = 44f;
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
                .SetInitialGeometry(0, 0, 440f, 600f)
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

        private static void AddTableCards(
            GuiColumn<PazaakViewModel> col,
            Expression<Func<PazaakViewModel, string>>[] images,
            Expression<Func<PazaakViewModel, string>>[] labels)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();
                for (var index = 0; index < images.Length; index++)
                {
                    row.AddImage()
                        .BindResref(images[index])
                        .BindTooltip(labels[index])
                        .SetAspect(NuiAspect.Exact)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetWidth(TableCardSize)
                        .SetHeight(TableCardSize);
                }
                row.AddSpacer();
            });
        }

        private static void AddTableTab(GuiGroup<PazaakViewModel> host)
        {
            AddTabPanel(host, col =>
            {
                AddCenteredLabel(col, model => model.OpponentHeader);
                AddTableCards(col, OpponentSlotImages, OpponentSlots);
                AddCenteredLabel(col, model => model.StatusText);
                AddCenteredLabel(col, model => model.LastSetText);
                AddTableCards(col, PlayerSlotImages, PlayerSlots);
                AddCenteredLabel(col, model => model.PlayerHeader);

                col.AddRow(row =>
                {
                    row.AddSpacer();
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
                    row.AddButton()
                        .BindText(model => model.SignButtonText)
                        .SetTooltip("The sign used when you play a +/- or tiebreaker card.")
                        .BindOnClicked(model => model.OnClickToggleSign())
                        .SetWidth(70f)
                        .SetHeight(CardButtonSize);
                    row.AddSpacer();
                });

                col.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("End Turn")
                        .SetTooltip("Pass the turn. You draw another card on your next turn.")
                        .BindIsEnabled(model => model.IsTurnActionEnabled)
                        .BindOnClicked(model => model.OnClickEndTurn())
                        .SetWidth(110f);
                    row.AddButton()
                        .SetText("Stand")
                        .SetTooltip("Keep your total for the rest of this set.")
                        .BindIsEnabled(model => model.IsTurnActionEnabled)
                        .BindOnClicked(model => model.OnClickStand())
                        .SetWidth(110f);
                    row.AddButton()
                        .SetText("Forfeit")
                        .BindIsEnabled(model => model.IsForfeitEnabled)
                        .BindOnClicked(model => model.OnClickForfeit())
                        .SetWidth(110f);
                    row.AddSpacer();
                });

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
