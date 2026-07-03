namespace SWLOR.Game.Server.Service.CombatService
{
    internal static class CombatFormula
    {
        public const int BaseAttackDelayMilliseconds = 1750;
        public const int MaxAttacksPerSwing = 3;
        public const int MinimumAttackDelayMilliseconds =
            (BaseAttackDelayMilliseconds + MaxAttacksPerSwing - 1) / MaxAttacksPerSwing;

        private const int AttackDelayUnitsPerSecond = 60;
        private const int MillisecondsPerSecond = 1000;
        private const int BaseAttackDelayUnits =
            BaseAttackDelayMilliseconds * AttackDelayUnitsPerSecond / MillisecondsPerSecond;

        public static int CalculateHitRate(
            int attackerAccuracy,
            int defenderEvasion,
            int percentageModifier)
        {
            const int BaseHitRate = 75;

            var hitRate = BaseHitRate + (int)Math.Floor((attackerAccuracy - defenderEvasion) / 2.0f) + percentageModifier;

            if (hitRate < 20)
                hitRate = 20;
            else if (hitRate > 95)
                hitRate = 95;

            return hitRate;
        }

        public static int CalculateCriticalRate(
            int attackerPER,
            int defenderVIT,
            int skillRank,
            int criticalModifier)
        {
            const int BaseCriticalRate = 5;
            const int MaxCriticalRate = 50;
            var skillBonus = Math.Max(0, skillRank / 10);
            var statBonus = Math.Clamp((int)Math.Floor((attackerPER - defenderVIT) / 5.0f), 0, 3);

            var criticalRate = BaseCriticalRate + skillBonus + statBonus + criticalModifier;
            if (criticalRate < BaseCriticalRate)
                criticalRate = BaseCriticalRate;
            else if (criticalRate > MaxCriticalRate)
                criticalRate = MaxCriticalRate;

            return criticalRate;
        }

        public static int CalculateAttackDelayMilliseconds(
            int rightHandDelayUnits,
            int leftHandDelayUnits,
            int attackDelayReductionPercent,
            int offhandAttackDelayReductionPercent)
        {
            attackDelayReductionPercent = Math.Min(attackDelayReductionPercent, 50);
            offhandAttackDelayReductionPercent = Math.Min(Math.Max(offhandAttackDelayReductionPercent, 0), 50);
            leftHandDelayUnits = CombatFormula.ApplyPercentReduction(leftHandDelayUnits, offhandAttackDelayReductionPercent);

            var delayUnits = CombatFormula.CalculateEquippedWeaponDelayUnits(rightHandDelayUnits, leftHandDelayUnits);
            return CombatFormula.CalculateAttackDelayMillisecondsFromDelayUnits(delayUnits, attackDelayReductionPercent);
        }

        public static int CalculateAttackDelayMillisecondsFromDelayUnits(
            int delayUnits,
            int attackDelayReductionPercent)
        {
            var delayMilliseconds = CombatFormula.ConvertAttackDelayUnitsToMilliseconds(delayUnits);
            return CombatFormula.ApplyAttackDelayReduction(delayMilliseconds, attackDelayReductionPercent);
        }

        public static int CalculateEffectiveAttackDelay(
            int attackerDelayMilliseconds,
            bool useDefaultMinimumDelay)
        {
            if (useDefaultMinimumDelay)
                return BaseAttackDelayMilliseconds;

            if (attackerDelayMilliseconds <= BaseAttackDelayMilliseconds)
                return BaseAttackDelayMilliseconds;

            var effectiveDelay = attackerDelayMilliseconds - BaseAttackDelayMilliseconds;
            return Math.Max(MinimumAttackDelayMilliseconds, effectiveDelay);
        }

        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds)
        {
            return CombatFormula.CalculateEffectiveAttackDelay(attackerDelayMilliseconds, false);
        }

        public static int CalculateAttackSwingDelay(int effectiveDelayMilliseconds)
        {
            return Math.Max(BaseAttackDelayMilliseconds, effectiveDelayMilliseconds);
        }

        public static int CalculateAttacksPerSwing(
            int effectiveDelayMilliseconds,
            float attackDebt,
            out float updatedAttackDebt)
        {
            if (effectiveDelayMilliseconds <= 0)
            {
                updatedAttackDebt = 0f;
                return 1;
            }

            var swingDelay = CombatFormula.CalculateAttackSwingDelay(effectiveDelayMilliseconds);
            var attacksOwed = attackDebt + swingDelay / (float)effectiveDelayMilliseconds;
            var attacks = Math.Clamp((int)attacksOwed, 1, MaxAttacksPerSwing);
            updatedAttackDebt = Math.Clamp(attacksOwed - attacks, 0f, MaxAttacksPerSwing);

            return attacks;
        }

        public static int CalculateEquippedWeaponDelayUnits(int rightHandDelay, int leftHandDelay)
        {
            rightHandDelay = Math.Max(0, rightHandDelay);
            leftHandDelay = Math.Max(0, leftHandDelay);

            var hasRightHandDelay = rightHandDelay > 0;
            var hasLeftHandDelay = leftHandDelay > 0;
            if (!hasRightHandDelay || !hasLeftHandDelay)
                return rightHandDelay + leftHandDelay;

            // Each equipped weapon delay includes the engine's default attack cadence.
            // The custom delay gate only needs to pay that baseline once for the pair.
            return BaseAttackDelayUnits +
                   Math.Max(0, rightHandDelay - BaseAttackDelayUnits) +
                   Math.Max(0, leftHandDelay - BaseAttackDelayUnits);
        }

        public static int ApplyAttackDelayReduction(int delayMilliseconds, int reductionPercentage)
        {
            if (delayMilliseconds <= 0 || reductionPercentage == 0)
                return delayMilliseconds;

            if (reductionPercentage > 0)
                return CombatFormula.ApplyPercentReduction(delayMilliseconds, reductionPercentage);

            var increaseAmount = (int)(delayMilliseconds * (Math.Abs(reductionPercentage) / 100f));
            return delayMilliseconds + increaseAmount;
        }

        public static int ApplyPercentReduction(int value, int reductionPercentage)
        {
            if (value <= 0 || reductionPercentage <= 0)
                return value;

            var reductionAmount = (int)(value * (reductionPercentage / 100f));
            return Math.Max(0, value - reductionAmount);
        }

        private static int ConvertAttackDelayUnitsToMilliseconds(int delayUnits)
        {
            return (int)(delayUnits / (float)AttackDelayUnitsPerSecond * MillisecondsPerSecond);
        }
    }
}
