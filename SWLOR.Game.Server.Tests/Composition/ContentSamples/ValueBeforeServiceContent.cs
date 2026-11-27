using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>A constructor that takes a value before a service, which the content check reports.</summary>
public sealed class ValueBeforeServiceContent
{
    public ValueBeforeServiceContent(int value, IPartyService party)
    {
    }
}
