using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using Nwn.Hosting.Bootstrap;
using Nwn.Hosting.Reload;
using Nwn.Hosting.Testing.Fixtures;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.Host.Services;
using SWLOR.Game.Host.Tests.Support;
using SWLOR.Game.NativeHooks;
using static Nwn.Hosting.Testing.Support.TestAssertions;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>
/// Hot reload is only safe when a retired game build can be unloaded: anything outside the build that still points into
/// it keeps the whole build in memory, and reloads would grow the server without bound. This loads the real Release
/// build of the game through the package's <see cref="ReloadHost"/> with SWLOR's load options 25 times, each time a new
/// collectible generation that validates, activates, binds its native sinks, runs every script of its handler manifest
/// and a few frames under the engine fakes, and is then retired by the next reload (the last by the host's dispose).
/// Every retired generation must be collected.
/// </summary>
/// <remarks>
/// The runtime never tiers code of a collectible context, so each generation compiles every method it runs fully
/// optimized (about 25 s of JIT per generation here). The project's runsettings and <c>scripts/ci/run-unit-tests.sh</c>
/// set <c>DOTNET_JITMinOpts=1</c> for this project, which compiles it as tier 0 would (about 3 s). Unoptimized code only
/// keeps locals alive longer, so it can make a generation look retained, never hide one that is.
/// </remarks>
[TestFixture]
public class RealBuildReloadTests
{
    private const int Generations = 25;
    private const int FramesPerGeneration = 3;
    private const int PendingClosuresPerGeneration = 3;
    private const uint ObjectInvalid = 0x7F000000;

    // Process settings the game build reads; the test points them at its own folder and puts them back.
    private static readonly string[] GameEnvironment = ["SWLOR_APP_LOG_DIRECTORY", "GAME_SERVER_CONTEXT"];

    private string _root = null!;
    private Dictionary<string, string?> _environment = null!;
    private TextWriter _consoleOut = null!;
    private TextWriter _consoleError = null!;
    private StreamWriter? _console;

