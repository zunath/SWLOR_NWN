using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class KillTargetObjective: IQuestObjective
    {
        public NPCGroup Group { get; }
        private readonly int _amount;

        public KillTargetObjective(NPCGroup group, int amount)
        {
            Group = group;
            _amount = amount;
        }

        public void Initialize(NWPlayer player, int questID)
        {
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
            status.KillTargets[Group] = _amount;
            
            DataService.Set(status);

        }

        public bool IsComplete(NWPlayer player, int questID)
        {
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
            var killsRemaining = status.KillTargets[Group];

            if (killsRemaining > 0)
                return false;

            return true;
        }
    }
}
