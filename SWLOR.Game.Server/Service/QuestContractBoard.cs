using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Lifecycle service for player-authored quest contracts: draft creation, reward escrow,
    /// publishing, takedown/cancellation, expiry, and delivery claiming.
    /// </summary>
    public static class QuestContractBoard
    {
        public const int MaxActiveContractsPerCDKey = 3;
        public const int MaxObjectives = 3;
        // SWLOR's stackable resource base items cap at 99 in baseitems.2da. The quest system can
        // count loose items and partial turn-ins, but this keeps an objective representable by one full stack.
        public const int MaxObjectiveQuantity = 99;
        public const int MaxRewardItems = 2;
        public const int MaxTitleLength = 60;
        public const int MaxDescriptionLength = 1000;
        public const int ContractDurationDays = 30;
        public const int PostingFeePercent = 5;
        public const int MinimumPostingFee = 100;
        public const int MinRewardCredits = 1;
        public const int MaxItemSearchResults = 50;

        private const string DeliveriesWaitingMessage = "Contract deliveries are waiting for you. Visit any Contract Board to claim your items and credits.";

        /// <summary>
        /// Runs after <see cref="Quest.RegisterQuests"/> finishes its reflection-based pass. Loads every
        /// published contract, expires (and refunds) any which are past their expiration date, and
        /// registers the rest as runtime quests. Expiry is only swept here at boot rather than on a
        /// running timer because the server reboots daily, so a periodic sweep would spend CPU for at
        /// most a day of extra contract lifetime.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestsRegistered)]
        public static void RegisterContracts()
        {
            var registeredCount = 0;
            var now = DateTime.UtcNow;

            foreach (var contract in GetPublishedContracts())
            {
                if (contract.DateExpires <= now)
                {
                    ExpireContract(contract);
                    continue;
                }

                var quest = QuestContractFactory.BuildQuest(contract);
                Quest.RegisterRuntimeQuest(quest);
                registeredCount++;
            }

            Log.Write(LogGroup.QuestContract, $"Registered {registeredCount} quest contract(s).", true);
        }

        private static List<QuestContract> GetPublishedContracts()
        {
            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.Status), (int)QuestContractStatus.Published);
            var count = (int)DB.SearchCount(query);

            return count <= 0
                ? new List<QuestContract>()
                : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// When a player enters the module, let them know if they have quest contract deliveries waiting.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyPendingDeliveries()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var query = new DBQuery<QuestContractDelivery>()
                .AddFieldSearch(nameof(QuestContractDelivery.PlayerId), playerId, false);
            var count = DB.SearchCount(query);

            if (count > 0)
            {
                SendMessageToPC(player, ColorToken.Green(DeliveriesWaitingMessage));
            }
        }

        /// <summary>
        /// Retrieves the player's existing Draft contract, or null if they don't have one. This is a
        /// read-only lookup: it never creates or persists a draft, so opening the editor to look around
        /// does not leave an empty draft cluttering the player's contract list.
        /// </summary>
        public static QuestContract GetDraft(uint player)
        {
            var playerId = GetObjectUUID(player);
            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.AuthorPlayerId), playerId, false)
                .AddFieldSearch(nameof(QuestContract.Status), (int)QuestContractStatus.Draft);

            return DB.Search(query).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the player's existing Draft contract, or creates and persists a new one if they don't
        /// have one. Only one draft is allowed per PlayerId at a time. Call this only from actions that
        /// actually commit content (saving details, adding an objective or reward item); read-only screens
        /// should use <see cref="GetDraft"/> so merely opening the editor does not persist an empty draft.
        /// </summary>
        public static QuestContract GetOrCreateDraft(uint player)
        {
            var existing = GetDraft(player);

            if (existing != null)
                return existing;

            var playerId = GetObjectUUID(player);
            var draft = new QuestContract
            {
                AuthorPlayerId = playerId,
                AuthorCDKey = GetPCPublicCDKey(player),
                AuthorName = GetName(player),
                Status = QuestContractStatus.Draft,
                CompletionsRemaining = 1
            };

            DB.Set(draft);
            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{playerId}] created quest contract draft '{draft.Id}'.");

            return draft;
        }

        /// <summary>
        /// Escrows an item from the player's inventory into their Draft contract's reward items.
        /// Mirrors the player market's item-listing rejection rules (no cursed/plot/container/legacy items).
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string AddRewardItem(uint player, uint item)
        {
            var draft = GetOrCreateDraft(player);

            if (draft.RewardItems.Count >= MaxRewardItems)
                return $"A contract can have at most {MaxRewardItems} reward items.";

            if (GetItemPossessor(item) != player)
                return "Item must be in your inventory.";

            if (GetHasInventory(item))
                return "Containers cannot be used as a contract reward.";

            if (GetItemCursedFlag(item) || GetPlotFlag(item))
                return "This item cannot be used as a contract reward.";

            if (Item.IsLegacyItem(item))
                return "Legacy items cannot be used as a contract reward.";

            var rewardItem = new QuestContractItem
            {
                Data = ObjectPlugin.Serialize(item),
                Name = GetName(item),
                Resref = GetResRef(item),
                StackSize = GetItemStackSize(item),
                IconResref = Item.GetIconResref(item)
            };

            draft.RewardItems.Add(rewardItem);
            DB.Set(draft);
            DestroyObject(item);

            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{draft.AuthorPlayerId}] escrowed reward item '{rewardItem.Name}' into contract draft '{draft.Id}'.");

            return string.Empty;
        }

        /// <summary>
        /// Removes an escrowed reward item from the player's Draft contract by its index in
        /// <see cref="QuestContract.RewardItems"/> and returns it to their inventory.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string RemoveRewardItem(uint player, int index)
        {
            var draft = GetOrCreateDraft(player);

            if (index < 0 || index >= draft.RewardItems.Count)
                return "That reward item could not be found.";

            var rewardItem = draft.RewardItems[index];
            var deserialized = ObjectPlugin.Deserialize(rewardItem.Data);

            if (!GetIsObjectValid(deserialized) || !ObjectPlugin.AcquireItem(player, deserialized))
            {
                if (GetIsObjectValid(deserialized))
                    DestroyObject(deserialized);

                return "The item could not be returned to your inventory. Make room and try again.";
            }

            draft.RewardItems.RemoveAt(index);
            DB.Set(draft);

            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{draft.AuthorPlayerId}] removed reward item '{rewardItem.Name}' from contract draft '{draft.Id}'.");

            return string.Empty;
        }

        /// <summary>
        /// Deletes the player's Draft contract. Any escrowed reward items are returned to them via a
        /// pending delivery so they cannot be lost to a full inventory.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string DeleteDraft(uint player)
        {
            var draft = GetDraft(player);
            if (draft == null)
                return "You do not have a draft contract.";

            if (draft.RewardItems.Count > 0)
            {
                var delivery = GetOrCreatePendingDelivery(draft.AuthorPlayerId, draft.Id, draft.Title);
                delivery.Items.AddRange(draft.RewardItems);
                DB.Set(delivery);
            }

            DB.Delete<QuestContract>(draft.Id);
            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{draft.AuthorPlayerId}] deleted quest contract draft '{draft.Id}' ('{draft.Title}').");

            return string.Empty;
        }

        /// <summary>
        /// Resolves the display icon for an escrowed contract item. Icons are captured at escrow time,
        /// so items escrowed before icon fixes may have an unusable placeholder stored; those are
        /// recomputed from the item's blueprint.
        /// </summary>
        public static string ResolveContractItemIcon(QuestContractItem item)
        {
            if (string.IsNullOrWhiteSpace(item.IconResref) || item.IconResref.StartsWith("iinvalid"))
                return Cache.GetItemIconByResref(item.Resref);

            return item.IconResref;
        }

        /// <summary>
        /// Validates and publishes the player's Draft contract: takes the escrowed credits + posting fee,
        /// marks it Published, and registers its runtime quest.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string PublishContract(uint player)
        {
            var draft = GetOrCreateDraft(player);
            var cdKey = GetPCPublicCDKey(player);

            var title = SanitizeContractText(draft.Title, MaxTitleLength);
            var description = SanitizeContractText(draft.Description, MaxDescriptionLength);

            var validationError = ValidateDraft(draft, title, description, Cache.GetItemNameByResref);
            if (!string.IsNullOrEmpty(validationError))
                return validationError;

            var escrowCredits = draft.RewardCredits;
            var fee = CalculatePostingFee(escrowCredits);
            var totalCost = escrowCredits + fee;

            if (GetGold(player) < totalCost)
                return $"You need {totalCost} credits to post this contract ({escrowCredits} escrowed reward + {fee} posting fee).";

            var activeQuery = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.AuthorCDKey), cdKey, false)
                .AddFieldSearch(nameof(QuestContract.Status), (int)QuestContractStatus.Published);
            var activeCount = DB.SearchCount(activeQuery);

            if (activeCount >= MaxActiveContractsPerCDKey)
                return $"You may only have {MaxActiveContractsPerCDKey} active contracts at a time.";

            AssignCommand(player, () => TakeGoldFromCreature(totalCost, player, true));

            draft.Title = title;
            draft.Description = description;
            draft.AuthorName = GetName(player);
            draft.AuthorCDKey = cdKey;
            // Contracts are single-completion: the first player to fulfill the objectives claims the reward.
            draft.CompletionsRemaining = 1;
            draft.Status = QuestContractStatus.Published;
            draft.DatePublished = DateTime.UtcNow;
            draft.DateExpires = DateTime.UtcNow.AddDays(ContractDurationDays);
            DB.Set(draft);

            var quest = QuestContractFactory.BuildQuest(draft);
            Quest.RegisterRuntimeQuest(quest);

            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{draft.AuthorPlayerId}] published contract '{draft.Id}' ('{draft.Title}') for {totalCost} credits ({fee} posting fee).");

            return string.Empty;
        }

        /// <summary>
        /// Takes down a published contract. Allowed for the contract's author, or any DM/admin.
        /// Remaining escrow (credits + items) is refunded to the author via a delivery. The posting fee
        /// is never refunded.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the requester.</returns>
        public static string CancelContract(uint requester, string contractId)
        {
            var contract = DB.Get<QuestContract>(contractId);
            if (contract == null)
                return "That contract could not be found.";

            if (contract.Status != QuestContractStatus.Published)
                return "That contract is not currently active.";

            var requesterId = GetObjectUUID(requester);
            var isAuthor = requesterId == contract.AuthorPlayerId;
            var authLevel = Authorization.GetAuthorizationLevel(requester);
            var isDM = authLevel == AuthorizationLevel.DM || authLevel == AuthorizationLevel.Admin;

            if (!isAuthor && !isDM)
                return "You do not have permission to take down this contract.";

            RefundEscrowToAuthor(contract);

            if (isAuthor)
            {
                contract.Status = QuestContractStatus.Cancelled;
            }
            else
            {
                contract.Status = QuestContractStatus.TakenDown;
                contract.TakedownPlayerId = requesterId;
                contract.TakedownReason = $"Removed by DM {GetName(requester)} [{requesterId}].";
            }

            DB.Set(contract);
            Quest.UnregisterRuntimeQuest(QuestContractFactory.BuildQuestId(contract.Id));
            VoidActiveContractQuests(contract, isAuthor ? "cancelled by its author" : "taken down");

            Log.Write(LogGroup.QuestContract, $"Contract '{contract.Id}' ('{contract.Title}') was {(isAuthor ? "cancelled by its author" : $"taken down by DM {GetName(requester)} [{requesterId}]")}.");

            return string.Empty;
        }

        /// <summary>
        /// Removes a voided contract's quest from every online player who had accepted it: clears it
        /// from their quest list, removes the journal entry, refreshes their quest UI, and notifies
        /// them of the reason. Offline players are cleaned up by <see cref="Quest.LoadPlayerQuests"/>
        /// at their next login.
        /// </summary>
        /// <param name="contract">The contract whose quest is being voided.</param>
        /// <param name="reason">Player-facing reason, e.g. "cancelled by its author".</param>
        private static void VoidActiveContractQuests(QuestContract contract, string reason)
        {
            var questId = QuestContractFactory.BuildQuestId(contract.Id);

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetIsDM(player))
                    continue;

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer == null ||
                    !dbPlayer.Quests.TryGetValue(questId, out var playerQuest) ||
                    playerQuest.DateLastCompleted != null)
                    continue;

                dbPlayer.Quests.Remove(questId);
                DB.Set(dbPlayer);

                // Custom journal entries cannot be removed by RemoveJournalQuestEntry, so the entry
                // is re-added hidden to clear it from the player's journal immediately.
                RemoveJournalQuestEntry(questId, player, false);
                PlayerPlugin.AddCustomJournalEntry(player, new JournalEntry
                {
                    Name = $"Contract: {contract.Title}",
                    Text = "This contract is no longer available.",
                    Tag = questId,
                    State = 1,
                    Priority = 1,
                    IsQuestCompleted = true,
                    IsQuestDisplayed = false,
                    Updated = 1,
                    CalendarDay = GetCalendarDay(),
                    TimeOfDay = GetTimeHour()
                }, true);

                Gui.PublishRefreshEvent(player, new QuestAbandonedRefreshEvent(questId));
                SendMessageToPC(player, ColorToken.Red($"The contract '{contract.Title}' has been {reason}. It has been removed from your journal."));
            }
        }

        /// <summary>
        /// Gives the player all credits and items from every pending <see cref="QuestContractDelivery"/>
        /// addressed to them. Items are delivered one at a time; a delivery's credits are only paid out
        /// and the delivery deleted once all of its items have been successfully acquired.
        /// </summary>
        public static void ClaimDeliveries(uint player)
        {
            var playerId = GetObjectUUID(player);
            var query = new DBQuery<QuestContractDelivery>()
                .AddFieldSearch(nameof(QuestContractDelivery.PlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);

            if (count <= 0)
            {
                SendMessageToPC(player, "You have no pending contract deliveries.");
                return;
            }

            var deliveries = DB.Search(query.AddPaging(count, 0)).ToList();
            var totalCredits = 0;
            var totalItems = 0;

            foreach (var delivery in deliveries)
            {
                if (delivery.Items.Count > 0)
                {
                    var remainingItems = new List<QuestContractItem>();

                    foreach (var deliveryItem in delivery.Items)
                    {
                        var deserialized = ObjectPlugin.Deserialize(deliveryItem.Data);

                        if (!GetIsObjectValid(deserialized) || !ObjectPlugin.AcquireItem(player, deserialized))
                        {
                            // The serialized blob stays on the delivery for a retry; destroy the failed
                            // copy so a later claim can't duplicate the item.
                            if (GetIsObjectValid(deserialized))
                                DestroyObject(deserialized);

                            remainingItems.Add(deliveryItem);
                            continue;
                        }

                        totalItems++;
                    }

                    if (remainingItems.Count > 0)
                    {
                        delivery.Items = remainingItems;
                        DB.Set(delivery);
                        continue;
                    }
                }

                if (delivery.Credits > 0)
                {
                    GiveGoldToCreature(player, delivery.Credits);
                    totalCredits += delivery.Credits;
                }

                DB.Delete<QuestContractDelivery>(delivery.Id);
            }

            if (totalCredits > 0 || totalItems > 0)
            {
                SendMessageToPC(player, $"You claimed {totalCredits} credits and {totalItems} item(s) from your quest contract deliveries.");
            }
            else
            {
                SendMessageToPC(player, "Some of your deliveries could not be claimed. Please try again.");
            }

            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{playerId}] claimed quest contract deliveries: {totalCredits} credits, {totalItems} items.");
        }

        /// <summary>
        /// Retrieves the pending delivery for a player sourced from a specific contract, creating one if
        /// it doesn't exist yet. Used so partial item turn-ins and escrow refunds accumulate into a single
        /// delivery per contract instead of creating one delivery per turn-in.
        /// </summary>
        public static QuestContractDelivery GetOrCreatePendingDelivery(string playerId, string contractId, string contractTitle)
        {
            var query = new DBQuery<QuestContractDelivery>()
                .AddFieldSearch(nameof(QuestContractDelivery.PlayerId), playerId, false)
                .AddFieldSearch(nameof(QuestContractDelivery.SourceContractId), contractId, false);
            var existing = DB.Search(query).FirstOrDefault();

            if (existing != null)
                return existing;

            var delivery = new QuestContractDelivery
            {
                PlayerId = playerId,
                SourceContractId = contractId,
                SourceContractTitle = contractTitle
            };

            DB.Set(delivery);
            NotifyDeliveryRecipientIfOnline(playerId);

            return delivery;
        }

        /// <summary>
        /// Sends the deliveries-waiting notice to a player immediately if they are online.
        /// Offline players are reminded at their next login by <see cref="NotifyPendingDeliveries"/>.
        /// Fires only when a new pending delivery is created (not on every item added to it),
        /// so a multi-item turn-in produces a single notice.
        /// </summary>
        private static void NotifyDeliveryRecipientIfOnline(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                {
                    SendMessageToPC(player, ColorToken.Green(DeliveriesWaitingMessage));
                    return;
                }
            }
        }

        /// <summary>
        /// Calculates the posting fee for a contract given its escrowed reward credits.
        /// Charges <see cref="PostingFeePercent"/>, floored at <see cref="MinimumPostingFee"/>.
        /// </summary>
        public static int CalculatePostingFee(int rewardCredits)
        {
            return Math.Max(MinimumPostingFee, rewardCredits * PostingFeePercent / 100);
        }

        /// <summary>
        /// Calculates the total credits a player must pay to publish a contract: the escrowed reward
        /// credits plus the posting fee.
        /// </summary>
        public static int CalculateTotalPublishCost(int rewardCredits)
        {
            return rewardCredits + CalculatePostingFee(rewardCredits);
        }

        /// <summary>
        /// Validates a contract draft's fields ahead of publishing. Pure aside from the injected
        /// <paramref name="resolveItemName"/> delegate, which is used to confirm each objective's item
        /// resref maps to a real item (normally <see cref="Cache.GetItemNameByResref"/>).
        /// </summary>
        /// <param name="draft">The draft contract to validate.</param>
        /// <param name="title">The sanitized contract title.</param>
        /// <param name="description">The sanitized contract description.</param>
        /// <param name="resolveItemName">Resolves an item resref to its name, or an empty string if invalid.</param>
        /// <returns>An empty string if the draft is valid, otherwise an error message to show the player.</returns>
        public static string ValidateDraft(QuestContract draft, string title, string description, Func<string, string> resolveItemName)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Please enter a title for your contract.";

            if (string.IsNullOrWhiteSpace(description))
                return "Please enter a description for your contract.";

            if (draft.Objectives == null || draft.Objectives.Count < 1 || draft.Objectives.Count > MaxObjectives)
                return $"A contract must have between 1 and {MaxObjectives} objectives.";

            foreach (var objective in draft.Objectives)
            {
                if (string.IsNullOrWhiteSpace(objective.ItemResref))
                    return "One or more objectives is missing an item.";

                if (objective.Quantity < 1 || objective.Quantity > MaxObjectiveQuantity)
                    return $"Objective quantities must be between 1 and {MaxObjectiveQuantity}.";

                var itemName = resolveItemName(objective.ItemResref);
                if (string.IsNullOrWhiteSpace(itemName))
                    return $"Item '{objective.ItemResref}' is not a valid item.";
            }

            if (draft.RewardCredits < MinRewardCredits)
                return $"Reward credits must be at least {MinRewardCredits}.";

            return string.Empty;
        }

        /// <summary>
        /// Sanitizes free-text contract input: strips color tokens and control characters, collapses
        /// repeated whitespace, trims, and caps the length. Modeled on <see cref="PlayerName.SanitizeKnownName"/>.
        /// </summary>
        public static string SanitizeContractText(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var strippedText = UtilPlugin.StripColors(input).Trim();
            var builder = new StringBuilder();

            foreach (var character in strippedText)
            {
                if (char.IsControl(character))
                    continue;

                builder.Append(character);
            }

            var sanitizedText = builder.ToString().Trim();

            while (sanitizedText.Contains("  "))
            {
                sanitizedText = sanitizedText.Replace("  ", " ");
            }

            if (sanitizedText.Length > maxLength)
                sanitizedText = sanitizedText.Substring(0, maxLength).TrimEnd();

            return sanitizedText;
        }

        /// <summary>
        /// Expires a published contract which is past its expiration date: refunds its remaining escrow to
        /// the author and leaves it unregistered (it is not rebuilt into a runtime quest).
        /// </summary>
        private static void ExpireContract(QuestContract contract)
        {
            RefundEscrowToAuthor(contract);
            contract.Status = QuestContractStatus.Expired;
            DB.Set(contract);
            Quest.UnregisterRuntimeQuest(QuestContractFactory.BuildQuestId(contract.Id));
            VoidActiveContractQuests(contract, "expired");

            Log.Write(LogGroup.QuestContract, $"Contract '{contract.Id}' ('{contract.Title}') expired.");
        }

        /// <summary>
        /// Refunds a contract's remaining escrowed credits and items to its author via a pending delivery,
        /// then clears the contract's reward items. Does not change the contract's status.
        /// </summary>
        private static void RefundEscrowToAuthor(QuestContract contract)
        {
            var refundCredits = contract.CompletionsRemaining * contract.RewardCredits;
            var delivery = GetOrCreatePendingDelivery(contract.AuthorPlayerId, contract.Id, contract.Title);
            delivery.Credits += refundCredits;
            delivery.Items.AddRange(contract.RewardItems);
            DB.Set(delivery);

            contract.RewardItems = new List<QuestContractItem>();
        }
    }
}
