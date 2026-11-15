using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.TypeMoves;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Runs <c>tools/Refactor/SWLOR.TypeMoves</c> with the domain order's kind of data (P5-1c, <c>DomainOrderTypeMoves.json</c>) over
/// a small repository in a temporary folder: each entry has its own source namespace; a type moves from <c>Feature</c> to
/// <c>Domain</c> and from <c>Domain.Foundation</c> to <c>Core</c>; qualified names, <c>cref</c>s, <c>nameof</c>, <c>using static</c>,
/// aliases, attributes and calls of a moved class's extension methods follow; a new using goes to its sorted place and the using
/// of the old namespace stays unless no type is left there; a qualifier that would reach another namespace is written in full;
/// the run is idempotent and a check lists the pending moves; master's code that names a moved type after the move is moved too;
/// a type moved twice (P2-Z3, then the domain order) is reached from master's first name.
/// </summary>
/// <remarks>The fixture names its own types; the tool's text rewrite never sees this file (both data files exclude it).</remarks>
[TestFixture]
public class TypeMovesDomainOrderRunnerTests
{
    private const string Data = """
        {
          "removeUnusedSourceUsings": false,
          "types": [
            { "type": "Recipe", "from": "SWLOR.Game.Server.Domain.Crafting", "to": "SWLOR.Game.Server.Domain.Foundation" },
            { "type": "GradeAttribute", "from": "SWLOR.Game.Server.Domain.Crafting", "to": "SWLOR.Game.Server.Domain.Foundation" },
            { "type": "HealAmount", "from": "SWLOR.Game.Server.Feature.AbilityDefinition", "to": "SWLOR.Game.Server.Domain.Combat" },
            { "type": "Log", "from": "SWLOR.Game.Server.Domain.Foundation", "to": "SWLOR.Game.Server.Core" },
            { "type": "LogText", "from": "SWLOR.Game.Server.Domain.Foundation", "to": "SWLOR.Game.Server.Core" }
          ],
          "textExcludes": [ "tools/Refactor/DomainOrderTypeMoves.json", "docs/Excluded.md" ]
        }
        """;

    private const string Project = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n";

    private const string TestProject = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n"
        + "  <ItemGroup>\n    <ProjectReference Include=\"..\\SWLOR.Game.Server\\SWLOR.Game.Server.csproj\" />\n  </ItemGroup>\n</Project>\n";

    // Recipe sees Tier, which stays in Crafting, from inside; it moves to Foundation.
    private const string RecipeSource = "namespace SWLOR.Game.Server.Domain.Crafting\n{\n    public enum Recipe\n    {\n        Invalid = 0,\n\n"
        + "        [Tier(1)]\n        Bread = 1\n    }\n}\n";

    private const string TierSource = "namespace SWLOR.Game.Server.Domain.Crafting\n{\n    public class TierAttribute : System.Attribute\n    {\n"
        + "        public TierAttribute(int tier)\n        {\n        }\n    }\n}\n";

    private const string GradeSource = "namespace SWLOR.Game.Server.Domain.Crafting;\n\npublic class GradeAttribute : System.Attribute\n{\n}\n";

    // Stays in Crafting and names two moved types by their simple names (one as an attribute, one in a cref).
    private const string BenchSource = "using System;\n\nnamespace SWLOR.Game.Server.Domain.Crafting\n{\n    /// <summary>Makes a <see cref=\"Recipe\"/>.</summary>\n"
        + "    public static class Bench\n    {\n        [Grade]\n        public static Recipe Make() => Recipe.Bread;\n    }\n}\n";

    // The only type of its namespace: once it moves, the namespace is gone and so is every using of it.
    private const string HealSource = "namespace SWLOR.Game.Server.Feature.AbilityDefinition\n{\n    public readonly record struct HealAmount(int Value);\n}\n";

    // Leaves Foundation, where it sees Keep from inside, for Core.
    private const string LogSource = "namespace SWLOR.Game.Server.Domain.Foundation\n{\n    public static class Log\n    {\n"
        + "        public static void Write(string message) => Keep.Count++;\n\n        public static void Flush()\n        {\n        }\n    }\n}\n";

    private const string LogTextSource = "namespace SWLOR.Game.Server.Domain.Foundation\n{\n    public static class LogText\n    {\n"
        + "        public static string Shout(this string text) => text + \"!\";\n    }\n}\n";

