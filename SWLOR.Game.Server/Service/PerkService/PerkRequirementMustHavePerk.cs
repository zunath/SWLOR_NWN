using SWLOR.Game.Server.Entity;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementMustHavePerk: IPerkRequirement
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly PerkType _mustHavePerkType;
        private readonly int _mustHavePerkLevel;

        public PerkRequirementMustHavePerk(PerkType mustHavePerkType, int mustHavePerkLevel = 1)
        {
            _mustHavePerkType = mustHavePerkType;
            _mustHavePerkLevel = mustHavePerkLevel;
        }

        public string CheckRequirements(uint player)
        {
            if (_mustHavePerkType == PerkType.Invalid)
                return string.Empty;

            var perkDetail = Perk.GetPerkDetails(_mustHavePerkType);
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.Perks.ContainsKey(_mustHavePerkType) || Perk.GetPerkLevel(player, _mustHavePerkType) < _mustHavePerkLevel)
               return $"You must have perk {perkDetail.Name} at level {_mustHavePerkLevel}.";

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var perkDetail = Perk.GetPerkDetails(_mustHavePerkType);
                return $"Must have perk {perkDetail.Name} at level {_mustHavePerkLevel}.";
            }
        }
    }
}
