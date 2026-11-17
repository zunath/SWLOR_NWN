#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The AI instance with nothing replaced, and a public door to each seam whose default body can run without a server,
/// for the tests that prove the defaults are the original calls into the enmity tables and the companion rules.
/// </summary>
public sealed partial class DefaultSeamsAIService : AIService
{
    public DefaultSeamsAIService(IAIPartyQuery party, ICompanionLookup beastMastery, IDMPresenceService dmPresence)
        : base(party, beastMastery, dmPresence, FacadeForwarder.LazyFor<IAbilityActivationService>(), FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActivityService>(), FacadeForwarder.LazyFor<ICompanionControlService>(), FacadeForwarder.For<IConversationMenuService>(), FacadeForwarder.For<IConversationService>(), FacadeForwarder.LazyFor<IEnmityService>(), FacadeForwarder.LazyFor<IEspionageInfiltrationService>(), FacadeForwarder.LazyFor<IPerkService>(), FacadeForwarder.LazyFor<ICreatureStatService>(), FacadeForwarder.LazyFor<IStatusEffectService>(), FacadeForwarder.LazyFor<IStealthService>())
    {
    }

    // One accessor per part of the AI split; each is this instance until its part moves (see TestAIService).
    public DefaultSeamsAIService Service => this;
    public DefaultSeamsAIService Events => this;
    public DefaultSeamsAIService Aggro => this;
    public DefaultSeamsAIService Decision => this;
    public DefaultSeamsAIService Action => this;
}
