using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>A subclass that passes its base's services on, as the conversion tool writes the forwarding constructors.</summary>
public sealed class DerivedContent : AbstractContent
{
    public DerivedContent(IPartyService party, SampleSingleton singleton)
        : base(party)
    {
        Singleton = singleton;
    }

    public SampleSingleton Singleton { get; }

    public IPartyService BaseParty => Party;
}
