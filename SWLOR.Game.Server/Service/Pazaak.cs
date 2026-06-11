using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PazaakService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Pazaak
    {
        public const int InitialRating = 1000;
        public const string CardItemLocalVariable = "PAZAAK_CARD_ID";
        public const string NpcProfileLocalVariable = "PAZAAK_NPC_PROFILE";
        public const string NpcRewardIdLocalVariable = "PAZAAK_NPC_REWARD_ID";
        public const string NpcDisplayNameLocalVariable = "PAZAAK_NPC_NAME";
        public const string MinimumWagerLocalVariable = "PAZAAK_MIN_WAGER";
        public const string MaximumWagerLocalVariable = "PAZAAK_MAX_WAGER";
        public const string VendorTierLocalVariable = "PAZAAK_VENDOR_TIER";
        public const string VendorStoreTagLocalVariable = "PAZAAK_VENDOR_STORE_TAG";
        private const int RatingKFactor = 32;
        private const int AbandonForfeitDelaySeconds = 30;
        private const string ConversationLocalVariable = "CONVERSATION";
        private const string CardStoreResref = "pazaak_cards";
        private const string CardTokenResref = "pazaak_card";
        private const string StoreServiceItemLocalVariable = "STORE_SERVICE_IS_STORE_ITEM";
        private const string WorldContentSpawnedLocalVariable = "PAZAAK_CONTENT_SPAWNED";

        private static readonly Dictionary<string, PazaakMatchSession> _sessionsByPlayerId = new();
        private static readonly Dictionary<string, PazaakTableLobby> _lobbiesByTableId = new();
        private static readonly Dictionary<string, string> _pendingAbandonByPlayerId = new();
        private static readonly List<PazaakWorldContentDefinition> _worldContentDefinitions = BuildWorldContentDefinitions();

        private class PazaakMatchSession
        {
            public PazaakMatchState Match { get; set; }
            public IPazaakRandom Random { get; set; }
            public string EscrowId { get; set; }
            public uint PlayerOne { get; set; }
            public uint PlayerTwo { get; set; }
            public PazaakNpcProfile NpcProfile { get; set; }
            public string NpcRewardId { get; set; }
            public int TurnTimerSeconds { get; set; }
            public int TurnTimerSequence { get; set; }
        }

        private class PazaakTableLobby
        {
            public string TableId { get; set; }
            public uint Table { get; set; }
            public uint Host { get; set; }
            public string HostId { get; set; }
            public string HostName { get; set; }
            public bool IsRated { get; set; }
            public int Wager { get; set; }
            public int TurnTimerSeconds { get; set; }
        }

        private class PazaakWorldContentDefinition
        {
            public string AreaResref { get; }
            public int VendorTier { get; }
            public List<PazaakWorldTable> Tables { get; }
            public List<PazaakWorldOpponent> Opponents { get; }
            public PazaakWorldVendor Vendor { get; }

            public PazaakWorldContentDefinition(
                string areaResref,
                int vendorTier,
                IEnumerable<PazaakWorldTable> tables,
                IEnumerable<PazaakWorldOpponent> opponents,
                PazaakWorldVendor vendor)
            {
                AreaResref = areaResref;
                VendorTier = vendorTier;
                Tables = tables.ToList();
                Opponents = opponents.ToList();
                Vendor = vendor;
            }
        }

        private class PazaakWorldTable
        {
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float Facing { get; }
            public string Name { get; }

            public PazaakWorldTable(float x, float y, float z, float facing, string name)
            {
                X = x;
                Y = y;
                Z = z;
                Facing = facing;
                Name = name;
            }
        }

        private class PazaakWorldOpponent
        {
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float Facing { get; }
            public string Name { get; }
            public string ProfileId { get; }
            public string CreatureResref { get; }
            public string RewardId { get; }

            public PazaakWorldOpponent(
                float x,
                float y,
                float z,
                float facing,
                string name,
                string profileId,
                string rewardId,
                string creatureResref = "femalegambler")
            {
                X = x;
                Y = y;
                Z = z;
                Facing = facing;
                Name = name;
                ProfileId = profileId;
                RewardId = rewardId;
                CreatureResref = creatureResref;
            }
        }

        private class PazaakWorldVendor
        {
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float Facing { get; }
            public string Name { get; }
            public string CreatureResref { get; }

            public PazaakWorldVendor(float x, float y, float z, float facing, string name, string creatureResref = "femalegambler")
            {
                X = x;
                Y = y;
                Z = z;
                Facing = facing;
                Name = name;
                CreatureResref = creatureResref;
            }
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void InitializeWorldContent()
        {
            SpawnWorldContent();
            PopulateCardVendorStores();
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RefundStaleEscrows()
        {
            var query = new DBQuery<PazaakEscrow>()
                .AddFieldSearch(nameof(PazaakEscrow.IsSettled), false);
            var count = (int)DB.SearchCount(query);
            var escrows = DB.Search(query.AddPaging(count, 0));

            foreach (var escrow in escrows)
            {
                if (escrow.IsSettled)
                    continue;

                AddPendingPayout(escrow.PlayerOneId, escrow.PlayerOneAmount);
                if (escrow.IsPvP)
                    AddPendingPayout(escrow.PlayerTwoId, escrow.PlayerTwoAmount);

                escrow.IsSettled = true;
                escrow.DateSettled = DateTime.UtcNow;
                DB.Set(escrow);
                Log.Write(LogGroup.Pazaak, $"Refunded stale Pazaak escrow '{escrow.Id}' to pending payouts.");
            }
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DeliverPendingPayout()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var profile = GetOrCreateProfile(player);
            if (profile.PendingCreditPayout <= 0)
                return;

            var amount = profile.PendingCreditPayout;
            profile.PendingCreditPayout = 0;
            profile.DateUpdated = DateTime.UtcNow;
            DB.Set(profile);
            GiveGoldToCreature(player, amount);
            SendMessageToPC(player, $"You receive {amount} credits from pending Pazaak winnings or refunds.");
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void ScheduleForfeitOnExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            ScheduleAbandonForfeit(player);
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ForfeitOnDeath()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            Forfeit(player);
        }

        public static PazaakProfile GetOrCreateProfile(uint player)
        {
            return GetOrCreateProfile(GetObjectUUID(player));
        }

        public static PazaakProfile GetOrCreateProfile(string playerId)
        {
            var profile = DB.Get<PazaakProfile>(playerId);
            if (profile != null)
            {
                EnsureProfileCollections(profile);
                return profile;
            }

            profile = new PazaakProfile(playerId);
            foreach (var card in PazaakCardCatalog.StarterDeck)
            {
                AddCard(profile, card, 1);
                profile.ActiveSideDeck.Add(card);
            }

            DB.Set(profile);
            return profile;
        }

        public static void GrantCard(uint player, PazaakCardType cardType, int count = 1)
        {
            if (!PazaakCardCatalog.IsValidCard(cardType))
                throw new ArgumentException($"Invalid Pazaak card type '{cardType}'.", nameof(cardType));

            var profile = GetOrCreateProfile(player);
            AddCard(profile, cardType, count);
            profile.DateUpdated = DateTime.UtcNow;
            DB.Set(profile);
        }

        public static int GetOwnedCount(PazaakProfile profile, PazaakCardType cardType)
        {
            EnsureProfileCollections(profile);
            return profile.Collection.TryGetValue((int)cardType, out var count) ? count : 0;
        }

        public static string ValidateActiveSideDeck(PazaakProfile profile)
        {
            return ValidateSideDeckForCollection(profile, profile.ActiveSideDeck);
        }

        public static string ValidateSideDeckForCollection(PazaakProfile profile, IEnumerable<PazaakCardType> sideDeck)
        {
            var cards = sideDeck.ToList();
            var validation = PazaakGameEngine.ValidateSideDeck(cards);
            if (!string.IsNullOrWhiteSpace(validation))
                return validation;

            var required = cards
                .GroupBy(x => x)
                .ToDictionary(x => x.Key, x => x.Count());

            foreach (var (card, count) in required)
            {
                if (GetOwnedCount(profile, card) < count)
                    return $"You do not own enough {PazaakCardCatalog.GetName(card)} cards.";
            }

            return string.Empty;
        }

        public static void SetActiveSideDeck(uint player, IEnumerable<PazaakCardType> sideDeck)
        {
            var profile = GetOrCreateProfile(player);
            var cards = sideDeck.ToList();
            var validation = ValidateSideDeckForCollection(profile, cards);
            if (!string.IsNullOrWhiteSpace(validation))
                throw new InvalidOperationException(validation);

            profile.ActiveSideDeck = cards;
            profile.DateUpdated = DateTime.UtcNow;
            DB.Set(profile);
        }

        public static IEnumerable<PazaakProfile> GetLeaderboard(int count)
        {
            var query = new DBQuery<PazaakProfile>()
                .OrderBy(nameof(PazaakProfile.PvPRating), false)
                .AddPaging(count, 0);

            return DB.Search(query);
        }

        public static PazaakMatchState GetActiveMatch(uint player)
        {
            var playerId = GetObjectUUID(player);
            if (!_sessionsByPlayerId.TryGetValue(playerId, out var session))
                return null;

            RebindPlayerObject(session, playerId, player);
            return session.Match;
        }

        public static bool IsInMatch(uint player)
        {
            return GetActiveMatch(player) != null;
        }

        public static string StartNpcMatch(uint player, string npcProfileId, int wager)
        {
            return StartNpcMatch(player, npcProfileId, string.Empty, string.Empty, wager);
        }

        public static string StartNpcMatch(uint player, string npcProfileId, string npcRewardId, string npcDisplayName, int wager)
        {
            if (IsInMatch(player))
                return "You are already in a Pazaak match.";

            var profile = GetOrCreateProfile(player);
            var validation = ValidateActiveSideDeck(profile);
            if (!string.IsNullOrWhiteSpace(validation))
                return validation;

            var npc = PazaakNpcProfileCatalog.Get(npcProfileId);
            if (wager < npc.MinimumWager || wager > npc.MaximumWager)
                return $"This opponent accepts wagers from {npc.MinimumWager} to {npc.MaximumWager} credits.";

            if (GetGold(player) < wager)
                return "You do not have enough credits for that wager.";

            if (wager > 0)
                AssignCommand(player, () => TakeGoldFromCreature(wager, player, true));

            var playerId = GetObjectUUID(player);
            var npcParticipantId = string.IsNullOrWhiteSpace(npcRewardId) ? npc.Id : npcRewardId;
            var npcName = string.IsNullOrWhiteSpace(npcDisplayName) ? npc.Name : npcDisplayName;
            var random = new PazaakSystemRandom();
            var match = PazaakGameEngine.CreateMatch(
                playerId,
                GetName(player),
                profile.ActiveSideDeck,
                npcParticipantId,
                npcName,
                npc.SideDeck,
                true,
                false,
                false,
                wager,
                random);
            var escrow = CreateEscrow(match, playerId, npcParticipantId, wager, wager, false, false);
            var session = new PazaakMatchSession
            {
                Match = match,
                Random = random,
                EscrowId = escrow.Id,
                PlayerOne = player,
                PlayerTwo = OBJECT_INVALID,
                NpcProfile = npc,
                NpcRewardId = npcRewardId,
                TurnTimerSeconds = 0,
            };

            _sessionsByPlayerId[playerId] = session;
            ProcessNpcTurns(session);
            Refresh(player);
            return string.Empty;
        }

        public static string CreateTableLobby(uint player, uint table, bool isRated, int wager, int turnTimerSeconds)
        {
            if (IsInMatch(player))
                return "You are already in a Pazaak match.";

            if (isRated && wager <= 0)
                return "Rated Pazaak requires a nonzero wager.";

            if (wager < 0)
                return "Wager cannot be negative.";

            if (GetGold(player) < wager)
                return "You do not have enough credits for that wager.";

            var profile = GetOrCreateProfile(player);
            var validation = ValidateActiveSideDeck(profile);
            if (!string.IsNullOrWhiteSpace(validation))
                return validation;

            var tableId = GetTableId(table);
            if (_lobbiesByTableId.TryGetValue(tableId, out var existing) &&
                existing.HostId != GetObjectUUID(player))
            {
                return $"{existing.HostName} is already waiting at this table.";
            }

            _lobbiesByTableId[tableId] = new PazaakTableLobby
            {
                TableId = tableId,
                Table = table,
                Host = player,
                HostId = GetObjectUUID(player),
                HostName = GetName(player),
                IsRated = isRated,
                Wager = wager,
                TurnTimerSeconds = Math.Max(15, turnTimerSeconds),
            };

            return string.Empty;
        }

        public static string JoinTableLobby(uint player, uint table)
        {
            if (IsInMatch(player))
                return "You are already in a Pazaak match.";

            var tableId = GetTableId(table);
            if (!_lobbiesByTableId.TryGetValue(tableId, out var lobby))
                return "No one is waiting at this Pazaak table.";

            var playerId = GetObjectUUID(player);
            if (lobby.HostId == playerId)
                return "You are already hosting this Pazaak table.";

            if (!GetIsObjectValid(lobby.Host))
            {
                _lobbiesByTableId.Remove(tableId);
                return "The table host is no longer available.";
            }

            if (GetGold(lobby.Host) < lobby.Wager || GetGold(player) < lobby.Wager)
                return "Both players must still be able to pay the wager.";

            var hostProfile = GetOrCreateProfile(lobby.Host);
            var joinerProfile = GetOrCreateProfile(player);
            var hostValidation = ValidateActiveSideDeck(hostProfile);
            if (!string.IsNullOrWhiteSpace(hostValidation))
                return $"{lobby.HostName}'s deck is invalid: {hostValidation}";

            var joinerValidation = ValidateActiveSideDeck(joinerProfile);
            if (!string.IsNullOrWhiteSpace(joinerValidation))
                return joinerValidation;

            if (lobby.Wager > 0)
            {
                AssignCommand(lobby.Host, () => TakeGoldFromCreature(lobby.Wager, lobby.Host, true));
                AssignCommand(player, () => TakeGoldFromCreature(lobby.Wager, player, true));
            }

            var random = new PazaakSystemRandom();
            var match = PazaakGameEngine.CreateMatch(
                lobby.HostId,
                lobby.HostName,
                hostProfile.ActiveSideDeck,
                playerId,
                GetName(player),
                joinerProfile.ActiveSideDeck,
                false,
                true,
                lobby.IsRated,
                lobby.Wager,
                random);
            var escrow = CreateEscrow(match, lobby.HostId, playerId, lobby.Wager, lobby.Wager, true, lobby.IsRated);
            var session = new PazaakMatchSession
            {
                Match = match,
                Random = random,
                EscrowId = escrow.Id,
                PlayerOne = lobby.Host,
                PlayerTwo = player,
                TurnTimerSeconds = lobby.TurnTimerSeconds,
            };

            _sessionsByPlayerId[lobby.HostId] = session;
            _sessionsByPlayerId[playerId] = session;
            _lobbiesByTableId.Remove(tableId);

            ScheduleTurnTimer(session);
            Refresh(lobby.Host);
            Refresh(player);
            return string.Empty;
        }

        public static string CancelTableLobby(uint player, uint table)
        {
            var tableId = GetTableId(table);
            if (!_lobbiesByTableId.TryGetValue(tableId, out var lobby) ||
                lobby.HostId != GetObjectUUID(player))
            {
                return "You are not hosting this Pazaak table.";
            }

            _lobbiesByTableId.Remove(tableId);
            return string.Empty;
        }

        public static string GetTableLobbyText(uint table)
        {
            var tableId = GetTableId(table);
            if (!_lobbiesByTableId.TryGetValue(tableId, out var lobby))
                return "No active table lobby.";

            return $"{lobby.HostName} is waiting for {(lobby.IsRated ? "rated" : "casual")} Pazaak, wager {lobby.Wager}.";
        }

        public static string GetNpcRewardProgressText(uint player, string npcRewardId, PazaakNpcProfile npc)
        {
            if (string.IsNullOrWhiteSpace(npcRewardId) ||
                npc == null ||
                npc.RewardWinCount <= 0 ||
                !PazaakCardCatalog.IsValidCard(npc.RewardCard))
            {
                return string.Empty;
            }

            var profile = GetOrCreateProfile(player);
            EnsureProfileCollections(profile);

            if (profile.ClaimedNpcRewards.Contains(npcRewardId))
                return $"Reward claimed: {PazaakCardCatalog.GetName(npc.RewardCard)}.";

            var wins = profile.NamedNPCWins.TryGetValue(npcRewardId, out var count) ? count : 0;
            return $"Reward: {PazaakCardCatalog.GetName(npc.RewardCard)} after {Math.Min(wins, npc.RewardWinCount)}/{npc.RewardWinCount} wins.";
        }

        public static string PlaySideCard(uint player, int sideHandIndex, int selectedValue)
        {
            if (!TryGetPlayerSession(player, out var session, out var slot))
                return string.Empty;

            try
            {
                PazaakGameEngine.PlaySideCard(session.Match, slot, sideHandIndex, selectedValue, session.Random);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            AfterPlayerAction(session);
            return string.Empty;
        }

        public static void EndTurn(uint player)
        {
            if (!TryGetPlayerSession(player, out var session, out var slot))
                return;

            PazaakGameEngine.EndTurn(session.Match, slot, session.Random);
            AfterPlayerAction(session);
        }

        public static void Stand(uint player)
        {
            if (!TryGetPlayerSession(player, out var session, out var slot))
                return;

            PazaakGameEngine.Stand(session.Match, slot, session.Random);
            AfterPlayerAction(session);
        }

        public static void Forfeit(uint player)
        {
            if (!TryGetPlayerSession(player, out var session, out var slot))
                return;

            PazaakGameEngine.Forfeit(session.Match, slot);
            SettleMatch(session);
        }

        public static void ScheduleAbandonForfeit(uint player)
        {
            if (!TryGetPlayerSession(player, out var session, out _))
                return;

            var playerId = GetObjectUUID(player);
            _pendingAbandonByPlayerId[playerId] = session.Match.MatchId;

            DelayCommand(AbandonForfeitDelaySeconds, () =>
            {
                if (!_pendingAbandonByPlayerId.TryGetValue(playerId, out var matchId) ||
                    matchId != session.Match.MatchId ||
                    session.Match.Status != PazaakMatchStatus.Active)
                {
                    return;
                }

                if (GetIsObjectValid(player) && Gui.IsWindowOpen(player, GuiWindowType.Pazaak))
                {
                    _pendingAbandonByPlayerId.Remove(playerId);
                    return;
                }

                ForfeitPlayerId(playerId);
            });
        }

        public static void CancelAbandonForfeit(uint player)
        {
            var playerId = GetObjectUUID(player);
            _pendingAbandonByPlayerId.Remove(playerId);

            if (_sessionsByPlayerId.TryGetValue(playerId, out var session))
                RebindPlayerObject(session, playerId, player);
        }

        public static bool IsPazaakCardItem(uint item)
        {
            return GetLocalInt(item, CardItemLocalVariable) > 0;
        }

        private static void SpawnWorldContent()
        {
            foreach (var definition in _worldContentDefinitions)
            {
                var area = Area.GetAreaByResref(definition.AreaResref);
                if (!GetIsObjectValid(area) || GetLocalBool(area, WorldContentSpawnedLocalVariable))
                    continue;

                SetLocalBool(area, WorldContentSpawnedLocalVariable, true);

                foreach (var table in definition.Tables)
                {
                    var placeable = CreateObject(ObjectType.Placeable, "pazaak_table", ToLocation(area, table.X, table.Y, table.Z, table.Facing));
                    if (!GetIsObjectValid(placeable))
                        continue;

                    SetName(placeable, table.Name);
                    SetPlotFlag(placeable, true);
                    SetLocalString(placeable, ConversationLocalVariable, "PazaakTableDialog");
                    SetEventScript(placeable, EventScript.Placeable_OnUsed, ScriptName.OnPlaceableGenericConversation);
                }

                foreach (var opponent in definition.Opponents)
                {
                    var npc = CreateObject(ObjectType.Creature, opponent.CreatureResref, ToLocation(area, opponent.X, opponent.Y, opponent.Z, opponent.Facing));
                    if (!GetIsObjectValid(npc))
                        continue;

                    var profile = PazaakNpcProfileCatalog.Get(opponent.ProfileId);
                    SetName(npc, opponent.Name);
                    SetPlotFlag(npc, true);
                    SetLocalString(npc, ConversationLocalVariable, "PazaakTableDialog");
                    SetLocalString(npc, NpcProfileLocalVariable, profile.Id);
                    SetLocalString(npc, NpcRewardIdLocalVariable, opponent.RewardId);
                    SetLocalString(npc, NpcDisplayNameLocalVariable, opponent.Name);
                    SetLocalInt(npc, MinimumWagerLocalVariable, profile.MinimumWager);
                    SetLocalInt(npc, MaximumWagerLocalVariable, profile.MaximumWager);
                    SetEventScript(npc, EventScript.Creature_OnDialogue, ScriptName.OnDialogStart);
                }

                if (definition.Vendor == null)
                    continue;

                var vendor = CreateObject(ObjectType.Creature, definition.Vendor.CreatureResref, ToLocation(area, definition.Vendor.X, definition.Vendor.Y, definition.Vendor.Z, definition.Vendor.Facing));
                var store = CreateObject(ObjectType.Store, CardStoreResref, ToLocation(area, definition.Vendor.X, definition.Vendor.Y, definition.Vendor.Z, definition.Vendor.Facing));
                if (GetIsObjectValid(store))
                {
                    SetLocalInt(store, VendorTierLocalVariable, definition.VendorTier);
                }

                if (!GetIsObjectValid(vendor))
                    continue;

                SetName(vendor, definition.Vendor.Name);
                SetPlotFlag(vendor, true);
                SetLocalString(vendor, ConversationLocalVariable, "PazaakTableDialog");
                SetLocalString(vendor, NpcProfileLocalVariable, PazaakNpcProfileCatalog.DefaultProfileId);
                SetLocalString(vendor, VendorStoreTagLocalVariable, CardStoreResref);
                SetLocalInt(vendor, VendorTierLocalVariable, definition.VendorTier);
                SetEventScript(vendor, EventScript.Creature_OnDialogue, ScriptName.OnDialogStart);
            }
        }

        private static void PopulateCardVendorStores()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var store = GetFirstObjectInArea(area); GetIsObjectValid(store); store = GetNextObjectInArea(area))
                {
                    if (GetObjectType(store) != ObjectType.Store)
                        continue;

                    var tier = GetLocalInt(store, VendorTierLocalVariable);
                    if (tier <= 0)
                        continue;

                    StockCardVendorStore(store, tier);
                }
            }
        }

        private static void StockCardVendorStore(uint store, int tier)
        {
            if (HasPazaakCardStock(store))
                return;

            var cards = GetVendorCardsByTier(tier)
                .Select(PazaakCardCatalog.Get)
                .OrderBy(x => x.Type)
                .ToList();

            var slot = 0;
            foreach (var card in cards)
            {
                var item = CreateItemOnObject(CardTokenResref, store);
                if (!GetIsObjectValid(item))
                    continue;

                SetName(item, $"Pazaak Card: {card.Name}");
                SetDescription(item, $"Adds one {card.Name} side card to your virtual Pazaak collection.");
                SetLocalInt(item, CardItemLocalVariable, (int)card.Type);
                SetLocalBool(item, StoreServiceItemLocalVariable, true);
                SetInfiniteFlag(item, true);
                ItemPlugin.SetBaseGoldPieceValue(item, card.VendorPrice);

                SetItemStackSize(item, 1);
                SetLocalInt(item, "PAZAAK_VENDOR_SLOT", slot++);
            }
        }

        private static bool HasPazaakCardStock(uint store)
        {
            for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
            {
                if (GetLocalInt(item, CardItemLocalVariable) > 0)
                    return true;
            }

            return false;
        }

        private static IEnumerable<PazaakCardType> GetVendorCardsByTier(int tier)
        {
            if (tier >= 4)
                return PazaakCardCatalog.GetAllCards().Select(x => x.Type);

            var cards = new List<PazaakCardType>
            {
                PazaakCardType.Plus1,
                PazaakCardType.Plus2,
                PazaakCardType.Plus3,
                PazaakCardType.Plus4,
                PazaakCardType.Minus1,
                PazaakCardType.Minus2,
                PazaakCardType.Minus3,
                PazaakCardType.Minus4,
            };

            if (tier >= 2)
            {
                cards.AddRange(new[]
                {
                    PazaakCardType.Plus5,
                    PazaakCardType.Plus6,
                    PazaakCardType.Minus5,
                    PazaakCardType.Minus6,
                    PazaakCardType.PlusMinus1,
                    PazaakCardType.PlusMinus2,
                    PazaakCardType.PlusMinus3,
                });
            }

            if (tier >= 3)
            {
                cards.AddRange(new[]
                {
                    PazaakCardType.PlusMinus4,
                    PazaakCardType.PlusMinus5,
                    PazaakCardType.PlusMinus6,
                    PazaakCardType.OneOrMinusTwo,
                    PazaakCardType.Flip2And4,
                });
            }

            return cards.Distinct();
        }

        private static Location ToLocation(uint area, float x, float y, float z, float facing)
        {
            return Location(area, Vector3(x, y, z), facing);
        }

        private static string BuildNpcRewardId(string profileId, string npcName)
        {
            var cleanedName = new string((npcName ?? string.Empty)
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray());

            while (cleanedName.Contains("__"))
            {
                cleanedName = cleanedName.Replace("__", "_");
            }

            cleanedName = cleanedName.Trim('_');
            return string.IsNullOrWhiteSpace(cleanedName)
                ? profileId
                : $"{profileId}_{cleanedName}";
        }

        private static List<PazaakWorldContentDefinition> BuildWorldContentDefinitions()
        {
            PazaakWorldTable Table(float x, float y, float z, float facing, string name)
            {
                return new PazaakWorldTable(x, y, z, facing, name);
            }

            PazaakWorldOpponent Opponent(float x, float y, float z, float facing, string name, string profileId, string creatureResref = "femalegambler")
            {
                return new PazaakWorldOpponent(x, y, z, facing, name, profileId, BuildNpcRewardId(profileId, name), creatureResref);
            }

            PazaakWorldVendor Vendor(float x, float y, float z, float facing, string name, string creatureResref = "femalegambler")
            {
                return new PazaakWorldVendor(x, y, z, facing, name, creatureResref);
            }

            return new List<PazaakWorldContentDefinition>
            {
                new PazaakWorldContentDefinition(
                    "veles_cantina",
                    2,
                    new[]
                    {
                        Table(4.2f, 21.8f, 0f, 90f, "Viscara Pazaak Table A"),
                        Table(12.6f, 20.8f, 0f, 270f, "Viscara Pazaak Table B"),
                        Table(17.3f, 29.4f, 0f, 180f, "Viscara Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(5.2f, 20.7f, 0f, 35f, "Denna Soln", "cantina_regular", "femalepatron2"),
                        Opponent(11.6f, 21.4f, 0f, 225f, "Jax Varlo", "outer_rim_sharp", "malepatron2"),
                        Opponent(16.4f, 28.4f, 0f, 45f, "Kessir Tal", "sector_hustler", "malerefugee"),
                    },
                    Vendor(15.2f, 28.8f, 0f, 180f, "Nara the Card Broker", "femalepatron1")),

                new PazaakWorldContentDefinition(
                    "nanostation015",
                    2,
                    new[]
                    {
                        Table(118.2f, 47.6f, 0f, 90f, "CZ-220 Pazaak Table A"),
                        Table(123.4f, 51.2f, 0f, 180f, "CZ-220 Pazaak Table B"),
                        Table(119.6f, 56.0f, 0f, 0f, "CZ-220 Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(117.3f, 48.8f, 0f, 55f, "Benn Ril", "cantina_regular", "malepatron2"),
                        Opponent(124.4f, 50.1f, 0f, 215f, "Asha Venn", "outer_rim_sharp", "femalepatron3"),
                        Opponent(120.6f, 55.0f, 0f, 180f, "Grast Fen", "sector_hustler", "malepatron3"),
                    },
                    Vendor(121.1f, 49.7f, 0f, 180f, "Frank's Card Runner", "femalegambler")),

                new PazaakWorldContentDefinition(
                    "moncaladaccityex",
                    3,
                    new[]
                    {
                        Table(144.8f, 111.0f, 0f, 90f, "Mon Cala Pazaak Table A"),
                        Table(149.4f, 112.1f, 0f, 270f, "Mon Cala Pazaak Table B"),
                        Table(147.2f, 115.2f, 0f, 180f, "Mon Cala Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(145.4f, 112.0f, 0f, 65f, "Solae Marr", "outer_rim_sharp", "femalegambler"),
                        Opponent(148.4f, 111.2f, 0f, 245f, "Tobin Nall", "sector_hustler", "mcdce_onmofo"),
                        Opponent(147.8f, 114.2f, 0f, 180f, "Vesh Daal", "champion", "mcdce_gamemstr"),
                    },
                    Vendor(146.4f, 110.0f, 0f, 0f, "Dac City Cardwright", "mcdce_waitress")),

                new PazaakWorldContentDefinition(
                    "hutlar_outpost",
                    3,
                    new[]
                    {
                        Table(44.2f, 13.0f, 0f, 90f, "Hutlar Pazaak Table A"),
                        Table(50.3f, 18.4f, 0f, 180f, "Hutlar Pazaak Table B"),
                        Table(24.8f, 44.0f, 0f, 0f, "Hutlar Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(45.0f, 14.2f, 0f, 70f, "Rusk Venn", "outer_rim_sharp", "voryx_ooang"),
                        Opponent(49.2f, 17.5f, 0f, 225f, "Kess Ooral", "sector_hustler", "cyylan_forevia"),
                        Opponent(25.8f, 43.4f, 0f, 180f, "Talaresh", "champion", "kieun_xorxca"),
                    },
                    Vendor(47.1f, 15.5f, 0f, 270f, "Outpost Card Trader", "guylan_verruchi")),

                new PazaakWorldContentDefinition(
                    "tat_anc_cantina",
                    2,
                    new[]
                    {
                        Table(8.2f, 10.8f, 0f, 90f, "Anchorhead Pazaak Table A"),
                        Table(12.3f, 10.2f, 0f, 270f, "Anchorhead Pazaak Table B"),
                        Table(11.8f, 14.4f, 0f, 180f, "Anchorhead Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(7.6f, 11.9f, 0f, 40f, "Rem Tolr", "cantina_regular", "malepatron2"),
                        Opponent(13.0f, 11.1f, 0f, 220f, "Vara Senn", "outer_rim_sharp", "human_smugfem01"),
                        Opponent(11.1f, 13.4f, 0f, 180f, "Dren Marr", "sector_hustler", "malepatron3"),
                    },
                    Vendor(9.5f, 8.8f, 0f, 180f, "Anchorhead Card Dealer", "femalegambler")),

                new PazaakWorldContentDefinition(
                    "ar_scor_korrcan",
                    4,
                    new[]
                    {
                        Table(20.6f, 22.1f, 0f, 90f, "Korriban Pazaak Table A"),
                        Table(29.6f, 22.1f, 0f, 270f, "Korriban Pazaak Table B"),
                        Table(25.1f, 18.4f, 0f, 0f, "Korriban Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(21.4f, 23.0f, 0f, 65f, "Seth Ruun", "outer_rim_sharp", "sith_male"),
                        Opponent(28.7f, 23.0f, 0f, 245f, "Maraq Sol", "sector_hustler", "mcdce_waitress"),
                        Opponent(25.2f, 19.6f, 0f, 180f, "Lord Vekk's Marker", "champion", "mcdce_gamemstr"),
                    },
                    Vendor(24.6f, 22.6f, 0f, 180f, "Korriban Card Fence", "mcdce_bartender")),

                new PazaakWorldContentDefinition(
                    "dath_tribevill",
                    4,
                    new[]
                    {
                        Table(96.6f, 40.6f, 0f, 90f, "Dathomir Pazaak Table A"),
                        Table(101.8f, 42.0f, 0f, 270f, "Dathomir Pazaak Table B"),
                        Table(103.4f, 38.4f, 0f, 180f, "Dathomir Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(97.4f, 41.6f, 0f, 60f, "Vexa of the Embers", "outer_rim_sharp", "female_hutlarian"),
                        Opponent(100.8f, 41.0f, 0f, 225f, "Orrin Blackleaf", "sector_hustler", "female_hutlarian"),
                        Opponent(102.6f, 39.4f, 0f, 180f, "The Ash Dealer", "champion", "female_hutlarian"),
                    },
                    Vendor(99.4f, 40.2f, 0f, 180f, "Dathomir Card Keeper", "female_hutlarian")),

                new PazaakWorldContentDefinition(
                    "dan_colonyspa",
                    1,
                    new[]
                    {
                        Table(54.6f, 24.0f, 0f, 90f, "Dantooine Pazaak Table A"),
                        Table(60.2f, 24.2f, 0f, 180f, "Dantooine Pazaak Table B"),
                        Table(66.0f, 24.4f, 0f, 270f, "Dantooine Pazaak Table C"),
                    },
                    new[]
                    {
                        Opponent(55.2f, 22.8f, 0f, 35f, "Pell Raan", "cantina_regular", "mcdce_waitress"),
                        Opponent(59.4f, 25.4f, 0f, 180f, "Bo Herran", "cantina_regular", "malepatron2"),
                        Opponent(65.2f, 23.4f, 0f, 235f, "Mira Vale", "outer_rim_sharp", "wook_female"),
                    },
                    Vendor(57.2f, 22.4f, 0f, 0f, "Dantooine Card Seller", "femalegambler")),
            };
        }

        private static void AfterPlayerAction(PazaakMatchSession session)
        {
            if (session.Match.Status == PazaakMatchStatus.Active)
                ProcessNpcTurns(session);

            if (session.Match.Status == PazaakMatchStatus.Active)
            {
                ScheduleTurnTimer(session);
                RefreshSession(session);
            }
            else
            {
                SettleMatch(session);
            }
        }

        private static void ProcessNpcTurns(PazaakMatchSession session)
        {
            var turns = 0;
            while (session.Match.Status == PazaakMatchStatus.Active &&
                   session.Match.ActiveParticipantIndex == 1 &&
                   session.Match.Participants[1].IsNpc &&
                   turns++ < 16)
            {
                var decision = PazaakAi.ChooseMove(session.Match, PazaakParticipantSlot.PlayerTwo, session.NpcProfile.Difficulty);
                if (decision.ShouldPlaySideCard)
                {
                    PazaakGameEngine.PlaySideCard(
                        session.Match,
                        PazaakParticipantSlot.PlayerTwo,
                        decision.SideHandIndex,
                        decision.SelectedValue,
                        session.Random);
                }

                if (session.Match.Status != PazaakMatchStatus.Active)
                    break;

                if (decision.ShouldStand)
                {
                    PazaakGameEngine.Stand(session.Match, PazaakParticipantSlot.PlayerTwo, session.Random);
                }
                else
                {
                    PazaakGameEngine.EndTurn(session.Match, PazaakParticipantSlot.PlayerTwo, session.Random);
                }
            }
        }

        private static void SettleMatch(PazaakMatchSession session)
        {
            var match = session.Match;
            if (match.WinnerIndex < 0)
                return;

            session.TurnTimerSequence++;

            var escrow = DB.Get<PazaakEscrow>(session.EscrowId);
            var winner = match.Participants[match.WinnerIndex];
            var loser = match.Participants[match.WinnerIndex == 0 ? 1 : 0];
            var totalPayout = escrow.PlayerOneAmount + escrow.PlayerTwoAmount;

            if (!winner.IsNpc && totalPayout > 0)
            {
                var winnerObject = match.WinnerIndex == 0 ? session.PlayerOne : session.PlayerTwo;
                PayCreditsOrPend(winner.ParticipantId, winnerObject, totalPayout);
            }

            ApplyNpcRewardProgress(session);
            UpdateRecords(match);

            escrow.IsSettled = true;
            escrow.DateSettled = DateTime.UtcNow;
            DB.Set(escrow);
            Log.Write(LogGroup.Pazaak, $"Pazaak match '{match.MatchId}' settled. Winner: {winner.Name} ({winner.ParticipantId}); Loser: {loser.Name} ({loser.ParticipantId}); Rated: {match.IsRated}; Wager: {match.Wager}; Status: {match.Status}.");

            _sessionsByPlayerId.Remove(match.Participants[0].ParticipantId);
            if (!match.Participants[1].IsNpc)
                _sessionsByPlayerId.Remove(match.Participants[1].ParticipantId);

            RefreshSession(session);
        }

        private static void ApplyNpcRewardProgress(PazaakMatchSession session)
        {
            var match = session.Match;
            if (match.IsPvP ||
                match.WinnerIndex < 0 ||
                match.Participants[match.WinnerIndex].IsNpc ||
                string.IsNullOrWhiteSpace(session.NpcRewardId) ||
                session.NpcProfile == null ||
                session.NpcProfile.RewardWinCount <= 0 ||
                !PazaakCardCatalog.IsValidCard(session.NpcProfile.RewardCard))
            {
                return;
            }

            var winner = match.Participants[match.WinnerIndex];
            var profile = GetOrCreateProfile(winner.ParticipantId);
            EnsureProfileCollections(profile);

            profile.NamedNPCWins.TryGetValue(session.NpcRewardId, out var currentWins);
            currentWins++;
            profile.NamedNPCWins[session.NpcRewardId] = currentWins;

            if (currentWins >= session.NpcProfile.RewardWinCount &&
                !profile.ClaimedNpcRewards.Contains(session.NpcRewardId))
            {
                AddCard(profile, session.NpcProfile.RewardCard, session.NpcProfile.RewardCardCount);
                profile.ClaimedNpcRewards.Add(session.NpcRewardId);

                var winnerObject = match.WinnerIndex == 0 ? session.PlayerOne : session.PlayerTwo;
                if (GetIsObjectValid(winnerObject) && GetIsPC(winnerObject))
                {
                    SendMessageToPC(
                        winnerObject,
                        $"You earn {PazaakCardCatalog.GetName(session.NpcProfile.RewardCard)} from {match.Participants[1].Name}.");
                }
            }

            profile.DateUpdated = DateTime.UtcNow;
            DB.Set(profile);
        }

        private static void ScheduleTurnTimer(PazaakMatchSession session)
        {
            if (session.TurnTimerSeconds <= 0 ||
                !session.Match.IsPvP ||
                session.Match.Status != PazaakMatchStatus.Active)
            {
                return;
            }

            var activeParticipantIndex = session.Match.ActiveParticipantIndex;
            var activePlayerId = session.Match.Participants[activeParticipantIndex].ParticipantId;
            var matchId = session.Match.MatchId;
            var sequence = ++session.TurnTimerSequence;

            DelayCommand(session.TurnTimerSeconds, () =>
            {
                if (session.Match.MatchId != matchId ||
                    session.Match.Status != PazaakMatchStatus.Active ||
                    session.TurnTimerSequence != sequence ||
                    session.Match.ActiveParticipantIndex != activeParticipantIndex)
                {
                    return;
                }

                var activePlayer = activeParticipantIndex == 0 ? session.PlayerOne : session.PlayerTwo;
                if (!GetIsObjectValid(activePlayer))
                {
                    ForfeitPlayerId(activePlayerId);
                    return;
                }

                PazaakGameEngine.EndTurn(session.Match, (PazaakParticipantSlot)activeParticipantIndex, session.Random);
                SendMessageToPC(activePlayer, "Your Pazaak turn timer expired. End Turn was selected automatically.");
                AfterPlayerAction(session);
            });
        }

        private static void ForfeitPlayerId(string playerId)
        {
            if (!_sessionsByPlayerId.TryGetValue(playerId, out var session))
                return;

            var slot = session.Match.Participants[0].ParticipantId == playerId
                ? PazaakParticipantSlot.PlayerOne
                : PazaakParticipantSlot.PlayerTwo;
            PazaakGameEngine.Forfeit(session.Match, slot);
            SettleMatch(session);
        }

        private static void UpdateRecords(PazaakMatchState match)
        {
            if (match.WinnerIndex < 0)
                return;

            var loserIndex = match.WinnerIndex == 0 ? 1 : 0;
            var winner = match.Participants[match.WinnerIndex];
            var loser = match.Participants[loserIndex];

            if (!winner.IsNpc)
            {
                var profile = GetOrCreateProfile(winner.ParticipantId);
                if (match.IsPvP)
                    profile.PvPWins++;
                else
                    profile.NPCWins++;

                profile.DateUpdated = DateTime.UtcNow;
                DB.Set(profile);
            }

            if (!loser.IsNpc)
            {
                var profile = GetOrCreateProfile(loser.ParticipantId);
                if (match.IsPvP)
                    profile.PvPLosses++;
                else
                    profile.NPCLosses++;

                profile.DateUpdated = DateTime.UtcNow;
                DB.Set(profile);
            }

            if (match.IsPvP && match.IsRated && !winner.IsNpc && !loser.IsNpc)
                UpdateRatings(winner.ParticipantId, loser.ParticipantId);
        }

        private static void UpdateRatings(string winnerId, string loserId)
        {
            var winnerProfile = GetOrCreateProfile(winnerId);
            var loserProfile = GetOrCreateProfile(loserId);
            var expectedWinner = ExpectedScore(winnerProfile.PvPRating, loserProfile.PvPRating);
            var expectedLoser = ExpectedScore(loserProfile.PvPRating, winnerProfile.PvPRating);

            winnerProfile.PvPRating += (int)Math.Round(RatingKFactor * (1 - expectedWinner));
            loserProfile.PvPRating += (int)Math.Round(RatingKFactor * (0 - expectedLoser));
            winnerProfile.DateUpdated = DateTime.UtcNow;
            loserProfile.DateUpdated = DateTime.UtcNow;
            DB.Set(winnerProfile);
            DB.Set(loserProfile);
        }

        private static double ExpectedScore(int rating, int opponentRating)
        {
            return 1d / (1d + Math.Pow(10d, (opponentRating - rating) / 400d));
        }

        private static void RefreshSession(PazaakMatchSession session)
        {
            Refresh(session.PlayerOne);
            if (GetIsObjectValid(session.PlayerTwo))
                Refresh(session.PlayerTwo);
        }

        private static void Refresh(uint player)
        {
            if (GetIsObjectValid(player) && GetIsPC(player))
                Gui.PublishRefreshEvent(player, new PazaakRefreshEvent());
        }

        private static void AddCard(PazaakProfile profile, PazaakCardType cardType, int count)
        {
            EnsureProfileCollections(profile);
            var key = (int)cardType;
            if (!profile.Collection.ContainsKey(key))
                profile.Collection[key] = 0;

            profile.Collection[key] += Math.Max(1, count);
        }

        private static void EnsureProfileCollections(PazaakProfile profile)
        {
            profile.Collection ??= new Dictionary<int, int>();
            profile.ActiveSideDeck ??= new List<PazaakCardType>();
            profile.NamedNPCWins ??= new Dictionary<string, int>();
            profile.ClaimedNpcRewards ??= new HashSet<string>();
        }

        private static PazaakEscrow CreateEscrow(
            PazaakMatchState match,
            string playerOneId,
            string playerTwoId,
            int playerOneAmount,
            int playerTwoAmount,
            bool isPvP,
            bool isRated)
        {
            var escrow = new PazaakEscrow(match.MatchId)
            {
                PlayerOneId = playerOneId,
                PlayerTwoId = playerTwoId,
                PlayerOneAmount = playerOneAmount,
                PlayerTwoAmount = playerTwoAmount,
                IsPvP = isPvP,
                IsRated = isRated,
            };

            DB.Set(escrow);
            Log.Write(LogGroup.Pazaak, $"Pazaak escrow '{escrow.Id}' created. P1: {playerOneId}/{playerOneAmount}; P2: {playerTwoId}/{playerTwoAmount}; Rated: {isRated}; PvP: {isPvP}.");
            return escrow;
        }

        private static void PayCreditsOrPend(string playerId, uint player, int amount)
        {
            if (amount <= 0)
                return;

            if (GetIsObjectValid(player) && GetIsPC(player))
            {
                GiveGoldToCreature(player, amount);
                SendMessageToPC(player, $"You win {amount} credits from Pazaak.");
            }
            else
            {
                AddPendingPayout(playerId, amount);
            }
        }

        private static void AddPendingPayout(string playerId, int amount)
        {
            if (string.IsNullOrWhiteSpace(playerId) || amount <= 0)
                return;

            var profile = GetOrCreateProfile(playerId);
            profile.PendingCreditPayout += amount;
            profile.DateUpdated = DateTime.UtcNow;
            DB.Set(profile);
        }

        private static bool TryGetPlayerSession(
            uint player,
            out PazaakMatchSession session,
            out PazaakParticipantSlot slot)
        {
            var playerId = GetObjectUUID(player);
            if (!_sessionsByPlayerId.TryGetValue(playerId, out session))
            {
                slot = PazaakParticipantSlot.PlayerOne;
                return false;
            }

            RebindPlayerObject(session, playerId, player);
            slot = session.Match.Participants[0].ParticipantId == playerId
                ? PazaakParticipantSlot.PlayerOne
                : PazaakParticipantSlot.PlayerTwo;
            return true;
        }

        private static void RebindPlayerObject(PazaakMatchSession session, string playerId, uint player)
        {
            if (session.Match.Participants[0].ParticipantId == playerId)
            {
                session.PlayerOne = player;
            }
            else if (!session.Match.Participants[1].IsNpc &&
                     session.Match.Participants[1].ParticipantId == playerId)
            {
                session.PlayerTwo = player;
            }
        }

        private static string GetTableId(uint table)
        {
            var id = GetObjectUUID(table);
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            return $"{GetTag(table)}:{GetName(GetArea(table))}";
        }
    }
}
