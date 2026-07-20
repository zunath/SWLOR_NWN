                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);

            return adjustment;
        }

        public static (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return (0, 0, 0);

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return (0, 0, 0);

            var damageBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType);
            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            var defenseIgnore = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType);

            return (damageBonus, criticalRate, defenseIgnore);
        }

        public static (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(
            uint creature)
        {
            var attackBonuses = ConsumeNextAttackGuardedHitAutoAttackBonuses(creature);
            var criticalRate = ConsumeNextAttackGuardedHitCriticalRateBonus(creature);

            return (attackBonuses.DMGBonus, criticalRate, attackBonuses.EnmityBonus);
        }

        public static (int DMGBonus, int EnmityBonus) ConsumeNextAttackGuardedHitAutoAttackBonuses(uint creature)
        {
            var dmgBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                StatType.NextAttackGuardedHitDMGBonus);
            var enmityBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                StatType.NextAttackGuardedHitDMGBonus);

            return (dmgBonus, enmityBonus);
        }

        public static int ConsumeNextAttackGuardedHitCriticalRateBonus(uint creature)
        {
            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                StatType.NextAttackGuardedHitDMGBonus);
        }

        public static void ApplyNextAttackGuardedHitEnmityBonus(
            uint attacker,
            uint defender,
            int enmityBonus)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                enmityBonus <= 0)
            {
                return;
            }

            Enmity.ModifyEnmity(attacker, defender, enmityBonus);
        }

        public static void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds)
        {
            var perkType = GetPerkTypeFromStat(perkTypeValue);
            GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRatePercentAdjustment, durationSeconds);
        }

        public static int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityDamageBonus,
                GetPerkTypeGroup(perkType));
        }

        public static int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }