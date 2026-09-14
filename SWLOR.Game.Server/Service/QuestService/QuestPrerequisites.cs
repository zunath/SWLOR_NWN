using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(uint player);
    }

    public class RequiredQuestPrerequisite : IQuestPrerequisite
    {
        public string QuestId { get; }

        public RequiredQuestPrerequisite(string questId)
        {
            QuestId = questId;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var timesCompleted = dbPlayer.Quests.ContainsKey(QuestId) ? dbPlayer.Quests[QuestId].TimesCompleted : 0;
            return timesCompleted > 0;
        }
    }

    public class RequiredKeyItemPrerequisite : IQuestPrerequisite
    {
        private readonly KeyItemType _keyItemType;

        public RequiredKeyItemPrerequisite(KeyItemType keyItemType)
        {
            _keyItemType = keyItemType;
        }

        public bool MeetsPrerequisite(uint player)
        {
            return KeyItem.HasKeyItem(player, _keyItemType);
        }
    }

    public class RequiredSkillRankPrerequisite : IQuestPrerequisite
    {
        public SkillType SkillType { get; }
        public int RequiredRank { get; }

        public RequiredSkillRankPrerequisite(SkillType skillType, int requiredRank)
        {
            SkillType = skillType;
            RequiredRank = requiredRank;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Skills.ContainsKey(SkillType))
                return false;

            return dbPlayer.Skills[SkillType].Rank >= RequiredRank;
        }
    }

    public class RequiredFactionStandingPrerequisite : IQuestPrerequisite
    {
        private readonly FactionType _factionType;
        private readonly int _requiredAmount;

        public RequiredFactionStandingPrerequisite(FactionType faction, int requiredAmount)
        {
            _factionType = faction;
            _requiredAmount = requiredAmount;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var standing = dbPlayer.Factions.ContainsKey(_factionType) ? dbPlayer.Factions[_factionType].Standing : 0;

            return standing >= _requiredAmount;
        }
    }
}
