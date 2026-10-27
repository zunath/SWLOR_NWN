using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Domain.Quests
{
    /// <summary>
    /// Lifecycle of player-authored quest contracts: draft creation, reward escrow, publishing, takedown and
    /// cancellation, expiry, and delivery claiming. The static facade <c>Service.QuestContractBoard</c> forwards to the
    /// container's instance of this interface and keeps the event handlers and the constants.
    /// </summary>
    public interface IQuestContractBoardService
    {
        /// <summary>
        /// Runs after the quests are registered. Loads every published contract, expires (and refunds) any which are
        /// past their expiration date, and registers the rest as runtime quests, then prunes settled history after
        /// 30 days. Expiry and history are only swept here at boot rather than on a running timer because the server
        /// reboots daily, so a periodic sweep would spend CPU for at most a day of extra contract lifetime.
        /// </summary>
        void RegisterContracts();

        /// <summary>
        /// When a player enters the module, let them know if they have quest contract deliveries waiting.
        /// </summary>
        void NotifyPendingDeliveries();

        /// <summary>
        /// Retrieves the player's existing Draft contract, or null if they don't have one. This is a
        /// read-only lookup: it never creates or persists a draft, so opening the editor to look around
        /// does not leave an empty draft cluttering the player's contract list.
        /// </summary>
        QuestContract GetDraft(uint player);

        /// <summary>
        /// Retrieves the player's existing Draft contract, or creates and persists a new one if they don't
        /// have one. Only one draft is allowed per PlayerId at a time. Call this only from actions that
        /// actually commit content (saving details, adding an objective or reward item); read-only screens
        /// should use <see cref="GetDraft"/> so merely opening the editor does not persist an empty draft.
        /// </summary>
        QuestContract GetOrCreateDraft(uint player);

        /// <summary>
        /// Escrows an item from the player's inventory into their Draft contract's reward items.
        /// Mirrors the player market's item-listing rejection rules (no cursed/plot/container/legacy items).
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        string AddRewardItem(uint player, uint item);

        /// <summary>
        /// Removes an escrowed reward item from the player's Draft contract by its index in
        /// <see cref="QuestContract.RewardItems"/> and returns it to their inventory.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        string RemoveRewardItem(uint player, int index);

        /// <summary>
        /// Deletes the player's Draft contract. Any escrowed reward items are returned to them via a
        /// pending delivery so they cannot be lost to a full inventory.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        string DeleteDraft(uint player);

        /// <summary>
        /// Identifies reusable terminal statuses. Active drafts, published contracts, and moderated
        /// takedowns cannot be copied through Post Again.
        /// </summary>
        bool CanPostAgain(QuestContract contract);

        /// <summary>
        /// Copies editable text, objectives, and credit reward into a new draft with its own identity.
        /// Escrowed items and lifecycle state are not copied; item rewards must be supplied again.
        /// The caller must validate authorization before persisting the draft.
        /// </summary>
        QuestContract CreateRepostDraft(QuestContract source);

        /// <summary>
        /// Creates an editable repost draft for the source's author without overwriting an existing
        /// draft. Creating the draft neither publishes the contract nor charges its posting cost.
        /// </summary>
        /// <returns>An empty string on success, otherwise a player-facing validation error.</returns>
        string PostAgain(uint player, string contractId);

        /// <summary>
        /// Checks source ownership, reusable status, and the author's single-draft limit before a copy
        /// is created. Missing and foreign contracts share the same error message.
        /// </summary>
        /// <returns>An empty string when reposting is allowed, otherwise a player-facing error.</returns>
        string ValidateRepost(QuestContract source, string authorId, bool hasDraft);

        /// <summary>
        /// Resolves the display icon for an escrowed contract item. Icons are captured at escrow time,
        /// so items escrowed before icon fixes may have an unusable placeholder stored; those are
        /// recomputed from the item's blueprint.
        /// </summary>
        string ResolveContractItemIcon(QuestContractItem item);

        /// <summary>
        /// Validates and publishes the player's Draft contract: takes the escrowed credits + posting fee,
        /// marks it Published, and registers its runtime quest.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        string PublishContract(uint player);

        /// <summary>
        /// Takes down a published contract. Allowed for the contract's author, or any DM/admin.
        /// Remaining escrow (credits + items) is refunded to the author via a delivery. The posting fee
        /// is never refunded.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the requester.</returns>
        string CancelContract(uint requester, string contractId);

        /// <summary>
        /// Gives the player all credits and items from every pending <see cref="QuestContractDelivery"/>
        /// addressed to them. Items are delivered one at a time; a delivery's credits are only paid out
        /// once all of its items have been successfully acquired. Reward payments retain an empty
        /// receipt while its source contract exists so settlement recovery cannot recreate a payment
        /// that was already claimed. Claims after history cleanup no longer need that receipt.
        /// </summary>
        void ClaimDeliveries(uint player);

        /// <summary>
        /// Retrieves the pending delivery for a player sourced from a specific contract, creating one if
        /// it doesn't exist yet. Refunds accumulate separately from held objective submissions and
        /// retained reward-payment receipts.
        /// </summary>
        QuestContractDelivery GetOrCreatePendingDelivery(string playerId, string contractId, string contractTitle);

        bool IsClaimable(QuestContractDelivery delivery);

        bool HasPendingDeliveries(string playerId);

        QuestContractDelivery GetOrCreateSubmission(string playerId, QuestContract contract);

        /// <summary>
        /// Changes ownership of the existing escrow record in place. A terminal contract releases the
        /// winner's submissions to the author and returns everyone else's. Abandoning releases only
        /// that player's attempt, which cannot be reused by a later acceptance.
        /// </summary>
        void ReleaseSubmissions(string contractId, string abandoningPlayerId = null);

        bool TryReleaseSubmission(QuestContractDelivery delivery, QuestContract contract, string abandoningPlayerId = null);

        /// <summary>
        /// Records the winner and closing date before settling a published contract. Persisting the
        /// pending flag first allows boot recovery to finish an interrupted settlement without reopening it.
        /// </summary>
        void CompleteContract(QuestContract contract, string playerId);

        void SettleCompletedContract(QuestContract contract);

        /// <summary>
        /// Calculates the posting fee for a contract given its escrowed reward credits.
        /// Charges the posting fee percent, floored at the minimum posting fee.
        /// </summary>
        int CalculatePostingFee(int rewardCredits);

        /// <summary>
        /// Calculates the total credits a player must pay to publish a contract: the escrowed reward
        /// credits plus the posting fee.
        /// </summary>
        int CalculateTotalPublishCost(int rewardCredits);

        /// <summary>
        /// Validates a contract draft's fields ahead of publishing. Pure aside from the injected
        /// <paramref name="resolveItemName"/> delegate, which is used to confirm each objective's item
        /// resref maps to a real item (normally the item name cache).
        /// </summary>
        /// <param name="draft">The draft contract to validate.</param>
        /// <param name="title">The sanitized contract title.</param>
        /// <param name="description">The sanitized contract description.</param>
        /// <param name="resolveItemName">Resolves an item resref to its name, or an empty string if invalid.</param>
        /// <returns>An empty string if the draft is valid, otherwise an error message to show the player.</returns>
        string ValidateDraft(QuestContract draft, string title, string description, Func<string, string> resolveItemName);

        /// <summary>
        /// Sanitizes free-text contract input: strips color tokens and control characters, collapses
        /// repeated whitespace, trims, and caps the length.
        /// </summary>
        string SanitizeContractText(string input, int maxLength);
    }
}
