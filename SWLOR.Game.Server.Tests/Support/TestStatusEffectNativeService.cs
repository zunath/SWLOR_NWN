using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 5 of the status effect split, <c>StatusEffectNativeService</c>, with the native effect tag read answered by the bench.
/// </summary>
public sealed class TestStatusEffectNativeService : StatusEffectNativeService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectNativeService(StatusEffectBench bench, IStatusEffectService statusEffect)
        : base(statusEffect)
    {
        _bench = bench;
    }

    protected override bool MayHaveEffectTag(uint creature, string tag) => _bench.NativeTags.Contains(tag);
}