    [SetUp]
    public void CreateRoot()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-host-reload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _environment = GameEnvironment.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        _consoleOut = Console.Out;
        _consoleError = Console.Error;
    }

    [TearDown]
    public void RestoreProcess()
    {
        Console.SetOut(_consoleOut);
        Console.SetError(_consoleError);
        _console?.Dispose();
        foreach (var (name, value) in _environment)
            Environment.SetEnvironmentVariable(name, value);

        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Passed)
        {
            try
            {
                Directory.Delete(_root, true);
            }
            catch (IOException)
            {
                // A load context that is still releasing its images keeps a file mapped; the temp folder is the OS's.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        else
        {
            TestContext.Out.WriteLine($"The game's logs and console output are in {_root}.");
        }
    }

    [Test]
    public async Task Every_retired_generation_of_the_real_game_build_is_collected_after_25_reloads()
    {
        var build = RealGameBuild.Locate();
        var gameAssembly = build.CopyTo(Path.Combine(_root, "game"));
        var scripts = RealGameBuild.ManifestScripts(gameAssembly);
        Require(scripts.Count(script => script.HasHandlers) > 200, $"The manifest of {gameAssembly} lists too few scripts ({scripts.Count}).");

        // The game's file logs and console go to the test's folder, not the test project's.
        Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", Path.Combine(_root, "logs") + Path.DirectorySeparatorChar);
        _console = new StreamWriter(Path.Combine(_root, "console.log")) { AutoFlush = true };
        var console = TextWriter.Synchronized(_console);
        Console.SetOut(console);
        Console.SetError(console);

        var fakes = new GenerationEngineFakes(build.FakesAssemblyPath, HostOptions.ContextNamePrefix);
        await using var engine = new FixtureNativeBindings();
        var native = new FakeInstallingBindings(engine, fakes);
        var closures = new CoreFunctionHandler(native.EngineThreadId, null, new RecordingClosureScheduler(), RejectedClosureHandling.Drop);
        var callbacks = new HostCallbacks();
        var hooks = new RecordingHookControl();
        var sinks = new NativeHookTable();
        var services = new HostServices()
            .Add<global::NWN.Core.ICoreFunctionHandler>(closures)
            .Add<IRedisConnectionSource>(new FakeRedisConnectionSource())
            .Add<IHostCallbacks>(callbacks)
            .Add<IHookControl>(hooks)
            .Add<INativeSinkBinding>(sinks);
        var settings = HostSettings.Read(
            name => name == HostSettings.CollectibleVariable ? "true" : null,
            Path.GetDirectoryName(gameAssembly)!);
        var host = new ReloadHost(native, services, HostOptions.LoadOptions(settings), closures);

        var reports = new List<ReloadReport>();
        var scriptTimes = new List<double>();
        var handledByFirst = 0;
        var heapAfterFirst = 0L;
        var heapAfterLast = 0L;
        ulong frame = 0;
        await using (host)
        {
            for (var generation = 1; generation <= Generations; generation++)
            {
                var jitBefore = JitInfo.GetCompilationTime();
                var gcBefore = GC.GetTotalPauseDuration();
                var report = await host.ReloadAsync(gameAssembly, DrainTimeout);
                Require(report.Outcome == ReloadOutcome.Applied && report.ActiveGeneration == generation,
                    $"Generation {generation} did not activate ({report.Outcome}): {report.Detail}");
                reports.Add(report);
                FailedNativeInitializer.Retouch();

                var handled = 0;
                var dispatch = Stopwatch.StartNew();
                await native.RunOnEngineThreadAsync(() =>
                {
                    RequireEverySinkBound(generation);
                    foreach (var script in scripts)
                    {
                        if (host.DispatchScriptResult(script.Name, ObjectInvalid) != NativeHost.ScriptNotHandled)
                            handled++;
                    }

                    for (var tick = 0; tick < FramesPerGeneration; tick++)
                    {
                        callbacks.BeginFrame(++frame);
                        host.PumpMainLoop();
                    }

                    // Closures still pending when the generation retires (the engine fakes keep the game's own): scheduled
                    // through NWN.Core as game code schedules them, so the generation's closure manager wraps each one in
                    // its own code. The host must drop them when it retires the generation.
                    global::NWN.Core.NWScript.DelayCommand(60f, () => { });
                    global::NWN.Core.NWScript.AssignCommand(ObjectInvalid, () => { });
                    global::NWN.Core.NWScript.ActionDoCommand(() => { });
                });
                dispatch.Stop();
                scriptTimes.Add(dispatch.Elapsed.TotalMilliseconds);
                if (generation == 1)
                    handledByFirst = handled;
                Require(handled > 0 && handled == handledByFirst,
                    $"Generation {generation} handled {handled} of the {scripts.Count} scripts of its manifest, generation 1 {handledByFirst}.");
                Require(callbacks.HasSignalHandler, $"Generation {generation} bound no signal handler.");
                Require(closures.PendingCount == PendingClosuresPerGeneration,
                    $"Generation {generation} left {closures.PendingCount} closures pending, not {PendingClosuresPerGeneration}.");

                if (generation == 1)
                    heapAfterFirst = SettledHeap();
                else if (generation == Generations)
                    heapAfterLast = SettledHeap();

                TestContext.Progress.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"generation={generation} validateMs={report.CandidateValidation.TotalMilliseconds:F0} " +
                    $"activateMs={report.ApplyPause.TotalMilliseconds:F0} scriptsMs={dispatch.ElapsedMilliseconds} handled={handled} " +
                    $"jitMs={(JitInfo.GetCompilationTime() - jitBefore).TotalMilliseconds:F0} gcPauseMs={(GC.GetTotalPauseDuration() - gcBefore).TotalMilliseconds:F0}"));
            }
        }

        // The host's dispose retired the last generation; its seam overrides come off at the next engine-thread call.
        await native.RunOnEngineThreadAsync(RequireNoSinkBound);
        FailedNativeInitializer.Retouch();
        Require(!callbacks.HasSignalHandler, "A retired generation's signal handler is still bound.");
        Require(closures.PendingCount == 0, $"{closures.PendingCount} closures of retired generations are still pending.");
        var dropped = native.Reports.Where(entry => entry.Category == "closures-dropped").Select(entry => entry.Message).ToArray();
        Require(dropped.Length == Generations && dropped.All(message => message.EndsWith($"dropped={PendingClosuresPerGeneration}", StringComparison.Ordinal)),
            $"The host dropped the pending closures of {dropped.Length} of {Generations} retired generations: {string.Join("; ", dropped)}");
        Require(fakes.Contexts.Count == Generations, $"{fakes.Contexts.Count} game load contexts were seen for {Generations} generations.");

        // Probed after the reload call sites are left, as the package requires.
        var probe = await host.ProbeRetiredContextsAsync();
        RequireNoRetainedContexts(probe, $"{Generations} reloads of the real game build");
        Require(probe.ProbedContexts == Generations - 1, $"{probe.ProbedContexts} retired contexts were probed, not {Generations - 1}.");
        foreach (var context in fakes.Contexts)
            Require(await LoadedGeneration.ProbeCollectedAsync(context), "A generation's load context was not collected after the host's dispose.");

        var validate = reports.Select(report => report.CandidateValidation.TotalMilliseconds).ToArray();
        var activate = reports.Select(report => report.ApplyPause.TotalMilliseconds).ToArray();
        TestContext.Progress.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"METRIC generations={Generations} scripts={scripts.Count} seams={fakes.SeamsPerGeneration} hooksEnabled={hooks.Enabled.Count} " +
            $"validateMs median={Median(validate):F0} max={validate.Max():F0} activateMs median={Median(activate):F0} max={activate.Max():F0} " +
            $"scriptsMs median={Median(scriptTimes.ToArray()):F0} max={scriptTimes.Max():F0} " +
            $"heapAfterFirst={heapAfterFirst} heapAfterLast={heapAfterLast} retained={probe.RetainedContexts} " +
            $"JITMinOpts={Environment.GetEnvironmentVariable("DOTNET_JITMinOpts") ?? "unset"}"));

        // A generation that stayed reachable would add its whole build each time; this bound only catches growth of that kind.
        Require(heapAfterLast < heapAfterFirst * 2,
            $"The managed heap grew from {heapAfterFirst} bytes after generation 1 to {heapAfterLast} after generation {Generations}.");
    }

    private static void RequireEverySinkBound(int generation)
    {
        foreach (var (name, sink) in BoundSinks())
            Require(sink != null, $"Generation {generation} did not bind its {name} sink.");
    }

    private static void RequireNoSinkBound()
    {
        foreach (var (name, sink) in BoundSinks())
            Require(sink == null, $"The {name} hook still forwards to a retired generation's sink.");
    }

    private static IEnumerable<(string Name, object? Sink)> BoundSinks()
    {
        yield return (nameof(NativeSinkSet.ChatPortraitSpeakers), ChatPortraitSpeakersTrampolines.Sink);
        yield return (nameof(NativeSinkSet.ClientJsonUtf8Guard), ClientJsonUtf8GuardTrampolines.Sink);
        yield return (nameof(NativeSinkSet.CombatRecorderEffectHooks), CombatRecorderEffectHooksTrampolines.Sink);
        yield return (nameof(NativeSinkSet.GetDamageRoll), GetDamageRollTrampolines.Sink);
        yield return (nameof(NativeSinkSet.LegacyItemProperties), LegacyItemPropertiesTrampolines.Sink);
        yield return (nameof(NativeSinkSet.NeckAppearanceSerialization), NeckAppearanceSerializationTrampolines.Sink);
        yield return (nameof(NativeSinkSet.OnAIActionAttackObject), OnAIActionAttackObjectTrampolines.Sink);
        yield return (nameof(NativeSinkSet.PlayerCreation), PlayerCreationTrampolines.Sink);
        yield return (nameof(NativeSinkSet.PlayerFileCheckpoint), PlayerFileCheckpointTrampolines.Sink);
        yield return (nameof(NativeSinkSet.PlayerListNamePrivacy), PlayerListNamePrivacyTrampolines.Sink);
        yield return (nameof(NativeSinkSet.PlayerSlotAssignment), PlayerSlotAssignmentTrampolines.Sink);
        yield return (nameof(NativeSinkSet.QuickbarFeatLoading), QuickbarFeatLoadingTrampolines.Sink);
        yield return (nameof(NativeSinkSet.ResolveAttackRoll), ResolveAttackRollTrampolines.Sink);
        yield return (nameof(NativeSinkSet.SavedPlayerInventory), SavedPlayerInventoryTrampolines.Sink);
        yield return (nameof(NativeSinkSet.StaleDriveAfterJump), StaleDriveAfterJumpTrampolines.Sink);
        yield return (nameof(NativeSinkSet.VitalityHitPoints), VitalityHitPointsTrampolines.Sink);
        yield return (nameof(NativeSinkSet.WeaponAttackAnimation), WeaponAttackAnimationTrampolines.Sink);
    }

    private static long SettledHeap()
    {
        for (var pass = 0; pass < 3; pass++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return GC.GetTotalMemory(forceFullCollection: true);
    }

    private static double Median(double[] values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
