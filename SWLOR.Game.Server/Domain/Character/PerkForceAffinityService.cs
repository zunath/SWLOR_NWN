using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// A creature's Force affinity and what it does to a Force ability: the side it favors, the magnitude multiplier, the hit chance
    /// adjustment and the restorative cap. They moved here from <see cref="PerkService"/> unchanged; the perk definitions stay there.
    /// </summary>
    public class PerkForceAffinityService : IPerkForceAffinityService
    {
        private const int ForceAffinityMinimum = -10;
        private const int ForceAffinityMaximum = 10;
        // Heals, shields and regeneration gain at most +25% from affinity; damage and accuracy keep the full range.
        private const float ForceAffinityRestorativeMaximumMultiplier = 1.25f;

        private readonly IPerkService _perk;
        private readonly IStatService _stat;

        public PerkForceAffinityService(IPerkService perk, IStatService stat)
        {
            _perk = perk;
            _stat = stat;
        }

        public int GetForceAffinity(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return 0;

            return Math.Clamp(
                GetCreatureStatAdjustment(creature, StatType.ForceAffinity),
                ForceAffinityMinimum,
                ForceAffinityMaximum);
        }

        public bool TryGetForceSideAffinity(uint creature, PerkType perkType, out int sideAffinity)
        {
            sideAffinity = 0;

            if (perkType == PerkType.Invalid ||
                !_perk.TryGetPerkDetails(perkType, out var detail))
            {
                return false;
            }

            if (detail.ForceAffinityType == null)
                return false;

            var forceAffinity = GetForceAffinity(creature);
            switch (detail.ForceAffinityType.Value)
            {
                case ForceAffinityType.Light:
                    sideAffinity = forceAffinity;
                    return true;
                case ForceAffinityType.Dark:
                    sideAffinity = -forceAffinity;
                    return true;
                default:
                    return false;
            }
        }

        public float GetForceAffinityMagnitudeMultiplier(uint creature, PerkType perkType)
        {
            return TryGetForceSideAffinity(creature, perkType, out var sideAffinity)
                ? Math.Clamp(1f + 0.05f * sideAffinity, 0.5f, 1.5f)
                : 1f;
        }

        public int GetForceAffinityHitChanceAdjustment(uint creature, PerkType perkType)
        {
            return TryGetForceSideAffinity(creature, perkType, out var sideAffinity)
                ? (int)Math.Floor(sideAffinity / 2f)
                : 0;
        }

        public float GetForceAffinityRestorativeMagnitudeMultiplier(uint creature, PerkType perkType)
        {
            return Math.Min(GetForceAffinityMagnitudeMultiplier(creature, perkType), ForceAffinityRestorativeMaximumMultiplier);
        }

        public int ApplyForceAffinityMagnitude(uint creature, PerkType perkType, int amount)
        {
            return amount <= 0 ? amount : ApplyMagnitudeMultiplier(amount, GetForceAffinityMagnitudeMultiplier(creature, perkType));
        }

        /// <summary>Applies affinity to a heal, shield or regeneration amount, which gains at most +25%.</summary>
        public int ApplyForceAffinityRestorativeMagnitude(uint creature, PerkType perkType, int amount)
        {
            return amount <= 0 ? amount : ApplyMagnitudeMultiplier(amount, GetForceAffinityRestorativeMagnitudeMultiplier(creature, perkType));
        }

        private int ApplyMagnitudeMultiplier(int amount, float multiplier)
        {
            if (Math.Abs(multiplier - 1f) < 0.001f)
                return amount;

            return Math.Max(1, (int)Math.Round(amount * multiplier, MidpointRounding.AwayFromZero));
        }

        public float ApplyForceAffinityMagnitude(uint creature, PerkType perkType, float amount)
        {
            if (amount <= 0f)
                return amount;

            return amount * GetForceAffinityMagnitudeMultiplier(creature, perkType);
        }

        protected virtual int GetCreatureStatAdjustment(uint creature, StatType stat)
        {
            return _stat.GetStatAdjustment(creature, stat);
        }
    }
}