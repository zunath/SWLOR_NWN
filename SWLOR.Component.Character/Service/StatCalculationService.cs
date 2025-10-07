using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;

namespace SWLOR.Component.Character.Service
{
    /// <inheritdoc />
    internal class StatCalculationService: IStatCalculationService
    {
        private readonly IStatGroupService _statGroupService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly ISkillService _skillService;

        public StatCalculationService(
            IStatGroupService statGroupService,
            IStatusEffectService statusEffectService,
            ISkillService skillService)
        {
            _statGroupService = statGroupService;
            _statusEffectService = statusEffectService;
            _skillService = skillService;
        }

        /// <inheritdoc />
        public int CalculateMaxHP(uint creature)
        {
            const int BaseHP = 70;
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return BaseHP + 
                   stats.GetStat(StatType.MaxHP) + 
                   effects.GetStat(StatType.MaxHP);
        }

        /// <inheritdoc />
        public int CalculateMaxFP(uint creature)
        {
            const int BaseFP = 10;
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var modifier = stats.GetStat(StatType.Willpower) * 10;

            return BaseFP +
                   modifier +
                   stats.GetStat(StatType.MaxFP) + 
                   effects.GetStat(StatType.MaxFP);
        }
        /// <inheritdoc />
        public int CalculateMaxSTM(uint creature)
        {
            const int BaseSTM = 10;
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var modifier = stats.GetStat(StatType.Perception) * 10;

            return BaseSTM +
                   modifier +
                   stats.GetStat(StatType.MaxSTM) + 
                   effects.GetStat(StatType.MaxSTM);
        }

        /// <inheritdoc />
        public int CalculateHPRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var hpRegen = stats.GetStat(StatType.HPRegen) + 
                          effects.GetStat(StatType.HPRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) * 4;
            return hpRegen + vitalityBonus;
        }

