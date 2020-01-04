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
