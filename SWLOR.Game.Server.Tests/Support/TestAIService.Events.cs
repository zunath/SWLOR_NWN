#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part AI-2, AICreatureEventService: what the creature event handlers call.
public sealed partial class TestAIService
{
    protected override void ResumeAttackAfterActionsCleared(uint creature) =>
        _bench.Engine.Note($"ResumeAttackAfterActionsCleared({creature})");

    protected override void RestoreNPCStats(bool outOfCombatRegen) =>
        _bench.Engine.Note($"RestoreNPCStats({(outOfCombatRegen ? "true" : "false")})");

    protected override void LoadNPCStats() => _bench.Engine.Note("LoadNPCStats()");
}
