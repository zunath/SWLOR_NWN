using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Quest.Objective
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
