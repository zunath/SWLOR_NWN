using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    public class QuestContractReward : IQuestReward
    {
        private readonly string _contractId;

        public QuestContractReward(string contractId)
        {
            _contractId = contractId;
        }

        public bool IsSelectable => false;
        public string MenuName => "Contract Reward";

        /// <summary>
        /// Records one completed batch, settles its escrow into a recoverable
        /// delivery, and attempts collection. Failed item transfers remain available at the board.
        /// </summary>
        public void GiveReward(uint player)
        {
            var contract = DB.Get<QuestContract>(_contractId);
            if (contract == null)
                return;

            // Escrow may only be paid out while the contract is published with stock remaining. This is
            // unreachable through the normal flow but prevents credits from being created out of thin air
            // if a new completion path is ever added.
            if (contract.Status != QuestContractStatus.Published || contract.CompletionsRemaining <= 0)
            {
                Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{GetObjectUUID(player)}] completed contract '{contract.Id}' ('{contract.Title}') but it has no escrow remaining (status: {contract.Status}, completions: {contract.CompletionsRemaining}). No reward was paid.");
                SendMessageToPC(player, $"The contract '{contract.Title}' is no longer active. No reward could be paid.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var attempt = DB.Get<Player>(playerId)?.Quests.GetValueOrDefault(QuestContractFactory.BuildQuestId(_contractId))?.TimesCompleted ?? 1;
            if (!QuestContractBoard.CompleteContract(contract, playerId, attempt))
            {
                SendMessageToPC(player, "This contract batch could not be paid. Any unfinished submissions remain returnable at the board.");
                return;
            }
            QuestContractBoard.ClaimDeliveries(player);

            for (var observer = GetFirstPC(); GetIsObjectValid(observer); observer = GetNextPC())
                Gui.PublishRefreshEvent(observer, new QuestContractPublishedRefreshEvent());

            SendMessageToPC(player, $"Your reward for '{contract.Title}' is ready. Any items or credits you could not receive remain claimable at any Contract Board.");
            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{GetObjectUUID(player)}] completed contract '{contract.Id}' ('{contract.Title}'); {contract.RewardCredits} credits and its reward items were assigned to their delivery.");
        }
    }
}
