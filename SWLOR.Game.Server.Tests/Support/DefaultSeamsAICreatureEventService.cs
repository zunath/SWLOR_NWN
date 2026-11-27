#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The creature event instance with nothing replaced, and a public door to the seam whose default body can run without a
/// server, for the test that proves the default is the original call into the enmity tables.
/// </summary>
public sealed class DefaultSeamsAICreatureEventService : AICreatureEventService
{
    public DefaultSeamsAICreatureEventService(IAIService ai, IAIDecisionService decision)
        : base(FacadeForwarder.For<IActivityService>(), ai, decision, FacadeForwarder.For<IConversationMenuService>(), FacadeForwarder.For<IConversationService>(), FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<IEnmityAttackService>(), FacadeForwarder.For<ICreatureStatService>())
    {
    }

    public uint DefaultHighestEnmityTarget(uint enemy) => GetHighestEnmityTarget(enemy);
}
