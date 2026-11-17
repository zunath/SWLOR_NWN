#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Seams that more than one part calls: the highest enmity target (the aggro, event and decision code), the attack command (events, leash, decision and action code) and the movement rate (the creature spawn and the leash).
public sealed partial class TestAIService
{
    protected override uint GetHighestEnmityTarget(uint enemy) =>
        Tables.TryGetValue(enemy, out var table) && table.Count > 0 ? table.MaxBy(entry => entry.Value).Key : AIBench.Invalid;

    protected override void AttackHighestEnmityTarget(uint creature)
    {
        _bench.Engine.Note($"AttackHighestEnmityTarget({creature})");
        Attacked[creature] = GetHighestEnmityTarget(creature);
    }

    protected override void ApplyCreatureMovementRate(uint creature) =>
        _bench.Engine.Note($"ApplyCreatureMovementRate({creature})");
}
