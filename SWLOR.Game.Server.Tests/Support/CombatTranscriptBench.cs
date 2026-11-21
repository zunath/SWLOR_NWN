#nullable enable
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The bench of the combat transcripts: the server's real container with recording stand-ins at its leaves, one engine
/// fake whose writes are recorded in order, a clock held still and the shared <c>Random</c> replaced by a seeded generator
/// that records every draw. A transcript is the one ordered list of what the code under test did: each call into a leaf
/// service with its arguments, each engine write, each random draw, and what it returned. The tests call the services
/// the container hands out by their interfaces, so the later PRs that split a service run the same text with other receivers.
/// </summary>
/// <remarks>
/// <para>Leaves are the services the Combat code reaches for stats, status effects, resources, recording, enmity and
/// activity (<see cref="DefaultLeaves"/>). Every other Combat service in the container is real, so a transcript crosses the
/// seams that the split moves.</para>
/// <para>Determinism: <c>Clock</c> is overridden, <c>Random</c> is seeded, and the leaves answer from tables the test
/// writes; no value is masked.</para>
/// </remarks>
public sealed class CombatTranscriptBench : IDisposable
{
    /// <summary>The seed every transcript starts its random generator with.</summary>
    public const int Seed = 20261118;

    public const uint Attacker = 11;
    public const uint Defender = 22;
    public const uint Other = 33;
    public const uint Master = 55;
    public const uint Ally = 66;
    public const uint Gone = 44;
    private const uint ObjectInvalid = 0x7F000000;

    /// <summary>Every service a transcript replaces with a recording stand-in unless the test names its own set.</summary>
    public static readonly Type[] DefaultLeaves =
    {
        typeof(IStatService), typeof(IStatusEffectService), typeof(IStatusEffectApplicationService),
        typeof(IStatusEffectCombatEventService), typeof(IStatusEffectDurationService), typeof(ITemporaryStatModifierService),
        typeof(ICombatResourcesService), typeof(ICombatRecorderService), typeof(ICombatRecording), typeof(ICharacterRecording),
        typeof(ICombatRecordingState), typeof(IEnmityService), typeof(ICombatActivityService), typeof(ICombatAttackTimingService),
        typeof(ICombatSuppressionService), typeof(IPairedRangedTraitsService), typeof(ICapstoneProofService),
        typeof(IResistanceService), typeof(IPartyService), typeof(ICombatRatingService)
    };

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<Type, TranscriptLeaf> _leaves = new();
    private readonly Dictionary<uint, string> _names = new()
    {
        [Attacker] = "Attacker", [Defender] = "Defender", [Other] = "Other", [Master] = "Master", [Ally] = "Ally", [Gone] = "Gone",
        [ObjectInvalid] = "Invalid"
    };

    private readonly List<Action> _delayed = new();
    private readonly Dictionary<(uint, StatType), int> _stats = new();
    private FakeNWScript _engine = null!;
    private DateTime _now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
    private int _defaultStat;

