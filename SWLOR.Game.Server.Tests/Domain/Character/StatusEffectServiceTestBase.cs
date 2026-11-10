using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The shared setup of the status effect instance tests: a scripted bench, a frozen clock, and the container the
/// effects' <c>CombatRecorder</c> calls resolve from. The instance itself comes from the bench, so nothing else in the
/// process shares its tables.
/// </summary>
public abstract class StatusEffectServiceTestBase
{
    protected const uint Caster = 100;
    protected const uint Target = 200;
    protected const uint Bystander = 300;
    protected const uint Invalid = StatusEffectBench.ObjectInvalid;

    protected static readonly DateTime Start = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private IDisposable _clockScope = null!;

    protected StatusEffectBench Bench { get; private set; } = null!;

    protected TestStatusEffectService Service => Bench.Service;

    protected List<string> Log => Bench.Log;

    protected DateTime Now { get; set; }

    [OneTimeSetUp]
    public void BuildContainer() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _facades.Dispose();

    [SetUp]
    public void SetUpBench()
    {
        _facadeScope = _facades.Install();
        Now = Start;
        _clockScope = Clock.OverrideForTests(() => Now);
        Bench = new StatusEffectBench();
    }

    [TearDown]
    public void TearDownBench()
    {
        Bench.Dispose();
        _clockScope.Dispose();
        _facadeScope.Dispose();
    }

    /// <summary>A probe wired to the bench's log, set up by <paramref name="setup"/>.</summary>
    protected T Probe<T>(Action<T>? setup = null, string? name = null)
        where T : ProbeEffect, new()
    {
        var effect = new T { Log = Log };
        if (name != null)
            effect.NameValue = name;
        setup?.Invoke(effect);
        return effect;
    }

    /// <summary>A permanent probe already on <paramref name="creature"/>, from <paramref name="source"/>.</summary>
    protected T Applied<T>(uint creature, uint source, Action<T>? setup = null, float seconds = 0f)
        where T : ProbeEffect, new()
    {
        var effect = Probe(setup);
        var tracker = Tracked(creature);
        effect.ApplyEffect(source, creature, seconds > 0f ? (int)Math.Ceiling(seconds / Math.Max(1f, effect.Frequency)) : -1, seconds);
        tracker.Add(effect);
        Log.Clear();
        return effect;
    }

    /// <summary>The tracker the service holds for <paramref name="creature"/>, created when it has none.</summary>
    protected CreatureStatusEffect Tracked(uint creature)
    {
        Service.OnApplyNWNStatusEffect(creature);
        return Service.GetCreatureStatusEffects(creature);
    }
}
