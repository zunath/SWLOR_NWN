using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementCannotHavePerk: IPerkRequirement
    {
        private readonly PerkType _cannotHavePerkType;

        public PerkRequirementCannotHavePerk(PerkType cannotHavePerkType)
        {
            _cannotHavePerkType = cannotHavePerkType;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.CannotHavePerk;

        public string CheckRequirements(uint player) =>
            _cannotHavePerkType == PerkType.Invalid ? string.Empty : CheckRequirements(DB.Get<Player>(GetObjectUUID(player)), null);

        public string CheckRequirements(Player dbPlayer, Beast activeBeast)
        {
            if (_cannotHavePerkType == PerkType.Invalid)
                return string.Empty;

            var perkDetail = Perk.GetPerkDetails(_cannotHavePerkType);

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
