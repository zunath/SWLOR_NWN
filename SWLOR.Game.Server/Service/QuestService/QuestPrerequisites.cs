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

        /// <summary>
        /// Whether the astromech skill that mirrors this one also qualifies. Off for a quest whose work needs a perk
        /// line the mirror does not copy (the Staff Sentinel and Rifle Marksman capstones).
        /// </summary>
        public bool AcceptsMirror { get; }

        public RequiredSkillRankPrerequisite(SkillType skillType, int requiredRank, bool acceptsMirror = true)
        {
            SkillType = skillType;
            RequiredRank = requiredRank;
            AcceptsMirror = acceptsMirror;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            return MeetsPrerequisite(DB.Get<Player>(playerId));
        }

        public bool MeetsPrerequisite(Player dbPlayer)
        {
            // An astromech proves a quest's skill with the skill that mirrors it (Utility Systems for First Aid).
            var skillType = AcceptsMirror ? SkillBodyFrames.GetMirror(SkillType, dbPlayer.BodyFrame) : SkillType;

            // A skill the body frame cannot train never qualifies, whatever rank an old record holds.
            if (!SkillBodyFrames.IsOpenTo(skillType, dbPlayer.BodyFrame))
                return false;
            if (!dbPlayer.Skills.ContainsKey(skillType))
                return false;

            return dbPlayer.Skills[skillType].Rank >= RequiredRank;
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
