using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// A creature's Force affinity and what it does to a Force ability: the side it favors, the magnitude multiplier, the hit chance
    /// adjustment and the restorative cap. The reads run per ability hit chance and magnitude and allocate nothing.
    /// </summary>
    public interface IPerkForceAffinityService
    {
        int GetForceAffinity(uint creature);

        bool TryGetForceSideAffinity(uint creature, PerkType perkType, out int sideAffinity);

        float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType);

        int GetForceAffinityHitChanceAdjustment(uint creature, PerkType perkType);

        float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType);

        int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount);

        /// <summary>Applies affinity to a heal, shield or regeneration amount, which gains at most +25%.</summary>
        int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount);

        float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount);
    }
}
