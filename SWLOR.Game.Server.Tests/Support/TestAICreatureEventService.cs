#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part AI-2, <see cref="AICreatureEventService"/>: what the creature event handlers call. The enmity tables are the model on
/// the <see cref="TestAIService"/> the handlers reach the core through; every other call is written into the bench's call list
/// as a note, in order with the engine calls.
/// </summary>
public sealed class TestAICreatureEventService : AICreatureEventService
{
    private readonly AIBench _bench;
    private readonly TestAIService _model;

    public TestAICreatureEventService(AIBench bench, TestAIService model)
        : base(FacadeForwarder.For<IActivityService>(), model, FacadeForwarder.For<IConversationMenuService>(), FacadeForwarder.For<IConversationService>(), FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<ICreatureStatService>())
    {
        _bench = bench;
        _model = model;
    }

    protected override uint GetHighestEnmityTarget(uint enemy) => _model.HighestEnmityTarget(enemy);

    protected override void AttackHighestEnmityTarget(uint creature) => _model.AttackHighest(creature);

    protected override void ApplyCreatureMovementRate(uint creature) => _model.RecordMovementRate(creature);

    protected override void ResumeAttackAfterActionsCleared(uint creature) =>
        _bench.Engine.Note($"ResumeAttackAfterActionsCleared({creature})");

    protected override void RestoreNPCStats(bool outOfCombatRegen) =>
        _bench.Engine.Note($"RestoreNPCStats({(outOfCombatRegen ? "true" : "false")})");

    protected override void LoadNPCStats() => _bench.Engine.Note("LoadNPCStats()");
}
