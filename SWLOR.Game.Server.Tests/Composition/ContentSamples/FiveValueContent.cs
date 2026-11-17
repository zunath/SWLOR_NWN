using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>Made content with five values.</summary>
public sealed class FiveValueContent
{
    public FiveValueContent(IPartyService party, int a, int b, int c, int d, string e)
    {
        Party = party;
        Values = $"{a},{b},{c},{d},{e}";
    }

    public IPartyService Party { get; }

    public string Values { get; }
}
