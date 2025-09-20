using SWLOR.Game.Server.Entity;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementCannotHavePerk: IPerkRequirement
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly PerkType _cannotHavePerkType;

        public PerkRequirementCannotHavePerk(PerkType cannotHavePerkType)
        {
            _cannotHavePerkType = cannotHavePerkType;
        }

        public string CheckRequirements(uint player)
        {
            if (_cannotHavePerkType == PerkType.Invalid)
                return string.Empty;

            var perkDetail = Perk.GetPerkDetails(_cannotHavePerkType);
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
                var perkDetail = Perk.GetPerkDetails(_cannotHavePerkType);
                return $"Cannot have perk: {perkDetail.Name}";
            }
        }
    }
}
