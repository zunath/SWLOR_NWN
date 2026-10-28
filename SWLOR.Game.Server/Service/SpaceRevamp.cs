using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Revamp ship stats and the support auras of the capital arrays (plan sections 7 and 8).
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// The revamp modules fitted to a ship, with their rows and slots. Modules that are not revamp modules
        /// are left out.
        /// </summary>
        public static List<ShipFittedModule> GetFittedRevampModules(ShipStatus status)
        {
            var fitted = new List<ShipFittedModule>();
            AddFitted(fitted, ShipModulePowerType.High, status.HighPowerModules);
            AddFitted(fitted, ShipModulePowerType.Mid, status.MidPowerModules);
            AddFitted(fitted, ShipModulePowerType.Low, status.LowPowerModules);
            return fitted;
        }

        private static void AddFitted(
            List<ShipFittedModule> fitted,
            ShipModulePowerType row,
            Dictionary<int, ShipStatus.ShipStatusModule> modules)
        {
            foreach (var (slot, module) in modules)
            {
                if (!_shipModules.TryGetValue(module.ItemTag, out var detail) || detail.Step == null)
                    continue;

                fitted.Add(new ShipFittedModule { Row = row, Slot = slot, Step = detail.Step, Traits = module.Traits });
            }
        }

        /// <summary>
        /// Works out a revamp ship's stats from its hull, fitted modules and the pilot's WIL and stores them on
        /// the status. Current hull, shield and capacitor are limited to the new maximums; with
        /// <paramref name="fill"/> they start full. Station slots count only through
        /// <paramref name="stationEffectiveness"/> (see <see cref="ShipStatCalculator.Calculate"/>); without it they
        /// are empty. Does nothing for a legacy ship.
        /// </summary>
        /// <returns>true when the ship is a revamp ship and was recalculated.</returns>
        public static bool RecalculateShipStats(
            ShipStatus status,
            int willpower,
            bool fill = false,
            Func<int, double> stationEffectiveness = null)
        {
            if (status == null || !_shipTypes.TryGetValue(status.ItemTag ?? string.Empty, out var detail) || detail.Hull == null)
                return false;

            var result = ShipStatCalculator.Calculate(detail.Hull, GetFittedRevampModules(status), willpower, stationEffectiveness, status.HullTraits);
            ShipStatCalculator.Apply(result, status, fill);
            return true;
        }

        /// <summary>
        /// Raises an ally's max shield by a percent until the aura lapses and fills the added shield at once
        /// the first time. Used by the Bulwark Shield Generator.
        /// </summary>
        public static void ApplyShieldAura(uint ally, ShipStatus allyStatus, int percent, double seconds)
        {
            var now = DateTime.UtcNow;
            var wasActive = now < allyStatus.ShieldAuraUntil && allyStatus.ShieldAuraPercent >= percent;

            allyStatus.ShieldAuraPercent = percent;
            allyStatus.ShieldAuraUntil = now.AddSeconds(seconds);

            if (!wasActive)
            {
                var bonus = allyStatus.GetEffectiveMaxShield(now) - allyStatus.MaxShield;
                RestoreShield(ally, allyStatus, bonus);
            }

            SaveShipStatus(ally, allyStatus);
            ExecuteScript("pc_target_upd", ally);
        }

        /// <summary>Restores shield to another ship and saves it.</summary>
        public static void RestoreAllyShield(uint ally, ShipStatus allyStatus, int amount)
        {
            RestoreShield(ally, allyStatus, amount);
            SaveShipStatus(ally, allyStatus);
            ExecuteScript("pc_target_upd", ally);
        }

        /// <summary>Lowers an enemy ship's Accuracy for a while. Used by the E-War Array.</summary>
        public static void ApplyAccuracyPenalty(uint enemy, ShipStatus enemyStatus, int penalty, double seconds)
        {
            enemyStatus.AccuracyPenalty = penalty;
            enemyStatus.AccuracyPenaltyUntil = DateTime.UtcNow.AddSeconds(seconds);

            SaveShipStatus(enemy, enemyStatus);
        }
    }
}
