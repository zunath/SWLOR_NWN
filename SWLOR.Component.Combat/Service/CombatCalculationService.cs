using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Enums;
using SWLOR.Component.Combat.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Combat.Service
{
    internal class CombatCalculationService : ICombatCalculationService
    {
        private readonly IStatCalculationService _statCalculationService;
        private readonly IWeaponStatService _weaponStatService;
        private readonly IRandomService _random;

        public CombatCalculationService(
            IStatCalculationService statCalculationService,
            IWeaponStatService weaponStatService,
            IRandomService random)
        {
            _statCalculationService = statCalculationService;
            _weaponStatService = weaponStatService;
            _random = random;
        }

        public int CalculateDamage(
            uint attacker, 
            uint defender, 
            uint weapon,
            bool isCritical)
        {
            var weaponStat = _weaponStatService.LoadWeaponStat(weapon);
            var attackerAttack = _statCalculationService.CalculateAttack(
                attacker, 
                weaponStat.DamageStat, 
                weaponStat.Skill);
            var attackerDMG = weaponStat.DMG;
            var attackerStat = _statCalculationService.CalculateAttribute(attacker, weaponStat.DamageStat);
            var defenderDefense = _statCalculationService.CalculateDefense(defender);
            var defenderStat = _statCalculationService.CalculateVitality(defender);
            var deltaCap = weaponStat.Tier;

            var (min, max) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                isCritical,
                deltaCap);

            var damage = (int)_random.NextFloat(min, max);
            damage = CalculateElementalDamage(defender, weapon, damage);

            return damage;
        }

        private int CalculateElementalDamage(
            uint defender,
            uint weapon,
            int damage)
        {
            var weaponStat = _weaponStatService.LoadWeaponStat(weapon);
            if (weaponStat.DamageType == CombatDamageType.Physical)
                return damage;

            var resistance = _statCalculationService.CalculateResistance(defender, weaponStat.DamageType);
            var percentResist = resistance * 0.01f;

            return (int)(damage * percentResist);
        }

        public HitResult CalculateHitType(
            uint attacker, 
            uint defender,
            uint weapon)
        {
            var hitRate = CalculateHitRate(attacker, defender, weapon);
            var result = new HitResult
            {
                HitRate = hitRate
            };

            if (CalculateIsParalyzed(attacker))
            {
                result.HitType = HitType.Paralyzed;
                return result;
            }
            var hitType = HitType.Miss;

            if (_random.D100(1) <= hitRate)
            {
                hitType = HitType.Normal;
                if (CalculateIsCritical(attacker, defender))
                    hitType = HitType.Critical;
            }

            result.HitType = hitType;

            return result;
        }

        /// <summary>
        /// Calculates the minimum and maximum damage possible with the provided stats.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="isCritical">Whether the attack is a critical hit.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A minimum and maximum damage range</returns>
        private (int, int) CalculateDamageRange(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            bool isCritical,
            int deltaCap = 0)
        {
            const float RatioMax = 3.625f;
            const float RatioMin = 0.01f;

            if (defenderDefense < 1)
                defenderDefense = 1;

            var statDelta = attackerStat - defenderStat;
            if (deltaCap > 0) 
                Math.Clamp(statDelta, -deltaCap, 8 + deltaCap);

            var baseDamage = attackerDMG + statDelta;
            var ratio = (float)attackerAttack / (float)defenderDefense;

            if (ratio > RatioMax)
                ratio = RatioMax;
            else if (ratio < RatioMin)
                ratio = RatioMin;

            var maxDamage = baseDamage * ratio;
            var minDamage = maxDamage * 0.70f;

            // Criticals - 25% bonus to damage range.
            if (isCritical)
            {
                minDamage *= 1.25f;
                maxDamage *= 1.25f;
            }

            return ((int)minDamage, (int)maxDamage);
        }

        /// <summary>
        /// Calculates the hit rate against a given target.
        /// Range is clamped to values between 20 and 95, inclusive.
        /// </summary>
        /// <param name="attackerAccuracy">The total accuracy of the attacker.</param>
        /// <param name="defenderEvasion">The total evasion of the defender.</param>
        /// <returns>The hit rate, clamped between 20 and 95, inclusive.</returns>
        private int CalculateHitRate(
            uint attacker,
            uint defender,
            uint weapon)
        {
            var weaponStat = _weaponStatService.LoadWeaponStat(weapon);
            var attackerAccuracy = _statCalculationService.CalculateAccuracy(
                attacker,
                weaponStat.AccuracyStat,
                weaponStat.Skill);
            var defenderEvasion = _statCalculationService.CalculateEvasion(defender);

            const int BaseHitRate = 75;

            var hitRate = BaseHitRate + 
                          (int)Math.Floor((attackerAccuracy - defenderEvasion) / 2.0f);

            if (hitRate < 20)
                hitRate = 20;
            else if (hitRate > 95)
                hitRate = 95;

            return hitRate;
        }

        /// <summary>
        /// Calculates the critical hit rate against a given target.
        /// </summary>
        /// <param name="attackerStat">The attacker's perception stat</param>
        /// <param name="defenderStat">The defender's might stat.</param>
        /// <returns>The critical rate, in a percentage</returns>
        private int CalculateCriticalRate(
            int attackerStat, 
            int defenderStat)
        {
            const int BaseCriticalRate = 5;
            var delta = attackerStat - defenderStat;

            if (delta < -5)
                delta = -5;
            else if (delta > 15)
                delta = 15;

            var criticalRate = BaseCriticalRate + delta;
            if (criticalRate < BaseCriticalRate)
                criticalRate = BaseCriticalRate;

            return criticalRate;
        }

        private bool CalculateIsCritical(uint attacker, uint defender)
        {
            if (GetIsImmune(defender, ImmunityType.CriticalHit, attacker))
                return false;

            var attackerStat = _statCalculationService.CalculatePerception(attacker);
            var defenderStat = _statCalculationService.CalculateMight(defender);
            var criticalRate = CalculateCriticalRate(attackerStat, defenderStat);

            return _random.D100(1) <= criticalRate;
        }

        private bool CalculateIsParalyzed(uint attacker)
        {
            var paralysis = _statCalculationService.CalculateParalysis(attacker);
            if (paralysis <= 0)
                return false;

            var isParalyzed = _random.D100(1) <= paralysis;
            if (isParalyzed)
            {
                return true;
            }

            return false;
        }

    }
}
