using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;

namespace SWLOR.Component.Character.Service
{
    internal class StatService: IStatServiceNew
    {
        private readonly IStatGroupService _statGroupService;
        private readonly IStatusEffectService _statusEffectService;

        public StatService(
            IStatGroupService statGroupService,
            IStatusEffectService statusEffectService)
        {
            _statGroupService = statGroupService;
            _statusEffectService = statusEffectService;
        }

        public int CalculateMaxHP(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.MaxHP) + effects.GetStat(StatType.MaxHP);
        }

        public int CalculateMaxFP(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.MaxFP) + effects.GetStat(StatType.MaxFP);
        }
        public int CalculateMaxSTM(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.MaxSTM) + effects.GetStat(StatType.MaxSTM);
        }

        public int CalculateHPRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var hpRegen = stats.GetStat(StatType.HPRegen) + effects.GetStat(StatType.HPRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) * 4;
            return hpRegen + vitalityBonus;
        }

        public int CalculateFPRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var fpRegen = stats.GetStat(StatType.FPRegen) + effects.GetStat(StatType.FPRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) / 2;
            return 1 + fpRegen + vitalityBonus;
        }

        public int CalculateSTMRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var stmRegen = stats.GetStat(StatType.STMRegen) + effects.GetStat(StatType.STMRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) / 2;
            return 1 + stmRegen + vitalityBonus;
        }

        public float CalculateRecastReduction(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            
            var recastReduction = (stats.GetStat(StatType.RecastReduction) + effects.GetStat(StatType.RecastReduction)) * 0.01f;
            if (recastReduction > 0.5f)
                recastReduction = 0.5f;

            return recastReduction;
        }

        public int CalculateDefense(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Defense) + effects.GetStat(StatType.Defense);
        }

        public int CalculateEvasion(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Evasion) + effects.GetStat(StatType.Evasion);
        }

        public int CalculateAccuracy(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Accuracy) + effects.GetStat(StatType.Accuracy);
        }

        public int CalculateAttack(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Attack) + effects.GetStat(StatType.Attack);
        }

        public int CalculateForceAttack(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ForceAttack) + effects.GetStat(StatType.ForceAttack);
        }

        public int CalculateMight(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Might) + effects.GetStat(StatType.Might);
        }

        public int CalculatePerception(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Perception) + effects.GetStat(StatType.Perception);
        }

        public int CalculateVitality(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Vitality) + effects.GetStat(StatType.Vitality);
        }

        public int CalculateAgility(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Agility) + effects.GetStat(StatType.Agility);
        }

        public int CalculateWillpower(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Willpower) + effects.GetStat(StatType.Willpower);
        }

        public int CalculateSocial(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Social) + effects.GetStat(StatType.Social);
        }

        public int CalculateShieldDeflection(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ShieldDeflection) + effects.GetStat(StatType.ShieldDeflection);
        }

        public int CalculateAttackDeflection(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.AttackDeflection) + effects.GetStat(StatType.AttackDeflection);
        }

        public int CalculateCriticalRate(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.CriticalRate) + effects.GetStat(StatType.CriticalRate);
        }

        public int CalculateEnmity(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Enmity) + effects.GetStat(StatType.Enmity);
        }

        public int CalculateHaste(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Haste) + effects.GetStat(StatType.Haste);
        }

        public int CalculateSlow(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.Slow) + effects.GetStat(StatType.Slow);
        }

        public int CalculateDamageReduction(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.DamageReduction) + effects.GetStat(StatType.DamageReduction);
        }

        public int CalculateForceDefense(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ForceDefense) + effects.GetStat(StatType.ForceDefense);
        }

        public int CalculateQueuedDMGBonus(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.QueuedDMGBonus) + effects.GetStat(StatType.QueuedDMGBonus);
        }

        public int CalculateParalysis(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var paralysis = stats.GetStat(StatType.Paralysis) + effects.GetStat(StatType.Paralysis);
            if (paralysis > 75)
                paralysis = 75;

            return paralysis;
        }

        public int CalculateAccuracyModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.AccuracyModifier) + effects.GetStat(StatType.AccuracyModifier);
        }

        public int CalculateRecastReductionModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.RecastReductionModifier) + effects.GetStat(StatType.RecastReductionModifier);
        }

        public int CalculateDefenseBypassModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.DefenseBypassModifier) + effects.GetStat(StatType.DefenseBypassModifier);
        }

        public int CalculateHealingModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.HealingModifier) + effects.GetStat(StatType.HealingModifier);
        }

        public int CalculateFPRestoreOnHit(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.FPRestoreOnHit) + effects.GetStat(StatType.FPRestoreOnHit);
        }

        public int CalculateDefenseModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.DefenseModifier) + effects.GetStat(StatType.DefenseModifier);
        }

        public int CalculateForceDefenseModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ForceDefenseModifier) + effects.GetStat(StatType.ForceDefenseModifier);
        }

        public int CalculateExtraAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ExtraAttackModifier) + effects.GetStat(StatType.ExtraAttackModifier);
        }

        public int CalculateAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.AttackModifier) + effects.GetStat(StatType.AttackModifier);
        }

        public int CalculateForceAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.ForceAttackModifier) + effects.GetStat(StatType.ForceAttackModifier);
        }

        public int CalculateEvasionModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.EvasionModifier) + effects.GetStat(StatType.EvasionModifier);
        }

        public int CalculateXPModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.XPModifier) + effects.GetStat(StatType.XPModifier);
        }

        public int CalculatePoisonResist(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return stats.GetStat(StatType.PoisonResist) + effects.GetStat(StatType.PoisonResist);
        }

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
    }
}
