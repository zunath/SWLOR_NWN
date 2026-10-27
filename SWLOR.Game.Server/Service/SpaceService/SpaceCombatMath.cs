namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The space combat rules (plan sections 5, 8 and 9) as pure functions. The combat simulator's SpaceShotMath
    /// holds the same rules, so both must give the same numbers.
    /// </summary>
    public static class SpaceCombatMath
    {
        /// <summary>Hit chance lost per size step the weapon is above its target.</summary>
        public const int SizeHitPenaltyPerStep = 10;

        /// <summary>Damage share lost per size step the weapon is below its target. Compounds per step.</summary>
        public const double SizeDamagePenaltyPerStep = 0.12;

        public const int MinimumResistance = -100;
        public const int MaximumResistance = 100;

        /// <summary>Energy: ship Accuracy + module Accuracy + AGI x 3. Launcher: the same with PER x 3.</summary>
        public static int Accuracy(int shipAccuracy, int moduleAccuracy, int agi, int per, SpaceWeaponFamily family) =>
            shipAccuracy + moduleAccuracy + (family == SpaceWeaponFamily.Energy ? agi : per) * 3;

        /// <summary>Ship Evasion + AGI x 3.</summary>
        public static int Evasion(int shipEvasion, int agi) => shipEvasion + agi * 3;

        /// <summary>The attribute damage scales with: PER for energy weapons, MGT for launchers.</summary>
        public static int DamageAttribute(SpaceWeaponFamily family, int per, int mgt) =>
            family == SpaceWeaponFamily.Energy ? per : mgt;

        /// <summary>Ship Attack + module Attack + the damage attribute.</summary>
        public static int Attack(int shipAttack, int moduleAttack, int damageAttribute) =>
            shipAttack + moduleAttack + damageAttribute;

        /// <summary>Ship Defense + the target's VIT.</summary>
        public static int Defense(int shipDefense, int vit) => shipDefense + vit;

        /// <summary>Size steps the weapon is above (positive) or below (negative) the target; 0 when it ignores size.</summary>
        public static int SizeSteps(ShipSize weaponSize, ShipSize targetSize, bool ignoreSize) =>
            ignoreSize ? 0 : (int)weaponSize - (int)targetSize;

        /// <summary>
        /// Combat.CalculateHitRate with the size penalty as its percentage modifier. The formula's own limits apply
        /// after the penalty.
        /// </summary>
        public static int HitChance(int accuracy, int evasion, ShipSize weaponSize, ShipSize targetSize, bool ignoreSize)
        {
            var penalty = Math.Max(0, SizeSteps(weaponSize, targetSize, ignoreSize)) * SizeHitPenaltyPerStep;
            return Combat.CalculateHitRate(accuracy, evasion, -penalty);
        }

        /// <summary>Damage multiplier for a weapon smaller than its target: (1 - penalty) per step, compounding.</summary>
        public static double SizeDamageMultiplier(ShipSize weaponSize, ShipSize targetSize, bool ignoreSize) =>
            Math.Pow(1.0 - SizeDamagePenaltyPerStep, Math.Max(0, -SizeSteps(weaponSize, targetSize, ignoreSize)));

        /// <summary>The damage formula's delta cap for a weapon of this rank.</summary>
        public static int DeltaCap(int rank) => rank <= 0 ? 1 : Math.Clamp(rank / 10 + 1, 1, 6);

        /// <summary>The ground critical rate with the module rank as weapon rank. PER against VIT for both families.</summary>
        public static int CriticalRate(int per, int targetVit, int moduleRank, int modifier) =>
            Combat.CalculateCriticalRate(per, targetVit, moduleRank, modifier);

        /// <summary>Combat.CalculateDamageRange with one Attack against one Defense and the target's VIT as defender stat.</summary>
        public static (int Minimum, int Maximum) DamageRange(
            int attack, int dmg, int damageAttribute, int defense, int targetVit, bool critical, int moduleRank) =>
            Combat.CalculateDamageRange(
                attack,
                dmg,
                damageAttribute,
                defense,
                targetVit,
                critical ? Combat.StandardCriticalRating : 0,
                DeltaCap(moduleRank));

        /// <summary>The share of damage a resistance lets through. The resistance is limited to -100..100.</summary>
        public static double ResistanceMultiplier(int resistancePercent) =>
            1.0 - Math.Clamp(resistancePercent, MinimumResistance, MaximumResistance) / 100.0;

        /// <summary>A target's resistance plus its Shield Hardener bonus while the hardener is still cycling.</summary>
        public static int HardenedResistance(int resistancePercent, int hardenedAmount, DateTime hardenedUntil, DateTime now) =>
            hardenedAmount > 0 && now < hardenedUntil ? resistancePercent + hardenedAmount : resistancePercent;

        /// <summary>
        /// Applies damage that has been through every other stage. It goes to the shield first (the shield loses the
        /// damage times <paramref name="shieldDamageMultiplier"/>) and the part the shield could not absorb goes to the
        /// hull at 1x; <paramref name="shieldBypass"/> sends all of it to the hull. Returns the shield and hull points
        /// removed, which leaves out damage beyond the hull's last point.
        /// </summary>
        public static double ApplyToPools(
            ref double shield, ref double hull, double damage, bool shieldBypass, double shieldDamageMultiplier)
        {
            if (damage <= 0 || hull <= 0)
                return 0;

            if (shieldDamageMultiplier <= 0)
                shieldDamageMultiplier = 1.0;

            var removed = 0.0;
            var toHull = damage;
            if (!shieldBypass && shield > 0)
            {
                var shieldCost = damage * shieldDamageMultiplier;
                if (shield >= shieldCost)
                {
                    shield -= shieldCost;
                    return shieldCost;
                }

                toHull = damage - shield / shieldDamageMultiplier;
                removed += shield;
                shield = 0;
            }

            var hullLoss = Math.Min(hull, toHull);
            hull -= hullLoss;
            return removed + hullLoss;
        }

        /// <summary>Share of max shield regained per second, by hull size.</summary>
        public static double ShieldRegenRate(ShipSize size) => size switch
        {
            ShipSize.Medium => 0.0055,
            ShipSize.Large => 0.006,
            ShipSize.Capital => 0.0065,
            _ => 0.005,
        };

        /// <summary>Shield regained per second: max shield x the size rate x the WIL factor x the regen bonus.</summary>
        public static double ShieldRegenPerSecond(double maxShield, ShipSize size, int wil, int shieldRegenBonusPercent) =>
            maxShield * ShieldRegenRate(size) * (1.0 + 0.02 * (wil - 10)) * (1.0 + shieldRegenBonusPercent / 100.0);

        /// <summary>Capacitor regained per second: the hull's regen plus 0.05 per point of WIL.</summary>
        public static double CapacitorRegenPerSecond(double hullRegen, int wil) => hullRegen + 0.05 * wil;

        // Pilot stats. The percent and flat stats below are the ground's (Stat.GetAccuracy and its siblings): the
        // flat stat joins the rating, then the percent stat scales the total, and a rating never falls below 1.

        /// <summary>The share of a blinded attacker's shots that miss, as the engine's blindness does on the ground.</summary>
        public const int BlindMissChance = 50;

        /// <summary>
        /// Accuracy, Evasion, Attack or Defense after the pilot's flat stat and percent stat. For Evasion the
        /// ranged Evasion percent is added to <paramref name="percent"/> because every space weapon is ranged.
        /// </summary>
        public static int AdjustRating(int rating, int flat, int percent) =>
            Math.Max(1, Stat.ApplyPercentAdjustment(rating + flat, percent));

        /// <summary>Damage after a critical hit: the pilot's critical damage percent, limited like the ground.</summary>
        public static int CriticalDamage(int damage, bool critical, int percent)
        {
            if (!critical || damage <= 0)
                return damage;

            percent = Combat.ClampCriticalDamagePercentAdjustment(percent);
            return percent == 0 ? damage : damage + damage * percent / 100;
        }

        /// <summary>
        /// The ground's "High Noon" rule on hull: a critical hit against a target whose hull is at or above the
        /// threshold percent earns the adjustment. Zero when the stats are not set.
        /// </summary>
        public static int HighHullCriticalDamageAdjustment(int hull, int maxHull, int thresholdPercent, int adjustment)
        {
            if (thresholdPercent <= 0 || adjustment == 0 || maxHull <= 0)
                return 0;

            return hull < maxHull * (thresholdPercent / 100f) ? 0 : adjustment;
        }

        /// <summary>
        /// The damage percent stat that applies to a shot: single-target damage for the weapon's own cycle and for
        /// single-target abilities, area damage for area abilities.
        /// </summary>
        public static int ShapeDamagePercent(bool isAbility, bool isArea, int singleTargetPercent, int areaPercent) =>
            isAbility && isArea ? areaPercent : singleTargetPercent;

        /// <summary>Damage after the attacker's damage-dealt percent and its shot-shape percent.</summary>
        public static int ApplyDamageDealt(int damage, int dealtPercent, int shapePercent) =>
            Combat.ApplyPercentDamageAdjustment(Combat.ApplyPercentDamageAdjustment(damage, dealtPercent), shapePercent);

        /// <summary>The typed Leadership reduction for ship damage (the ground's "other" damage). Applied before resistance.</summary>
        public static int ApplyLeadershipReduction(int damage, int leadershipPercent) =>
            Combat.ApplyPercentDamageAdjustment(damage, leadershipPercent);

        /// <summary>The target's damage-taken percent, after resistance, with the ground's reduction limits.</summary>
        public static int ApplyDamageTaken(int damage, int takenPercent) =>
            Combat.ApplyCombinedDamageTakenAdjustment(damage, 0, takenPercent);

        /// <summary>The threat a repair generates: half of what was restored, as the ground's heal threat is.</summary>
        public static int RepairEnmity(int restored) =>
            restored <= 0 ? 0 : Enmity.CalculateHealingEnmity(restored, 1, 1 + restored);

        /// <summary>
        /// Whether damage took the hull from at or above a percent of its maximum to below it (the ground's
        /// "reduced below N% HP" rule applied to hull).
        /// </summary>
        public static bool CrossedHullThreshold(int hullBefore, int hullAfter, int maxHull, int thresholdPercent)
        {
            if (maxHull <= 0 || thresholdPercent <= 0 || hullAfter <= 0)
                return false;

            var threshold = maxHull * (thresholdPercent / 100f);
            return hullBefore >= threshold && hullAfter < threshold;
        }

        /// <summary>
        /// The ground's Critical Ward cooldown: 16 s, reduced by 0.5 s per Might, to a minimum of 12 s.
        /// </summary>
        public static int CriticalWardCooldownMilliseconds(int might) =>
            Math.Max(12000, 16000 - Math.Max(0, might) * 500);

        /// <summary>The ground's Shield Bash bonus damage: a percent of Defense, rounded up.</summary>
        public static int PercentOfDefense(int defense, int percent) =>
            Math.Max(0, (int)Math.Ceiling(defense * (percent / 100f)));
    }
}
