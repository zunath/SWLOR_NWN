using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementUnlock: IPerkRequirement
    {
        private readonly PerkType _perkType;

        public PerkRequirementUnlock(PerkType perkType)
        {
            _perkType = perkType;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.Unlock;

        public string CheckRequirements(uint player) => CheckRequirements(DB.Get<Player>(GetObjectUUID(player)), null);

        public string CheckRequirements(Player dbPlayer, Beast activeBeast)
        {
            return !dbPlayer.UnlockedPerks.ContainsKey(_perkType)
                ? "Perk has not been unlocked yet."
                : string.Empty;
        }

        public string RequirementText => "Perk must be unlocked.";
    }
}
