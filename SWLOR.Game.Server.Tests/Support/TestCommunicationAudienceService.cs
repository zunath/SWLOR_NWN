using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The Comms audience with the space-mode check answered by a set.</summary>
public sealed class TestCommunicationAudienceService : CommunicationAudienceService
{
    public TestCommunicationAudienceService(
        IDBService db,
        IDMPresenceService dmPresence,
        IDMPreferenceService dmPreference,
        IPartyService party,
        IPlanetService planet,
        IAreaService area,
        IPropertyService property)
        : base(db, dmPresence, dmPreference, party, planet, area, property, FacadeForwarder.For<ISpaceService>())
    {
    }

    /// <summary>Players the space-mode check answers true for.</summary>
    public HashSet<uint> InSpace { get; } = new();

    protected override bool IsPlayerInSpaceMode(uint player) => InSpace.Contains(player);
}
