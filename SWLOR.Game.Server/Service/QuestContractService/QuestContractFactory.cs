using System.Text;
using SWLOR.Game.Server.Entity;
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
                Name = contract.Title,
                IsRepeatable = false,
                AllowRewardSelection = false,
                CollectedItemHandler = (player, item) => HandleCollectedItem(contract.Id, player, item)
            };

            quest.Prerequisites.Add(new QuestContractPrerequisite(contract.Id));
            quest.Rewards.Add(new QuestContractReward(contract.Id));

            var state = new QuestStateDetail
            {
                JournalText = BuildJournalText(contract)
            };

            foreach (var objective in contract.Objectives)
            {
                var producerRequirement = objective.MustBePlayerProduced
                    ? CollectItemProducerRequirementType.PlayerProduced
                    : CollectItemProducerRequirementType.None;

                state.AddObjective(new CollectItemObjective(objective.ItemResref, objective.Quantity, producerRequirement));
            }

            quest.States[1] = state;

            return quest;
        }

        private static string BuildJournalText(QuestContract contract)
        {
            var sb = new StringBuilder();
            sb.Append(contract.Description);
            sb.Append("\n\nObjectives:\n");

            foreach (var objective in contract.Objectives)
            {
                sb.Append($"{objective.Quantity}x {objective.ItemName}");

                if (objective.MustBePlayerProduced)
                    sb.Append(" (must be player-crafted)");

                sb.Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Reroutes a turned-in objective item to the contract author's pending delivery instead of letting
        /// it be destroyed. Partial turn-ins across multiple sessions accumulate into the same delivery.
        /// </summary>
        private static void HandleCollectedItem(string contractId, uint player, uint item)
        {
            var contract = DB.Get<QuestContract>(contractId);
            if (contract == null)
                return;

            var delivery = QuestContractBoard.GetOrCreatePendingDelivery(contract.AuthorPlayerId, contract.Id, contract.Title);
            delivery.Items.Add(new QuestContractItem
            {
                Data = ObjectPlugin.Serialize(item),
                Name = GetName(item),
                Resref = GetResRef(item),
                StackSize = GetItemStackSize(item),
                IconResref = Item.GetIconResref(item)
            });

            DB.Set(delivery);
        }
    }
}
