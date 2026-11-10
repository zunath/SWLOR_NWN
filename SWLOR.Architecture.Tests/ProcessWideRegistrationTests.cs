using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Fixtures.Rules;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// Nothing in the server subscribes to a process-wide event or starts a thread outside a short allow list (P4-5), so a
/// retired build can let go of everything that points into it. The fixtures prove each side of the rule; the last tests
/// apply it to the real server.
/// </summary>
[TestFixture]
public class ProcessWideRegistrationTests
{
    private const string Core = "SWLOR.Game.Server.Core";

    /// <summary>The one type that subscribes to the process: it holds the two subscriptions the server keeps.</summary>
    private static readonly TypeKey[] Subscribers = [new TypeKey(Core, "AppDomainProcessEventSource")];

    /// <summary>No type may start a thread: the combat record writer runs on the pool since P4-6.</summary>
    private static readonly TypeKey[] ThreadStarters = [];

    private static readonly AssemblyFacts FixtureFacts = AssemblyFactScanner.Scan(typeof(ProcessWideRegistrationTests).Assembly.Location);

    private static IReadOnlyList<string> FindInFixtures() => ProcessWideRegistrationRule.Find(FixtureFacts, [], []);

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);

    [TestCase(typeof(ProcessExitSubscriber), "System.AppDomain.ProcessExit")]
    [TestCase(typeof(UnhandledExceptionSubscriber), "System.AppDomain.UnhandledException")]
    [TestCase(typeof(LoadContextUnloadingSubscriber), "AssemblyLoadContext.Unloading")]
    [TestCase(typeof(CancelKeyPressSubscriber), "System.Console.CancelKeyPress")]
    [TestCase(typeof(UnobservedTaskSubscriber), "TaskScheduler.UnobservedTaskException")]
    public void Subscribing_to_a_process_wide_event_is_reported(Type offender, string subscribedTo)
    {
        FindInFixtures().Should().Contain(problem => problem.StartsWith(KeyOf(offender).ToString() + " ") && problem.Contains(subscribedTo));
    }

    [Test]
    public void Starting_a_thread_is_reported()
    {
        FindInFixtures().Should().Contain(problem => problem.StartsWith(KeyOf(typeof(ThreadStarter)).ToString() + " ") && problem.Contains("starts a thread"));
    }

    [Test]
    public void Using_the_same_types_without_subscribing_or_starting_a_thread_is_not_reported()
    {
        FindInFixtures().Should().NotContain(problem => problem.StartsWith(KeyOf(typeof(ProcessNeutralUser)).ToString() + " "));
    }

    [Test]
    public void A_type_on_the_allow_list_is_not_reported()
    {
        var problems = ProcessWideRegistrationRule.Find(
            FixtureFacts,
            [KeyOf(typeof(ProcessExitSubscriber))],
            [KeyOf(typeof(ThreadStarter))]);

        problems.Should().NotContain(problem => problem.StartsWith(KeyOf(typeof(ProcessExitSubscriber)).ToString() + " "));
        problems.Should().NotContain(problem => problem.StartsWith(KeyOf(typeof(ThreadStarter)).ToString() + " "));
        problems.Should().Contain(problem => problem.StartsWith(KeyOf(typeof(UnhandledExceptionSubscriber)).ToString() + " "), "the allow list is per type");
    }

    [Test]
    public void The_server_subscribes_to_the_process_only_where_the_allow_list_says()
    {
        ArchitectureProject.Facts.ExternalMemberUses.Should().Contain(
            use => use.Source == Subscribers[0] && use.ExternalType == "System.AppDomain" && use.MemberName == "add_ProcessExit",
            "the rule sees the kept subscriptions, so an empty result is not a blind scan");

        var problems = ProcessWideRegistrationRule.Find(ArchitectureProject.Facts, Subscribers, ThreadStarters)
            .Where(problem => problem.Contains("subscribes to"))
            .ToList();

        Fail(problems, "Process-wide event subscriptions outside the allow list");
    }

    [Test]
    public void The_server_starts_a_thread_only_where_the_allow_list_says()
    {
        // The fixture test above proves the rule sees a thread start, so an empty result here is not a blind scan.
        var problems = ProcessWideRegistrationRule.Find(ArchitectureProject.Facts, Subscribers, ThreadStarters)
            .Where(problem => problem.Contains("starts a thread"))
            .ToList();

        Fail(problems, "Threads started in the server (the allow list is empty)");
    }

    private static void Fail(IReadOnlyList<string> problems, string headline)
    {
        if (problems.Count == 0)
            return;

        Assert.Fail($"{headline} ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }
}
