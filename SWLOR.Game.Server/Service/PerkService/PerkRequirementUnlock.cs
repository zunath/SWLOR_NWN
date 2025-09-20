using SWLOR.Game.Server.Entity;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementUnlock: IPerkRequirement
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly PerkType _perkType;

        public PerkRequirementUnlock(PerkType perkType)
        {
            _perkType = perkType;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            return !dbPlayer.UnlockedPerks.ContainsKey(_perkType) 
                ? "Perk has not been unlocked yet." 
                : string.Empty;
        }

        public string RequirementText => "Perk must be unlocked.";
    }
}
