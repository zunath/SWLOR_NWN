using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The instance with its calls into unconverted code answered by the bench: part 1 of the stat split, <c>StatService</c>. The
/// other parts are their own doubles (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>; part 2's is
/// <see cref="TestCreatureStatService"/>, part 3's <see cref="TestStatRecordService"/>, part 5's
/// <see cref="TestCombatRatingService"/>, part 6's <see cref="TestCombatDeflectionReactionService"/> and part 7's
/// <see cref="TestCombatResourcesService"/>). What a seam answers is a property of the bench (<c>StatBench.Seams.cs</c>), so a
/// seam that several parts call answers the same in each part's double.
/// </summary>
public sealed partial class TestStatService : StatService
{
    private readonly StatBench _bench;

    public TestStatService(
        StatBench bench,
        IDBService db,
        ICombatRecorderService combatRecorder,
        IPlayerFeedbackService playerFeedback,
        IStatusEffectService statusEffect,
        IMimicryTraitStatsService mimicryTraitStats,
        ICompanionLookup beastMastery)
        : base(db, combatRecorder, playerFeedback, statusEffect, mimicryTraitStats, beastMastery,
            FacadeForwarder.For<IPerkStatBonusService>(), FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<ITemporaryStatModifierService>(),
            FacadeForwarder.For<IResourceRestoredListener>())
    {
        _bench = bench;
    }
}
