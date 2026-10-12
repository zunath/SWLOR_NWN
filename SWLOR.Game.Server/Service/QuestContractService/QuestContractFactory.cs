using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    public static class QuestContractFactory
    {
        /// <summary>
        /// Builds the runtime quest Id for a given contract. Deterministic so it can be recomputed
        /// anywhere a contract Id is available without needing to build the full quest.
        /// </summary>
        /// <param name="contractId">The contract's entity Id.</param>
        /// <returns>The runtime quest Id associated with this contract.</returns>
        public static string BuildQuestId(string contractId)
        {
            return "qcontract_" + contractId;
        }

        /// <summary>
        /// Builds a single-state runtime <see cref="QuestDetail"/> for a published quest contract.
        /// The quest reuses the existing collect-item objective/journal/reward pipeline; only the
        /// prerequisite (contract eligibility) and reward (escrow payout) are contract-specific.
        /// </summary>
        /// <param name="contract">The contract to build a runtime quest for.</param>
        /// <returns>A quest detail ready to be registered with <see cref="Quest.RegisterRuntimeQuest"/>.</returns>
        public static QuestDetail BuildQuest(QuestContract contract)
        {
            var quest = new QuestDetail
            {
                QuestId = BuildQuestId(contract.Id),
                // Prefixed so player-posted contracts are clearly distinguishable from standard
                // quests in the journal and quest UIs.
                Name = $"Contract: {contract.Title}",
                IsRepeatable = false,
                AllowRewardSelection = false,
                // Player-authored content would let players farm quest achievements.
                CountsTowardAchievements = false,
                CollectedItemHandler = (player, item) => HandleCollectedItem(contract.Id, player, item)
            };

            quest.Prerequisites.Add(new QuestContractPrerequisite(contract.Id));
            quest.Rewards.Add(new QuestContractReward(contract.Id));
            quest.OnAbandonActions.Add(player => QuestContractBoard.ReleaseSubmissions(contract.Id, GetObjectUUID(player)));

            var state = new QuestStateDetail
            {
                JournalText = BuildJournalText(contract)
            };

            foreach (var objective in contract.Objectives)
            {
                state.AddObjective(new CollectItemObjective(objective.ItemResref, objective.Quantity, CollectItemProducerRequirementType.None));
            }

            quest.States[1] = state;

            return quest;
        }

        private static string BuildJournalText(QuestContract contract)
        {
            var sb = new StringBuilder();
            sb.Append("This is a player-posted contract. Deliver the requested items by using the Turn In option at any Contract Board.\n\n");
            sb.Append(contract.Description);
            sb.Append("\n\nObjectives:\n");

            foreach (var objective in contract.Objectives)
            {
                sb.Append($"{objective.Quantity}x {objective.ItemName}");
                sb.Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Holds objective items for this player's attempt. Only a completed attempt delivers them to
        /// the author; abandoned or unsuccessful attempts return their submissions to the player.
        /// </summary>
        private static void HandleCollectedItem(string contractId, uint player, uint item)
        {
            var contract = DB.Get<QuestContract>(contractId);
            var contractItem = new QuestContractItem
            {
                Data = ObjectPlugin.Serialize(item),
                Name = GetName(item),
                Resref = GetResRef(item),
                StackSize = GetItemStackSize(item),
                IconResref = Item.GetIconResref(item)
            };

            if (contract == null || contract.Status != QuestContractStatus.Published || contract.CompletionsRemaining <= 0)
            {
                var playerId = GetObjectUUID(player);
                var refund = QuestContractBoard.GetOrCreatePendingDelivery(playerId, contractId, contract?.Title ?? "Unavailable contract");
                refund.Items.Add(contractItem);
                DB.Set(refund);

                SendMessageToPC(player, "This contract is no longer active. Your items have been placed in a delivery - claim them at any contract board.");
                Log.Write(LogGroup.QuestContract, $"{GetName(player)} [{playerId}] turned in item '{contractItem.Name}' for inactive contract '{contractId}'. The item was routed back to them as a delivery.");
                return;
            }

            var delivery = QuestContractBoard.GetOrCreateSubmission(GetObjectUUID(player), contract);
            delivery.Items.Add(contractItem);
            DB.Set(delivery);
        }
    }
}