        /// <inheritdoc />
        public int CalculateFPRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var fpRegen = stats.GetStat(StatType.FPRegen) + 
                          effects.GetStat(StatType.FPRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) / 2;
            return 1 + fpRegen + vitalityBonus;
        }

        /// <inheritdoc />
        public int CalculateSTMRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var stmRegen = stats.GetStat(StatType.STMRegen) + 
                           effects.GetStat(StatType.STMRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) / 2;
            return 1 + stmRegen + vitalityBonus;
        }

        /// <inheritdoc />
        public float CalculateRecastReduction(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var recastReduction = (stats.GetStat(StatType.RecastReduction) + 
                                   effects.GetStat(StatType.RecastReduction)) * 0.01f;
            if (recastReduction > 0.5f)
                recastReduction = 0.5f;

            return recastReduction;
        }

        /// <inheritdoc />
        public int CalculateDefense(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Armor);
            var ability = stats.GetStat(AbilityType.Vitality);
            var bonus = stats.GetStat(StatType.Defense) + 
                        effects.GetStat(StatType.Defense);

            return CalculateDefense(skill, ability, bonus);
        }

        /// <inheritdoc />
        public int CalculateEvasion(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Armor);
            var ability = stats.GetStat(AbilityType.Agility);
            var bonus = stats.GetStat(StatType.Evasion) + 
                        effects.GetStat(StatType.Evasion);

            return CalculateEvasion(skill, ability, bonus);
        }

        /// <inheritdoc />
        public int CalculateAccuracy(uint creature, AbilityType abilityType, SkillType skillType)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, skillType);
            var ability = stats.GetStat(abilityType);
            var bonus = stats.GetStat(StatType.Accuracy) + effects.GetStat(StatType.Accuracy);

            return CalculateAccuracy(skill, ability, bonus);
        }

        /// <inheritdoc />
        public int CalculateAttack(uint creature, AbilityType abilityType, SkillType skillType)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, skillType);
            var ability = stats.GetStat(abilityType);
            var bonus = stats.GetStat(StatType.Attack) + effects.GetStat(StatType.Attack);

            return CalculateAttack(skill, ability, bonus);
        }

        /// <inheritdoc />
        public int CalculateForceAttack(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Force);
            var ability = stats.GetStat(AbilityType.Willpower);
            var bonus = stats.GetStat(StatType.ForceAttack) + 
                        effects.GetStat(StatType.ForceAttack);

            return 8 + (2 * skill) + ability + bonus;
        }

        /// <inheritdoc />
        public int CalculateMight(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Might) + effects.GetStat(StatType.Might);
        }

        /// <inheritdoc />
        public int CalculatePerception(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Perception) + effects.GetStat(StatType.Perception);
        }

        /// <inheritdoc />
        public int CalculateVitality(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Vitality) + effects.GetStat(StatType.Vitality);
        }

        /// <inheritdoc />
        public int CalculateAgility(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Agility) + effects.GetStat(StatType.Agility);
        }

        /// <inheritdoc />
        public int CalculateWillpower(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Willpower) + effects.GetStat(StatType.Willpower);
        }

        /// <inheritdoc />
        public int CalculateSocial(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Social) + effects.GetStat(StatType.Social);
        }

        /// <inheritdoc />
        public int CalculateShieldDeflection(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ShieldDeflection) + effects.GetStat(StatType.ShieldDeflection);
        }

        /// <inheritdoc />
        public int CalculateAttackDeflection(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.AttackDeflection) + effects.GetStat(StatType.AttackDeflection);
        }

        /// <inheritdoc />
        public int CalculateCriticalRate(uint creature)
        {
            const int BaseRate = 5;
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var rate = BaseRate + 
                       stats.GetStat(StatType.CriticalRate) + 
                       effects.GetStat(StatType.CriticalRate);

            if (rate < 0)
                rate = 0;
            else if (rate > 90)
                rate = 90;

            return rate;
        }

        /// <inheritdoc />
        public int CalculateEnmity(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Enmity) + effects.GetStat(StatType.Enmity);
        }

        /// <inheritdoc />
        public int CalculateHaste(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Haste) + effects.GetStat(StatType.Haste);
        }

        /// <inheritdoc />
        public int CalculateSlow(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Slow) + effects.GetStat(StatType.Slow);
        }

        /// <inheritdoc />
        public int CalculateDamageReduction(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.DamageReduction) + effects.GetStat(StatType.DamageReduction);
        }

        /// <inheritdoc />
        public int CalculateForceDefense(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Armor);
            var ability = stats.GetStat(AbilityType.Willpower);
            var bonus = stats.GetStat(StatType.ForceDefense) + effects.GetStat(StatType.ForceDefense);

            return (int)(8 + (ability * 1.5f) + skill + bonus);
        }

        /// <inheritdoc />
        public int CalculateQueuedDMGBonus(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.QueuedDMGBonus) + effects.GetStat(StatType.QueuedDMGBonus);
        }

        /// <inheritdoc />
        public int CalculateParalysis(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var paralysis = stats.GetStat(StatType.Paralysis) + effects.GetStat(StatType.Paralysis);
            if (paralysis > 75)
                paralysis = 75;

            return paralysis;
        }

        /// <inheritdoc />
        public int CalculateAccuracyModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.AccuracyModifier) + effects.GetStat(StatType.AccuracyModifier);
        }

        /// <inheritdoc />
        public int CalculateRecastReductionModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.RecastReductionModifier) + effects.GetStat(StatType.RecastReductionModifier);
        }

        /// <inheritdoc />
        public int CalculateDefenseBypassModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.DefenseBypassModifier) + effects.GetStat(StatType.DefenseBypassModifier);
        }

        /// <inheritdoc />
        public int CalculateHealingModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.HealingModifier) + effects.GetStat(StatType.HealingModifier);
        }

        /// <inheritdoc />
        public int CalculateFPRestoreOnHit(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.FPRestoreOnHit) + effects.GetStat(StatType.FPRestoreOnHit);
        }

        /// <inheritdoc />
        public int CalculateDefenseModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.DefenseModifier) + effects.GetStat(StatType.DefenseModifier);
        }

        /// <inheritdoc />
        public int CalculateForceDefenseModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ForceDefenseModifier) + effects.GetStat(StatType.ForceDefenseModifier);
        }

        /// <inheritdoc />
        public int CalculateExtraAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ExtraAttackModifier) + effects.GetStat(StatType.ExtraAttackModifier);
        }

        /// <inheritdoc />
        public int CalculateAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.AttackModifier) + effects.GetStat(StatType.AttackModifier);
        }

        /// <inheritdoc />
        public int CalculateForceAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ForceAttackModifier) + effects.GetStat(StatType.ForceAttackModifier);
        }

        /// <inheritdoc />
        public int CalculateEvasionModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.EvasionModifier) + effects.GetStat(StatType.EvasionModifier);
        }

        /// <inheritdoc />
        public int CalculateXPModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.XPModifier) + effects.GetStat(StatType.XPModifier);
        }

        /// <inheritdoc />
        public int CalculatePoisonResist(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.PoisonResist) + effects.GetStat(StatType.PoisonResist);
        }

        /// <inheritdoc />
        public int CalculateLevel(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Level) + effects.GetStat(StatType.Level);
        }

        private float CalculateAttackSpeedModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            // Get haste and slow from both stats and status effects
            var haste = stats.GetStat(StatType.Haste) + effects.GetStat(StatType.Haste);
            if (haste < 0)
                haste = 0;
            
            var slow = stats.GetStat(StatType.Slow) + effects.GetStat(StatType.Slow);
            if (slow < 0)
                slow = 0;

            // Haste reduces attack delay (negative modifier), slow increases attack delay (positive modifier)
            // Formula: (slow - haste) * 0.01f
            // This means:
            // - Positive haste values result in negative modifier (faster attacks)
            // - Positive slow values result in positive modifier (slower attacks)
            var modifier = (slow - haste) * 0.01f;
            
            // Cap only negative modifiers (haste) to prevent extreme values
            // Slow can go beyond +50% to allow for more severe slow effects
            if (modifier < -0.5f)
                modifier = -0.5f;

            return modifier;
        }

        /// <inheritdoc />
        public int CalculateAttackDelay(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var rightHandDelay = stats.RightHandStat.Delay;
            var leftHandDelay = stats.LeftHandStat.Delay;
            var delay = rightHandDelay + leftHandDelay;
            var attackSpeedModifier = CalculateAttackSpeedModifier(creature);

            // Convert delay units to milliseconds: 60 delay units = 1 second
            var baseDelay = delay / 60f * 1000;
            
            // Apply attack speed modifier
            // Positive modifier (slow) increases delay, negative modifier (haste) decreases delay
            var finalDelay = (int)(baseDelay * (1.0f + attackSpeedModifier));
            
            return finalDelay;
        }

        /// <inheritdoc />
        public int CalculateControl(uint creature, CraftType craftType)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            StatType statType;

            switch (craftType)
            {
                case CraftType.Smithery:
                    statType = StatType.ControlSmithery;
                    break;
                case CraftType.Engineering:
                    statType = StatType.ControlEngineering;
                    break;
                case CraftType.Fabrication:
                    statType = StatType.ControlFabrication;
                    break;
                case CraftType.Agriculture:
                    statType = StatType.ControlAgriculture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(craftType), craftType, null);
            }

            return stats.GetStat(statType) + effects.GetStat(statType);
        }

        /// <inheritdoc />
        public int CalculateCraftsmanship(uint creature, CraftType craftType)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            StatType statType;

            switch (craftType)
            {
                case CraftType.Smithery:
                    statType = StatType.CraftsmanshipSmithery;
                    break;
                case CraftType.Engineering:
                    statType = StatType.CraftsmanshipEngineering;
                    break;
                case CraftType.Fabrication:
                    statType = StatType.CraftsmanshipFabrication;
                    break;
                case CraftType.Agriculture:
                    statType = StatType.CraftsmanshipAgriculture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(craftType), craftType, null);
            }

            return stats.GetStat(statType) + effects.GetStat(statType);
        }

        /// <inheritdoc />
        public int CalculateAttack(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        /// <inheritdoc />
        public int CalculateDefense(int level, int stat, int bonus)
        {
            return (int)(8 + (stat * 1.5f) + level + bonus);
        }

        /// <inheritdoc />
        public int CalculateAccuracy(int level, int stat, int bonus)
        {
            return stat * 3 + level + bonus;
        }

        /// <inheritdoc />
        public int CalculateEvasion(int level, int stat, int bonus)
        {
            return stat * 3 + level + bonus;
        }

    }
}
