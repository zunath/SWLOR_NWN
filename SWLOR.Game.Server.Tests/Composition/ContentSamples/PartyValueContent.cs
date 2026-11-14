using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>Made content with one server service and one value, for the factory without a container.</summary>
public sealed class PartyValueContent
{
    public PartyValueContent(IPartyService party, int value)
    {
        Party = party;
        Value = value;
    }

    public IPartyService Party { get; }

    public int Value { get; }
}
