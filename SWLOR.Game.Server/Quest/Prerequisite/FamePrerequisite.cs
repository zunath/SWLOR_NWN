using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Prerequisite
{
    public class FamePrerequisite: IQuestPrerequisite
    {
        private readonly FameRegion _fameRegionID;
        private readonly int _amount;

        public FamePrerequisite(FameRegion fameRegionID, int amount)
        {
            _fameRegionID = fameRegionID;
            _amount = amount;
        }

        public bool MeetsPrerequisite(NWPlayer player)
        {
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var fame = dbPlayer.RegionalFame.ContainsKey(_fameRegionID) ?
                dbPlayer.RegionalFame[_fameRegionID] :
                0;
            
            return fame >= _amount;
        }
    }
}
