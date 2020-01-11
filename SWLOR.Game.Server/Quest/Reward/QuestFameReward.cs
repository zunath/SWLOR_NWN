using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestFameReward: IQuestReward
    {
        private readonly FameRegion _fameRegionID;
        private readonly int _amount;

        public QuestFameReward(FameRegion fameRegionID, int amount, bool isSelectable)
        {
            _fameRegionID = fameRegionID;
            _amount = amount;
            IsSelectable = isSelectable;

            MenuName = _amount + " " + _fameRegionID.GetDescriptionAttribute() + " Fame";

        }

        public bool IsSelectable { get; }
        public string MenuName { get; }

        public void GiveReward(NWPlayer player)
        {
            if (_amount <= 0 || _fameRegionID == FameRegion.Invalid) return;

            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var fame = dbPlayer.RegionalFame.ContainsKey(_fameRegionID) ?
                dbPlayer.RegionalFame[_fameRegionID] :
                0;
            
            fame += _amount;
            dbPlayer.RegionalFame[_fameRegionID] = fame;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Set);
        }
    }
}
