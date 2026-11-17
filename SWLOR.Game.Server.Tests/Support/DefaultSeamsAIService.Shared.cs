#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Seams that more than one part calls.
public sealed partial class DefaultSeamsAIService
{
    public uint DefaultHighestEnmityTarget(uint enemy) => GetHighestEnmityTarget(enemy);
}
