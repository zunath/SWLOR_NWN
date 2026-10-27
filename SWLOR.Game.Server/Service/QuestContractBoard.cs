using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the player-authored quest contracts. The logic lives in <see cref="IQuestContractBoardService"/>;
    /// every member here forwards to the container's instance, and the event handlers stay here so their names in the
    /// handler order manifest do not change.
    /// </summary>
    public static class QuestContractBoard
    {
        // Compile-time constants: callers (including the toolset) get the values inlined, as before.
        public const int MaxActiveContractsPerCDKey = Domain.Quests.QuestContractBoardService.MaxActiveContractsPerCDKey;
        public const int MaxObjectives = Domain.Quests.QuestContractBoardService.MaxObjectives;
        public const int MaxObjectiveQuantity = Domain.Quests.QuestContractBoardService.MaxObjectiveQuantity;
        public const int MaxRewardItems = Domain.Quests.QuestContractBoardService.MaxRewardItems;
        public const int MaxTitleLength = Domain.Quests.QuestContractBoardService.MaxTitleLength;
        public const int MaxDescriptionLength = Domain.Quests.QuestContractBoardService.MaxDescriptionLength;
        public const int ContractDurationDays = Domain.Quests.QuestContractBoardService.ContractDurationDays;
        public const int PostingFeePercent = Domain.Quests.QuestContractBoardService.PostingFeePercent;
        public const int MinimumPostingFee = Domain.Quests.QuestContractBoardService.MinimumPostingFee;
        public const int MinRewardCredits = Domain.Quests.QuestContractBoardService.MinRewardCredits;

        // Only the editor window uses this one; the service logic does not, so it lives here.
        public const int MaxItemSearchResults = 50;

        private static IQuestContractBoardService Instance => FacadeAccessor.Resolve<IQuestContractBoardService>();

        /// <summary>
        /// Runs after <see cref="Quest.RegisterQuests"/> finishes its reflection-based pass. Loads every
        /// published contract, expires (and refunds) any which are past their expiration date, and
        /// registers the rest as runtime quests, then prunes settled history after 30 days. Expiry and
        /// history are only swept here at boot rather than on a
        /// running timer because the server reboots daily, so a periodic sweep would spend CPU for at
        /// most a day of extra contract lifetime.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestsRegistered)]
        public static void RegisterContracts()
        {
            Instance.RegisterContracts();
        }

        /// <summary>
        /// When a player enters the module, let them know if they have quest contract deliveries waiting.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyPendingDeliveries()
        {
            Instance.NotifyPendingDeliveries();
        }

        /// <summary>
        /// Retrieves the player's existing Draft contract, or null if they don't have one. This is a
        /// read-only lookup: it never creates or persists a draft, so opening the editor to look around
        /// does not leave an empty draft cluttering the player's contract list.
        /// </summary>
        public static QuestContract GetDraft(uint player)
        {
            return Instance.GetDraft(player);
        }

        /// <summary>
        /// Retrieves the player's existing Draft contract, or creates and persists a new one if they don't
        /// have one. Only one draft is allowed per PlayerId at a time. Call this only from actions that
        /// actually commit content (saving details, adding an objective or reward item); read-only screens
        /// should use <see cref="GetDraft"/> so merely opening the editor does not persist an empty draft.
        /// </summary>
        public static QuestContract GetOrCreateDraft(uint player)
        {
            return Instance.GetOrCreateDraft(player);
        }

        /// <summary>
        /// Escrows an item from the player's inventory into their Draft contract's reward items.
        /// Mirrors the player market's item-listing rejection rules (no cursed/plot/container/legacy items).
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string AddRewardItem(uint player, uint item)
        {
            return Instance.AddRewardItem(player, item);
        }

        /// <summary>
        /// Removes an escrowed reward item from the player's Draft contract by its index in
        /// <see cref="QuestContract.RewardItems"/> and returns it to their inventory.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string RemoveRewardItem(uint player, int index)
        {
            return Instance.RemoveRewardItem(player, index);
        }

        /// <summary>
        /// Deletes the player's Draft contract. Any escrowed reward items are returned to them via a
        /// pending delivery so they cannot be lost to a full inventory.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string DeleteDraft(uint player)
        {
            return Instance.DeleteDraft(player);
        }

        /// <summary>
        /// Identifies reusable terminal statuses. Active drafts, published contracts, and moderated
        /// takedowns cannot be copied through Post Again.
        /// </summary>
        public static bool CanPostAgain(QuestContract contract)
        {
            return Instance.CanPostAgain(contract);
        }

        /// <summary>
        /// Copies editable text, objectives, and credit reward into a new draft with its own identity.
        /// Escrowed items and lifecycle state are not copied; item rewards must be supplied again.
        /// The caller must validate authorization before persisting the draft.
        /// </summary>
        public static QuestContract CreateRepostDraft(QuestContract source)
        {
            return Instance.CreateRepostDraft(source);
        }

        /// <summary>
        /// Creates an editable repost draft for the source's author without overwriting an existing
        /// draft. Creating the draft neither publishes the contract nor charges its posting cost.
        /// </summary>
        /// <returns>An empty string on success, otherwise a player-facing validation error.</returns>
        public static string PostAgain(uint player, string contractId)
        {
            return Instance.PostAgain(player, contractId);
        }

        /// <summary>
        /// Checks source ownership, reusable status, and the author's single-draft limit before a copy
        /// is created. Missing and foreign contracts share the same error message.
        /// </summary>
        /// <returns>An empty string when reposting is allowed, otherwise a player-facing error.</returns>
        public static string ValidateRepost(QuestContract source, string authorId, bool hasDraft)
        {
            return Instance.ValidateRepost(source, authorId, hasDraft);
        }

        /// <summary>
        /// Resolves the display icon for an escrowed contract item. Icons are captured at escrow time,
        /// so items escrowed before icon fixes may have an unusable placeholder stored; those are
        /// recomputed from the item's blueprint.
        /// </summary>
        public static string ResolveContractItemIcon(QuestContractItem item)
        {
            return Instance.ResolveContractItemIcon(item);
        }

        /// <summary>
        /// Validates and publishes the player's Draft contract: takes the escrowed credits + posting fee,
        /// marks it Published, and registers its runtime quest.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string PublishContract(uint player)
        {
            return Instance.PublishContract(player);
        }

        /// <summary>
        /// Takes down a published contract. Allowed for the contract's author, or any DM/admin.
        /// Remaining escrow (credits + items) is refunded to the author via a delivery. The posting fee
        /// is never refunded.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the requester.</returns>
        public static string CancelContract(uint requester, string contractId)
        {
            return Instance.CancelContract(requester, contractId);
        }

        /// <summary>
        /// Gives the player all credits and items from every pending <see cref="QuestContractDelivery"/>
        /// addressed to them. Items are delivered one at a time; a delivery's credits are only paid out
        /// once all of its items have been successfully acquired. Reward payments retain an empty
        /// receipt while its source contract exists so settlement recovery cannot recreate a payment
        /// that was already claimed. Claims after history cleanup no longer need that receipt.
        /// </summary>
        public static void ClaimDeliveries(uint player)
        {
            Instance.ClaimDeliveries(player);
        }

        /// <summary>
        /// Retrieves the pending delivery for a player sourced from a specific contract, creating one if
        /// it doesn't exist yet. Refunds accumulate separately from held objective submissions and
        /// retained reward-payment receipts.
        /// </summary>
        public static QuestContractDelivery GetOrCreatePendingDelivery(string playerId, string contractId, string contractTitle)
        {
            return Instance.GetOrCreatePendingDelivery(playerId, contractId, contractTitle);
        }

        public static bool IsClaimable(QuestContractDelivery delivery)
        {
            return Instance.IsClaimable(delivery);
        }

        public static bool HasPendingDeliveries(string playerId)
        {
            return Instance.HasPendingDeliveries(playerId);
        }

        public static QuestContractDelivery GetOrCreateSubmission(string playerId, QuestContract contract)
        {
            return Instance.GetOrCreateSubmission(playerId, contract);
        }

        /// <summary>
        /// Changes ownership of the existing escrow record in place. A terminal contract releases the
        /// winner's submissions to the author and returns everyone else's. Abandoning releases only
        /// that player's attempt, which cannot be reused by a later acceptance.
        /// </summary>
        public static void ReleaseSubmissions(string contractId, string abandoningPlayerId = null)
        {
            Instance.ReleaseSubmissions(contractId, abandoningPlayerId);
        }

        public static bool TryReleaseSubmission(QuestContractDelivery delivery, QuestContract contract, string abandoningPlayerId = null)
        {
            return Instance.TryReleaseSubmission(delivery, contract, abandoningPlayerId);
        }

        /// <summary>
        /// Records the winner and closing date before settling a published contract. Persisting the
        /// pending flag first allows boot recovery to finish an interrupted settlement without reopening it.
        /// </summary>
        public static void CompleteContract(QuestContract contract, string playerId)
        {
            Instance.CompleteContract(contract, playerId);
        }

        public static void SettleCompletedContract(QuestContract contract)
        {
            Instance.SettleCompletedContract(contract);
        }

        /// <summary>
        /// Calculates the posting fee for a contract given its escrowed reward credits.
        /// Charges <see cref="PostingFeePercent"/>, floored at <see cref="MinimumPostingFee"/>.
        /// </summary>
        public static int CalculatePostingFee(int rewardCredits)
        {
            return Instance.CalculatePostingFee(rewardCredits);
        }

        /// <summary>
        /// Calculates the total credits a player must pay to publish a contract: the escrowed reward
        /// credits plus the posting fee.
        /// </summary>
        public static int CalculateTotalPublishCost(int rewardCredits)
        {
            return Instance.CalculateTotalPublishCost(rewardCredits);
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
            return Instance.ValidateDraft(draft, title, description, resolveItemName);
        }

        /// <summary>
        /// Sanitizes free-text contract input: strips color tokens and control characters, collapses
        /// repeated whitespace, trims, and caps the length. Modeled on <see cref="PlayerName.SanitizeKnownName"/>.
        /// </summary>
        public static string SanitizeContractText(string input, int maxLength)
        {
            return Instance.SanitizeContractText(input, maxLength);
        }
    }
}
