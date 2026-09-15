using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    /// <summary>A claim checkpoint saved in the same character file as the awarded inventory and gold.</summary>
    public class QuestContractClaim
    {
        public int Revision { get; set; }
        public List<int> ItemIndexes { get; set; } = new();
        public int Credits { get; set; }

        /// <summary>
        /// Native awards and their checkpoint are saved together before the database delivery is
        /// reduced. A retry finishes that reduction without issuing the saved awards again.
        /// </summary>
        public static QuestContractDelivery Claim(QuestContractDelivery delivery,
            Func<QuestContractClaim> loadCheckpoint, Action<QuestContractClaim> saveCheckpoint,
            Func<QuestContractItem, bool> acquireItem, Action<int> giveCredits,
            Action saveCharacter, Action<QuestContractDelivery> saveDelivery)
        {
            var checkpoint = loadCheckpoint();
            if (checkpoint != null && checkpoint.Revision > delivery.ClaimRevision + 1)
                throw new InvalidOperationException("Contract claim checkpoint is ahead of its delivery.");

            if (checkpoint == null || checkpoint.Revision <= delivery.ClaimRevision)
            {
                checkpoint = new QuestContractClaim { Revision = checked(delivery.ClaimRevision + 1) };
                for (var index = 0; index < delivery.Items.Count; index++)
                {
                    if (!acquireItem(delivery.Items[index]))
                        continue;
                    checkpoint.ItemIndexes.Add(index);
                    saveCheckpoint(checkpoint);
                }

                if (checkpoint.ItemIndexes.Count == delivery.Items.Count && delivery.Credits > 0)
                {
                    giveCredits(delivery.Credits);
                    checkpoint.Credits = delivery.Credits;
                    saveCheckpoint(checkpoint);
                }
            }

            if (checkpoint.ItemIndexes.Count == 0 && checkpoint.Credits == 0)
                return delivery;

            if (checkpoint.ItemIndexes.Distinct().Count() != checkpoint.ItemIndexes.Count ||
                checkpoint.ItemIndexes.Any(index => index < 0 || index >= delivery.Items.Count) ||
                (checkpoint.Credits != 0 && checkpoint.Credits != delivery.Credits))
                throw new InvalidOperationException("Contract claim checkpoint does not match its delivery.");

            // Keep the in-memory checkpoint if either save fails. A live retry must save the character
            // again; a reconnect loads either the old file without awards or the saved file with this
            // checkpoint. Never mutate DB.Get's cached delivery before persistence succeeds.
            saveCharacter();
            var remaining = JsonConvert.DeserializeObject<QuestContractDelivery>(JsonConvert.SerializeObject(delivery));
            remaining.Items = delivery.Items.Where((_, index) => !checkpoint.ItemIndexes.Contains(index)).ToList();
            remaining.Credits -= checkpoint.Credits;
            remaining.ClaimRevision = checkpoint.Revision;
            saveDelivery(remaining);
            return remaining;
        }
    }
}
