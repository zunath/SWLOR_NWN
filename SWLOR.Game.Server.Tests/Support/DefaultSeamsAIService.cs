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
    public DefaultSeamsAIService(IPartyRoster party, ICompanionLookup beastMastery, IDMPresenceService dmPresence)
        : base(party, beastMastery, dmPresence, FacadeForwarder.For<IAbilityActivationService>(), FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActivityService>(), FacadeForwarder.For<ICompanionControlStoreService>(), FacadeForwarder.For<ICompanionTargetAuthorityService>(), FacadeForwarder.LazyFor<IEnmityService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IStatusEffectService>())
    {
        Aggro = new DefaultSeamsAIAggroService(this);
    }

    // One accessor per part of the AI split; each is this instance until its part moves (see TestAIService).
    public DefaultSeamsAIService Service => this;
    public DefaultSeamsAIAggroService Aggro { get; }
    public DefaultSeamsAIService Decision => this;
    public DefaultSeamsAIService Action => this;
}
