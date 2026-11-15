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
        Data.Moves.Should().OnlyContain(move => domains.Contains(move.To) || move.To == "SWLOR.Game.Server.Core", "each type joins the root namespace of its domain (Log and FrameProfiler: Core, beside Clock)");
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

        renames.Should().Contain(("SWLOR.Game.Server.Service.Log.OnApplicationShutdown", "SWLOR.Game.Server.Core.Log.OnApplicationShutdown"))
            .And.Contain(("SWLOR.Game.Server.Service.FrameProfiler.Load", "SWLOR.Game.Server.Core.FrameProfiler.Load"));
        renames.Should().OnlyContain(rename => manifest.Contains(rename.Branch) && !manifest.Contains(rename.Master));
        manifest.Where(name => Data.Moves.Any(move => name.StartsWith(move.OldFullName + ".", StringComparison.Ordinal)))
            .Should().BeEmpty("no manifest line keeps a moved type's old name");
    }

    [Test]
    public void The_domain_order_moves_are_in_the_shape_the_tool_leaves()
    {
        using var map = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "SWLOR.Architecture.Tests", "DomainMap.json")));
        var targets = map.RootElement.GetProperty("domains").EnumerateObject().Select(domain => "SWLOR.Game.Server.Domain." + domain.Name)
            .Append("SWLOR.Game.Server.Core")
            .ToHashSet(StringComparer.Ordinal);
        var data = TypeMoveData.Load(Path.Combine(Root, "tools", "Refactor", "DomainOrderTypeMoves.json"));

        data.From.Should().BeNull("each entry names the namespace its type leaves");
        data.RemoveUnusedSourceUsings.Should().BeFalse("a file keeps its using of the old namespace (the smallest edit)");
        data.ByName["RecipeType"].Should().Be(new TypeMove("RecipeType", "SWLOR.Game.Server.Domain.Crafting", "SWLOR.Game.Server.Domain.Foundation", "RecipeType.cs"),
            "the pilot of P5-1c (MaintainabilityRefactorDomainOrder.md, Appendix B)");
        data.Moves.Should().OnlyContain(move => targets.Contains(move.To), "each type joins the root namespace of a domain, or Core (Platform)");
        foreach (var move in data.Moves)
        {
            File.Exists(Path.Combine(Root, move.TargetPath)).Should().BeTrue($"{move.Name} lives at {move.TargetPath}");
            File.Exists(Path.Combine(Root, move.SourcePath)).Should().BeFalse($"{move.SourcePath} moved");
        }

        File.ReadAllLines(Path.Combine(Root, "tools", "Refactor", "MechanicalScripts.txt")).Where(line => line.StartsWith("tools/", StringComparison.Ordinal))
            .Should().EndWith(new[] { "tools/Refactor/Move-ServiceTypes.ps1", "tools/Refactor/Move-DomainOrderTypes.ps1" },
                "the domain order's moves run after the namespace moves and the P2-Z3 type moves turned master's names into the branch's");
    }

    [Test]
    public void A_data_file_names_a_source_per_entry_and_moves_across_roots()
    {
        var data = TypeMoveData.Parse("""
            {
              "from": "SWLOR.Game.Server.Service",
              "removeUnusedSourceUsings": false,
              "types": [
                { "type": "Gauge", "to": "SWLOR.Game.Server.Domain.Gadgets" },
                { "type": "Recipe", "from": "SWLOR.Game.Server.Domain.Crafting", "to": "SWLOR.Game.Server.Domain.Foundation" },
                { "type": "HealAmount", "from": "SWLOR.Game.Server.Feature.AbilityDefinition", "to": "SWLOR.Game.Server.Domain.Combat" },
                { "type": "Log", "from": "SWLOR.Game.Server.Domain.Foundation", "to": "SWLOR.Game.Server.Core" },
                { "type": "Lure", "from": "SWLOR.Game.Server.Domain.World.Fishing", "to": "SWLOR.Game.Server.Domain.World" }
              ]
            }
            """, "data.json");

        data.RemoveUnusedSourceUsings.Should().BeFalse();
        data.Sources.Should().BeEquivalentTo(new[] { "SWLOR.Game.Server.Service", "SWLOR.Game.Server.Domain.Crafting", "SWLOR.Game.Server.Feature.AbilityDefinition",
            "SWLOR.Game.Server.Domain.Foundation", "SWLOR.Game.Server.Domain.World.Fishing" });
        data.Moves.Select(move => (move.Name, move.CommonParent, move.RelativeSource, move.RelativeTarget)).Should().Equal(
            ("Gauge", "SWLOR.Game.Server", "Service", "Domain.Gadgets"),
            ("Recipe", "SWLOR.Game.Server.Domain", "Crafting", "Foundation"),
            ("HealAmount", "SWLOR.Game.Server", "Feature.AbilityDefinition", "Domain.Combat"),
            ("Log", "SWLOR.Game.Server", "Domain.Foundation", "Core"),
            ("Lure", "SWLOR.Game.Server.Domain", "World.Fishing", "World"));
        data.ByName["HealAmount"].SourcePath.Should().Be("SWLOR.Game.Server/Feature/AbilityDefinition/HealAmount.cs");
        data.ByName["HealAmount"].TargetPath.Should().Be("SWLOR.Game.Server/Domain/Combat/HealAmount.cs");
        data.ByName["Log"].TargetPath.Should().Be("SWLOR.Game.Server/Core/Log.cs");
        data.ByOldFullName["SWLOR.Game.Server.Domain.Crafting.Recipe"].NewFullName.Should().Be("SWLOR.Game.Server.Domain.Foundation.Recipe");
        data.Files.Select(file => file.Key).Should().Contain("SWLOR.Game.Server/Domain/Crafting/Recipe.cs");
    }

    [TestCase("{ \"types\": [ { \"type\": \"A\", \"to\": \"SWLOR.Game.Server.Domain.X\" } ] }", "\"from\" is missing or empty",
        TestName = "An entry without a from and no default is refused")]
    [TestCase("{ \"types\": [ { \"type\": \"A\", \"from\": \"Elsewhere.Crafting\", \"to\": \"SWLOR.Game.Server.Domain.X\" } ] }", "\"from\" must be a namespace below SWLOR.Game.Server",
        TestName = "An entry's from outside the server is refused")]
    [TestCase("{ \"types\": [ { \"type\": \"A\", \"from\": \"SWLOR.Game.Server.Domain.Crafting\", \"to\": \"SWLOR.Game.Server\" } ] }", "A must move to a namespace below SWLOR.Game.Server outside SWLOR.Game.Server.Domain.Crafting",
        TestName = "The server's root namespace is no target")]
    [TestCase("{ \"types\": [ { \"type\": \"A\", \"from\": \"SWLOR.Game.Server.Domain.Crafting\", \"to\": \"SWLOR.Game.Server.Domain.Crafting.Inner\" } ] }", "A must move to a namespace below SWLOR.Game.Server outside SWLOR.Game.Server.Domain.Crafting",
        TestName = "A target inside the source is refused")]
    [TestCase("{ \"removeUnusedSourceUsings\": \"no\", \"types\": [ { \"type\": \"A\", \"from\": \"SWLOR.Game.Server.Domain.Crafting\", \"to\": \"SWLOR.Game.Server.Core\" } ] }", "\"removeUnusedSourceUsings\" must be true or false",
        TestName = "The using rule is true or false")]
    [TestCase("{ \"types\": [ { \"type\": \"A\", \"from\": \"SWLOR.Game.Server.Domain.Crafting\", \"to\": \"SWLOR.Game.Server.Core\" }, "
              + "{ \"type\": \"B\", \"from\": \"SWLOR.Game.Server.Domain.World\", \"to\": \"SWLOR.Game.Server.Core\", \"file\": \"A.cs\" } ] }", "two files would move to SWLOR.Game.Server/Core/A.cs",
        TestName = "Two files that would land on one path are refused")]
    public void A_malformed_entry_with_its_own_source_is_refused(string json, string expected)
    {
        var parse = () => TypeMoveData.Parse(json, "data.json");

        parse.Should().Throw<TypeMoveException>().Which.Message.Should().Contain(expected);
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
