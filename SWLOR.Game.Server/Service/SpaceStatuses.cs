using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ground statuses on ships (plan section 9, status effects). A ship in space is its pilot's creature, so the
    /// shared status system already tracks, ticks and expires statuses on it, and their stat effects (Weakened,
    /// Exposed, Disarming Shot) reach the space formulas through the Stat service. This partial adds what the
    /// ground assumes and ships do not have: damage over time that hits the ship's pools, and the crowd-control
    /// checks the toggle loop and ability system make.
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// True for a player flying a ship or an NPC ship. Ground creatures and players on foot are not ships.
        /// </summary>
        public static bool IsShipInSpace(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            return GetIsPC(creature)
                ? _playersInSpace.Contains(creature)
                : _shipNPCs.ContainsKey(creature);
        }

        /// <summary>
        /// Applies a ground status (Burn, Blind, Dazed, Exposed, Disarming Shot, Weakened, Fragmentation and the
        /// rest) to a ship through the shared status system. Resistance families, the hard crowd-control rules
        /// (cooldown, immunity, caps) and PvP limits are the status system's. Returns false when the target is not
        /// a ship or the status did not apply.
        /// </summary>
        public static bool ApplyStatusToShip(
            uint source,
            uint ship,
            Type statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
        {
            return IsShipInSpace(ship) &&
                   StatusEffect.ApplyStatusEffect(source, ship, statusEffect, durationSeconds, resistanceOverride);
        }

        /// <summary>
        /// Applies a configured status instance to a ship, for statuses with numbers
        /// (<c>new BurnStatusEffect(level)</c>, <c>new FragmentationStatusEffect(damage, seconds)</c>,
        /// <c>new DisarmingShotStatusEffect(percent)</c>, <c>new ExposedStatusEffect(percent)</c>).
        /// </summary>
        public static bool ApplyStatusToShip(
            uint source,
            uint ship,
            IStatusEffect statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
        {
            return IsShipInSpace(ship) &&
                   StatusEffect.ApplyStatusEffect(source, ship, statusEffect, durationSeconds, resistanceOverride);
        }

        /// <summary>
        /// Routes one tick of a damage-over-time status to the ship's pools when the creature is a ship in space.
        /// The tick goes through <see cref="ApplyShipDamage(uint,uint,int)"/>, so the shield absorbs it first, and
        /// raises the source's threat the way ground damage does. Returns false for any other creature, which
        /// takes the damage as hit points.
        /// </summary>
        public static bool TryApplyStatusDamage(uint source, uint creature, int amount)
        {
            if (!IsShipInSpace(creature))
                return false;

            if (amount <= 0)
                return true;

            var damager = GetIsObjectValid(source) ? source : creature;
            AssignCommand(damager, () =>
            {
                if (!GetIsObjectValid(creature))
                    return;

                ApplyShipDamage(damager, creature, amount);
                if (damager != creature)
                {
                    Enmity.ModifyEnmity(damager, creature, Enmity.CalculateDamageEnmity(
                        amount,
                        Stat.GetStatAdjustment(damager, StatType.DamageEnmityPercentAdjustment)));
                }
            });
            return true;
        }

        /// <summary>
        /// Whether a creature has a crowd-control effect that stops it acting: Dazed and the other incapacitating
        /// statuses or native effects the ground ability system treats as blocking a cast.
        /// </summary>
        public static bool IsShipIncapacitated(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            if (StatusEffect.HasStatusEffectCategory(creature, StatusEffectCategory.Incapacitating))
                return true;

            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (QuickCastPolicy.BlocksCasting(GetEffectType(effect)))
                    return true;
            }

            return false;
        }

        /// <summary>Whether a creature has the Blind status's native blindness.</summary>
        public static bool IsBlinded(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.Blindness)
                    return true;
            }

            return false;
        }
    }
}
