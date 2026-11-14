using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>Made content whose constructor allocates nothing but the object, for the allocation pin of the factory.</summary>
public sealed class AllocationContent
{
    public AllocationContent(IPartyService party, SampleSingleton singleton, int value)
    {
        Party = party;
        Singleton = singleton;
        Value = value;
    }

    public IPartyService Party { get; }

    public SampleSingleton Singleton { get; }

    public int Value { get; }
}
