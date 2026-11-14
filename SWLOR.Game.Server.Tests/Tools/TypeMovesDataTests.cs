using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Composition;
using SWLOR.Game.Server.Tests.Core;
using SWLOR.TypeMoves;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Keeps <c>tools/Refactor/TypeMoves.json</c> (P2-Z3) in step with the branch: each type moves to a declared domain, the
/// branch is in the tool's shape (every listed file at its new path, none at its old one, no folder <c>Service</c> left),
/// and each moved handler has its manifest line under the new name with a rename line. Also proves the data file's checks.
/// </summary>
[TestFixture]
public class TypeMovesDataTests
{
    private static string Root => ClockSeamSources.FindRepositoryRoot();

    private static TypeMoveData Data => TypeMoveData.Load(Path.Combine(Root, "tools", "Refactor", "TypeMoves.json"));

    [Test]
    public void Every_type_moves_to_a_domain_of_the_domain_map()
    {
        using var map = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "SWLOR.Architecture.Tests", "DomainMap.json")));
        var domains = map.RootElement.GetProperty("domains").EnumerateObject().Select(domain => "SWLOR.Game.Server.Domain." + domain.Name).ToHashSet(StringComparer.Ordinal);

        Data.From.Should().Be("SWLOR.Game.Server.Service");
        Data.Moves.Should().HaveCount(20);
        Data.Moves.Should().OnlyContain(move => domains.Contains(move.To), "each type joins the root namespace of its domain");
        Data.ByName["INamedAnimationRuntime"].File.Should().Be("NamedAnimationPlayback.cs", "it shares its file with NamedAnimationPlayback");
    }

    [Test]
    public void The_branch_is_in_the_shape_the_tool_leaves()
    {
        foreach (var move in Data.Moves)
        {
            File.Exists(Path.Combine(Root, move.TargetPath)).Should().BeTrue($"{move.Name} lives at {move.TargetPath}");
            File.Exists(Path.Combine(Root, move.SourcePath)).Should().BeFalse($"{move.SourcePath} moved");
        }

        Directory.Exists(Path.Combine(Root, "SWLOR.Game.Server", "Service")).Should().BeFalse("nothing is left in the folder Service");
        typeof(SWLOR.Game.Server.Composition.CompositionRoot).Assembly.GetTypes()
            .Where(type => type.Namespace == Data.From)
            .Should().BeEmpty("no type is declared directly in the namespace Service any more");
    }

    [Test]
    public void Every_moved_handler_keeps_its_manifest_line_under_its_new_name()
    {
        var manifest = File.ReadAllLines(Path.Combine(Root, "SWLOR.Game.Server", "Core", "ScriptHandlerOrder.txt"))
            .Select(line => line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length == 2 && (parts[0] == "action" || parts[0] == "conditional"))
            .Select(parts => parts[1])
            .ToHashSet(StringComparer.Ordinal);
        var renames = ConvertedServiceFixtures.HandlerRenames()
            .Where(rename => Data.Moves.Any(move => rename.Master.StartsWith(move.OldFullName + ".", StringComparison.Ordinal)))
            .ToList();

        renames.Should().Contain(("SWLOR.Game.Server.Service.Log.OnApplicationShutdown", "SWLOR.Game.Server.Domain.Foundation.Log.OnApplicationShutdown"))
            .And.Contain(("SWLOR.Game.Server.Service.FrameProfiler.Load", "SWLOR.Game.Server.Domain.Foundation.FrameProfiler.Load"));
        renames.Should().OnlyContain(rename => manifest.Contains(rename.Branch) && !manifest.Contains(rename.Master));
        manifest.Where(name => Data.Moves.Any(move => name.StartsWith(move.OldFullName + ".", StringComparison.Ordinal)))
            .Should().BeEmpty("no manifest line keeps a moved type's old name");
    }

    [TestCase("{ \"types\": [] }", "\"from\" is missing or empty")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"A\" } ] }", "\"to\" is missing or empty")]
    [TestCase("{ \"from\": \"Elsewhere.Service\", \"types\": [] }", "\"from\" must be a namespace below SWLOR.Game.Server")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\" }", "has no \"types\" list")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"A\", \"to\": \"SWLOR.Game.Server.Service.Inner\" } ] }", "A must move to a namespace below SWLOR.Game.Server outside SWLOR.Game.Server.Service")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"A.B\", \"to\": \"SWLOR.Game.Server.Domain.X\" } ] }", "'A.B' is not a type name")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"A\", \"to\": \"SWLOR.Game.Server.Domain.X\", \"file\": \"Sub/A.cs\" } ] }", "the file of A must be a plain .cs file name")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"A\", \"to\": \"SWLOR.Game.Server.Domain.X\" }, { \"type\": \"A\", \"to\": \"SWLOR.Game.Server.Domain.Y\" } ] }", "lists A more than once")]
    [TestCase("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"A\", \"to\": \"SWLOR.Game.Server.Domain.X\" }, { \"type\": \"B\", \"to\": \"SWLOR.Game.Server.Domain.Y\", \"file\": \"A.cs\" } ] }", "move to different namespaces")]
    [TestCase("not json", "is not valid JSON")]
    public void A_malformed_data_file_is_refused(string json, string expected)
    {
        var parse = () => TypeMoveData.Parse(json, "data.json");

        parse.Should().Throw<TypeMoveException>().Which.Message.Should().Contain(expected);
    }

    [Test]
    public void A_move_knows_its_paths_and_its_relative_namespace()
    {
        var data = TypeMoveData.Parse("{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"Gauge\", \"to\": \"SWLOR.Game.Server.Domain.Gadgets\" } ], \"textExcludes\": [ \"docs/**\", \"a/*.md\" ] }", "data.json");
        var move = data.ByName["Gauge"];

        move.SourcePath.Should().Be("SWLOR.Game.Server/Service/Gauge.cs");
        move.TargetPath.Should().Be("SWLOR.Game.Server/Domain/Gadgets/Gauge.cs");
        move.RelativeTarget.Should().Be("Domain.Gadgets");
        move.OldFullName.Should().Be("SWLOR.Game.Server.Service.Gauge");
        move.NewFullName.Should().Be("SWLOR.Game.Server.Domain.Gadgets.Gauge");
        data.IsExcluded("docs/deep/x.md").Should().BeTrue();
        data.IsExcluded("a/x.md").Should().BeTrue();
        data.IsExcluded("a/b/x.md").Should().BeFalse("* stays within one folder");
    }
}
