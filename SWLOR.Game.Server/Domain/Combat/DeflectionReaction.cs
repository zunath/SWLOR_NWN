using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What one deflection's payload stats read (<see cref="CombatDeflectionReactionService"/>, part 6 of the stat split): the
    /// reads fill it, in their order, before any reaction applies, so the applies see the values the deflection had when it
    /// landed. The names are those of the locals the reads and the applies shared before the split. A value type, so handing it
    /// over allocates nothing.
    /// </summary>
    public readonly struct DeflectionReaction
    {
        /// <summary>The Stamina restore stat of the deflection's source, or <see cref="StatType.Invalid"/>.</summary>
        public StatType StaminaRestoreStat { get; init; }

        /// <summary>The cooldown stat of the Stamina restore, or <see cref="StatType.Invalid"/>.</summary>
        public StatType StaminaRestoreCooldownStat { get; init; }

        public int StaminaRestore { get; init; }

        /// <summary>The FP restore stat of the deflection's source, or <see cref="StatType.Invalid"/>.</summary>
        public StatType FPRestoreStat { get; init; }

        public int FPRestore { get; init; }

        /// <summary>The cooldown stat of the FP restore, or <see cref="StatType.Invalid"/>.</summary>
        public StatType FPRestoreCooldownStat { get; init; }

        public int StaminaRestorePercent { get; init; }

        public int StaminaRestoreCooldown { get; init; }

        public int FPRestoreCooldown { get; init; }

        public int EvasionBoost { get; init; }

        public int EvasionEnmityBoost { get; init; }

        public int EnmityBoost { get; init; }

        public int DefenseBoost { get; init; }

        public int ForceDefenseBoost { get; init; }

        public RecastGroup RecastReductionGroup { get; init; }

        public int RecastReductionSeconds { get; init; }

        public int RecastReductionCooldown { get; init; }

        public SkillType NextSkillAbilitySkillType { get; init; }

        public int NextSkillAbilityDamageBonus { get; init; }

        public int NextSkillAbilityCriticalRate { get; init; }

        public int NextSkillAbilityNoDelay { get; init; }

        public int NextSkillAbilityDamageWindow { get; init; }

        public int NextSkillAbilityCriticalWindow { get; init; }

        public int NextSkillAbilityNoDelayWindow { get; init; }

        public SkillType NextAutoAttackCriticalRateSkillType { get; init; }

        public int NextAutoAttackCriticalRate { get; init; }

        public int NextAutoAttackCriticalRateWindow { get; init; }
    }
}
