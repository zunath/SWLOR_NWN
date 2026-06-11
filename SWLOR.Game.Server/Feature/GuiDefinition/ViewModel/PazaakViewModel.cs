using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PazaakViewModel : GuiViewModelBase<PazaakViewModel, PazaakPayload>,
        IGuiRefreshable<PazaakRefreshEvent>
    {
        public const int DeckTabId = 0;
        public const int MatchTabId = 1;
        public const int TableTabId = 2;
        public const int LeaderboardTabId = 3;
        public const string ContentPartialElement = "pazaak_content";
        public const string DeckPartial = "PAZAAK_DECK";
        public const string MatchPartial = "PAZAAK_MATCH";
        public const string TablePartial = "PAZAAK_TABLE";
        public const string LeaderboardPartial = "PAZAAK_LEADERBOARD";

        private readonly List<PazaakCardType> _collectionCards = new();
        private readonly List<PazaakCardType> _draftSideDeck = new();
        private uint _table;
        private string _npcProfileId;
        private string _npcRewardId;
        private string _npcDisplayName;
        private bool _isInitialized;

        public int SelectedTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_isInitialized)
                    ChangePartialView(ContentPartialElement, GetPartialName(value));
            }
        }

        public string DeckStatus
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> CollectionNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CollectionIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CollectionOwned
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CollectionInDeck
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> DeckNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> DeckIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string WagerText
        {
            get => Get<string>();
            set
            {
                var sanitized = Regex.Replace(value ?? string.Empty, "[^0-9]", string.Empty);
                if (string.IsNullOrWhiteSpace(sanitized))
                    sanitized = "0";

                Set(sanitized);
            }
        }

        public string TurnTimerText
        {
            get => Get<string>();
            set
            {
                var sanitized = Regex.Replace(value ?? string.Empty, "[^0-9]", string.Empty);
                if (string.IsNullOrWhiteSpace(sanitized))
                    sanitized = "60";

                Set(sanitized);
            }
        }

        public bool IsRated
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsTableAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string TableStatus
        {
            get => Get<string>();
            set => Set(value);
        }

        public string NpcStatus
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MatchStatus
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MatchScore
        {
            get => Get<string>();
            set => Set(value);
        }

        public string YourTotal
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OpponentTotal
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActiveTurnText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedSideValue
        {
            get => Get<string>();
            set
            {
                var sanitized = Regex.Replace(value ?? string.Empty, "[^0-9\\-]", string.Empty);
                Set(sanitized);
            }
        }

        public GuiBindingList<string> SideHandNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> SideHandIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> YourBoardCards
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> YourBoardCardIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> OpponentBoardCards
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> OpponentBoardCardIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> LeaderboardRanks
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> LeaderboardNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> LeaderboardRatings
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        protected override void Initialize(PazaakPayload initialPayload)
        {
            _table = initialPayload?.Table ?? OBJECT_INVALID;
            _npcProfileId = string.IsNullOrWhiteSpace(initialPayload?.NpcProfileId)
                ? PazaakNpcProfileCatalog.DefaultProfileId
                : initialPayload.NpcProfileId;
            _npcRewardId = initialPayload?.NpcRewardId ?? string.Empty;
            _npcDisplayName = initialPayload?.NpcDisplayName ?? string.Empty;
            Pazaak.CancelAbandonForfeit(Player);

            WagerText = "0";
            TurnTimerText = "60";
            SelectedSideValue = "0";
            IsRated = false;
            SelectedTabId = DeckTabId;

            LoadAll();
            WatchOnClient(model => model.SelectedTabId);
            WatchOnClient(model => model.WagerText);
            WatchOnClient(model => model.TurnTimerText);
            WatchOnClient(model => model.IsRated);
            WatchOnClient(model => model.SelectedSideValue);

            _isInitialized = true;
            ChangePartialView(ContentPartialElement, DeckPartial);
        }

        private void LoadAll()
        {
            LoadDeck();
            LoadMatch();
            LoadTable();
            LoadLeaderboard();
        }

        private void LoadDeck()
        {
            var profile = Pazaak.GetOrCreateProfile(Player);
            if (_draftSideDeck.Count <= 0)
                _draftSideDeck.AddRange(profile.ActiveSideDeck);

            var collectionNames = new GuiBindingList<string>();
            var collectionIconResrefs = new GuiBindingList<string>();
            var collectionOwned = new GuiBindingList<string>();
            var collectionInDeck = new GuiBindingList<string>();
            _collectionCards.Clear();

            foreach (var card in PazaakCardCatalog.GetAllCards())
            {
                var owned = Pazaak.GetOwnedCount(profile, card.Type);
                var inDeck = _draftSideDeck.Count(x => x == card.Type);
                _collectionCards.Add(card.Type);
                collectionNames.Add(card.Name);
                collectionIconResrefs.Add(card.IconResref);
                collectionOwned.Add(owned.ToString());
                collectionInDeck.Add(inDeck.ToString());
            }

            CollectionNames = collectionNames;
            CollectionIconResrefs = collectionIconResrefs;
            CollectionOwned = collectionOwned;
            CollectionInDeck = collectionInDeck;

            var deckNames = new GuiBindingList<string>();
            var deckIconResrefs = new GuiBindingList<string>();
            foreach (var card in _draftSideDeck)
            {
                deckNames.Add(PazaakCardCatalog.GetName(card));
                deckIconResrefs.Add(GetCardIconResref(card));
            }

            DeckNames = deckNames;
            DeckIconResrefs = deckIconResrefs;
            var validation = Pazaak.ValidateSideDeckForCollection(profile, _draftSideDeck);
            DeckStatus = $"Deck: {_draftSideDeck.Count}/{PazaakGameEngine.RequiredSideDeckSize} cards. {validation}";
        }

        private void LoadMatch()
        {
            var match = Pazaak.GetActiveMatch(Player);
            if (match == null)
            {
                MatchStatus = "No active Pazaak match.";
                MatchScore = string.Empty;
                YourTotal = string.Empty;
                OpponentTotal = string.Empty;
                ActiveTurnText = string.Empty;
                SideHandNames = new GuiBindingList<string>();
                SideHandIconResrefs = new GuiBindingList<string>();
                YourBoardCards = new GuiBindingList<string>();
                YourBoardCardIconResrefs = new GuiBindingList<string>();
                OpponentBoardCards = new GuiBindingList<string>();
                OpponentBoardCardIconResrefs = new GuiBindingList<string>();
                return;
            }

            var playerId = GetObjectUUID(Player);
            var yourIndex = match.Participants[0].ParticipantId == playerId ? 0 : 1;
            var opponentIndex = yourIndex == 0 ? 1 : 0;
            var you = match.Participants[yourIndex];
            var opponent = match.Participants[opponentIndex];

            MatchStatus = match.StatusText;
            MatchScore = $"{you.Name} {you.SetsWon} - {opponent.SetsWon} {opponent.Name}";
            YourTotal = $"Your Total: {you.Total}";
            OpponentTotal = $"Opponent Total: {opponent.Total}";
            ActiveTurnText = match.Status == PazaakMatchStatus.Active
                ? $"Turn: {match.Participants[match.ActiveParticipantIndex].Name}"
                : "Match complete";

            var sideHandNames = new GuiBindingList<string>();
            var sideHandIconResrefs = new GuiBindingList<string>();
            foreach (var card in you.SideHand)
            {
                sideHandNames.Add(PazaakCardCatalog.GetName(card));
                sideHandIconResrefs.Add(GetCardIconResref(card));
            }

            SideHandNames = sideHandNames;
            SideHandIconResrefs = sideHandIconResrefs;
            YourBoardCards = ToBoardList(you);
            YourBoardCardIconResrefs = ToBoardIconList(you);
            OpponentBoardCards = ToBoardList(opponent);
            OpponentBoardCardIconResrefs = ToBoardIconList(opponent);
        }

        private void LoadTable()
        {
            IsTableAvailable = GetIsObjectValid(_table);
            TableStatus = IsTableAvailable
                ? Pazaak.GetTableLobbyText(_table)
                : "Open this window from a Pazaak table to host or join PvP.";

            var npc = PazaakNpcProfileCatalog.Get(_npcProfileId);
            var npcName = string.IsNullOrWhiteSpace(_npcDisplayName) ? npc.Name : _npcDisplayName;
            var rewardText = Pazaak.GetNpcRewardProgressText(Player, _npcRewardId, npc);
            NpcStatus = $"NPC: {npcName}. Wager range {npc.MinimumWager}-{npc.MaximumWager}. {rewardText}";
        }

        private void LoadLeaderboard()
        {
            var ranks = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var ratings = new GuiBindingList<string>();
            var rank = 1;

            foreach (var profile in Pazaak.GetLeaderboard(25))
            {
                var dbPlayer = DB.Get<Player>(profile.PlayerId);
                ranks.Add(rank.ToString());
                names.Add(dbPlayer?.Name ?? profile.PlayerId);
                ratings.Add(profile.PvPRating.ToString());
                rank++;
            }

            LeaderboardRanks = ranks;
            LeaderboardNames = names;
            LeaderboardRatings = ratings;
        }

        public Action OnClickAddCollectionCard() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _collectionCards.Count)
                return;

            if (_draftSideDeck.Count >= PazaakGameEngine.RequiredSideDeckSize)
            {
                SendMessageToPC(Player, "Your side deck already has 10 cards.");
                return;
            }

            _draftSideDeck.Add(_collectionCards[index]);
            LoadDeck();
        };

        public Action OnClickRemoveDeckCard() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _draftSideDeck.Count)
                return;

            _draftSideDeck.RemoveAt(index);
            LoadDeck();
        };

        public Action OnClickResetDeck() => () =>
        {
            var profile = Pazaak.GetOrCreateProfile(Player);
            _draftSideDeck.Clear();
            _draftSideDeck.AddRange(profile.ActiveSideDeck);
            LoadDeck();
        };

        public Action OnClickSaveDeck() => () =>
        {
            try
            {
                Pazaak.SetActiveSideDeck(Player, _draftSideDeck);
                SendMessageToPC(Player, "Pazaak side deck saved.");
            }
            catch (Exception ex)
            {
                SendMessageToPC(Player, ex.Message);
            }

            LoadDeck();
        };

        public Action OnClickHostTable() => () =>
        {
            if (!IsTableAvailable)
                return;

            var result = Pazaak.CreateTableLobby(Player, _table, IsRated, ParseInt(WagerText), ParseInt(TurnTimerText));
            if (!string.IsNullOrWhiteSpace(result))
                SendMessageToPC(Player, result);

            LoadTable();
        };

        public Action OnClickJoinTable() => () =>
        {
            if (!IsTableAvailable)
                return;

            var result = Pazaak.JoinTableLobby(Player, _table);
            if (!string.IsNullOrWhiteSpace(result))
                SendMessageToPC(Player, result);

            LoadAll();
            SelectedTabId = MatchTabId;
        };

        public Action OnClickCancelTable() => () =>
        {
            if (!IsTableAvailable)
                return;

            var result = Pazaak.CancelTableLobby(Player, _table);
            if (!string.IsNullOrWhiteSpace(result))
                SendMessageToPC(Player, result);

            LoadTable();
        };

        public Action OnClickStartNpc() => () =>
        {
            var result = Pazaak.StartNpcMatch(Player, _npcProfileId, _npcRewardId, _npcDisplayName, ParseInt(WagerText));
            if (!string.IsNullOrWhiteSpace(result))
                SendMessageToPC(Player, result);

            LoadAll();
            SelectedTabId = MatchTabId;
        };

        public Action OnClickPlaySideCard() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var result = Pazaak.PlaySideCard(Player, index, ParseInt(SelectedSideValue));
            if (!string.IsNullOrWhiteSpace(result))
                SendMessageToPC(Player, result);

            LoadAll();
        };

        public Action OnClickEndTurn() => () =>
        {
            Pazaak.EndTurn(Player);
            LoadAll();
        };

        public Action OnClickStand() => () =>
        {
            Pazaak.Stand(Player);
            LoadAll();
        };

        public Action OnClickForfeit() => () =>
        {
            Pazaak.Forfeit(Player);
            LoadAll();
        };

        public void Refresh(PazaakRefreshEvent payload)
        {
            LoadAll();
        }

        public override Action OnWindowClosed() => () =>
        {
            Pazaak.ScheduleAbandonForfeit(Player);
        };

        private static GuiBindingList<string> ToBoardList(PazaakParticipantState participant)
        {
            var list = new GuiBindingList<string>();
            foreach (var card in participant.Board)
            {
                list.Add($"{card.Label} ({card.Value})");
            }

            return list;
        }

        private static GuiBindingList<string> ToBoardIconList(PazaakParticipantState participant)
        {
            var list = new GuiBindingList<string>();
            foreach (var card in participant.Board)
            {
                list.Add(GetPlayedCardIconResref(card));
            }

            return list;
        }

        private static string GetPlayedCardIconResref(PazaakPlayedCard card)
        {
            return card.IsMainDeckCard &&
                   card.Value >= 1 &&
                   card.Value <= 10
                ? $"pz_main{card.Value}"
                : GetCardIconResref(card.CardType);
        }

        private static string GetCardIconResref(PazaakCardType cardType)
        {
            return PazaakCardCatalog.IsValidCard(cardType)
                ? PazaakCardCatalog.Get(cardType).IconResref
                : "pazaak_card";
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out var result) ? Math.Max(0, result) : 0;
        }

        private static string GetPartialName(int tabId)
        {
            return tabId switch
            {
                MatchTabId => MatchPartial,
                TableTabId => TablePartial,
                LeaderboardTabId => LeaderboardPartial,
                _ => DeckPartial,
            };
        }
    }
}
