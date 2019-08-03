﻿using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Prerequisite
{
    public class FamePrerequisite: IQuestPrerequisite
    {
        private readonly int _fameRegionID;
        private readonly int _amount;

        public FamePrerequisite(int fameRegionID, int amount)
        {
            _fameRegionID = fameRegionID;
            _amount = amount;
        }

        public bool MeetsPrerequisite(NWPlayer player)
        {
            var fame = DataService.PCRegionalFame.GetByPlayerIDAndFameRegionIDOrDefault(player.GlobalID, _fameRegionID);
            int fameAmount = fame?.Amount ?? 0;

            return fameAmount >= _amount;
        }
    }
}
