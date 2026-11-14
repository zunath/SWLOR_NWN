using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Composition.ContentSamples;

/// <summary>Made content with six values, the most a made type takes.</summary>
public sealed class SixValueContent
{
    public SixValueContent(IPartyService party, int a, int b, int c, int d, int e, string f)
    {
        Party = party;
        Values = $"{a},{b},{c},{d},{e},{f}";
    }

    public IPartyService Party { get; }

    public string Values { get; }
}
