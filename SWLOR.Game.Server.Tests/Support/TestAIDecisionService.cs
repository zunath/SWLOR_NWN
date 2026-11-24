#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The decision engine, <see cref="AIDecisionService"/>: the effect pre-check and the attackable target. The enmity tables are
/// the model on the <see cref="TestAIService"/> the engine reaches the core through; the decision runs the action engine
/// <see cref="TestAIActionService"/>.
/// </summary>
public sealed class TestAIDecisionService : AIDecisionService
{
    private readonly TestAIService _model;

    public TestAIDecisionService(TestAIService model, TestAIActionService action)
        : base(action, FacadeForwarder.For<IActivityService>(), model, FacadeForwarder.For<IEnmityService>())
    {
        _model = model;
    }

    protected override uint GetHighestEnmityTarget(uint enemy) => _model.HighestEnmityTarget(enemy);

    protected override void AttackHighestEnmityTarget(uint creature) => _model.AttackHighest(creature);

    protected override bool MayHaveScriptType(uint creature, EffectTypeScript effectType) => _model.MayHaveEffect;

    /// <summary>The context reads the static enmity tables for its target, so they are made to hold this model first.</summary>
    protected override void UseAttackableEnmityTarget(AIContext context) => RealEnmityTables.Replace(_model.Tables);
}
