using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// Compiles a small game build (one <see cref="IGameplayGeneration"/>) to a temporary folder, so the host's tests load a
/// real assembly through the package's <c>ReloadHost</c> with SWLOR's load options. The build records what it sees into
/// the <see cref="System.Collections.Concurrent.ConcurrentQueue{T}"/> of strings the test registers as a host service, and
/// answers scripts by name: <c>act</c> (handled), <c>cond_t</c> / <c>cond_f</c> (a true or false conditional),
/// <c>throw</c> (the dispatcher throws), anything else not handled.
/// </summary>
internal static class FixtureGenerationBuilder
{
    private const string Source = """
        using System;
        using System.Collections.Concurrent;
        using System.Threading;
        using System.Threading.Tasks;
        using Nwn.Hosting.Contracts.Generations;
        using SWLOR.Game.Contracts;

        namespace HostFixture;

        public sealed class FixtureGeneration : IGameplayGeneration
        {
            private ConcurrentQueue<string> _record;
            private IHostCallbacks _callbacks;
            private Signals _signals;
            private long _generation;

            public string BuildId => "fixture";
        #if REQUIRED_SCHEMA
            public int RequiredSchemaVersion => REQUIRED_SCHEMA_VERSION;
        #endif
            public IScriptDispatcher Scripts { get; private set; }
            public IMainLoopSink MainLoop { get; private set; }

            public void ConfigureSourceDirectory(string directoryPath) => Directory = directoryPath;
            public static string Directory;

            public ValueTask ValidateAsync(CancellationToken cancellationToken) =>
        #if FAIL_VALIDATE
                ValueTask.FromException(new InvalidOperationException("validation failed"));
        #else
                new(Task.Run(() => ValidatedOnThread = Environment.CurrentManagedThreadId));
        #endif
            public static int ValidatedOnThread;

            public ValueTask ActivateAsync(GenerationActivation activation, CancellationToken cancellationToken)
            {
                var services = activation.HostServices;
                _record = (ConcurrentQueue<string>)services.Resolve(typeof(ConcurrentQueue<string>));
                _generation = activation.Generation;
        #if FAIL_ACTIVATE
                throw new InvalidOperationException("activation failed");
        #else
                var lifecycle = services.Resolve(typeof(IHostLifecycle)) as IHostLifecycle;
                _record.Enqueue($"lifecycle generation={activation.Generation} moduleLoaded={lifecycle?.ModuleLoaded}");
                _record.Enqueue($"activate generation={activation.Generation} thread={Environment.CurrentManagedThreadId} validated={ValidatedOnThread} source={Directory}");
                _callbacks = (IHostCallbacks)services.Resolve(typeof(IHostCallbacks));
                var redis = (IRedisConnectionSource)services.Resolve(typeof(IRedisConnectionSource));
                var closures = (NWN.Core.ICoreFunctionHandler)services.Resolve(typeof(NWN.Core.ICoreFunctionHandler));
                _record.Enqueue($"services redis={redis != null} closures={closures != null}");
                closures.ClosureDelayCommand(7, 1f, () => _record.Enqueue("closure ran"));
                _signals = new Signals(_record);
                _callbacks.BindSignals(_signals);
                Scripts = new Dispatcher(_record);
                MainLoop = new Loop(_record, _callbacks);
                return ValueTask.CompletedTask;
        #endif
            }

            public ValueTask BeginQuiescenceAsync(CancellationToken cancellationToken)
            {
                _record?.Enqueue($"quiesce generation={_generation} thread={Environment.CurrentManagedThreadId}");
                return ValueTask.CompletedTask;
            }

            public ValueTask DrainAsync(TimeSpan timeout, CancellationToken cancellationToken) => ValueTask.CompletedTask;

            public ValueTask DisposeAsync()
            {
                if (_callbacks != null) _callbacks.UnbindSignals(_signals);
                _record?.Enqueue(_record != null && _generation != 0 ? $"dispose generation={_generation}" : "dispose");
                return ValueTask.CompletedTask;
            }

            private sealed class Signals(ConcurrentQueue<string> record) : IHostSignalHandler
            {
                public void OnSignal(string signal) => record.Enqueue("signal " + signal);
            }

            private sealed class Loop(ConcurrentQueue<string> record, IHostCallbacks callbacks) : IMainLoopSink
            {
                public void Tick() => record.Enqueue("tick frame=" + callbacks.Frame);
            }

            private sealed class Dispatcher(ConcurrentQueue<string> record) : IScriptDispatcher
            {
                public bool TryHandle(string scriptName, uint objectSelf, out bool conditionalResult)
                {
                    record.Enqueue($"script {scriptName} self={objectSelf}");
                    conditionalResult = false;
                    switch (scriptName)
                    {
                        case "act": return true;
                        case "cond_t": conditionalResult = true; return true;
                        case "cond_f": return true;
                        case "throw": throw new InvalidOperationException("dispatcher failed");
                        default: return false;
                    }
                }
            }
        }
        """;

    /// <summary>
    /// Compiles the fixture build into a new folder under <paramref name="root"/> and returns the assembly's path. With
    /// <paramref name="requiredSchemaVersion"/> the build declares that server migration version; otherwise it keeps the
    /// contract's default (0).
    /// </summary>
    public static string Build(string root, bool failActivation = false, bool failValidation = false, int? requiredSchemaVersion = null)
    {
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        return BuildInto(directory, "HostFixture.dll", failActivation, failValidation, requiredSchemaVersion);
    }

    /// <summary>
    /// Compiles the fixture build into <paramref name="directory"/> as <paramref name="fileName"/> (a reload candidate's
    /// folder and the game assembly's file name) and returns the assembly's path. The assembly keeps its own name; the host
    /// loads it from bytes.
    /// </summary>
    public static string BuildInto(string directory, string fileName, bool failActivation = false, bool failValidation = false,
        int? requiredSchemaVersion = null)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);

        var symbols = new List<string>();
        if (failActivation)
            symbols.Add("FAIL_ACTIVATE");
        if (failValidation)
            symbols.Add("FAIL_VALIDATE");
        var source = Source;
        if (requiredSchemaVersion is { } version)
        {
            symbols.Add("REQUIRED_SCHEMA");
            source = source.Replace("REQUIRED_SCHEMA_VERSION", version.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: symbols));
        var compilation = CSharpCompilation.Create(
            "HostFixture",
            [tree],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        using var output = File.Create(path);
        var result = compilation.Emit(output);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            throw new InvalidOperationException("The fixture build did not compile: " + string.Join(Environment.NewLine, errors));
        }

        return path;
    }

    private static IEnumerable<MetadataReference> References()
    {
        var framework = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => Path.GetDirectoryName(path) == RuntimeEnvironment.GetRuntimeDirectory().TrimEnd(Path.DirectorySeparatorChar));

        return framework
            .Append(typeof(IGameplayGeneration).Assembly.Location)
            .Append(typeof(IHostCallbacks).Assembly.Location)
            .Append(typeof(NWN.Core.ICoreFunctionHandler).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
