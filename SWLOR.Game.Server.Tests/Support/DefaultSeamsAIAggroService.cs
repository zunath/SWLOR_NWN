#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The aggro instance with nothing replaced, and a public door to each seam whose default body can run without a server,
/// for the tests that prove the defaults are the original calls into the enmity tables.
/// </summary>
public sealed class DefaultSeamsAIAggroService : AIAggroService
{
    public DefaultSeamsAIAggroService(IAIService ai)
        : base(ai, FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<IEspionageInfiltrationService>(), FacadeForwarder.For<IStealthService>())
    {
    }

    public bool DefaultHasProximityEnmity(uint creature, uint enemy) => HasProximityEnmity(creature, enemy);

    public bool DefaultRemoveTrackedProximityEnmity(uint creature, uint enemy) => RemoveTrackedProximityEnmity(creature, enemy);
}
