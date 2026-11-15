using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The instance with its calls into unconverted code answered by the bench. The overrides are grouped in partial files by
/// the part of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) whose code makes the call, so each
/// part can take its own double when it moves; every answer is a property of <see cref="AbilityBench"/>.
/// </summary>
public sealed partial class TestAbilityService : AbilityService
{
    private readonly AbilityBench _bench;

    public TestAbilityService(
        AbilityBench bench,
        IStatusEffectService statusEffect,
        IEnmityService enmity,
        ICombatPointService combatPoint,
        IRecastService recast,
        ITelegraphService telegraph,
        IMessagingService messaging,
        IMusicService music,
        INamedAnimationService namedAnimation,
        IMimicryService mimicry,
        IActivityService activity,
        IPartyService party,
        IPlayerFeedbackService playerFeedback,
        ICombatRecorderService combatRecorder,
        IMeleeTechniqueService meleeTechnique,
        ICapstoneProofService capstoneProof,
        IResistanceService resistance,
        IStealthService stealth,
        IPairedRangedTraitsService pairedRangedTraits)
        : base(statusEffect, enmity, combatPoint, recast, telegraph, messaging, music, namedAnimation, mimicry, activity, party,
            playerFeedback, combatRecorder, meleeTechnique, capstoneProof, resistance, stealth, pairedRangedTraits, FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<IPerkService>(), FacadeForwarder.LazyFor<ICombatAbilityBonusService>(), FacadeForwarder.LazyFor<ICombatAbilityHitService>(), FacadeForwarder.LazyFor<ICombatAbilityImpactService>(), FacadeForwarder.LazyFor<ICombatActivityService>(), FacadeForwarder.LazyFor<ICombatDamageModifiersService>(), FacadeForwarder.LazyFor<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatFeedbackService>(), FacadeForwarder.LazyFor<ICombatHitEffectsService>(), FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>(), FacadeForwarder.LazyFor<ISpaceService>())
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);
}
