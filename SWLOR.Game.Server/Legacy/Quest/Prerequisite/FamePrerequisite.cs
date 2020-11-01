using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Prerequisite
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
            var fameAmount = fame?.Amount ?? 0;

            return fameAmount >= _amount;
        }
    }
}