    private const string KeepSource = "namespace SWLOR.Game.Server.Domain.Foundation\n{\n    public static class Keep\n    {\n        public static int Count;\n    }\n}\n";

    private const string UserSource = "using System;\nusing SWLOR.Game.Server.Domain.Crafting;\nusing SWLOR.Game.Server.Domain.Foundation;\n"
        + "using SWLOR.Game.Server.Feature.AbilityDefinition;\nusing Heal = SWLOR.Game.Server.Feature.AbilityDefinition.HealAmount;\n"
        + "using static SWLOR.Game.Server.Domain.Crafting.Recipe;\n\nnamespace SWLOR.Game.Server.Feature\n{\n"
        + "    /// <summary>Uses <see cref=\"Recipe\"/>, <see cref=\"Domain.Crafting.Recipe\"/> and <see cref=\"Feature.AbilityDefinition.HealAmount\"/>.</summary>\n"
        + "    public static class User\n    {\n        public static string Run()\n        {\n            Log.Write(\"x\".Shout());\n"
        + "            var heal = new HealAmount(1);\n            Heal other = new(2);\n"
        + "            return nameof(Recipe) + nameof(Domain.Crafting.Recipe) + Bread + SWLOR.Game.Server.Domain.Crafting.Recipe.Invalid + heal + other + Keep.Count;\n"
        + "        }\n    }\n}\n";

    // A qualifier shorter than the source namespace below the namespace both share (Feature.AbilityDefinition).
    private const string MendSource = "namespace SWLOR.Game.Server.Feature.Spells\n{\n    public static class Mend\n    {\n"
        + "        public static object Amount() => new AbilityDefinition.HealAmount(3);\n    }\n}\n";

    // In SWLOR.Game.Server.Tests, Core means SWLOR.Game.Server.Tests.Core: the relative qualifier would reach the wrong namespace.
    private const string LogTestsSource = "namespace SWLOR.Game.Server.Tests\n{\n    public static class LogTests\n    {\n"
        + "        public static void Run() => Domain.Foundation.Log.Write(\"t\");\n    }\n}\n";

    private const string Guide = "`Domain.Crafting.Recipe` is in SWLOR.Game.Server/Domain/Crafting/Recipe.cs, `SWLOR.Game.Server.Domain.Foundation.Log` in "
        + "Domain/Foundation/Log.cs, Feature/AbilityDefinition/HealAmount.cs; Domain/Combat/Recipe.cs and Domain.Combat.Recipe are other files.\n";

    private string _root = null!;

