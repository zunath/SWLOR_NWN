using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The instance with its calls into unconverted code answered without allocating.</summary>
internal sealed class AllocationStatService : StatService
{
    public AllocationStatService(
        IDBService db,
        ICombatRecorderService combatRecorder,
        IPlayerFeedbackService playerFeedback,
        IStatusEffectService statusEffect,
        IMimicryTraitStatsService mimicryTraitStats,
        ICompanionLookup beastMastery,
        IPerkService perk,
        ISkillService skill,
        ITemporaryStatModifierService temporaryStatModifier,
        IResourceRestoredListener resourceRestored)
        : base(db, combatRecorder, playerFeedback, statusEffect, mimicryTraitStats, beastMastery, perk, skill, temporaryStatModifier,
            resourceRestored)
    {
    }

    protected override int GetPerkStatBonus(uint creature, StatType stat) => 0;

    protected override int GetTemporaryStatAdjustment(uint creature, StatType stat) => 0;

    protected override bool IsDroid(uint creature) => false;
}
