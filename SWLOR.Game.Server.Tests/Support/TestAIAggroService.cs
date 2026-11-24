#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part AI-3, <see cref="AIAggroService"/>: proximity enmity, infiltration and the stealth check. The enmity tables are the
/// model on the <see cref="TestAIService"/> the aggro code reaches the core through.
/// </summary>
public sealed class TestAIAggroService : AIAggroService
{
    private readonly AIBench _bench;
    private readonly TestAIService _model;

    public TestAIAggroService(AIBench bench, TestAIService model, TestAIDecisionService decision)
        : base(model, decision, FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<IEspionageInfiltrationService>(), FacadeForwarder.For<IStealthService>())
    {
        _bench = bench;
        _model = model;
    }

    protected override uint GetHighestEnmityTarget(uint enemy) => _model.HighestEnmityTarget(enemy);

    protected override void AttackHighestEnmityTarget(uint creature) => _model.AttackHighest(creature);

    protected override bool HasProximityEnmity(uint creature, uint enemy) =>
        _model.Proximity.TryGetValue(enemy, out var set) && set.Contains(creature);

    protected override void ModifyProximityEnmity(uint creature, uint enemy, int amount)
    {
        _bench.Engine.Note($"ModifyProximityEnmity({creature}, {enemy}, {amount})");
        _model.SetProximityEnmity(enemy, creature, amount);
    }

    protected override bool RemoveTrackedProximityEnmity(uint creature, uint enemy)
    {
        _bench.Engine.Note($"RemoveTrackedProximityEnmity({creature}, {enemy})");
        if (!HasProximityEnmity(creature, enemy))
            return false;

        _model.Proximity[enemy].Remove(creature);
        _model.Tables[enemy].Remove(creature);
        return true;
    }

    protected override bool TryBeginInfiltration(uint player, uint npc)
    {
        _bench.Engine.Note($"TryBeginInfiltration({player}, {npc})");
        return _model.InfiltrationBegins.Contains(player);
    }

    protected override void CompleteInfiltration(uint player, uint npc) =>
        _bench.Engine.Note($"CompleteInfiltration({player}, {npc})");

    protected override bool CanAcquireAggro(uint observer, uint target)
    {
        _bench.Engine.Note($"CanAcquireAggro({observer}, {target})");
        return !_model.HiddenFromAggro.Contains(target);
    }
}
