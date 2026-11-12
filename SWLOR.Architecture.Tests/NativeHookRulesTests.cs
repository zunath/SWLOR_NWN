using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Fixtures.Hooks;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Elsewhere;
using SWLOR.Architecture.Tests.Fixtures.Hooks.Sinks;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The native hook rules (P4-9, P4-11). The fixtures prove each side of every rule; the last tests apply them to the real
/// hooks assembly, the server and the contracts assembly.
/// </summary>
[TestFixture]
public class NativeHookRulesTests
{
    private const string FixtureSinkNamespace = "SWLOR.Architecture.Tests.Fixtures.Hooks.Contracts";
    private const string FixtureHookNamespace = "SWLOR.Architecture.Tests.Fixtures.Hooks";
    private const string FixtureSinkHome = "SWLOR.Architecture.Tests.Fixtures.Hooks.Sinks";
    private const string ContractsNamespace = "SWLOR.Game.Contracts";
    private const string SinksNamespace = "SWLOR.Game.Server.NativeSinks";
    private const string TrampolineNamespace = "SWLOR.Game.NativeHooks";

    /// <summary>The namespace of the native entry's four NWNX callbacks, the server's only other unmanaged entry points.</summary>
    private const string ServerCallbackNamespace = "SWLOR.Game.Server.Core";

    /// <summary>The 36 hooked engine functions: 17 hook files.</summary>
    private const int HookCount = 36;

    /// <summary>The sink interfaces: one per hook file.</summary>
    private const int SinkInterfaceCount = 17;

    private static readonly string[] AllowedContractsReferences = ["Nwn.Hosting.Contracts", "StackExchange.Redis"];
    private static readonly string[] AllowedHooksReferences = ["NWNX.NET", "SWLOR.Game.Contracts"];

    private static readonly NativeHookFacts FixtureFacts =
        NativeHookScanner.Scan(typeof(NativeHookRulesTests).Assembly.Location, FixtureSinkNamespace);

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);

    private static IReadOnlyList<string> FixtureProblems(Type hook) =>
        NativeHookRules.TrampolineViolations(FixtureFacts, FixtureHookNamespace)
            .Where(problem => problem.StartsWith(KeyOf(hook) + "::", StringComparison.Ordinal))
            .ToList();

    [Test]
    public void A_trampoline_that_reads_its_sink_from_a_volatile_field_and_runs_the_engine_function_without_one_is_not_reported()
    {
        FixtureProblems(typeof(TrampolineHook)).Should().BeEmpty();
    }

    [TestCase(typeof(HelperCallingHook), "HelperCallingHook::Adjust")]
    [TestCase(typeof(FrameworkCallingHook), "System.Math::Abs")]
    public void A_hook_that_does_work_of_its_own_is_reported(Type offender, string call)
    {
        FixtureProblems(offender).Should().Contain(problem => problem.StartsWith(KeyOf(offender) + "::Forward calls ") && problem.Contains(call));
    }

    [Test]
    public void A_hook_with_no_sink_is_reported()
    {
        FixtureProblems(typeof(NoSinkHook)).Should().Contain(problem => problem.Contains("::Forward calls no sink"));
    }

    [Test]
    public void A_hook_that_only_forwards_is_reported_because_it_cannot_run_without_a_sink()
    {
        FixtureProblems(typeof(ForwardingHook)).Should().Contain(problem => problem.Contains("never runs the engine function"));
    }

    [Test]
    public void A_hook_that_reads_its_sink_from_a_plain_field_is_reported()
    {
        FixtureProblems(typeof(FunctionPointerHook)).Should().ContainSingle()
            .Which.Should().Contain("reads no volatile field");
    }

    [Test]
    public void An_entry_point_outside_the_allowed_namespace_is_reported()
    {
        NativeHookRules.EntryPointsOutside(FixtureFacts, "Some.Other.Namespace")
            .Should().Contain(problem => problem.StartsWith(KeyOf(typeof(TrampolineHook)) + "::Forward is an [UnmanagedCallersOnly] entry point"));
        NativeHookRules.EntryPointsOutside(FixtureFacts, FixtureHookNamespace).Should().BeEmpty();
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
    public void A_hooks_reference_to_the_game_is_reported_and_the_allowed_ones_are_not()
    {
        var problems = NativeHookRules.HooksReferenceViolations(
            ["System.Runtime", "NWNX.NET", "SWLOR.Game.Contracts", "SWLOR.Game.Server", "NWN.Native"],
            AllowedHooksReferences);

        problems.Should().HaveCount(2);
        problems.Should().Contain(problem => problem.StartsWith("The native hooks assembly references SWLOR.Game.Server:"));
        problems.Should().Contain(problem => problem.StartsWith("The native hooks assembly references NWN.Native:"));
    }

    [Test]
    public void The_trampolines_forward_to_their_sinks_or_run_the_engine_function()
    {
        var facts = ArchitectureProject.HooksAssemblyHooks;

        facts.Hooks.Where(hook => hook.Owner.Namespace == TrampolineNamespace).Should().HaveCount(HookCount,
            "the rule sees every hooked engine function, so an empty result is not a blind scan");
        facts.Hooks.Should().OnlyContain(hook => hook.Owner.Namespace == TrampolineNamespace);
        Fail(NativeHookRules.TrampolineViolations(facts, TrampolineNamespace), "Trampolines that do more than forward or run the engine function");
    }

    [Test]
    public void The_server_holds_no_hook_entry_point()
    {
        var facts = ArchitectureProject.NativeHooks;

        facts.Hooks.Should().NotBeEmpty("the native entry's NWNX callbacks are seen, so an empty result is not a blind scan");
        Fail(NativeHookRules.EntryPointsOutside(facts, ServerCallbackNamespace), "Unmanaged entry points of the game");
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

    [Test]
    public void The_hooks_assembly_references_nothing_of_the_game()
    {
        var references = NativeHookScanner.ReadAssemblyReferences(ArchitectureProject.HooksAssemblyPath);

        references.Should().Contain("NWNX.NET", "the scan reads the compiled hooks assembly");
        Fail(NativeHookRules.HooksReferenceViolations(references, AllowedHooksReferences), "References of the native hooks assembly");
    }

    private static void Fail(IReadOnlyList<string> problems, string headline)
    {
        if (problems.Count == 0)
            return;

        Assert.Fail($"{headline} ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }
}
