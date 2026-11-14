using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>The host rule (P4-10): the permanent host references nothing of the game.</summary>
[TestFixture]
public class HostRulesTests
{
    private static readonly string[] AllowedHostReferences =
    [
        "NWN.Core", "NWN.Native", "NWNX.NET", "Nwn.Hosting", "Nwn.Hosting.Contracts", "SWLOR.Game.Contracts", "SWLOR.Game.NativeHooks",
        "StackExchange.Redis"
    ];

    [Test]
    public void A_host_reference_to_the_game_is_reported_and_the_allowed_ones_are_not()
    {
        var problems = HostRules.HostReferenceViolations(
            ["System.Runtime", "netstandard", "mscorlib", "NWN.Core", "Nwn.Hosting", "SWLOR.Game.Contracts", "SWLOR.Game.Server", "SWLOR.NWN.API", "Serilog"],
            AllowedHostReferences);

        problems.Should().HaveCount(3);
        problems.Should().Contain(problem => problem.Contains("references SWLOR.Game.Server:"));
        problems.Should().Contain(problem => problem.Contains("references SWLOR.NWN.API:"));
        problems.Should().Contain(problem => problem.Contains("references Serilog:"));
    }

    [Test]
    public void The_host_assembly_references_nothing_of_the_game()
    {
        var references = NativeHookScanner.ReadAssemblyReferences(ArchitectureProject.HostAssemblyPath);

        references.Should().Contain("Nwn.Hosting", "the scan reads the compiled host assembly");
        var problems = HostRules.HostReferenceViolations(references, AllowedHostReferences);
        if (problems.Count > 0)
            Assert.Fail($"References of the host assembly ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }
}
