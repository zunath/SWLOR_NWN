using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.Game.Server.EngineTests
{
    /// <summary>
    /// In-engine integration test runner. When the server boots with
    /// SWLOR_ENGINE_TESTS_ENABLED=true (and is not a production environment),
    /// all [EngineTest] methods are discovered and executed inside the live
    /// NWN server after module load. Results are written to a JSON report and
    /// echoed to the console for CI log parsing, then the server shuts down.
    /// </summary>
    public static class EngineTest
    {
        private const string ConsolePrefix = "[ENGINE_TESTS]";
        private const string ReportFileName = "engine-test-results.json";
        private const float TimeoutGraceSeconds = 10f;

        private static bool _hasRun;
        private static bool _suiteAborted;

        /// <summary>
        /// Aborts the remainder of the suite after the current test finishes. For test
        /// infrastructure that detects a SYSTEMIC failure (e.g. a behavior sweep hitting its
        /// failure threshold - a broken shared fixture would repeat the same timed-out
        /// failures in every remaining tree and blow the CI job budget before the report
        /// is written). The current test still completes and reports normally.
        /// </summary>
        public static void RequestSuiteAbort(string reason)
        {
            if (_suiteAborted)
                return;

            _suiteAborted = true;
            Console.WriteLine($"{ConsolePrefix} WARNING - suite abort requested: {reason}");
            Log.Write(LogGroup.EngineTest, $"Suite abort requested: {reason}", true);
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleEngineTests()
        {
            var settings = ApplicationSettings.Get();
            if (!settings.EngineTestsEnabled)
                return;

            if (settings.ServerEnvironment == ServerEnvironmentType.Production)
            {
                Log.Write(LogGroup.Error, $"{ConsolePrefix} Engine tests are enabled but the environment is Production. Refusing to run.", true);
                return;
            }

            // Fail closed: a mistyped SWLOR_ENVIRONMENT (e.g. 'prodction') resolves to
            // Development, which must never be enough to run destructive tests on what may
            // actually be a live server. Only an explicitly recognized dev/test value runs.
            if (!settings.ServerEnvironmentIsExplicit)
            {
                Log.Write(LogGroup.Error, $"{ConsolePrefix} Engine tests are enabled but SWLOR_ENVIRONMENT is missing or unrecognized. Refusing to run - set it explicitly to 'dev' or 'test'.", true);
                return;
            }

            if (_hasRun)
                return;
            _hasRun = true;

            var delay = settings.EngineTestStartupDelaySeconds;
            Console.WriteLine($"{ConsolePrefix} Engine tests scheduled to start in {delay} seconds.");
            DelayCommand(delay, () => _ = RunAllTestsAsync());
        }

        private static async Task RunAllTestsAsync()
        {
            var settings = ApplicationSettings.Get();
            var report = new EngineTestReport
            {
                StartedUtc = DateTime.UtcNow
            };

            var arena = OBJECT_INVALID;
            Location spawnLocation = null;
            var arenaIsInstanced = false;

            try
            {
                _suiteAborted = false;
                var tests = DiscoverTests(settings.EngineTestFilter);
                Console.WriteLine($"{ConsolePrefix} Discovered {tests.Count} engine test(s).");

                (arena, spawnLocation, arenaIsInstanced) = ResolveArena(settings.EngineTestArenaResref);

                // CreateArea's initialization scripts (for the area and everything placed in it)
                // only run after the creating script yields. Let the instanced arena settle
                // before the first test acts inside it.
                await NwTask.Delay(TimeSpan.FromSeconds(1));

                for (var index = 0; index < tests.Count; index++)
                {
                    var (method, attribute) = tests[index];
                    Console.WriteLine($"{ConsolePrefix} RUN [{index + 1}/{tests.Count}] {attribute.Name}");

                    var result = await RunSingleTestAsync(method, attribute, arena, spawnLocation);
                    report.Results.Add(result);

                    var marker = result.Outcome switch
                    {
                        EngineTestOutcome.Passed => "PASS",
                        EngineTestOutcome.Skipped => "SKIP",
                        _ => "FAIL"
                    };
                    var suffix = string.IsNullOrWhiteSpace(result.Message) ? string.Empty : $" - {result.Message}";
                    Console.WriteLine($"{ConsolePrefix} [{index + 1}/{tests.Count}] {marker} {result.Name} ({result.DurationMilliseconds}ms) - {tests.Count - index - 1} test(s) remaining{suffix}");

                    if (_suiteAborted)
                    {
                        // Isolation is gone - either a timed-out test never settled after
                        // cancellation (and may still be running against the shared arena) or
                        // cleanup failed and left objects behind. Any further results would be
                        // untrustworthy - skip the remainder instead of producing noise.
                        var abortMessage = $"Suite aborted after '{attribute.Name}' ({result.Message}); this test did not run.";
                        Console.WriteLine($"{ConsolePrefix} ABORT - {abortMessage}");
                        Log.Write(LogGroup.EngineTest, abortMessage, true);

                        for (var remaining = index + 1; remaining < tests.Count; remaining++)
                        {
                            var (_, remainingAttribute) = tests[remaining];
                            report.Results.Add(new EngineTestResult
                            {
                                Name = remainingAttribute.Name,
                                Category = remainingAttribute.Category,
                                Outcome = EngineTestOutcome.Skipped,
                                Message = abortMessage
                            });
                            Console.WriteLine($"{ConsolePrefix} SKIP {remainingAttribute.Name} (0ms) - {abortMessage}");
                        }

                        break;
                    }

                    // Give the engine a frame between tests so destroyed objects are fully removed.
                    await NwTask.NextFrame();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ConsolePrefix} CRASH - The test runner itself failed: {ex}");
                Log.Write(LogGroup.EngineTest, $"Test runner crashed: {ex}", true);
                report.Results.Add(new EngineTestResult
                {
                    Name = "EngineTestRunner",
                    Category = "Runner",
                    Outcome = EngineTestOutcome.Failed,
                    Message = $"Test runner crashed: {ex.Message}"
                });
            }
            finally
            {
                // A server kept alive for debugging (SWLOR_ENGINE_TEST_SHUTDOWN=false) must not
                // retain the hidden instanced arena and its contents indefinitely. The fallback
                // case runs in the real starting area, which is never destroyed.
                if (arenaIsInstanced && GetIsObjectValid(arena))
                {
                    DestroyArea(arena);
                }
            }

            report.FinishedUtc = DateTime.UtcNow;
            report.Total = report.Results.Count;
            report.Passed = report.Results.Count(r => r.Outcome == EngineTestOutcome.Passed);
            report.Failed = report.Results.Count(r => r.Outcome == EngineTestOutcome.Failed);
            report.Skipped = report.Results.Count(r => r.Outcome == EngineTestOutcome.Skipped);

            WriteReport(settings, report);

            Console.WriteLine($"{ConsolePrefix} SUMMARY total={report.Total} passed={report.Passed} failed={report.Failed} skipped={report.Skipped}");
            Console.WriteLine($"{ConsolePrefix} COMPLETE");

            if (settings.EngineTestShutdownOnCompletion)
            {
                // NwTask, not DelayCommand: this method runs as an async continuation with no
                // valid OBJECT_SELF, and DelayCommand callbacks scheduled from such a context
                // never fire. The shutdown call itself is assigned to the module for the same
                // reason - it needs a valid object script context to take effect.
                await NwTask.Delay(TimeSpan.FromSeconds(3));
                Console.WriteLine($"{ConsolePrefix} Shutting down server.");
                AssignCommand(GetModule(), () => AdministrationPlugin.ShutdownServer());
            }
        }

        private static List<(MethodInfo Method, EngineTestAttribute Attribute)> DiscoverTests(string filter)
        {
            var tests = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                })
                // Deliberately wider than the supported (public static) surface: a misplaced
                // [EngineTest] on a private or instance method must surface as a failed
                // result from IsValidTestMethod, not silently vanish from the run.
                .SelectMany(t => t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly))
                .Select(m => (Method: m, Attribute: m.GetCustomAttribute<EngineTestAttribute>()))
                .Where(x => x.Attribute != null)
                .OrderBy(x => x.Attribute.Category)
                .ThenBy(x => x.Attribute.Name)
                .ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var filters = filter.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                tests = tests
                    .Where(x => filters.Any(term =>
                        x.Attribute.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        x.Attribute.Category.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return tests;
        }

        private static (uint Arena, Location SpawnLocation, bool IsInstanced) ResolveArena(string arenaResrefOverride)
        {
            var startingLocation = GetStartingLocation();
            var startingArea = GetAreaFromLocation(startingLocation);
            var hasOverride = !string.IsNullOrWhiteSpace(arenaResrefOverride);
            var arenaResref = hasOverride
                ? arenaResrefOverride
                : GetResRef(startingArea);

            var isInstanced = true;
            var arena = CreateArea(arenaResref);
            if (!GetIsObjectValid(arena))
            {
                Console.WriteLine($"{ConsolePrefix} WARNING - Could not instance arena from resref '{arenaResref}'. Falling back to the module starting area.");
                Log.Write(LogGroup.EngineTest, $"Could not instance arena from resref '{arenaResref}'. Falling back to the module starting area.", true);
                arena = startingArea;
                hasOverride = false;
                isInstanced = false;
            }

            // The module entry position is only meaningful inside (a copy of) the starting area.
            // An override arena anchors at its geometric center instead (tiles are 10m square;
            // creatures are placed on the ground at that XY) - pick a small, flat area when
            // overriding.
            var spawnPosition = hasOverride
                ? Vector3(
                    GetAreaSize(Dimension.Width, arena) * 10f / 2f,
                    GetAreaSize(Dimension.Height, arena) * 10f / 2f,
                    0f)
                : GetPositionFromLocation(startingLocation);

            var spawnLocation = Location(arena, spawnPosition, 0f);
            return (arena, spawnLocation, isInstanced);
        }

        private static async Task<EngineTestResult> RunSingleTestAsync(
            MethodInfo method,
            EngineTestAttribute attribute,
            uint arena,
            Location spawnLocation)
        {
            var result = new EngineTestResult
            {
                Name = attribute.Name,
                Category = attribute.Category,
                Outcome = EngineTestOutcome.Passed,
                Message = string.Empty
            };

            var context = new EngineTestContext(attribute.Name, arena, spawnLocation);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!IsValidTestMethod(method))
                {
                    result.Outcome = EngineTestOutcome.Failed;
                    result.Message = $"Invalid signature on {method.DeclaringType?.Name}.{method.Name}: engine tests must be public static, take a single EngineTestContext parameter, and return Task (synchronous void bodies run outside the cooperative timeout's reach; async void is not observable).";
                    return result;
                }

                var testTask = InvokeTestAsync(method, context);
                var timeoutTask = NwTask.Delay(TimeSpan.FromSeconds(attribute.TimeoutSeconds));
                await NwTask.WhenAny(testTask, timeoutTask);

                if (!testTask.IsCompleted)
                {
                    // Cooperative cancellation: the context's wait helpers throw on their next
                    // poll once cancelled. Give the test a grace window to unwind BEFORE cleanup
                    // runs and the next test starts, so a timed-out test cannot keep acting on
                    // the shared arena underneath its successor.
                    context.CancelTest();
                    var settled = testTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.ExecuteSynchronously);
                    await NwTask.WhenAny(settled, NwTask.Delay(TimeSpan.FromSeconds(TimeoutGraceSeconds)));

                    result.Outcome = EngineTestOutcome.Failed;
                    result.Message = testTask.IsCompleted
                        ? $"Timed out after {attribute.TimeoutSeconds} seconds (cancelled)."
                        : $"Timed out after {attribute.TimeoutSeconds} seconds and did not stop within the {TimeoutGraceSeconds}s cancellation grace period - the suite will abort because later results could not be trusted.";

                    if (!testTask.IsCompleted)
                    {
                        // The stuck task may still be acting on the shared arena; running more
                        // tests beside it would produce untrustworthy results. Signal the outer
                        // loop to abort the remainder of the suite.
                        _suiteAborted = true;
                        Console.WriteLine($"{ConsolePrefix} WARNING - {result.Message}");
                        Log.Write(LogGroup.EngineTest, $"[{attribute.Name}] {result.Message}", true);
                    }

                    return result;
                }

                await testTask;

                if (!string.IsNullOrWhiteSpace(context.ResultDetail))
                {
                    result.Message = context.ResultDetail;
                }
            }
            catch (Exception ex)
            {
                var unwrapped = Unwrap(ex);
                switch (unwrapped)
                {
                    case EngineTestSkippedException skipped:
                        result.Outcome = EngineTestOutcome.Skipped;
                        result.Message = skipped.Message;
                        break;
                    case EngineTestAssertionException assertion:
                        result.Outcome = EngineTestOutcome.Failed;
                        result.Message = assertion.Message;
                        break;
                    case OperationCanceledException:
                        result.Outcome = EngineTestOutcome.Failed;
                        result.Message = $"Timed out after {attribute.TimeoutSeconds} seconds (cancelled).";
                        break;
                    default:
                        result.Outcome = EngineTestOutcome.Failed;
                        result.Message = $"{unwrapped.GetType().Name}: {unwrapped.Message}";
                        Log.Write(LogGroup.EngineTest, $"[{attribute.Name}] Unhandled exception: {unwrapped}", true);
                        break;
                }
            }
            finally
            {
                stopwatch.Stop();
                result.DurationMilliseconds = stopwatch.ElapsedMilliseconds;

                try
                {
                    context.Cleanup();
                }
                catch (Exception cleanupEx)
                {
                    // Leftover objects/areas mean isolation is gone: this result can't be
                    // trusted even if the test itself passed, and neither can later tests.
                    _suiteAborted = true;
                    result.Outcome = EngineTestOutcome.Failed;
                    var cleanupMessage = $"Cleanup failed after the test ran: {cleanupEx.Message}";
                    result.Message = string.IsNullOrWhiteSpace(result.Message)
                        ? cleanupMessage
                        : $"{result.Message} | {cleanupMessage}";
                    Console.WriteLine($"{ConsolePrefix} WARNING - [{attribute.Name}] {cleanupMessage}");
                    Log.Write(LogGroup.EngineTest, $"[{attribute.Name}] {cleanupMessage}", true);
                }
            }

            return result;
        }

        private static bool IsValidTestMethod(MethodInfo method)
        {
            if (!method.IsStatic || !method.IsPublic)
                return false;

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(EngineTestContext))
                return false;

            // Only Task-returning tests are accepted. async void is unobservable (reflection
            // returns at the first incomplete await and the runner would clean up under a
            // still-running test), and a synchronous void body executes entirely outside the
            // timeout's reach - TimeoutSeconds is cooperative and can only preempt a test at
            // an await. Requiring Task keeps every accepted test timeout-armable; sweep-style
            // tests must additionally yield periodically inside long loops.
            return method.ReturnType == typeof(Task);
        }

        private static async Task InvokeTestAsync(MethodInfo method, EngineTestContext context)
        {
            // Yield BEFORE invoking so the caller can arm its timeout task first - without
            // this, the test body's synchronous prefix (everything up to its first incomplete
            // await) runs to completion before the timeout task is even created.
            await NwTask.NextFrame();

            var returnValue = method.Invoke(null, new object[] { context });
            if (returnValue is not Task task)
            {
                // A Task-signature method that returns null (e.g. a non-async body written as
                // `return null`) would otherwise complete this wrapper successfully and be
                // recorded as a pass without any body having been awaited.
                throw new EngineTestAssertionException(
                    $"{method.DeclaringType?.Name}.{method.Name} returned {(returnValue == null ? "null" : returnValue.GetType().Name)} instead of a Task - the test body was not observed and cannot be trusted.");
            }

            await task;
        }

        private static Exception Unwrap(Exception ex)
        {
            while (ex is TargetInvocationException { InnerException: not null } tie)
            {
                ex = tie.InnerException;
            }

            return ex;
        }

        private static void WriteReport(ApplicationSettings settings, EngineTestReport report)
        {
            try
            {
                var directory = settings.EngineTestResultsDirectory;
                if (string.IsNullOrWhiteSpace(directory))
                    directory = "./engine_tests/";

                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, ReportFileName);
                File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
                Console.WriteLine($"{ConsolePrefix} Report written to {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ConsolePrefix} WARNING - Failed to write report file: {ex.Message}");
                Log.Write(LogGroup.EngineTest, $"Failed to write report file: {ex}", true);
            }
        }
    }
}
