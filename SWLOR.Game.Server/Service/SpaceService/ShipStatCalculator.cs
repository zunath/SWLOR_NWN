using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The revamp ship stat rules (Design Bible "Starships (Revamp)", ship stat rules) as pure functions.
    /// </summary>
    public static class ShipStatCalculator
    {
        public const int CapacitorPerWillpower = 2;

        /// <summary>Output multiplier of a module's hull, shield, capacitor and capacitor regen on this hull size.</summary>
        public static double ModuleSizeMultiplier(ShipSize size) => size == ShipSize.Small || size == ShipSize.Invalid ? 1.0 : 1.1;

        /// <summary>Damage multiplier of a weapon that fires as the hull's size.</summary>
        public static double WeaponSizeMultiplier(ShipSize size) => size switch
        {
            ShipSize.Medium => 1.1,
            ShipSize.Large => 1.35,
            ShipSize.Capital => 1.35,
            _ => 1.0,
        };

        /// <summary>The hotbar feat slot number (1-30) of a slot: High 1-10, Low 11-20, Mid 21-30.</summary>
        public static int GetHotbarSlot(ShipModulePowerType row, int slot)
        {
            return row switch
            {
                ShipModulePowerType.High => slot,
                ShipModulePowerType.Low => 10 + slot,
                ShipModulePowerType.Mid => 20 + slot,
                _ => 0,
            };
        }

        /// <summary>
        /// Tags every slot of a hull. In each row the first slots are Helm and the last ones are station slots:
        /// High is Gunner, Low is Engineer, and Mid is Engineer except the last Mid station slot on a Capital hull,
        /// which is Sensors.
        /// </summary>
        public static Dictionary<int, ShipSlotTag> BuildSlotTags(ShipHullEntry hull)
        {
            var tags = new Dictionary<int, ShipSlotTag>();
            AddRow(tags, ShipModulePowerType.High, hull.High, hull.StationHigh, hull.Size);
            AddRow(tags, ShipModulePowerType.Mid, hull.Mid, hull.StationMid, hull.Size);
            AddRow(tags, ShipModulePowerType.Low, hull.Low, hull.StationLow, hull.Size);
            return tags;
        }

        private static void AddRow(Dictionary<int, ShipSlotTag> tags, ShipModulePowerType row, int count, int stations, ShipSize size)
        {
            stations = Math.Clamp(stations, 0, count);
            var firstStation = count - stations + 1;
            for (var slot = 1; slot <= count; slot++)
            {
                ShipSlotTag tag;
                if (slot < firstStation)
                    tag = ShipSlotTag.Helm;
                else if (row == ShipModulePowerType.High)
                    tag = ShipSlotTag.Gunner;
                else if (row == ShipModulePowerType.Mid && size == ShipSize.Capital && slot == count)
                    tag = ShipSlotTag.Sensors;
                else
                    tag = ShipSlotTag.Engineer;

                tags[GetHotbarSlot(row, slot)] = tag;
            }
        }

        /// <summary>The seat name shown to a player for a slot tag.</summary>
        public static string GetSeatName(ShipSlotTag tag) => tag switch
        {
            ShipSlotTag.Gunner => "Gunner",
            ShipSlotTag.Engineer => "Engineer",
            ShipSlotTag.Sensors => "Sensors",
            _ => "Helm",
        };

        /// <summary>Whether the tag is a crew station (anything other than the Helm).</summary>
        public static bool IsStation(ShipSlotTag tag) => tag != ShipSlotTag.Helm && tag != ShipSlotTag.Invalid;

        /// <summary>
        /// Works out a ship's stats. Modules in station slots add nothing until their seat is crewed: then
        /// <paramref name="stationEffectiveness"/> (hotbar slot to effectiveness, 0 for an empty seat) scales their
        /// hull, shield, capacitor and capacitor regen, as the balance model does. Their best-of Defense, Evasion
        /// and Accuracy, speed, resistances and damage bonuses apply in full while the seat works them. With no
        /// <paramref name="stationEffectiveness"/>, every station slot is empty (a solo pilot runs the Helm only).
        /// </summary>
        public static ShipStatResult Calculate(
            ShipHullEntry hull,
            IEnumerable<ShipFittedModule> modules,
            int willpower,
            Func<int, double> stationEffectiveness = null)
        {
            var tags = BuildSlotTags(hull);
            var size = hull.Size == ShipSize.Invalid ? ShipSize.Small : hull.Size;
            var moduleMultiplier = ModuleSizeMultiplier(size);

            double hullPoints = hull.Hull;
            double shieldPoints = hull.Shield;
            double capacitor = hull.Capacitor + CapacitorPerWillpower * willpower;
            var capacitorRegen = hull.CapacitorRegen;

            var bestDefense = 0;
            var bestEvasion = 0;
            var evasionPenalty = 0;
            var bestAccuracy = 0;
            var speedPercent = 0;
            var resistAll = hull.ResistAll;
            var resistThermal = 0;
            var resistEm = 0;
            var resistExplosive = 0;
            var damageThermal = hull.DamagePercent;
            var damageEm = hull.DamagePercent;
            var damageExplosive = hull.DamagePercent + hull.ExplosivePercent;

            foreach (var fitted in modules ?? Enumerable.Empty<ShipFittedModule>())
            {
                var step = fitted?.Step;
                if (step == null) continue;

                var hotbarSlot = GetHotbarSlot(fitted.Row, fitted.Slot);
                var effectiveness = 1.0;
                if (tags.TryGetValue(hotbarSlot, out var tag) && IsStation(tag))
                {
                    effectiveness = stationEffectiveness?.Invoke(hotbarSlot) ?? 0.0;
                    if (effectiveness <= 0.0)
                        continue;
                }

                var outputMultiplier = moduleMultiplier * effectiveness;
                hullPoints += Round(step.Hull * outputMultiplier);
                shieldPoints += Round(step.Shield * outputMultiplier);
                capacitor += Round(step.MaxCapacitor * outputMultiplier);
                capacitorRegen += step.CapacitorRegen * outputMultiplier;

                bestDefense = Math.Max(bestDefense, step.ModuleDefense);
                bestAccuracy = Math.Max(bestAccuracy, step.ModuleAccuracy);
                if (step.ModuleEvasion > 0)
                    bestEvasion = Math.Max(bestEvasion, step.ModuleEvasion);
                else
                    evasionPenalty += step.ModuleEvasion;

                speedPercent += step.SpeedPercent;

                // A Shield Hardener's resistance applies only while it cycles.
                if (step.Family != ShipModuleFamily.ShieldHardener)
                    resistAll += step.ResistAll;
                resistThermal += step.ResistThermal;
                resistEm += step.ResistEM;
                resistExplosive += step.ResistExplosive;

                damageThermal += step.DamageThermal + step.DamageAll;
                damageEm += step.DamageEM + step.DamageAll;
                damageExplosive += step.DamageExplosive + step.DamageAll;
            }

            return new ShipStatResult
            {
                Size = size,
                CapitalShip = size == ShipSize.Capital,
                MaxHull = (int)Round(hullPoints * (1 + hull.HullPercent / 100.0)),
                MaxShield = (int)Round(shieldPoints * (1 + hull.ShieldPercent / 100.0)),
                MaxCapacitor = (int)Round(capacitor * (1 + hull.CapacitorPercent / 100.0)),
                CapacitorRegenPerSecond = capacitorRegen,
                Attack = hull.Attack,
                Accuracy = hull.Accuracy + bestAccuracy,
                Defense = hull.Defense + bestDefense,
                Evasion = hull.Evasion + bestEvasion + evasionPenalty,
                ShieldRegenBonusPercent = hull.ShieldRegenPercent,
                CriticalBonus = hull.CriticalBonus,
                Speed = hull.Speed * (1 + speedPercent / 100.0),
                Resistances = new Dictionary<ResistanceType, int>
                {
                    [ResistanceType.Thermal] = resistAll + resistThermal,
                    [ResistanceType.EM] = resistAll + resistEm,
                    [ResistanceType.Explosive] = resistAll + resistExplosive,
                },
                DamageBonusPercent = new Dictionary<CombatDamageType, int>
                {
                    [CombatDamageType.Thermal] = damageThermal,
                    [CombatDamageType.EM] = damageEm,
                    [CombatDamageType.Explosive] = damageExplosive,
                },
                SlotTags = tags,
            };
        }

        /// <summary>
        /// Copies a result onto a ship status. Current hull, shield and capacitor are limited to the new
        /// maximums; with <paramref name="fill"/> (first registration) they start full.
        /// </summary>
        public static void Apply(ShipStatResult result, ShipStatus status, bool fill)
        {
            status.Size = result.Size;
            status.CapitalShip = result.CapitalShip;
            status.MaxHull = result.MaxHull;
            status.MaxShield = result.MaxShield;
            status.MaxCapacitor = result.MaxCapacitor;
            status.CapacitorRegenPerSecond = result.CapacitorRegenPerSecond;
            status.Attack = result.Attack;
            status.Accuracy = result.Accuracy;
            status.Defense = result.Defense;
            status.Evasion = result.Evasion;
            status.ShieldRegenBonusPercent = result.ShieldRegenBonusPercent;
            status.CriticalBonus = result.CriticalBonus;
            status.SpeedMultiplier = result.Speed;
            status.Resistances = new Dictionary<ResistanceType, int>(result.Resistances);
            status.DamageBonusPercent = new Dictionary<CombatDamageType, int>(result.DamageBonusPercent);
            status.SlotTags = new Dictionary<int, ShipSlotTag>(result.SlotTags);

            if (fill)
            {
                status.Hull = status.MaxHull;
                status.Shield = status.MaxShield;
                status.Capacitor = status.MaxCapacitor;
            }
            else
            {
                status.Hull = Math.Min(status.Hull, status.MaxHull);
                status.Shield = Math.Min(status.Shield, status.MaxShield);
                status.Capacitor = Math.Min(status.Capacitor, status.MaxCapacitor);
            }
        }

        private static double Round(double value) => Math.Round(value);
    }
}
