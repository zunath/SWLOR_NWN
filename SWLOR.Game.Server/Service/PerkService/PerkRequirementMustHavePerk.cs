using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementMustHavePerk: IPerkRequirement
    {
        private readonly PerkType _mustHavePerkType;
        private readonly int _mustHavePerkLevel;

        public PerkRequirementMustHavePerk(PerkType mustHavePerkType, int mustHavePerkLevel = 0)
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
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Perks.ContainsKey(_mustHavePerkType))
                return $"You must have perk: {perkDetail.Name}";

            if (_mustHavePerkLevel > 0 && Perk.GetEffectivePerkLevel(player, _mustHavePerkType) < _mustHavePerkLevel)
                return $"You must have perk {perkDetail.Name} leveled to {_mustHavePerkLevel}.";

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var perkDetail = Perk.GetPerkDetails(_mustHavePerkType);
                if(_mustHavePerkLevel > 0)
                    return $"Must have perk {perkDetail.Name} at level {_mustHavePerkLevel}.";
                return $"Must have perk {perkDetail.Name}.";
            }
        }
    }
}
