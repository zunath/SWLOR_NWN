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
        /// <summary>Whether the ship's next weapon cycle runs with no extra delay.</summary>
        public static bool HasNoDelayCycle(uint ship, ShipModuleType moduleType)
        {
            return ShipToggleRules.IsOffensive(moduleType) && GetLimitedEffect(ship) != null;
        }

        /// <summary>Spends one cycle of the ship's limited weapon cycle effect.</summary>
        public static void ConsumeCycle(uint ship, ShipModuleType moduleType)
        {
            if (!ShipToggleRules.IsOffensive(moduleType))
                return;

            GetLimitedEffect(ship)?.ConsumeWeaponCycle();
        }

        private static ILimitedWeaponCycleStatusEffect GetLimitedEffect(uint ship)
        {
            return StatusEffect.GetCreatureStatusEffects(ship)
                .GetAllEffects()
                .OfType<ILimitedWeaponCycleStatusEffect>()
                .FirstOrDefault(effect => !effect.IsFlaggedForRemoval && effect.RemainingCycles > 0);
        }
    }
}
