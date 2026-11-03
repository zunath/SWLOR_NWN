using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// What a restart does to a journaled transfer (Player City Revamp, farming and the Civic Exchange): when an entry is on record, when
    /// the character file holds its mark, and when the database holds its receipt. No database or engine calls:
    /// <see cref="TransferJournalProcess"/> orders the steps and <see cref="TransferJournal"/> reads and writes the records. The marks are
    /// the ones <see cref="CityProjectService.CityProjectDonationRules"/> keeps for donations, and the settling follows the same rules.
    /// </summary>
    public static class TransferJournalRules
    {
        private const char MarkSeparator = ',';
        private const char PayloadSeparator = '|';

        /// <summary>What a character reads when their file could not be saved and the action was not carried out.</summary>
        public const string NotSavedText = "Your character could not be saved, so nothing was changed. Please try again.";

        #region Building

        /// <summary>A new entry. Add its items with <see cref="Plain"/> and <see cref="Held"/>.</summary>
        public static TransferEntry Create(
            string playerId,
            string kind,
            TransferDirection direction,
            string targetId,
            string summary,
            DateTime nowUtc,
            params string[] payload)
        {
            return new TransferEntry
            {
                PlayerId = playerId ?? string.Empty,
                Kind = kind ?? string.Empty,
                Direction = direction,
                TargetId = targetId ?? string.Empty,
                Summary = summary ?? string.Empty,
                Payload = BuildPayload(payload),
                CreatedUtc = nowUtc
            };
        }

        /// <summary>Plain stackable items of a blueprint.</summary>
        public static TransferItem Plain(string resref, string name, int count)
        {
            return new TransferItem { Resref = resref ?? string.Empty, Name = name ?? string.Empty, Count = count };
        }

        /// <summary>A single item saved exactly as the engine wrote it.</summary>
        public static TransferItem Held(string resref, string name, string data)
        {
            return new TransferItem { Resref = resref ?? string.Empty, Name = name ?? string.Empty, Count = 1, Data = data ?? string.Empty };
        }

        /// <summary>The payload parts joined. Parts must not contain '|'.</summary>
        public static string BuildPayload(params string[] parts)
        {
            return parts == null ? string.Empty : string.Join(PayloadSeparator, parts);
        }

        /// <summary>The parts of an entry's payload, in the order they were built.</summary>
        public static string[] ParsePayload(string payload)
        {
            return string.IsNullOrEmpty(payload) ? new string[0] : payload.Split(PayloadSeparator);
        }

        #endregion

        #region Deciding

        /// <summary>
        /// What to do with an entry. A deposit's character file is saved without the items and with the mark before the target gets them,
        /// so a mark means the items left the character: no mark, they never left, so the entry goes; a mark and the target's receipt, the
        /// target holds them, so the entry and the mark go; a mark and no receipt, the target takes them if it can and the character gets
        /// them back if not. A withdrawal's database side changes, with its receipt, before the character file is saved with the items
        /// and the mark, so a mark means the character holds them: a mark, the entry and the mark go; a receipt and no mark, the character
        /// is owed the items; neither, nothing happened.
        /// </summary>
        /// <param name="marked">Whether the character file names the entry.</param>
        /// <param name="applied">Whether the database holds the entry's change.</param>
        /// <param name="targetTakes">A deposit: whether the target can take every item.</param>
        public static TransferSettlement Settle(TransferDirection direction, bool marked, bool applied, bool targetTakes)
        {
            if (direction == TransferDirection.Deposit)
            {
                if (!marked)
                    return TransferSettlement.Drop;
                if (applied)
                    return TransferSettlement.Forget;

                return targetTakes ? TransferSettlement.Apply : TransferSettlement.ToOverflow;
            }

            if (marked)
                return TransferSettlement.Forget;

            return applied ? TransferSettlement.ToOverflow : TransferSettlement.Drop;
        }

        #endregion

        #region Marks

        /// <summary>The entry ids a character file's mark names.</summary>
        public static List<string> ParseMarks(string mark)
        {
            return string.IsNullOrWhiteSpace(mark)
                ? new List<string>()
                : mark.Split(MarkSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal).ToList();
        }

        /// <summary>The mark with the entry's id added, once.</summary>
        public static string AddMark(string mark, string entryId)
        {
            var ids = ParseMarks(mark);
            if (!string.IsNullOrWhiteSpace(entryId) && !ids.Contains(entryId))
                ids.Add(entryId);

            return string.Join(MarkSeparator, ids);
        }

        /// <summary>The mark without the entry's id. Empty when nothing is left.</summary>
        public static string RemoveMark(string mark, string entryId)
        {
            return string.Join(MarkSeparator, ParseMarks(mark).Where(id => id != entryId));
        }

        #endregion

        #region Overflow

        /// <summary>
        /// The overflow record's id for one chunk of one item of an entry. It never changes, so writing an entry's items again after a
        /// restart, or after a part of them was written, writes the same records instead of a second set.
        /// </summary>
        public static string GetOverflowRecordId(string entryId, int itemIndex, int chunk)
        {
            return $"transfer:{entryId}:{itemIndex}:{chunk}";
        }

        /// <summary>What a character reads when an entry's items went to their overflow store.</summary>
        public static string DescribeOverflow(TransferEntry entry)
        {
            var total = entry.Items.Sum(item => item.Count);
            var items = string.Join(", ", entry.Items.Select(item => $"{item.Count} x {(string.IsNullOrWhiteSpace(item.Name) ? item.Resref : item.Name)}"));
            var subject = string.IsNullOrWhiteSpace(entry.Summary) ? "Your last action" : entry.Summary;
            return total == 1
                ? $"{subject} could not be finished, so {items} is now in your overflow storage. Use /overflow to claim it."
                : $"{subject} could not be finished, so {items} are now in your overflow storage. Use /overflow to claim them.";
        }

        #endregion
    }
}
