#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-5: the companion rules.
public sealed partial class DefaultSeamsAIService
{
    public bool DefaultIsControlledCompanion(uint creature) => IsControlledCompanion(creature);

    public bool DefaultAreCompanionAbilitiesEnabled(uint companion) => AreCompanionAbilitiesEnabled(companion);

    public uint DefaultResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget) =>
        ResolveHostileAbilityTarget(companion, ability, authorizedTarget, selectedTarget);
}
