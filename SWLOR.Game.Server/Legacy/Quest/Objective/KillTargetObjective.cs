using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Objective
{
    public class KillTargetObjective: IQuestObjective
    {
        private readonly NPCGroupType _group;
        private readonly int _amount;

        public KillTargetObjective(NPCGroupType group, int amount)
        {
            _group = group;
            _amount = amount;
        }

        public void Initialize(NWPlayer player, int questID)
        {
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
            var pcKT = new PCQuestKillTargetProgress
            {
                RemainingToKill = _amount,
                NPCGroupID = (int)_group,
                PCQuestStatusID = status.ID,
                PlayerID = player.GlobalID
            };
            DataService.SubmitDataChange(pcKT, DatabaseActionType.Insert);

        }

        public bool IsComplete(NWPlayer player, int questID)
        {
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
            var itemProgress = DataService.PCQuestKillTargetProgress.GetAllByPlayerIDAndNPCGroupID(player.GlobalID, (int)_group);

            foreach (var progress in itemProgress)
            {
                if (progress.RemainingToKill > 0)
                    return false;
            }

            return true;
        }
    }
}
