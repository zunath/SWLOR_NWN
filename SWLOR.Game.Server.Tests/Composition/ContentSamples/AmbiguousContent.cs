using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>Two constructors that take as many parameters for one value: the factory cannot pick, so it refuses.</summary>
public sealed class AmbiguousContent
{
    public AmbiguousContent(IPartyService party, int value)
    {
    }

    public AmbiguousContent(SampleSingleton singleton, int value)
    {
    }
}
