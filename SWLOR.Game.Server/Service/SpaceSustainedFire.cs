using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Consecutive energy weapon hits against one target add DMG to a ship's shots (the Ace style's Sustained
    /// Fire). Each weapon owns its stacks, as one ground weapon does: a landed energy shot from that weapon, from its
    /// own cycle or an ability, adds a stack, and the stacks' DMG, shared between the energy weapons the ship fires
    /// (SpaceRepeatedHits.GetShare), joins that same shot's damage roll. The stat values come from the pilot's perks
    /// (<see cref="StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit"/> and its companions), so ground weapons,
    /// which read the ground stat family, are never affected.
    /// </summary>
    public static class SpaceSustainedFire
    {
        private static readonly Dictionary<(uint Ship, int Slot), SpaceRepeatedHitState> _states = new();

        /// <summary>
        /// Registers a landed hit from the weapon in <paramref name="weaponSlot"/> and returns the DMG its Sustained Fire
        /// stacks add to this shot: each weapon owns its stacks, and their DMG is shared between the
        /// <paramref name="energyWeapons"/> the ship fires (<see cref="SpaceRepeatedHits.GetShare"/>).
        /// </summary>
        public static int RegisterHit(
            uint attacker,
            uint target,
            SpaceWeaponFamily family,
            uint statSource = OBJECT_INVALID,
            int weaponSlot = 0,
            int energyWeapons = 1)
        {
            // A crew member's station shot reads their own Sustained Fire perk; the run stays on the weapon.
            if (!GetIsObjectValid(statSource))
                statSource = attacker;

            if (family != SpaceWeaponFamily.Energy ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(target) ||
                attacker == target)
            {
                return 0;
            }

            var bonusPerHit = Stat.GetStatAdjustment(statSource, StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit);
            var maximumBonus = Stat.GetStatAdjustment(statSource, StatType.SpaceEnergyRepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(statSource, StatType.SpaceEnergyRepeatedTargetDamageDurationSeconds);
            if (bonusPerHit <= 0 || maximumBonus <= 0 || durationSeconds <= 0)
            {
                Clear(attacker);
                return 0;
            }

            var key = (attacker, weaponSlot);
            _states.TryGetValue(key, out var current);
            var state = SpaceRepeatedHits.Advance(
                current, target, bonusPerHit, maximumBonus, durationSeconds, DateTime.UtcNow);
            _states[key] = state;

            var maximumStacks = SpaceRepeatedHits.GetMaximumStacks(bonusPerHit, maximumBonus);
            var bonus = SpaceRepeatedHits.GetShare(
                SpaceRepeatedHits.GetBonus(state.Stacks, bonusPerHit, maximumBonus), energyWeapons);

            StatusEffect.RemoveStatusEffect(attacker, typeof(SustainedFireStatusEffect), false);
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new SustainedFireStatusEffect(state.Stacks, maximumStacks, bonus),
                durationSeconds);
            if (GetIsPC(attacker))
            {
                PlayerFeedback.ShowDiagnosticFloatingText(
                    ColorToken.Combat($"Sustained Fire {state.Stacks}/{maximumStacks} (+{bonus} DMG)"),
                    attacker,
                    false);
            }

            return bonus;
        }

        /// <summary>
        /// The energy weapons the ship fires: fitted energy weapon modules in Helm slots or in crew-station slots
        /// someone is working.
        /// </summary>
        public static int CountFiringEnergyWeapons(uint ship, ShipStatus status)
        {
            if (status == null)
                return 1;

            var count = 0;
            foreach (var fitted in Space.GetFittedRevampModules(status))
            {
                var step = fitted.Step;
                if (step == null || step.WeaponFamily != SpaceWeaponFamily.Energy || !ShipModuleActions.IsShipWeapon(step.Family))
                    continue;

                var slot = ShipStatCalculator.GetHotbarSlot(fitted.Row, fitted.Slot);
                if (ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slot) == ShipCrewSeat.Pilot ||
                    SpaceCrew.IsSeatCrewed(ship, status, slot))
                {
                    count++;
                }
            }

            return Math.Max(1, count);
        }

        /// <summary>Ends every weapon's run on the ship and removes the status.</summary>
        public static void Clear(uint ship)
        {
            var keys = _states.Keys.Where(key => key.Ship == ship).ToList();
            foreach (var key in keys)
                _states.Remove(key);
            if (keys.Count > 0)
                StatusEffect.RemoveStatusEffect(ship, typeof(SustainedFireStatusEffect), false);
        }
    }
}
