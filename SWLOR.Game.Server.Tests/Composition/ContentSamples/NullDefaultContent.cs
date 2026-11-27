using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>Made content whose optional values default to null and to a nullable enum member.</summary>
public sealed class NullDefaultContent
{
    public NullDefaultContent(IPartyService party, int potency, string? note = null, SampleKind? kind = SampleKind.First)
    {
        Potency = potency;
        Note = note;
        Kind = kind;
    }

    public int Potency { get; }

    public string? Note { get; }

    public SampleKind? Kind { get; }
}
