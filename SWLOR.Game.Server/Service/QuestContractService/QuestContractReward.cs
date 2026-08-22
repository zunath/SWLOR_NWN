using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWNX;

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
        /// Pays out the escrowed credits and reward items for a single completion, then decrements the
        /// contract's remaining completion count. When completions reach zero the contract is fulfilled
        /// and its runtime quest is unregistered so it can no longer be accepted.
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

            if (contract.RewardCredits > 0)
                GiveGoldToCreature(player, contract.RewardCredits);

            if (contract.RewardItems.Count > 0)
            {
                foreach (var rewardItem in contract.RewardItems)
                {
                    var item = ObjectPlugin.Deserialize(rewardItem.Data);
                    ObjectPlugin.AcquireItem(player, item);
                }

                contract.RewardItems = new List<QuestContractItem>();
            }

            contract.CompletionsRemaining--;

            if (contract.CompletionsRemaining <= 0)
            {
                contract.Status = QuestContractStatus.Fulfilled;
                Quest.UnregisterRuntimeQuest(QuestContractFactory.BuildQuestId(contract.Id));
            }

            DB.Set(contract);

            // Refresh the completing player's contract board (if open) so the fulfilled contract
            // disappears from the Browse list immediately.
            Gui.PublishRefreshEvent(player, new QuestContractPublishedRefreshEvent());

            SendMessageToPC(player, $"You received {contract.RewardCredits} credits for completing the contract '{contract.Title}'.");
            Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{GetObjectUUID(player)}] completed contract '{contract.Id}' ('{contract.Title}') and received {contract.RewardCredits} credits.");
        }
    }
}
