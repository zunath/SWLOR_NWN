using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    public static class QuestContractHistory
    {
        public const int RetentionDays = 30;

        public static bool PruneOrphanedDelivery(QuestContractDelivery delivery, Func<string, bool> sourceExists,
            Action<string> deleteDelivery)
        {
            if (delivery.HeldForCompletion || delivery.Credits != 0 || delivery.Items.Count > 0 ||
                sourceExists(delivery.SourceContractId))
                return false;

            deleteDelivery(delivery.Id);
            return true;
        }

        public static bool Prune(QuestContract contract, DateTime now,
            Func<List<QuestContractDelivery>> readDeliveries, Action<QuestContract> saveContract,
            Action<string> deleteContract, Action<string> deleteDelivery)
        {
            if (contract.Status != QuestContractStatus.Fulfilled && contract.Status != QuestContractStatus.Expired &&
                contract.Status != QuestContractStatus.Cancelled && contract.Status != QuestContractStatus.TakenDown)
                return false;

            if (contract.DateClosed == null)
            {
                // Expiry has a known date. Older completions and cancellations get a full retention
                // window from their first sweep because their actual closing date was not recorded.
                contract.DateClosed = contract.Status == QuestContractStatus.Expired && contract.DateExpires != default
                    ? contract.DateExpires : now;
                saveContract(contract);
            }

            if (contract.DateClosed > now.AddDays(-RetentionDays) || contract.SettlementPending != 0 ||
                contract.RewardItems.Count > 0)
                return false;

            var deliveries = readDeliveries();
            if (deliveries.Any(delivery => delivery.HeldForCompletion))
                return false;

            // Keep payments and refunds with any remaining value independently of the history.
            // Remove empty receipts first so a failed deletion is retried with the contract next boot.
            // SettlementPending is already clear, so terminal contracts cannot recreate these payments.
            foreach (var delivery in deliveries.Where(delivery => delivery.Credits == 0 && delivery.Items.Count == 0))
                deleteDelivery(delivery.Id);
            deleteContract(contract.Id);
            return true;
        }
    }
}
