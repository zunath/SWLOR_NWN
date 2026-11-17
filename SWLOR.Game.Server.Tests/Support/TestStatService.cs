using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The instance with its calls into unconverted code answered by the bench. The overrides are split into partial files by the
/// part of the stat split whose code calls each seam (<c>TestStatService.&lt;Part&gt;.cs</c>; <c>.Shared.cs</c> holds the seams
/// several parts call), so a part that moves takes its file as its own double
/// (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>). What a seam answers is a property of the bench
/// (<c>StatBench.Seams.cs</c>), so a seam that several parts call answers the same in each part's double.
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
        IMimicryService mimicry,
        ITwinPistolEquipmentService twinPistolEquipment,
        IPowerGloveEquipmentService powerGloveEquipment,
        IItemService item,
        IRecastService recast,
        ICompanionLookup beastMastery,
        IEnmityService enmity,
        IResistanceService resistance,
        IPvpCombatService pvpCombat)
        : base(db, combatRecorder, playerFeedback, statusEffect, mimicry, twinPistolEquipment, powerGloveEquipment, item, recast, beastMastery, enmity, resistance, pvpCombat,
            FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<ITemporaryStatModifierService>(), FacadeForwarder.LazyFor<ICombatAttackTimingService>(), FacadeForwarder.For<ICombatDamageCatalogService>(), FacadeForwarder.LazyFor<ICombatDamageModifiersService>(), FacadeForwarder.LazyFor<ICombatDamageTakenService>(), FacadeForwarder.LazyFor<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>())
    {
        _bench = bench;
    }
}
