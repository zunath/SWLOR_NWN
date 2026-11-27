#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Seams that more than one part calls: the highest enmity target (the aggro, event, leash and decision code), the attack command (events, aggro, leash, decision and action code) and the movement rate (the creature spawn and the leash). The model answers them for every part.
public sealed partial class TestAIService
{
    /// <summary>The highest enmity target the model holds for an enemy, or the invalid object.</summary>
    public uint HighestEnmityTarget(uint enemy) =>
        Tables.TryGetValue(enemy, out var table) && table.Count > 0 ? table.MaxBy(entry => entry.Value).Key : AIBench.Invalid;

    /// <summary>Records the attack on the highest enmity target, as <c>AttackHighestEnmityTarget</c> does in every part.</summary>
    public void AttackHighest(uint creature)
    {
        _bench.Engine.Note($"AttackHighestEnmityTarget({creature})");
        Attacked[creature] = HighestEnmityTarget(creature);
    }

    /// <summary>Records the movement rate reset, as <c>ApplyCreatureMovementRate</c> does in every part.</summary>
    public void RecordMovementRate(uint creature) =>
        _bench.Engine.Note($"ApplyCreatureMovementRate({creature})");

    protected override uint GetHighestEnmityTarget(uint enemy) => HighestEnmityTarget(enemy);

    protected override void AttackHighestEnmityTarget(uint creature) => AttackHighest(creature);

    protected override void ApplyCreatureMovementRate(uint creature) => RecordMovementRate(creature);
}
