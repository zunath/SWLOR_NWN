#nullable enable
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The container of one weapon ability part test: the services the parts reach are recording stand-ins that answer false, 0 or null
/// unless the test scripts them, and every call a part makes lands in one transcript the test reads.
/// </summary>
public sealed class WeaponAbilityPartBench : IDisposable
{
    private readonly CombatTranscriptBench _bench = new(WeaponAbilityTraceBench.Leaves);

    public WeaponAbilityPartBench()
    {
        // A status effect the code asks the catalog for is a real one, so the categories it reads are the effect's own.
        _bench.Leaf<IStatusEffectCatalogService>()
            .Script(nameof(IStatusEffectCatalogService.CreateStatusEffect), args => Activator.CreateInstance((Type)args[0]!));
    }

    /// <summary>The recording stand-in of a service, to script its answers.</summary>
    public TranscriptLeaf Leaf<T>()
        where T : class => _bench.Leaf<T>();

    /// <summary>A part, built by the container with the stand-ins.</summary>
    public T Part<T>()
        where T : class => _bench.Get<T>();

    /// <summary>Runs the action and returns the calls it made, one per line, in order.</summary>
    public string Run(Action action)
    {
        var start = _bench.Length;
        _bench.Step("part", action);
        return _bench.TextFrom(start);
    }

    public void Dispose() => _bench.Dispose();
}
