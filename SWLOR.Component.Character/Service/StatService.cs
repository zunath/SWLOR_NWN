using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Component.Character.Service
{
    internal class StatService: IStatServiceNew
    {
        private readonly IStatGroupService _statGroupService;

        public StatService(IStatGroupService statGroupService)
        {
            _statGroupService = statGroupService;
        }

        public int CalculateMaxHP(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);

            return stats.GetStat(StatType.MaxHP);
        }

        public int CalculateMaxFP(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.MaxFP);
        }
        public int CalculateMaxSTM(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.MaxSTM);
        }

        public int CalculateHPRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var hpRegen = stats.GetStat(StatType.HPRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) * 4;
            return hpRegen + vitalityBonus;
        }

        public int CalculateFPRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var fpRegen = stats.GetStat(StatType.FPRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) / 2;
            return 1 + fpRegen + vitalityBonus;
        }

        public int CalculateSTMRegen(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var fpRegen = stats.GetStat(StatType.STMRegen);
            var vitalityBonus = stats.GetStat(StatType.Vitality) / 2;
            return 1 + fpRegen + vitalityBonus;
        }

        public float CalculateRecastReduction(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            var recastReduction = stats.GetStat(StatType.RecastReduction) * 0.01f;
            if (recastReduction > 0.5f)
                recastReduction = 0.5f;

            return recastReduction;
        }

        public int CalculateDefense(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Defense);
        }

        public int CalculateEvasion(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Evasion);
        }

        public int CalculateAccuracy(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Accuracy);
        }

        public int CalculateAttack(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Attack);
        }

        public int CalculateForceAttack(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.ForceAttack);
        }

        public int CalculateMight(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Might);
        }

        public int CalculatePerception(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Perception);
        }

        public int CalculateVitality(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Vitality);
        }

        public int CalculateAgility(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Agility);
        }

        public int CalculateWillpower(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Willpower);
        }

        public int CalculateSocial(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Social);
        }

        public int CalculateShieldDeflection(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.ShieldDeflection);
        }

        public int CalculateAttackDeflection(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.AttackDeflection);
        }

        public int CalculateCriticalRate(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.CriticalRate);
        }

        public int CalculateEnmity(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Enmity);
        }

        public int CalculateHaste(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Haste);
        }

        public int CalculateSlow(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Slow);
        }

        public int CalculateDamageReduction(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.DamageReduction);
        }

        public int CalculateForceDefense(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.ForceDefense);
        }

        public int CalculateQueuedDMGBonus(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.QueuedDMGBonus);
        }

        public int CalculateParalysis(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Paralysis);
        }

        public int CalculateAccuracyModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.AccuracyModifier);
        }

        public int CalculateRecastReductionModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.RecastReductionModifier);
        }

        public int CalculateDefenseBypassModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.DefenseBypassModifier);
        }

        public int CalculateHealingModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.HealingModifier);
        }

        public int CalculateFPRestoreOnHit(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.FPRestoreOnHit);
        }

        public int CalculateDefenseModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.DefenseModifier);
        }

        public int CalculateForceDefenseModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.ForceDefenseModifier);
        }

        public int CalculateExtraAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.ExtraAttackModifier);
        }

        public int CalculateAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.AttackModifier);
        }

        public int CalculateForceAttackModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.ForceAttackModifier);
        }

        public int CalculateEvasionModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.EvasionModifier);
        }

        public int CalculateXPModifier(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.XPModifier);
        }

        public int CalculatePoisonResist(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.PoisonResist);
        }

        public int CalculateLevel(uint creature)
        {
            var stats = _statGroupService.LoadStats(creature);
            return stats.GetStat(StatType.Level);
        }
    }
}
