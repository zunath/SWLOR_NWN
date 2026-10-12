using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Service.QuestContractService
{
    public class QuestContractPrerequisite : IQuestPrerequisite
    {
        private readonly string _contractId;

        public QuestContractPrerequisite(string contractId)
        {
            _contractId = contractId;
        }

        /// <summary>
        /// A contract may only be accepted while it is still published with completions remaining,
        /// and only by a player who isn't the author (including alts on the author's account).
        /// </summary>
        public bool MeetsPrerequisite(uint player)
        {
            var contract = DB.Get<QuestContract>(_contractId);
            if (contract == null)
                return false;

            if (contract.Status != QuestContractStatus.Published)
                return false;

            if (contract.CompletionsRemaining <= 0)
                return false;

            var playerId = GetObjectUUID(player);
            if (playerId == contract.AuthorPlayerId)
                return false;

            var cdKey = GetPCPublicCDKey(player);
            if (!string.IsNullOrWhiteSpace(contract.AuthorCDKey) && cdKey == contract.AuthorCDKey)
                return false;

            return true;
        }
    }
}