    [SetUp]
    public void CreateRepository()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-type-moves-order-" + Guid.NewGuid().ToString("N")[..8]);
        Write("SWLOR.Game.Server.sln", string.Empty);
        Write("SWLOR.Game.Server/SWLOR.Game.Server.csproj", Project);
        Write("SWLOR.Game.Server/Domain/Crafting/Recipe.cs", "using SWLOR.Game.Server.Domain.Foundation;\n\n" + RecipeSource);
        Write("SWLOR.Game.Server/Domain/Crafting/TierAttribute.cs", TierSource);
        Write("SWLOR.Game.Server/Domain/Crafting/GradeAttribute.cs", GradeSource);
        Write("SWLOR.Game.Server/Domain/Crafting/Bench.cs", BenchSource);
        Write("SWLOR.Game.Server/Feature/AbilityDefinition/HealAmount.cs", HealSource);
        Write("SWLOR.Game.Server/Domain/Combat/Hit.cs", "namespace SWLOR.Game.Server.Domain.Combat\n{\n    public static class Hit\n    {\n    }\n}\n");
        Write("SWLOR.Game.Server/Domain/Foundation/Log.cs", LogSource);
        Write("SWLOR.Game.Server/Domain/Foundation/LogText.cs", LogTextSource);
        Write("SWLOR.Game.Server/Domain/Foundation/Keep.cs", KeepSource);
        Write("SWLOR.Game.Server/Core/Script.cs", "namespace SWLOR.Game.Server.Core\n{\n    public static class Script\n    {\n    }\n}\n");
        Write("SWLOR.Game.Server/Feature/User.cs", UserSource);
        Write("SWLOR.Game.Server/Feature/Spells/Mend.cs", MendSource);
        Write("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt", "mod_load\n  action      SWLOR.Game.Server.Domain.Foundation.Log.Flush\n");
        Write("SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj", TestProject);
        Write("SWLOR.Game.Server.Tests/Core/Probe.cs", "namespace SWLOR.Game.Server.Tests.Core\n{\n    public static class Probe\n    {\n    }\n}\n");
        Write("SWLOR.Game.Server.Tests/LogTests.cs", LogTestsSource);
        Write("tools/Refactor/HandlerRenames.txt", "# renames\nSWLOR.Game.Server.Service.Log.Flush SWLOR.Game.Server.Domain.Foundation.Log.Flush\n");
        Write("tools/Refactor/DomainOrderTypeMoves.json", Data);
        Write("docs/Guide.md", Guide);
        Write("docs/Excluded.md", Guide);
    }

    [TearDown]
    public void DeleteRepository()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void Apply_moves_each_type_from_its_own_namespace_and_rewrites_every_reference()
    {
        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        File.Exists(Full("SWLOR.Game.Server/Domain/Crafting/Recipe.cs")).Should().BeFalse();
        Text("SWLOR.Game.Server/Domain/Foundation/Recipe.cs").Should().Be("using SWLOR.Game.Server.Domain.Crafting;\n\n"
            + RecipeSource.Replace("namespace SWLOR.Game.Server.Domain.Crafting", "namespace SWLOR.Game.Server.Domain.Foundation"),
            "a moved file imports what it saw from inside its old namespace (an attribute by its short name), and its using of the "
            + "namespace it joins goes although types also leave that namespace");
        Text("SWLOR.Game.Server/Domain/Foundation/GradeAttribute.cs").Should().Be(GradeSource.Replace("Domain.Crafting;", "Domain.Foundation;"));
        Text("SWLOR.Game.Server/Domain/Combat/HealAmount.cs").Should().Be(HealSource.Replace("Feature.AbilityDefinition", "Domain.Combat"), "Feature to Domain");
        Text("SWLOR.Game.Server/Core/Log.cs").Should().Be("using SWLOR.Game.Server.Domain.Foundation;\n\n" + LogSource.Replace("namespace SWLOR.Game.Server.Domain.Foundation", "namespace SWLOR.Game.Server.Core"),
            "Domain.Foundation to Core");
        Text("SWLOR.Game.Server/Core/LogText.cs").Should().Be(LogTextSource.Replace("namespace SWLOR.Game.Server.Domain.Foundation", "namespace SWLOR.Game.Server.Core"));
        Directory.Exists(Full("SWLOR.Game.Server/Feature/AbilityDefinition")).Should().BeFalse("the emptied folder goes");

        Text("SWLOR.Game.Server/Domain/Crafting/Bench.cs").Should().Be(BenchSource.Replace("using System;\n", "using System;\nusing SWLOR.Game.Server.Domain.Foundation;\n"),
            "a file that stays in the old namespace imports the new one for a simple name, an attribute and a cref");
        Text("SWLOR.Game.Server/Feature/User.cs").Should().Be(
            "using System;\nusing SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Domain.Combat;\nusing SWLOR.Game.Server.Domain.Crafting;\nusing SWLOR.Game.Server.Domain.Foundation;\n"
            + "using Heal = SWLOR.Game.Server.Domain.Combat.HealAmount;\n"
            + "using static SWLOR.Game.Server.Domain.Foundation.Recipe;\n\nnamespace SWLOR.Game.Server.Feature\n{\n"
            + "    /// <summary>Uses <see cref=\"Recipe\"/>, <see cref=\"Domain.Foundation.Recipe\"/> and <see cref=\"Domain.Combat.HealAmount\"/>.</summary>\n"
            + "    public static class User\n    {\n        public static string Run()\n        {\n            Log.Write(\"x\".Shout());\n"
            + "            var heal = new HealAmount(1);\n            Heal other = new(2);\n"
            + "            return nameof(Recipe) + nameof(Domain.Foundation.Recipe) + Bread + SWLOR.Game.Server.Domain.Foundation.Recipe.Invalid + heal + other + Keep.Count;\n"
            + "        }\n    }\n}\n",
            "new usings go to their sorted places, the old namespace's using stays while it has types and goes once it has none, and "
            + "qualified names, crefs, nameof, aliases and using static follow");
        Text("SWLOR.Game.Server/Feature/Spells/Mend.cs").Should().Be(MendSource.Replace("new AbilityDefinition.HealAmount(3)", "new Domain.Combat.HealAmount(3)"),
            "a qualifier shorter than the source below the shared namespace is replaced whole");
        Text("SWLOR.Game.Server.Tests/LogTests.cs").Should().Be(LogTestsSource.Replace("Domain.Foundation.Log", "SWLOR.Game.Server.Core.Log"),
            "Core.Log would mean SWLOR.Game.Server.Tests.Core.Log there, so the qualifier is written in full");
        log.ToString().Should().Contain("Qualified SWLOR.Game.Server.Tests/LogTests.cs line 5: SWLOR.Game.Server.Core.Log");

        Text("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt").Should().Be("mod_load\n  action      SWLOR.Game.Server.Core.Log.Flush\n");
        Text("tools/Refactor/HandlerRenames.txt").Should().Be("# renames\nSWLOR.Game.Server.Service.Log.Flush SWLOR.Game.Server.Core.Log.Flush\n",
            "master's name stays on the left and the branch's name follows the move");
        Text("docs/Guide.md").Should().Be("`Domain.Foundation.Recipe` is in SWLOR.Game.Server/Domain/Foundation/Recipe.cs, `SWLOR.Game.Server.Core.Log` in "
            + "Core/Log.cs, Domain/Combat/HealAmount.cs; Domain/Combat/Recipe.cs and Domain.Combat.Recipe are other files.\n",
            "a path or a name in another folder or namespace is another file");
        Text("docs/Excluded.md").Should().Be(Guide);

        var after = Snapshot();
        Run(RunMode.Apply).Should().Be(TypeMoveRunner.Ok);
        Snapshot().Should().BeEquivalentTo(after, "a second run changes nothing");
        Run(RunMode.Check).Should().Be(TypeMoveRunner.Ok, "the check is clean after an apply");
    }

    [Test]
    public void Check_lists_the_pending_moves_and_writes_nothing()
    {
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Check, log).Should().Be(TypeMoveRunner.Differs);

        Snapshot().Should().BeEquivalentTo(before);
        log.ToString().Should().Contain("Would move 5 file(s) (5 type(s))")
            .And.Contain("  move  SWLOR.Game.Server/Domain/Crafting/Recipe.cs -> SWLOR.Game.Server/Domain/Foundation/Recipe.cs")
            .And.Contain("  move  SWLOR.Game.Server/Feature/AbilityDefinition/HealAmount.cs -> SWLOR.Game.Server/Domain/Combat/HealAmount.cs")
            .And.Contain("  move  SWLOR.Game.Server/Domain/Foundation/Log.cs -> SWLOR.Game.Server/Core/Log.cs")
            .And.Contain("  edit  SWLOR.Game.Server/Feature/User.cs");
    }

    [Test]
    public void Master_code_that_names_the_old_places_after_the_move_is_moved_too()
    {
        Run(RunMode.Apply).Should().Be(TypeMoveRunner.Ok);

        // Master's code after a merge (and Move-SupportNamespaces.ps1): it still reaches the types through their old namespaces.
        Write("SWLOR.Game.Server/Feature/FromMaster.cs", "using SWLOR.Game.Server.Domain.Crafting;\nusing SWLOR.Game.Server.Domain.Foundation;\n\n"
            + "namespace SWLOR.Game.Server.Feature\n{\n    public static class FromMaster\n    {\n        public static string Run()\n        {\n"
            + "            Log.Write(\"m\");\n            return Recipe.Bread + \"\".Shout() + Domain.Crafting.Recipe.Invalid;\n        }\n    }\n}\n");
        Write("SWLOR.Game.Server/Domain/Crafting/Station.cs", "namespace SWLOR.Game.Server.Domain.Crafting\n{\n"
            + "    public static class Station\n    {\n        public static Recipe Last = Recipe.Invalid;\n    }\n}\n");

        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        Text("SWLOR.Game.Server/Feature/FromMaster.cs").Should().Be(
            "using SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Domain.Crafting;\nusing SWLOR.Game.Server.Domain.Foundation;\n\n"
            + "namespace SWLOR.Game.Server.Feature\n{\n    public static class FromMaster\n    {\n        public static string Run()\n        {\n"
            + "            Log.Write(\"m\");\n            return Recipe.Bread + \"\".Shout() + Domain.Foundation.Recipe.Invalid;\n        }\n    }\n}\n");
        Text("SWLOR.Game.Server/Domain/Crafting/Station.cs").Should().Be("using SWLOR.Game.Server.Domain.Foundation;\n\nnamespace SWLOR.Game.Server.Domain.Crafting\n{\n"
            + "    public static class Station\n    {\n        public static Recipe Last = Recipe.Invalid;\n    }\n}\n",
            "a file in the old namespace reached the type without a using");
        Run(RunMode.Check).Should().Be(TypeMoveRunner.Ok);
    }

    [Test]
    public void A_type_moved_twice_is_reached_from_masters_first_name_when_the_first_move_points_at_its_last_namespace()
    {
        Run(RunMode.Apply).Should().Be(TypeMoveRunner.Ok);

        // P2-Z3 moved Log out of Service; the domain order moved it on to Core. TypeMoves.json points at Core, so master's code
        // that still names Service is moved there in one step (the folder Service is gone from the branch).
        Write("tools/Refactor/TypeMoves.json", "{ \"from\": \"SWLOR.Game.Server.Service\", \"types\": [ { \"type\": \"Log\", \"to\": \"SWLOR.Game.Server.Core\" } ], "
            + "\"textExcludes\": [ \"tools/Refactor/*.json\", \"docs/Excluded.md\" ] }");
        Write("SWLOR.Game.Server/Feature/OldMaster.cs", "using SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Feature\n{\n"
            + "    public static class OldMaster\n    {\n        public static void Run() => Log.Write(\"o\");\n    }\n}\n");

        var log = new StringWriter();
        TypeMoveRunner.Run(new RunOptions(RunMode.Apply, _root, Full("tools/Refactor/TypeMoves.json")), log).Should().Be(TypeMoveRunner.Ok, log.ToString());
        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        Text("SWLOR.Game.Server/Feature/OldMaster.cs").Should().Be("using SWLOR.Game.Server.Core;\n\nnamespace SWLOR.Game.Server.Feature\n{\n"
            + "    public static class OldMaster\n    {\n        public static void Run() => Log.Write(\"o\");\n    }\n}\n");
        Run(RunMode.Check).Should().Be(TypeMoveRunner.Ok);
    }

    [Test]
    public void A_later_move_is_not_stopped_by_a_name_a_file_reaches_through_both_namespaces_of_an_earlier_move()
    {
        Run(RunMode.Apply).Should().Be(TypeMoveRunner.Ok);

        // The file keeps the using of the old namespace and has the new one: beside the stand-in of the moved type, the name is
        // ambiguous in the analysis, and a property named like the type (read through its own type) binds to the property there.
        // That says nothing about what the name meant, so it must not block the move that follows.
        Write("SWLOR.Game.Server/Feature/Holder.cs", "using SWLOR.Game.Server.Domain.Crafting;\nusing SWLOR.Game.Server.Domain.Foundation;\n\n"
            + "namespace SWLOR.Game.Server.Feature\n{\n    public class Holder\n    {\n"
            + "        public virtual Recipe Recipe => Recipe.Invalid;\n\n        public static int Count => Keep.Count + System.Math.Min(1, 2);\n    }\n}\n");
        Write("tools/Refactor/DomainOrderTypeMoves.json", Data.Replace("{ \"type\": \"LogText\"",
            "{ \"type\": \"Keep\", \"from\": \"SWLOR.Game.Server.Domain.Foundation\", \"to\": \"SWLOR.Game.Server.Core\" },\n    { \"type\": \"LogText\""));

        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        Text("SWLOR.Game.Server/Core/Keep.cs").Should().Contain("namespace SWLOR.Game.Server.Core");
        Text("SWLOR.Game.Server/Feature/Holder.cs").Should().Contain("using SWLOR.Game.Server.Core;");
    }

    private int Run(RunMode mode, TextWriter? log = null) =>
        TypeMoveRunner.Run(new RunOptions(mode, _root, Full("tools/Refactor/DomainOrderTypeMoves.json")), log ?? TextWriter.Null);

    private string Full(string relativePath) => Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private string Text(string relativePath) => File.ReadAllText(Full(relativePath), Encoding.Latin1);

    private void Write(string relativePath, string content)
    {
        var path = Full(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private Dictionary<string, string> Snapshot() =>
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(_root, file), file => File.ReadAllText(file, Encoding.Latin1));
}
