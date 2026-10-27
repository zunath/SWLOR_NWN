using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Consecutive energy weapon hits against one target add DMG to a ship's shots (the Ace style's Sustained
    /// Fire). A landed energy shot, from the weapon's own cycle or an ability, adds a stack, and the stacks'
    /// DMG joins that same shot's damage roll. The stat values come from the pilot's perks
    /// (<see cref="StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit"/> and its companions), so ground weapons,
    /// which read the ground stat family, are never affected.
    /// </summary>
    public static class SpaceSustainedFire
    {
        private static readonly Dictionary<uint, SpaceRepeatedHitState> _states = new();

        /// <summary>
        /// Counts a landed shot and returns the DMG its stacks add, this hit included. Zero for other weapon
        /// families and for a pilot with no stacks to build.
        /// </summary>
        public static int RegisterHit(uint attacker, uint target, SpaceWeaponFamily family)
        {
            if (family != SpaceWeaponFamily.Energy ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(target) ||
                attacker == target)
            {
                return 0;
            }

            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit);
            var maximumBonus = Stat.GetStatAdjustment(attacker, StatType.SpaceEnergyRepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.SpaceEnergyRepeatedTargetDamageDurationSeconds);
            if (bonusPerHit <= 0 || maximumBonus <= 0 || durationSeconds <= 0)
            {
                Clear(attacker);
                return 0;
            }

            _states.TryGetValue(attacker, out var current);
            var state = SpaceRepeatedHits.Advance(
                current, target, bonusPerHit, maximumBonus, durationSeconds, DateTime.UtcNow);
            _states[attacker] = state;

            var maximumStacks = SpaceRepeatedHits.GetMaximumStacks(bonusPerHit, maximumBonus);
            var bonus = SpaceRepeatedHits.GetBonus(state.Stacks, bonusPerHit, maximumBonus);

            StatusEffect.RemoveStatusEffect(attacker, typeof(SustainedFireStatusEffect), false);
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new SustainedFireStatusEffect(state.Stacks, maximumStacks, bonus),
                durationSeconds);
            if (GetIsPC(attacker))
            {
                // A ship's weapons hit every cycle, so the stack count is a diagnostic, not a message on every shot.
                PlayerFeedback.ShowDiagnosticFloatingText(
                    ColorToken.Combat($"Sustained Fire {state.Stacks}/{maximumStacks} (+{bonus} DMG)"),
                    attacker,
                    false);
            }

            return bonus;
        }

        /// <summary>Forgets a ship's run, and its status, when it loses the perk or leaves space.</summary>
        public static void Clear(uint ship)
        {
            if (_states.Remove(ship))
                StatusEffect.RemoveStatusEffect(ship, typeof(SustainedFireStatusEffect), false);
        }
    }
}
