using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Objective
{
    public class CollectItemObjective: IQuestObjective
    {
        private readonly string _resref;
        private readonly int _quantity;
        private readonly bool _mustBeCraftedByPlayer;

        public CollectItemObjective(string resref, int quantity, bool mustBeCraftedByPlayer)
        {
            _resref = resref;
            _quantity = quantity;
            _mustBeCraftedByPlayer = mustBeCraftedByPlayer;
        }

        public void Initialize(NWPlayer player, int questID)
        {
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
            var itemProgress = new PCQuestItemProgress
            {
                Resref = _resref,
                PlayerID = status.PlayerID,
                PCQuestStatusID = status.ID,
                Remaining = _quantity,
                MustBeCraftedByPlayer = _mustBeCraftedByPlayer
            };
            DataService.SubmitDataChange(itemProgress, DatabaseActionType.Insert);
        }

        public bool IsComplete(NWPlayer player, int questID)
        {
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
            var itemProgress = DataService.PCQuestItemProgress.GetAllByPCQuestStatusID(status.ID);
            foreach (var progress in itemProgress)
            {
                if (progress.Remaining > 0)
                    return false;
            }

            return true;
        }
    }
}
