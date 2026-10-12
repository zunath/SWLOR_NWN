using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    /// <summary>
    /// Reserves one funded batch before creating its retained payment receipt. Every write can
    /// be replayed after a restart, including delivery ownership transfers and claimed payments.
    /// </summary>
    public static class QuestContractSettlement
    {
        public static bool Complete(QuestContract contract, string playerId, int attempt,
            Func<List<QuestContractDelivery>> readDeliveries, Action<QuestContract> saveContract,
            Action<QuestContractDelivery> saveDelivery)
        {
            Settle(contract, readDeliveries, saveContract, saveDelivery);
            var deliveries = readDeliveries();
            if (contract.Status != QuestContractStatus.Published || contract.CompletionsRemaining <= 0 ||
                deliveries.Any(x => x.IsRewardPayment && x.PlayerId == playerId && x.CompletionAttempt == attempt))
                return false;

            var submission = deliveries.SingleOrDefault(x => x.HeldForCompletion &&
                x.PlayerId == playerId && x.CompletionAttempt == attempt);
            if (submission == null)
                return false;

            contract.CompletedByPlayerId = playerId;
            contract.CompletionAttempt = attempt;
            contract.CompletedSubmissionId = submission.Id;
            contract.CompletionsRemaining--;
            if (contract.CompletionsRemaining == 0)
                contract.Status = QuestContractStatus.Fulfilled;
            contract.SettlementPending = 1;
            saveContract(contract);

            Settle(contract, readDeliveries, saveContract, saveDelivery);
            return true;
        }

        public static string PaymentId(QuestContract contract)
        {
            // Keep the existing receipt ID for single-completion and legacy contracts.
            return contract.TotalCompletions == 1
                ? contract.Id + "-reward"
                : contract.Id + "-reward-" + (contract.TotalCompletions - contract.CompletionsRemaining);
        }

        public static void Settle(QuestContract contract, Func<List<QuestContractDelivery>> readDeliveries,
            Action<QuestContract> saveContract, Action<QuestContractDelivery> saveDelivery)
        {
            if (contract.SettlementPending == 0 || string.IsNullOrWhiteSpace(contract.CompletedByPlayerId))
                return;

            var deliveries = readDeliveries();
            var receiptId = PaymentId(contract);
            if (deliveries.All(x => x.Id != receiptId))
            {
                saveDelivery(new QuestContractDelivery
                {
                    Id = receiptId,
                    PlayerId = contract.CompletedByPlayerId,
                    SourceContractId = contract.Id,
                    SourceContractTitle = contract.Title,
                    CompletionAttempt = contract.CompletionAttempt,
                    IsRewardPayment = true,
                    Credits = contract.RewardCredits,
                    Items = contract.RewardItems.ToList()
                });
            }

            foreach (var delivery in deliveries.Where(x => x.HeldForCompletion))
            {
                if (IsWinningSubmission(delivery, contract))
                    delivery.PlayerId = contract.AuthorPlayerId;
                else if (contract.Status == QuestContractStatus.Published)
                    continue;

                delivery.HeldForCompletion = false;
                saveDelivery(delivery);
            }

            contract.RewardItems.Clear();
            contract.SettlementPending = 0;
            saveContract(contract);
        }

        public static bool IsWinningSubmission(QuestContractDelivery delivery, QuestContract contract)
        {
            if (!string.IsNullOrEmpty(contract.CompletedSubmissionId))
                return delivery.Id == contract.CompletedSubmissionId;

            // Old pending settlements predate attempt-specific submission IDs.
            return contract.Status == QuestContractStatus.Fulfilled &&
                delivery.PlayerId == contract.CompletedByPlayerId && delivery.CompletionAttempt == contract.CompletionAttempt;
        }
    }
}
