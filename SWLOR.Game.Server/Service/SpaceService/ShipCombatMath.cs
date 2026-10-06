using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipCombatMath
    {
        public const double OutputPerRank = 0.002;
        public const double OutputPerPerception = 0.0025;

        public static double WeaponOutput(ShipModuleProfile module, ShipModuleVariant variant,
            int gunnery, int perception, double permanentBonus = 0, double penalty = 0,
            IEnumerable<double> temporaryBonuses = null)
        {
            CheckRank(gunnery);
            return ShipModuleTuning.Output(module.Output, variant.Output,
                permanentBonus + OutputPerRank * gunnery + OutputPerPerception * (Math.Clamp(perception, 10, 26) - 10),
                penalty, temporaryBonuses);
        }

        public static double RecoveryOutput(ShipModuleProfile module, ShipModuleVariant variant,
            int systems, double permanentBonus = 0, double penalty = 0,
            IEnumerable<double> temporaryBonuses = null)
        {
            CheckRank(systems);
            return ShipModuleTuning.Output(module.Output, variant.Output,
                permanentBonus + OutputPerRank * systems, penalty, temporaryBonuses);
        }

        public static double HitChance(int gunnery, int targetPiloting, int perception, int targetAgility,
            double accuracy, double evasion, double tracking, double signature,
            double weaponResolution, double targetSpeed)
        {
            CheckRank(gunnery);
            CheckRank(targetPiloting);
            if (!double.IsFinite(accuracy) || !double.IsFinite(evasion) ||
                !double.IsFinite(tracking) || tracking < 0 || !double.IsFinite(signature) || signature <= 0 ||
                !double.IsFinite(weaponResolution) || weaponResolution <= 0 ||
                !double.IsFinite(targetSpeed) || targetSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(tracking));
            var basis = 0.80 + 0.002 * gunnery - 0.0015 * targetPiloting +
                0.0025 * (Math.Clamp(perception, 10, 26) - 10) -
                0.0015 * (Math.Clamp(targetAgility, 10, 26) - 10) + accuracy - evasion;
            var trackingFactor = Math.Min(1, tracking / 100 * signature / weaponResolution / targetSpeed);
            return Math.Clamp(basis * trackingFactor, 0.10, 0.95);
        }

        private static void CheckRank(int rank)
        {
            if (rank < 0 || rank > 50) throw new ArgumentOutOfRangeException(nameof(rank));
        }
    }
}
