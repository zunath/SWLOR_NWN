#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-3: proximity enmity.
public sealed partial class DefaultSeamsAIService
{
    public bool DefaultHasProximityEnmity(uint creature, uint enemy) => HasProximityEnmity(creature, enemy);

    public bool DefaultRemoveTrackedProximityEnmity(uint creature, uint enemy) => RemoveTrackedProximityEnmity(creature, enemy);
}
