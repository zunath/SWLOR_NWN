using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
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
