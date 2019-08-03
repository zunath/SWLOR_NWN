using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class KillTargetObjective: IQuestObjective
    {
        private string _resref;
        private int _amount;

        public KillTargetObjective(string resref, int amount)
        {
            _resref = resref;
            _amount = amount;
        }

        public bool IsComplete(NWPlayer player)
        {
            // todo: check persistence
            return false;
        }
    }
}
