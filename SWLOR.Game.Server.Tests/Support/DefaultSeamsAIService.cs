#nullable enable
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The AI instance with nothing replaced, and a public door to each seam whose default body can run without a server,
/// for the tests that prove the defaults are the original calls into the enmity tables and the companion rules.
/// </summary>
public sealed class DefaultSeamsAIService : AIService
{
    public DefaultSeamsAIService(IPartyService party, IBeastMasteryService beastMastery, IDMPresenceService dmPresence)
        : base(party, beastMastery, dmPresence)
    {
    }

    public uint DefaultHighestEnmityTarget(uint enemy) => GetHighestEnmityTarget(enemy);

    public Dictionary<uint, int> DefaultEnmityTable(uint enemy) => GetEnmityTable(enemy);

    public bool DefaultHasProximityEnmity(uint creature, uint enemy) => HasProximityEnmity(creature, enemy);

    public bool DefaultRemoveTrackedProximityEnmity(uint creature, uint enemy) => RemoveTrackedProximityEnmity(creature, enemy);

    public void DefaultRemoveEnmity(uint creature, uint enemy) => RemoveEnmity(creature, enemy);

    public void DefaultClearEnmityTable(uint enemy) => ClearEnmityTable(enemy);

    public bool DefaultIsControlledCompanion(uint creature) => IsControlledCompanion(creature);

    public bool DefaultAreCompanionAbilitiesEnabled(uint companion) => AreCompanionAbilitiesEnabled(companion);

    public uint DefaultResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget) =>
        ResolveHostileAbilityTarget(companion, ability, authorizedTarget, selectedTarget);
}
