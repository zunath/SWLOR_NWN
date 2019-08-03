using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestFameReward: IQuestReward
    {
        private readonly int _fameRegionID;
        private readonly int _amount;

        public QuestFameReward(int fameRegionID, int amount)
        {
            _fameRegionID = fameRegionID;
            _amount = amount;
        }

        public void GiveReward(NWPlayer player)
        {
            if (_amount <= 0 || _fameRegionID <= 0) return;

            PCRegionalFame fame = DataService.PCRegionalFame.GetByPlayerIDAndFameRegionIDOrDefault(player.GlobalID, _fameRegionID);
            DatabaseActionType action = DatabaseActionType.Update;

            if (fame == null)
            {
                fame = new PCRegionalFame
                {
                    PlayerID = player.GlobalID,
                    FameRegionID = _fameRegionID,
                    Amount = 0
                };

                action = DatabaseActionType.Insert;
            }

            fame.Amount += _amount;
            DataService.SubmitDataChange(fame, action);
        }
    }
}
