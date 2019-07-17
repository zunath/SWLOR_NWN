using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Quest]")]
    public class Quest: IEntity
    {
        public Quest()
        {
            OnAcceptRule = "";
            OnAdvanceRule = "";
            OnCompleteRule = "";
            OnKillTargetRule = "";
            OnAcceptArgs = "";
            OnAdvanceArgs = "";
            OnCompleteArgs = "";
            OnKillTargetArgs = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public string JournalTag { get; set; }
        public int FameRegionID { get; set; }
        public int RequiredFameAmount { get; set; }
        public bool AllowRewardSelection { get; set; }
        public int RewardGold { get; set; }
        public int? RewardKeyItemID { get; set; }
        public int RewardFame { get; set; }
        public bool IsRepeatable { get; set; }
        public string MapNoteTag { get; set; }
        public int? StartKeyItemID { get; set; }
        public bool RemoveStartKeyItemAfterCompletion { get; set; }
        public string OnAcceptRule { get; set; }
        public string OnAdvanceRule { get; set; }
        public string OnCompleteRule { get; set; }
        public string OnKillTargetRule { get; set; }
        public string OnAcceptArgs { get; set; }
        public string OnAdvanceArgs { get; set; }
        public string OnCompleteArgs { get; set; }
        public string OnKillTargetArgs { get; set; }
        public int? RewardGuildID { get; set; }
        public int RewardGuildPoints { get; set; }

        public IEntity Clone()
        {
            return new Quest
            {
                ID = ID,
                Name = Name,
                JournalTag = JournalTag,
                FameRegionID = FameRegionID,
                RequiredFameAmount = RequiredFameAmount,
                AllowRewardSelection = AllowRewardSelection,
                RewardGold = RewardGold,
                RewardKeyItemID = RewardKeyItemID,
                RewardFame = RewardFame,
                IsRepeatable = IsRepeatable,
                MapNoteTag = MapNoteTag,
                StartKeyItemID = StartKeyItemID,
                RemoveStartKeyItemAfterCompletion = RemoveStartKeyItemAfterCompletion,
                OnAcceptRule = OnAcceptRule,
                OnAdvanceRule = OnAdvanceRule,
                OnCompleteRule = OnCompleteRule,
                OnKillTargetRule = OnKillTargetRule,
                OnAcceptArgs = OnAcceptArgs,
                OnAdvanceArgs = OnAdvanceArgs,
                OnCompleteArgs = OnCompleteArgs,
                OnKillTargetArgs = OnKillTargetArgs,
                RewardGuildID = RewardGuildID,
                RewardGuildPoints = RewardGuildPoints
            };
        }
    }
}
