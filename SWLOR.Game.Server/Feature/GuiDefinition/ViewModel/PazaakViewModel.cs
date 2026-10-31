using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PazaakService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PazaakViewModel : GuiViewModelBase<PazaakViewModel, GuiPayloadBase>,
        IGuiRefreshable<PazaakRefreshEvent>
    {
        private const int TableTabId = 0;
        private const int SideDeckTabId = 1;
        public const string TabContentElement = "pazaak_tab_content";
        public const string TablePartial = "PAZAAK_TAB_TABLE";
        public const string SideDeckPartial = "PAZAAK_TAB_SIDE_DECK";

        private static readonly GuiTabGroup<PazaakViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<PazaakViewModel, GuiPayloadBase>()
                .AddTab(TableTabId, TablePartial, m => m.RefreshTable())
                .AddTab(SideDeckTabId, SideDeckPartial, m => m.RefreshSideDeck());

        private static readonly GuiToggleGroupSync TabToggles = new(TableTabId, SideDeckTabId);

        private readonly List<PazaakCardType> _deck = new();
        private bool _playNegative;
        private bool _isModalOpen;
        private uint _promptedChallenger = OBJECT_INVALID;

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        public string OpponentHeader { get => Get<string>(); set => Set(value); }
        public string PlayerHeader { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public string LastSetText { get => Get<string>(); set => Set(value); }
        public string SignButtonText { get => Get<string>(); set => Set(value); }
        public bool IsTurnActionEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsForfeitEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsChallengeVisible { get => Get<bool>(); set => Set(value); }
        public string ChallengeText { get => Get<string>(); set => Set(value); }

        public string DeckHeader { get => Get<string>(); set => Set(value); }
        public string DeckStatusText { get => Get<string>(); set => Set(value); }
        public bool IsSaveEnabled { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<string> CardNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> CardDescriptions { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> CardAddEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string OpponentSlot0 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot1 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot2 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot3 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot4 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot5 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot6 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot7 { get => Get<string>(); set => Set(value); }
        public string OpponentSlot8 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot0 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot1 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot2 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot3 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot4 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot5 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot6 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot7 { get => Get<string>(); set => Set(value); }
        public string PlayerSlot8 { get => Get<string>(); set => Set(value); }
        public string Hand0 { get => Get<string>(); set => Set(value); }
        public string Hand1 { get => Get<string>(); set => Set(value); }
        public string Hand2 { get => Get<string>(); set => Set(value); }
        public string Hand3 { get => Get<string>(); set => Set(value); }
        public string Hand0Tooltip { get => Get<string>(); set => Set(value); }
        public string Hand1Tooltip { get => Get<string>(); set => Set(value); }
        public string Hand2Tooltip { get => Get<string>(); set => Set(value); }
        public string Hand3Tooltip { get => Get<string>(); set => Set(value); }
        public bool IsHand0Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsHand1Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsHand2Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsHand3Enabled { get => Get<bool>(); set => Set(value); }
        public string DeckSlot0 { get => Get<string>(); set => Set(value); }
        public string DeckSlot1 { get => Get<string>(); set => Set(value); }
        public string DeckSlot2 { get => Get<string>(); set => Set(value); }
        public string DeckSlot3 { get => Get<string>(); set => Set(value); }
        public string DeckSlot4 { get => Get<string>(); set => Set(value); }
        public string DeckSlot5 { get => Get<string>(); set => Set(value); }
        public string DeckSlot6 { get => Get<string>(); set => Set(value); }
        public string DeckSlot7 { get => Get<string>(); set => Set(value); }
        public string DeckSlot8 { get => Get<string>(); set => Set(value); }
        public string DeckSlot9 { get => Get<string>(); set => Set(value); }

        private Action<string>[] OpponentSlotSetters => new Action<string>[] { v => OpponentSlot0 = v, v => OpponentSlot1 = v, v => OpponentSlot2 = v, v => OpponentSlot3 = v, v => OpponentSlot4 = v, v => OpponentSlot5 = v, v => OpponentSlot6 = v, v => OpponentSlot7 = v, v => OpponentSlot8 = v };
        private Action<string>[] PlayerSlotSetters => new Action<string>[] { v => PlayerSlot0 = v, v => PlayerSlot1 = v, v => PlayerSlot2 = v, v => PlayerSlot3 = v, v => PlayerSlot4 = v, v => PlayerSlot5 = v, v => PlayerSlot6 = v, v => PlayerSlot7 = v, v => PlayerSlot8 = v };
        private Action<string>[] HandSetters => new Action<string>[] { v => Hand0 = v, v => Hand1 = v, v => Hand2 = v, v => Hand3 = v };
        private Action<string>[] HandTooltipSetters => new Action<string>[] { v => Hand0Tooltip = v, v => Hand1Tooltip = v, v => Hand2Tooltip = v, v => Hand3Tooltip = v };
        private Action<bool>[] HandEnabledSetters => new Action<bool>[] { v => IsHand0Enabled = v, v => IsHand1Enabled = v, v => IsHand2Enabled = v, v => IsHand3Enabled = v };
        private Action<string>[] DeckSlotSetters => new Action<string>[] { v => DeckSlot0 = v, v => DeckSlot1 = v, v => DeckSlot2 = v, v => DeckSlot3 = v, v => DeckSlot4 = v, v => DeckSlot5 = v, v => DeckSlot6 = v, v => DeckSlot7 = v, v => DeckSlot8 = v, v => DeckSlot9 = v };

        public Action OnClickHand0() => () => PlayHandCard(0);
        public Action OnClickHand1() => () => PlayHandCard(1);
        public Action OnClickHand2() => () => PlayHandCard(2);
        public Action OnClickHand3() => () => PlayHandCard(3);
        public Action OnClickDeckSlot0() => () => RemoveDeckCard(0);
        public Action OnClickDeckSlot1() => () => RemoveDeckCard(1);
        public Action OnClickDeckSlot2() => () => RemoveDeckCard(2);
        public Action OnClickDeckSlot3() => () => RemoveDeckCard(3);
        public Action OnClickDeckSlot4() => () => RemoveDeckCard(4);
        public Action OnClickDeckSlot5() => () => RemoveDeckCard(5);
        public Action OnClickDeckSlot6() => () => RemoveDeckCard(6);
        public Action OnClickDeckSlot7() => () => RemoveDeckCard(7);
        public Action OnClickDeckSlot8() => () => RemoveDeckCard(8);
        public Action OnClickDeckSlot9() => () => RemoveDeckCard(9);

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _deck.Clear();
            _deck.AddRange(Pazaak.GetSideDeck(Player));
            _playNegative = false;
            _isModalOpen = false;
            _promptedChallenger = OBJECT_INVALID;

            var cardNames = new GuiBindingList<string>();
            var cardDescriptions = new GuiBindingList<string>();
            foreach (var card in PazaakCards.All)
            {
                cardNames.Add(card.Label);
                cardDescriptions.Add(card.Description);
            }
            CardNames = cardNames;
            CardDescriptions = cardDescriptions;
            DeckStatusText = string.Empty;
            TabToggleValue = TableTabId;

            RefreshTable();
            RefreshSideDeck();

            WatchOnClient(model => model.TabToggleValue);

            SelectTab(TableTabId);
            PromptPendingChallenge();
        }

        public void Refresh(PazaakRefreshEvent payload)
        {
            if (payload.MatchStarted)
            {
                _playNegative = false;
                if (_isModalOpen)
                    SelectedTabId = TableTabId;
                else
                    SelectTab(TableTabId);
            }

            RefreshTable();
            PromptPendingChallenge();
        }

        public override Action OnWindowClosed() => () =>
        {
            if (Pazaak.IsInActiveMatch(Player))
                SendMessageToPC(Player, "Your Pazaak match continues. Use /pazaak to return to the table.");
        };

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, v => TabToggleValue = v);
            Tabs.Select(this, TabContentElement, tabId);
        }

        protected override void OnModalClosedRestore()
        {
            _isModalOpen = false;
            TabToggles.SyncTo(SelectedTabId, v => TabToggleValue = v);
            Tabs.Select(this, TabContentElement, SelectedTabId);
        }

        private void PromptPendingChallenge()
        {
            var challenger = Pazaak.GetPendingChallenger(Player);
            if (challenger == OBJECT_INVALID || challenger == _promptedChallenger || _isModalOpen)
                return;

            _promptedChallenger = challenger;
            _isModalOpen = true;
            ShowModal(
                $"{PlayerName.GetDisplayName(Player, challenger)} challenges you to a game of Pazaak. Do you accept?",
                () =>
                {
                    _promptedChallenger = OBJECT_INVALID;
                    var error = Pazaak.AcceptChallenge(Player);
                    if (!string.IsNullOrEmpty(error))
                        SendMessageToPC(Player, ColorToken.Red(error));
                },
                () =>
                {
                    _promptedChallenger = OBJECT_INVALID;
                    Pazaak.DeclineChallenge(Player);
                },
                "Accept",
                "Decline");
        }

        private void RefreshTable()
        {
            var match = Pazaak.GetMatch(Player);
            SignButtonText = _playNegative ? "Play +/- as -" : "Play +/- as +";

            if (match == null)
            {
                OpponentHeader = "No opponent";
                PlayerHeader = "You";
                StatusText = "No match in progress.";
                LastSetText = string.Empty;
                for (var index = 0; index < PazaakMatch.TableSize; index++)
                {
                    OpponentSlotSetters[index](string.Empty);
                    PlayerSlotSetters[index](string.Empty);
                }

                for (var index = 0; index < PazaakCards.HandSize; index++)
                {
                    HandSetters[index](string.Empty);
                    HandTooltipSetters[index](string.Empty);
                    HandEnabledSetters[index](false);
                }

                IsTurnActionEnabled = false;
                IsForfeitEnabled = false;
                IsChallengeVisible = true;
                ChallengeText = "Challenge a player within 10 meters to start a match.";
                return;
            }

            var seatIndex = Pazaak.GetSeatIndex(match, Player);
            var mySeat = match.Seats[seatIndex];
            var opponentSeat = match.Seats[PazaakMatch.GetOpponent(seatIndex)];
            var opponentName = GetIsObjectValid(opponentSeat.Player)
                ? PlayerName.GetDisplayName(Player, opponentSeat.Player)
                : "Your opponent";
            var isMyTurn = !match.IsFinished && match.CurrentSeat == seatIndex;

            OpponentHeader = BuildHeader(opponentName, opponentSeat);
            PlayerHeader = BuildHeader("You", mySeat);

            for (var index = 0; index < PazaakMatch.TableSize; index++)
            {
                OpponentSlotSetters[index](index < opponentSeat.Table.Count ? opponentSeat.Table[index].Label : string.Empty);
                PlayerSlotSetters[index](index < mySeat.Table.Count ? mySeat.Table[index].Label : string.Empty);
            }

            var canPlaySide = isMyTurn && !mySeat.HasPlayedSideCardThisTurn && mySeat.Table.Count < PazaakMatch.TableSize;
            for (var index = 0; index < PazaakCards.HandSize; index++)
            {
                if (index < mySeat.Hand.Count)
                {
                    var card = PazaakCards.Get(mySeat.Hand[index]);
                    HandSetters[index](card.Label);
                    HandTooltipSetters[index](card.Description);
                    HandEnabledSetters[index](canPlaySide);
                }
                else
                {
                    HandSetters[index](string.Empty);
                    HandTooltipSetters[index](string.Empty);
                    HandEnabledSetters[index](false);
                }
            }

            if (match.IsFinished)
            {
                StatusText = match.WinnerSeat == seatIndex
                    ? "You win the match!"
                    : $"{opponentName} wins the match.";
            }
            else if (isMyTurn)
            {
                StatusText = opponentSeat.IsStanding
                    ? $"Set {match.SetNumber} - Your turn. {opponentName} stands at {opponentSeat.Total}."
                    : $"Set {match.SetNumber} - Your turn.";
            }
            else
            {
                StatusText = $"Set {match.SetNumber} - {opponentName}'s turn.";
            }

            LastSetText = Pazaak.DescribeSetResult(match, seatIndex, Player);
            IsTurnActionEnabled = isMyTurn;
            IsForfeitEnabled = !match.IsFinished;
            IsChallengeVisible = match.IsFinished;
            ChallengeText = "Challenge a player within 10 meters to play again.";
        }

        private static string BuildHeader(string name, PazaakSeat seat)
        {
            var sets = $"Sets {seat.SetsWon}/{PazaakMatch.SetsToWin}";
            var standing = seat.IsStanding ? " - Standing" : string.Empty;
            return $"{name} - {sets} - Total {seat.Total}{standing}";
        }

        private void RefreshSideDeck()
        {
            for (var index = 0; index < PazaakCards.SideDeckSize; index++)
            {
                DeckSlotSetters[index](index < _deck.Count ? PazaakCards.Get(_deck[index]).Label : string.Empty);
            }

            var isFull = _deck.Count >= PazaakCards.SideDeckSize;
            var cardAddEnabled = new GuiBindingList<bool>();
            foreach (var _ in PazaakCards.All)
            {
                cardAddEnabled.Add(!isFull);
            }
            CardAddEnabled = cardAddEnabled;

            DeckHeader = $"Your side deck ({_deck.Count}/{PazaakCards.SideDeckSize})";
            IsSaveEnabled = _deck.Count == PazaakCards.SideDeckSize;
        }

        private void PlayHandCard(int handIndex)
        {
            var error = Pazaak.PlaySideCard(Player, handIndex, _playNegative);
            if (!string.IsNullOrEmpty(error))
                SendMessageToPC(Player, ColorToken.Red(error));
        }

        private void RemoveDeckCard(int index)
        {
            if (index >= _deck.Count)
                return;

            _deck.RemoveAt(index);
            DeckStatusText = "Unsaved changes.";
            RefreshSideDeck();
        }

        public Action OnClickAddCard() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= PazaakCards.All.Count || _deck.Count >= PazaakCards.SideDeckSize)
                return;

            _deck.Add(PazaakCards.All[index].Type);
            DeckStatusText = "Unsaved changes.";
            RefreshSideDeck();
        };

        public Action OnClickSaveDeck() => () =>
        {
            DeckStatusText = Pazaak.SaveSideDeck(Player, _deck)
                ? "Side deck saved. It is used in your next match."
                : $"Your side deck needs exactly {PazaakCards.SideDeckSize} cards.";
        };

        public Action OnClickDefaultDeck() => () =>
        {
            _deck.Clear();
            _deck.AddRange(PazaakCards.DefaultSideDeck);
            DeckStatusText = "Unsaved changes.";
            RefreshSideDeck();
        };

        public Action OnClickToggleSign() => () =>
        {
            _playNegative = !_playNegative;
            SignButtonText = _playNegative ? "Play +/- as -" : "Play +/- as +";
        };

        public Action OnClickEndTurn() => () =>
        {
            var error = Pazaak.EndTurn(Player);
            if (!string.IsNullOrEmpty(error))
                SendMessageToPC(Player, ColorToken.Red(error));
        };

        public Action OnClickStand() => () =>
        {
            var error = Pazaak.Stand(Player);
            if (!string.IsNullOrEmpty(error))
                SendMessageToPC(Player, ColorToken.Red(error));
        };

        public Action OnClickForfeit() => () =>
        {
            _isModalOpen = true;
            ShowModal("Forfeit this Pazaak match?", () => Pazaak.Forfeit(Player));
        };

        public Action OnClickChallenge() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Creature, "Click the player you want to challenge to Pazaak.",
                target =>
                {
                    var error = Pazaak.Challenge(Player, target);
                    if (!string.IsNullOrEmpty(error))
                        SendMessageToPC(Player, ColorToken.Red(error));
                });
        };
    }
}
