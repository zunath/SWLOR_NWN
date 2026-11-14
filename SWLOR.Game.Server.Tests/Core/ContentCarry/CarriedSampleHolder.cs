using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core.ContentCarry;

/// <summary>
/// The owner of carried sample effects, as <c>StatusEffectService</c> owns the status effects: one effect can be in two collections.
/// </summary>
public sealed class CarriedSampleHolder
{
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<uint, List<CarriedSampleEffectBase>> _byCreature = new();

    [ReloadState(ReloadStateKind.Carried)]
    private readonly List<CarriedSampleEffectBase> _ticking = new();

    public IReadOnlyDictionary<uint, List<CarriedSampleEffectBase>> ByCreature => _byCreature;

    public IReadOnlyList<CarriedSampleEffectBase> Ticking => _ticking;

    public void Add(uint creature, CarriedSampleEffectBase effect)
    {
        if (!_byCreature.TryGetValue(creature, out var effects))
            _byCreature[creature] = effects = new List<CarriedSampleEffectBase>();

        effects.Add(effect);
        _ticking.Add(effect);
    }
}
