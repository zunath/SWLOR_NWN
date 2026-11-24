#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The action instance with nothing replaced, and a public door to the companion rules, for the test that proves the defaults
/// are the original calls.
/// </summary>
public sealed class DefaultSeamsAIActionService : AIActionService
{
    public DefaultSeamsAIActionService(IAIService ai)
        : base(FacadeForwarder.For<IAbilityActivationService>(), ai, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<ICompanionControlStoreService>(), FacadeForwarder.For<ICompanionTargetAuthorityService>(), FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<IPerkService>())
    {
    }

    public bool DefaultIsControlledCompanion(uint creature) => IsControlledCompanion(creature);

    public bool DefaultAreCompanionAbilitiesEnabled(uint companion) => AreCompanionAbilitiesEnabled(companion);

    public uint DefaultResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget) =>
        ResolveHostileAbilityTarget(companion, ability, authorizedTarget, selectedTarget);
}
