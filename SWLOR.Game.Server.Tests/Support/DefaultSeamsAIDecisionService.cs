#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The decision instance with nothing replaced, and a public door to the seam whose default body can run without a server,
/// for the test that proves the default is the original call into the enmity tables.
/// </summary>
public sealed class DefaultSeamsAIDecisionService : AIDecisionService
{
    public DefaultSeamsAIDecisionService(IAIService ai, IAIActionService action)
        : base(action, FacadeForwarder.For<IActivityService>(), ai, FacadeForwarder.For<IEnmityService>())
    {
    }

    public uint DefaultHighestEnmityTarget(uint enemy) => GetHighestEnmityTarget(enemy);
}
