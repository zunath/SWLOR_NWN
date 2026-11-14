using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>
/// Made content as the design writes it: services first (an interface and a class-typed singleton), then the values it takes today,
/// optional values last, and a values-free constructor that forwards.
/// </summary>
public sealed class SampleContent
{
    public SampleContent(IPartyService party, SampleSingleton singleton)
        : this(party, singleton, 0)
    {
    }

    public SampleContent(IPartyService party, SampleSingleton singleton, int potency, string label = "default", SampleKind kind = SampleKind.Second,
        int? cap = 7)
    {
        Party = party;
        Singleton = singleton;
        Potency = potency;
        Label = label;
        Kind = kind;
        Cap = cap;
    }

    public SampleContent(IPartyService party, SampleSingleton singleton, int potency, uint target)
        : this(party, singleton, potency, "targeted")
    {
        Target = target;
    }

    public IPartyService Party { get; }

    public SampleSingleton Singleton { get; }

    public int Potency { get; }

    public string Label { get; }

    public SampleKind Kind { get; }

    public int? Cap { get; }

    public uint Target { get; }
}
