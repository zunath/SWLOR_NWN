using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>An abstract base of made content: the factory never builds one.</summary>
public abstract class AbstractContent
{
    protected AbstractContent(IPartyService party)
    {
        Party = party;
    }

    protected IPartyService Party { get; }
}
