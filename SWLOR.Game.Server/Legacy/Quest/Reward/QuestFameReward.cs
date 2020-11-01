using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Reward
{
    public class QuestFameReward: IQuestReward
    {
        private readonly int _fameRegionID;
        private readonly int _amount;

        public QuestFameReward(int fameRegionID, int amount, bool isSelectable)
        {
            _fameRegionID = fameRegionID;
            _amount = amount;
            IsSelectable = isSelectable;

            var fameRegion = DataService.FameRegion.GetByID(fameRegionID);
            MenuName = _amount + " " + fameRegion.Name + " Fame";

        }

        public bool IsSelectable { get; }
        public string MenuName { get; }

        public void GiveReward(NWPlayer player)
        {
            if (_amount <= 0 || _fameRegionID <= 0) return;

            var fame = DataService.PCRegionalFame.GetByPlayerIDAndFameRegionIDOrDefault(player.GlobalID, _fameRegionID);
            var action = DatabaseActionType.Update;

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
