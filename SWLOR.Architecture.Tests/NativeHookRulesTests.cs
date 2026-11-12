using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Fixtures.Hooks;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Elsewhere;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Sinks;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The native sink rules (P4-9). The fixtures prove each side of every rule; the last tests apply them to the real
/// server and its contracts assembly.
/// </summary>
[TestFixture]
public class NativeHookRulesTests
{
    private const string FixtureSinkNamespace = "SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts";
    private const string FixtureHookNamespace = "SWLOR.Architecture.Tests.Fixtures.Hooks";
    private const string FixtureSinkHome = "SWLOR.Architecture.Tests.Fixtures.Hooks.Sinks";
    private const string ContractsNamespace = "SWLOR.Game.Contracts";
    private const string SinksNamespace = "SWLOR.Game.Server.NativeSinks";
    private const string HooksNamespace = "SWLOR.Game.Server.Native";

    /// <summary>The 36 hooked engine functions: 17 hook files in the server.</summary>
    private const int HookCount = 36;

    /// <summary>The sink interfaces: one per hook file.</summary>
    private const int SinkInterfaceCount = 17;

    private static readonly string[] AllowedContractsReferences = ["Nwn.Hosting.Contracts", "StackExchange.Redis"];

    private static readonly NativeHookFacts FixtureFacts =
        NativeHookScanner.Scan(typeof(NativeHookRulesTests).Assembly.Location, FixtureSinkNamespace);

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);

    [Test]
    public void A_hook_that_only_forwards_to_its_sink_is_not_reported()
    {
        NativeHookRules.HookBodyViolations(FixtureFacts, FixtureHookNamespace)
            .Should().NotContain(problem => problem.StartsWith(KeyOf(typeof(ForwardingHook)) + "::"));
    }

    [TestCase(typeof(HelperCallingHook), "HelperCallingHook::Adjust")]
    [TestCase(typeof(FrameworkCallingHook), "System.Math::Abs")]
    [TestCase(typeof(FunctionPointerHook), "a call through a function pointer")]
    public void A_hook_that_does_work_of_its_own_is_reported(Type offender, string call)
    {
        NativeHookRules.HookBodyViolations(FixtureFacts, FixtureHookNamespace)
            .Should().Contain(problem => problem.StartsWith(KeyOf(offender) + "::Forward calls ") && problem.Contains(call));
    }

    [Test]
    public void A_hook_with_no_sink_is_reported()
    {
        NativeHookRules.HookBodyViolations(FixtureFacts, FixtureHookNamespace)
            .Should().Contain(problem => problem.StartsWith(KeyOf(typeof(NoSinkHook)) + "::Forward calls no sink"));
    }

    [Test]
    public void A_sink_outside_the_sink_namespace_is_reported_and_one_inside_is_not()
    {
        var problems = NativeHookRules.SinkLocationViolations(FixtureFacts, FixtureSinkHome);

        problems.Should().Contain(problem => problem.StartsWith(KeyOf(typeof(MisplacedSink)) + " implements IFixtureSink"));
        problems.Should().NotContain(problem => problem.StartsWith(KeyOf(typeof(HomedSink)) + " "));
    }

    [Test]
    public void The_scanner_finds_the_fixture_sink_interface()
    {
        FixtureFacts.SinkInterfaces.Should().Contain(new TypeKey(FixtureSinkNamespace, "IFixtureSink"));
    }

    [Test]
    public void A_contracts_reference_to_the_game_is_reported_and_the_allowed_ones_are_not()
    {
        var problems = NativeHookRules.ContractsReferenceViolations(
            ["System.Runtime", "System.Collections", "netstandard", "Nwn.Hosting.Contracts", "StackExchange.Redis", "SWLOR.Game.Server", "SWLOR.NWN.API", "Newtonsoft.Json"],
            AllowedContractsReferences);

        problems.Should().HaveCount(3);
        problems.Should().Contain(problem => problem.Contains("references SWLOR.Game.Server:"));
        problems.Should().Contain(problem => problem.Contains("references SWLOR.NWN.API:"));
        problems.Should().Contain(problem => problem.Contains("references Newtonsoft.Json:"));
    }

    [Test]
    public void The_server_hooks_only_forward_to_their_sinks()
    {
        var facts = ArchitectureProject.NativeHooks;

        facts.Hooks.Where(hook => hook.Owner.Namespace == HooksNamespace).Should().HaveCount(HookCount,
            "the rule sees every hooked engine function, so an empty result is not a blind scan");
        Fail(NativeHookRules.HookBodyViolations(facts, HooksNamespace), "Native hooks that do more than forward to their sink");
    }

    [Test]
    public void Every_sink_implementation_of_the_server_lives_in_the_native_sinks_folder()
    {
        var facts = ArchitectureProject.NativeHooks;

        facts.Implementers.Should().HaveCount(SinkInterfaceCount, "each sink interface has exactly one implementation in the server");
        Fail(NativeHookRules.SinkLocationViolations(facts, SinksNamespace), "Sink implementations outside NativeSinks");
    }

    [Test]
    public void Every_sink_interface_has_one_implementation_and_belongs_to_the_contracts_assembly()
    {
        var contracts = NativeHookScanner.Scan(ArchitectureProject.ContractsAssemblyPath, ContractsNamespace);
        var server = ArchitectureProject.NativeHooks;

        contracts.SinkInterfaces.Should().HaveCount(SinkInterfaceCount);
        server.Implementers.Select(implementer => implementer.Interface)
            .Should().BeEquivalentTo(contracts.SinkInterfaces.Select(key => key.Name), "one implementation per sink interface");
    }

    [Test]
    public void The_contracts_assembly_references_nothing_of_the_game()
    {
        var references = NativeHookScanner.ReadAssemblyReferences(ArchitectureProject.ContractsAssemblyPath);

        references.Should().NotBeEmpty("the scan reads the compiled assembly");
        Fail(NativeHookRules.ContractsReferenceViolations(references, AllowedContractsReferences), "References of the contracts assembly");
    }

    private static void Fail(IReadOnlyList<string> problems, string headline)
    {
        if (problems.Count == 0)
            return;

        Assert.Fail($"{headline} ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }
}
