using System.Linq;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Limited "next N weapon cycles" effects on a ship: while one has cycles left, a weapon module's next cycle
    /// starts as soon as the auto-fire loop can run it (<see cref="ShipToggleRules.LoopSeconds"/>) instead of
    /// waiting out its normal cycle time. The toggle loop asks before it schedules a weapon's cycle and reports
    /// the cycle once it has fired, so the cycle that fires under a critical rate bonus is the one that is counted.
    /// </summary>
    public static class SpaceWeaponCycles
    {
        /// <summary>
        /// Whether the ship's next weapon cycle fired by <paramref name="shooter"/> (the pilot's ship, the crew member at
        /// the slot's station, or OBJECT_INVALID for a droid) runs with no extra delay. The effect reaches only its
        /// taker's weapons (<see cref="SpaceStanceRules.ReachesShooter"/>).
        /// </summary>
        public static bool HasNoDelayCycle(uint ship, ShipModuleType moduleType, uint shooter)
        {
            return ShipToggleRules.IsOffensive(moduleType) && GetLimitedEffect(ship, shooter) != null;
        }

        /// <summary>Spends one cycle of the limited weapon cycle effect that reaches the shooter's weapons.</summary>
        public static void ConsumeCycle(uint ship, ShipModuleType moduleType, uint shooter)
        {
            if (!ShipToggleRules.IsOffensive(moduleType))
                return;

            GetLimitedEffect(ship, shooter)?.ConsumeWeaponCycle();
        }

        private static ILimitedWeaponCycleStatusEffect GetLimitedEffect(uint ship, uint shooter)
        {
            return StatusEffect.GetCreatureStatusEffects(ship)
                .GetAllEffects()
                .OfType<ILimitedWeaponCycleStatusEffect>()
                .FirstOrDefault(effect => !effect.IsFlaggedForRemoval && effect.RemainingCycles > 0 &&
                                          SpaceStanceRules.ReachesShooter(ship, shooter, effect.Source));
        }
    }
}
