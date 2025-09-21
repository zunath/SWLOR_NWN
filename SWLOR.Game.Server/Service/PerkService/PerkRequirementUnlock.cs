using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementUnlock: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly PerkType _perkType;

        public PerkRequirementUnlock(IDatabaseService db, PerkType perkType)
        {
            _db = db;
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
