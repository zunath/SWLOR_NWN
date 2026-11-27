using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    /// <summary>
    /// Withdraws a published contract nobody can complete any more (it asks for an item the game retired): the contract
    /// closes as Cancelled, not Taken Down (a moderation status that blocks Post Again), so its author can post it again
    /// with the retired objective replaced, and gets the escrowed reward and the posting fee back in a delivery of its
    /// own, since the author did nothing wrong. The delivery's fixed id makes a rerun refund nothing twice.
    /// </summary>
    public static class QuestContractRetirement
    {
        public static string RefundDeliveryId(string contractId) => contractId + "-retired";

        /// <summary>
        /// Closes the contract and returns the author's refund to save before it, or null when
        /// <paramref name="refundSaved"/> says an earlier run already saved it.
        /// </summary>
        public static QuestContractDelivery Retire(QuestContract contract, bool refundSaved, int postingFee, string reason, DateTime now)
        {
            QuestContractDelivery refund = null;
            if (!refundSaved)
            {
                refund = new QuestContractDelivery
                {
                    Id = RefundDeliveryId(contract.Id),
                    PlayerId = contract.AuthorPlayerId,
                    SourceContractId = contract.Id,
                    SourceContractTitle = contract.Title,
                    Credits = contract.CompletionsRemaining * contract.RewardCredits + postingFee,
                    Items = contract.RewardItems?.ToList() ?? new List<QuestContractItem>(),
                };
            }

            contract.RewardItems = new List<QuestContractItem>();
            contract.Status = QuestContractStatus.Cancelled;
            contract.TakedownReason = reason;
            contract.DateClosed = now;
            return refund;
        }
    }
}
