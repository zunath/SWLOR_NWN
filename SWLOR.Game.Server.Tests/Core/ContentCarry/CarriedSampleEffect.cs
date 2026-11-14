using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Tests.Composition.ContentSamples;

namespace SWLOR.Game.Server.Tests.Core.ContentCarry;

/// <summary>
/// A carried made type that takes services, as a converted status effect will: an interface service and a class-typed content
/// singleton through its constructor, a values-free constructor for the carry, and data fields that a reload carries.
/// </summary>
public sealed class CarriedSampleEffect : CarriedSampleEffectBase
{
    private int _potency;

    public CarriedSampleEffect(IPartyService party, SampleSingleton singleton)
        : this(party, singleton, 0)
    {
    }

    public CarriedSampleEffect(IPartyService party, SampleSingleton singleton, int potency)
    {
        Party = party;
        Singleton = singleton;
        _potency = potency;
    }

    public IPartyService Party { get; }

    public SampleSingleton Singleton { get; }

    public int Potency => _potency;
}
