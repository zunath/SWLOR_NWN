using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>A receipt saved in the character file with its delivered food portions.</summary>
    public class CraftedFoodClaim
    {
        public const string CheckpointVariable = "CRAFTED_FOOD_CLAIM";

        public string DeliveryId { get; set; } = string.Empty;
        public int Revision { get; set; }
        public List<int> ItemIndexes { get; set; } = new();

        /// <summary>
        /// Saves the awarded portions and their receipt together before reducing the stored batch.
        /// A retry finishes that reduction without awarding those portions again.
        /// </summary>
        public static CraftedFoodDelivery Claim(CraftedFoodDelivery delivery,
            Func<CraftedFoodClaim> loadCheckpoint, Action<CraftedFoodClaim> saveCheckpoint,
            Func<string, bool> acquireItem, Action saveCharacter, Action<CraftedFoodDelivery> saveDelivery)
        {
            ArgumentNullException.ThrowIfNull(delivery);
            ArgumentNullException.ThrowIfNull(loadCheckpoint);
            ArgumentNullException.ThrowIfNull(saveCheckpoint);
            ArgumentNullException.ThrowIfNull(acquireItem);
            ArgumentNullException.ThrowIfNull(saveCharacter);
            ArgumentNullException.ThrowIfNull(saveDelivery);

            var checkpoint = loadCheckpoint();
            if (checkpoint != null && (checkpoint.DeliveryId != delivery.Id ||
                checkpoint.Revision <= 0 || checkpoint.Revision > checked(delivery.ClaimRevision + 1)))
                throw new InvalidOperationException("The crafted food receipt does not match its delivery.");

            if (checkpoint == null || checkpoint.Revision <= delivery.ClaimRevision)
            {
                checkpoint = new CraftedFoodClaim
                {
                    DeliveryId = delivery.Id,
                    Revision = checked(delivery.ClaimRevision + 1)
                };
                for (var index = 0; index < delivery.Items.Count; index++)
                {
                    if (!acquireItem(delivery.Items[index]))
                        continue;
                    checkpoint.ItemIndexes.Add(index);
                    saveCheckpoint(checkpoint);
                }
            }

            if (checkpoint.ItemIndexes == null ||
                checkpoint.ItemIndexes.Distinct().Count() != checkpoint.ItemIndexes.Count ||
                checkpoint.ItemIndexes.Any(index => index < 0 || index >= delivery.Items.Count))
                throw new InvalidOperationException("The crafted food receipt contains invalid portions.");

            if (checkpoint.ItemIndexes.Count == 0)
                return delivery;

            // Keep the receipt if either save fails. Reconnecting loads either the old character
            // without these portions, or the saved character carrying this same receipt.
            saveCharacter();
            var remaining = JsonConvert.DeserializeObject<CraftedFoodDelivery>(JsonConvert.SerializeObject(delivery));
            remaining.Items = delivery.Items.Where((_, index) => !checkpoint.ItemIndexes.Contains(index)).ToList();
            remaining.ClaimRevision = checkpoint.Revision;
            saveDelivery(remaining);
            return remaining;
        }
    }
}