    public CombatTranscriptBench(IEnumerable<Type>? leaves = null, Action<IServiceCollection, CombatTranscriptBench>? configure = null)
    {
        FacadeContainer? facades = null;
        try
        {
            var leafTypes = (leaves ?? DefaultLeaves).ToArray();
            Lines = new List<string>();

            _engine = new FakeNWScript();
            _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or ObjectInvalid or Gone));
            _engine.SetReturnHandler("GetIsReactionTypeHostile", args => (uint)args[0]! != (uint)args[1]!);
            _engine.SetReturnHandler("GetIsDead", args => false);
            _engine.SetReturnHandler("GetMaxHitPoints", args => 200);
            _engine.SetReturnHandler("GetCurrentHitPoints", args => 100);
            _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is Attacker or Defender);
            _engine.SetReturnHandler("GetObjectUUID", args => "uuid-" + Name((uint)args[0]!));
            _engine.SetReturnHandler("GetArea", args => 900u);
            _engine.SetReturnHandler("GetDistanceBetween", args => 3.0f);
            _engine.SetReturnHandler("GetTag", args => "tag-" + Name((uint)args[0]!));
            _engine.SetReturnHandler("GetName", args => Name((uint)args[0]!));

            _scopes.Add(Clock.OverrideForTests(() => _now, () => (long)(_now - new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds));
            _scopes.Add(RecordingRandom.Install(Seed, Lines, Name));
            _scopes.Add(NWScriptRuntime.Override(EngineProxy.Wrap<INWScript>(_engine, this)));
            var objectPlugin = new FakeObjectPlugin();
            objectPlugin.SetReturnHandler("GetCurrentHitPoints", args => 100);
            _scopes.Add(ObjectPluginRuntime.Override(EngineProxy.Wrap<IObjectPlugin>(objectPlugin, this)));
            _scopes.Add(CreaturePluginRuntime.Override(EngineProxy.Wrap<ICreaturePlugin>(new FakeCreaturePlugin(), this)));
            Events = new FakeEventsPlugin();
            _scopes.Add(EventsPluginRuntime.Override(Events));

            Db = new InMemoryDBService();
            foreach (var id in new[] { "uuid-Attacker", "uuid-Defender" })
            {
                var player = new Player(id);
                foreach (var skill in Enum.GetValues<SkillType>())
                    player.Skills[skill] = new PlayerSkill { Rank = 20 };
                Db.With(player);
            }
            facades = new FacadeContainer(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IDBService>(Db));
                foreach (var type in leafTypes)
                {
                    var leaf = CreateLeaf(type);
                    services.Replace(ServiceDescriptor.Singleton(type, leaf));
                }

                configure?.Invoke(services, this);
            });
            Facades = facades;
            _scopes.Add(facades.Install());
            InstallStatAnswers();
            // The recording flag is read on every event and is part of how a recorder is split; its reads are not part of a transcript.
            if (_leaves.TryGetValue(typeof(ICombatRecorderService), out var recorder))
                recorder.Silence("get_IsRecording");
            if (_leaves.TryGetValue(typeof(ICombatRecording), out var recording))
                recording.Silence("get_IsRecording");
        }
        catch
        {
            for (var index = _scopes.Count - 1; index >= 0; index--)
                _scopes[index].Dispose();
            _scopes.Clear();
            facades?.Dispose();
            throw;
        }
    }

    /// <summary>The ordered transcript of the step being recorded.</summary>
    public List<string> Lines { get; }

    /// <summary>The database of the two players (the attacker and the defender), in memory.</summary>
    public InMemoryDBService Db { get; }

    /// <summary>The container with the leaves replaced.</summary>
    public FacadeContainer Facades { get; }

    public FakeNWScript Engine => _engine;

    /// <summary>The NWNX events plugin fake; a test answers <c>GetEventData</c> with it.</summary>
    public FakeEventsPlugin Events { get; }

    /// <summary>The stat every unscripted read answers; 0 unless a scenario raises it so that every rider is taken.</summary>
    public int DefaultStat
    {
        get => _defaultStat;
        set => _defaultStat = value;
    }

    /// <summary>The clock the code under test reads.</summary>
    public DateTime Now
    {
        get => _now;
        set => _now = value;
    }

    public T Get<T>()
        where T : class => Facades.Get<T>();

    /// <summary>The recording stand-in that replaced a leaf, to script or silence members.</summary>
    public TranscriptLeaf Leaf<T>()
        where T : class => _leaves[typeof(T)];

    /// <summary>The recording stand-in that replaced a leaf, by its contract, for a fixture that sets up every leaf alike.</summary>
    public TranscriptLeaf Leaf(Type contract) => _leaves[contract];

    /// <summary>Sets what a stat of a creature reads.</summary>
    public void SetStat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    public void ClearStats() => _stats.Clear();

    public string Name(uint creature) => _names.TryGetValue(creature, out var name) ? name : creature.ToString(CultureInfo.InvariantCulture);

    public void NameCreature(uint creature, string name) => _names[creature] = name;

    /// <summary>Writes a free line to the transcript (a step heading, a note about what the scenario did).</summary>
    public void Note(string text) => Lines.Add("-- " + text);

    private readonly StringBuilder _text = new();
    private int _step;

    /// <summary>
    /// Runs one step of a scenario and appends "== step N name", then every line the step wrote, in order, then any exception
    /// (type and first line of the message). The delayed actions the step left are run as a second part of the same step.
    /// </summary>
    public void Step(string name, Action action, bool runDelayed = true)
    {
        Lines.Clear();
        _text.Append("== step ").Append(++_step).Append(' ').Append(name).Append('\n');
        try
        {
            action();
            if (runDelayed)
                RunDelayed();
        }
        catch (Exception exception) when (IsNativeBoundary(exception))
        {
            // The engine's native library is not loaded in a unit test: the transcript ends where the code first reaches it.
            _delayed.Clear();
            Lines.Add("-- stopped at the native boundary (" + exception.GetType().Name + ")");
        }
        catch (Exception exception) when (exception is not AssertionException)
        {
            // A transcript of a step that threw would pin the path to the throw, not the behaviour; the scenario must supply what the code needs.
            _delayed.Clear();
            Assert.Fail($"Step {name} threw {exception.GetType().Name}: {exception.Message} {exception.StackTrace} Transcript so far: {string.Join(" | ", Lines.TakeLast(12))}");
        }

        foreach (var line in Lines)
            _text.Append(line).Append('\n');
        Lines.Clear();
    }

    private static bool IsNativeBoundary(Exception exception) =>
        exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException
        || exception is TypeInitializationException { InnerException: { } inner } && IsNativeBoundary(inner);

    /// <summary>The transcript so far, as the committed text.</summary>
    public string Text => _text.ToString();

    /// <summary>The length of the transcript so far, read without copying it.</summary>
    public int Length => _text.Length;

    /// <summary>The part of the transcript written since <paramref name="start"/> (an earlier <see cref="Length"/>), without copying the rest.</summary>
    public string TextFrom(int start) => _text.ToString(start, _text.Length - start);

    /// <summary>Runs the actions the code under test delayed, in the order it delayed them, writing each to the transcript.</summary>
    public void RunDelayed()
    {
        while (_delayed.Count > 0)
        {
            var action = _delayed[0];
            _delayed.RemoveAt(0);
            Lines.Add("delayed run");
            action();
        }
    }

    internal void Delay(float seconds, Action action)
    {
        Lines.Add($"Engine.DelayCommand({TranscriptFormat.Plain(seconds)})");
        _delayed.Add(action);
    }

    /// <summary>An action put on the creature's action queue runs after the step's own calls, as the engine runs it later.</summary>
    internal void Queue(string call, Action action)
    {
        Lines.Add("Engine." + call);
        _delayed.Add(action);
    }

    internal void Observe(string member, object?[] arguments)
    {
        if (!IsWrite(member))
            return;

        Lines.Add($"Engine.{member}({string.Join(", ", arguments.Select(Format))})");
    }

    public string Format(object? argument) => argument switch
    {
        uint id when _names.TryGetValue(id, out var name) => name,
        // The listener a service registers is a different class after a split; the transcript names the role, not the class.
        IStatusEffectAppliedListener => "<applied listener>",
        _ => TranscriptFormat.Plain(argument)
    };

    private static bool IsWrite(string member) =>
        !member.StartsWith("Get", StringComparison.Ordinal) && !member.StartsWith("get_", StringComparison.Ordinal) && !member.StartsWith("Is", StringComparison.Ordinal) &&
        !member.StartsWith("Has", StringComparison.Ordinal) && member is not
            ("OBJECT_SELF" or "StringToObject" or "StringToInt" or "IntToString" or "FloatToString" or "ObjectToString" or "TagEffect" or
             "ResManGetAliasFor" or "DelayCommand" or "AssignCommand" or "Random" or "d2" or "d3" or "d4" or "d6" or "d8" or "d10" or "d12" or
             "d20" or "d100" or "ObjectToString" or "FloatToInt" or "IntToFloat" or "GetStringLength") &&
        !member.StartsWith("Effect", StringComparison.Ordinal) && !member.StartsWith("Vector", StringComparison.Ordinal) &&
        !member.StartsWith("Location", StringComparison.Ordinal);

    private TranscriptLeaf CreateLeaf(Type contract)
    {
        var proxy = typeof(DispatchProxy).GetMethods().Single(method => method.Name == "Create" && method.IsGenericMethodDefinition).MakeGenericMethod(contract, typeof(TranscriptLeaf))
            .Invoke(null, null)!;
        var leaf = (TranscriptLeaf)proxy;
        var name = contract.Name;
        leaf.Label = (name.StartsWith('I') ? name[1..] : name).Replace("Service", string.Empty).Replace("Combat", string.Empty);
        leaf.Lines = Lines;
        leaf.Format = Format;
        _leaves[contract] = leaf;
        return leaf;
    }

    // The stat reads answer from the table, then from the default; they are recorded like every other call.
    private void InstallStatAnswers()
    {
        if (!_leaves.TryGetValue(typeof(IStatService), out var stat))
            return;

        stat.Script(nameof(IStatService.GetStatAdjustment), args =>
            _stats.TryGetValue(((uint)args[0]!, (StatType)args[1]!), out var value) ? value : _defaultStat);
    }

    /// <summary>
    /// Compares a transcript with its committed text, whole, byte for byte; regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c>
    /// only for a deliberate change. The text uses LF line ends, whatever the platform.
    /// </summary>
    public static void AssertGolden(string repositoryRelativePath, string actual)
    {
        var path = Path.Combine(FindRepositoryRoot(), repositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var actualBytes = new UTF8Encoding(false).GetBytes(actual);
        if (Environment.GetEnvironmentVariable("SWLOR_UPDATE_SNAPSHOTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, actualBytes);
        }

        File.Exists(path).Should().BeTrue("the transcript is committed beside the tests");
        var expectedBytes = File.ReadAllBytes(path);
        if (expectedBytes.AsSpan().SequenceEqual(actualBytes))
            return;

        var expected = new UTF8Encoding(false).GetString(expectedBytes).Split('\n');
        var lines = actual.Split('\n');
        for (var index = 0; index < Math.Max(expected.Length, lines.Length); index++)
        {
            var left = index < expected.Length ? expected[index] : "<end>";
            var right = index < lines.Length ? lines[index] : "<end>";
            if (left != right)
                Assert.Fail($"{repositoryRelativePath} differs at line {index + 1}:\n  expected: {left}\n  actual:   {right}");
        }

        Assert.Fail($"{repositoryRelativePath} differs in its line ends or its final newline.");
    }

    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln.");
    }

    public void Dispose()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--)
            _scopes[index].Dispose();
        _scopes.Clear();
        Facades.Dispose();
    }

    /// <summary>Records every write the engine fake receives, then lets the fake answer; delayed and assigned actions are routed.</summary>
    public class EngineProxy : DispatchProxy
    {
        public object Target { get; set; } = null!;

        public CombatTranscriptBench Bench { get; set; } = null!;

        public static T Wrap<T>(T target, CombatTranscriptBench bench)
            where T : class
        {
            var proxy = DispatchProxy.Create<T, EngineProxy>();
            var engine = (EngineProxy)(object)proxy;
            engine.Target = target;
            engine.Bench = bench;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var arguments = args ?? Array.Empty<object?>();
            var member = targetMethod!.Name;
            if (member == "DelayCommand" && arguments[1] is Action delayed)
            {
                Bench.Delay((float)arguments[0]!, delayed);
                return null;
            }

            if (member == "ActionDoCommand" && arguments[0] is Action queued)
            {
                Bench.Queue("ActionDoCommand()", queued);
                return null;
            }

            if (member == "AssignCommand" && arguments[1] is Action assigned)
            {
                Bench.Lines.Add($"Engine.AssignCommand({Bench.Format(arguments[0])})");
                assigned();
                return null;
            }

            Bench.Observe(member, arguments);
            try
            {
                return targetMethod.Invoke(Target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
                throw;
            }
        }
    }
}
