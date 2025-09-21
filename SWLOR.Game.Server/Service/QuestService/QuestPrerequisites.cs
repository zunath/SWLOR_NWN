using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestPrerequisite
    {
        bool MeetsPrerequisite(uint player);
    }

    public class RequiredQuestPrerequisite : IQuestPrerequisite
    {
        private readonly IDatabaseService _db;
        private readonly string _questID;

        public RequiredQuestPrerequisite(IDatabaseService db, string questID)
        {
            _db = db;
            _questID = questID;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var timesCompleted = dbPlayer.Quests.ContainsKey(_questID) ? dbPlayer.Quests[_questID].TimesCompleted : 0;
            return timesCompleted > 0;
        }
    }

    public class RequiredKeyItemPrerequisite : IQuestPrerequisite
    {
        private readonly KeyItemType _keyItemType;
        private readonly IKeyItemService _keyItemService;

        public RequiredKeyItemPrerequisite(KeyItemType keyItemType, IKeyItemService keyItemService)
        {
            _keyItemType = keyItemType;
            _keyItemService = keyItemService;
        }

        public bool MeetsPrerequisite(uint player)
        {
            return _keyItemService.HasKeyItem(player, _keyItemType);
        }
    }

    public class RequiredFactionStandingPrerequisite : IQuestPrerequisite
    {
        private readonly IDatabaseService _db;
        private readonly FactionType _factionType;
        private readonly int _requiredAmount;

        public RequiredFactionStandingPrerequisite(IDatabaseService db, FactionType faction, int requiredAmount)
        {
            _db = db;
            _factionType = faction;
            _requiredAmount = requiredAmount;
        }

        public bool MeetsPrerequisite(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var standing = dbPlayer.Factions.ContainsKey(_factionType) ? dbPlayer.Factions[_factionType].Standing : 0;

            return standing >= _requiredAmount;
        }
    }
}
