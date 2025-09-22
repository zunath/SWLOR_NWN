using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Enums;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementCannotHavePerk: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly PerkType _cannotHavePerkType;

        public PerkRequirementCannotHavePerk(IDatabaseService db, PerkType cannotHavePerkType)
        {
            _db = db;
            _cannotHavePerkType = cannotHavePerkType;
        }

        public string CheckRequirements(uint player)
        {
            if (_cannotHavePerkType == PerkType.Invalid)
                return string.Empty;

            var perkDetail = _perkService.GetPerkDetails(_cannotHavePerkType);
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.Perks.ContainsKey(_cannotHavePerkType) &&
                dbPlayer.Perks[_cannotHavePerkType] > 0)
                return $"You cannot have perk: {perkDetail.Name}";

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var perkDetail = _perkService.GetPerkDetails(_cannotHavePerkType);
                return $"Cannot have perk: {perkDetail.Name}";
            }
        }
    }
}
