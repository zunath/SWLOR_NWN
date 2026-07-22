using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
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

            try
            {
                var tests = DiscoverTests(settings.EngineTestFilter);
                Console.WriteLine($"{ConsolePrefix} Discovered {tests.Count} engine test(s).");

                var (arena, spawnLocation) = ResolveArena(settings.EngineTestArenaResref);

                foreach (var (method, attribute) in tests)
                {
                    var result = await RunSingleTestAsync(method, attribute, arena, spawnLocation);
                    report.Results.Add(result);

                    var marker = result.Outcome switch
                    {
                        EngineTestOutcome.Passed => "PASS",
                        EngineTestOutcome.Skipped => "SKIP",
                        _ => "FAIL"
                    };
                    var suffix = string.IsNullOrWhiteSpace(result.Message) ? string.Empty : $" - {result.Message}";
                    Console.WriteLine($"{ConsolePrefix} {marker} {result.Name} ({result.DurationMilliseconds}ms){suffix}");

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
                DelayCommand(3f, AdministrationPlugin.ShutdownServer);
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
                tests = tests
                    .Where(x => x.Attribute.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                x.Attribute.Category.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return tests;
        }

        private static (uint Arena, Location SpawnLocation) ResolveArena(string arenaResrefOverride)
        {
            var startingLocation = GetStartingLocation();
            var startingArea = GetAreaFromLocation(startingLocation);
            var arenaResref = string.IsNullOrWhiteSpace(arenaResrefOverride)
                ? GetResRef(startingArea)
                : arenaResrefOverride;

            var arena = CreateArea(arenaResref);
            if (!GetIsObjectValid(arena))
            {
                Console.WriteLine($"{ConsolePrefix} WARNING - Could not instance arena from resref '{arenaResref}'. Falling back to the module starting area.");
                Log.Write(LogGroup.EngineTest, $"Could not instance arena from resref '{arenaResref}'. Falling back to the module starting area.", true);
                arena = startingArea;
            }

            var spawnLocation = Location(arena, GetPositionFromLocation(startingLocation), 0f);
            return (arena, spawnLocation);
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
                    result.Message = $"Invalid signature on {method.DeclaringType?.Name}.{method.Name}: engine tests must be public static, take a single EngineTestContext parameter, and return void or Task.";
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
                        : $"Timed out after {attribute.TimeoutSeconds} seconds and did not stop within the {TimeoutGraceSeconds}s cancellation grace period - subsequent test results may be unreliable.";

                    if (!testTask.IsCompleted)
                    {
                        Console.WriteLine($"{ConsolePrefix} WARNING - {result.Message}");
                        Log.Write(LogGroup.EngineTest, $"[{attribute.Name}] {result.Message}", true);
                    }

                    return result;
                }

                await testTask;
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
                    Log.Write(LogGroup.EngineTest, $"[{attribute.Name}] Cleanup failed: {cleanupEx.Message}", true);
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

            return method.ReturnType == typeof(void) || method.ReturnType == typeof(Task);
        }

        private static async Task InvokeTestAsync(MethodInfo method, EngineTestContext context)
        {
            var returnValue = method.Invoke(null, new object[] { context });
            if (returnValue is Task task)
            {
                await task;
            }
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
