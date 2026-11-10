using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.TypeMoves;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Runs <c>tools/Refactor/SWLOR.TypeMoves</c> (P2-Z3) over a small repository in a temporary folder: the listed files move
/// with their namespace; qualified names, <c>using static</c> and aliases follow; a file that names a moved type gets the new
/// using at its sorted place; a using of the old namespace goes unless a type that stays there still binds through it; a
/// name the move would rebind is qualified; other text, the handler manifest and its rename list follow; the run is
/// idempotent and a check writes nothing; code written for the old place after the move (master's, after a merge) is moved
/// too; a tree with both files is refused.
/// </summary>
/// <remarks>The fixture names its own types; the tool's text rewrite never sees this file (TypeMoves.json excludes it).</remarks>
[TestFixture]
public class TypeMovesRunnerTests
{
    private const string Data = """
        {
          "from": "SWLOR.Game.Server.Service",
          "types": [
            { "type": "Random", "to": "SWLOR.Game.Server.Domain.Foundation" },
            { "type": "Log", "to": "SWLOR.Game.Server.Domain.Foundation" },
            { "type": "Menu", "to": "SWLOR.Game.Server.Domain.Economy" }
          ],
          "textExcludes": [ "docs/Excluded.md", "tools/Refactor/TypeMoves.json" ]
        }
        """;

    private const string Project = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n";

    private const string TestProject = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n"
        + "  <ItemGroup>\n    <ProjectReference Include=\"..\\SWLOR.Game.Server\\SWLOR.Game.Server.csproj\" />\n  </ItemGroup>\n</Project>\n";

    private const string RandomSource = "namespace SWLOR.Game.Server.Service\n{\n    public static class Random\n    {\n        public static int Next(int max) => max - 1;\n    }\n}\n";

    // Inside Service it sees Stay, Random and Menu without a using; Random joins it in Foundation, Menu goes elsewhere.
    private const string LogSource = "using System;\n\nnamespace SWLOR.Game.Server.Service\n{\n    public static class Log\n    {\n"
        + "        public static void Write(string message) => Console.WriteLine(message + Stay.Value + Random.Next(2) + Menu.Title);\n\n"
        + "        public static void Flush()\n        {\n        }\n    }\n}\n";

    private const string MenuSource = "namespace SWLOR.Game.Server.Service;\n\npublic static class Menu\n{\n    public const string Title = \"menu\";\n}\n";

    private const string StaySource = "namespace SWLOR.Game.Server.Service\n{\n    public static class Stay\n    {\n        public const int Value = 1;\n    }\n}\n";

    private const string ScriptSource = "namespace SWLOR.Game.Server.Core\n{\n    public static class Script\n    {\n        public static int Random(int max) => max;\n\n        public static int Log => 1;\n    }\n}\n";

    // Random means System.Random and Script.Random, Log means Script.Log here; once Random and Log join the namespace, only the
    // invocation still binds as before.
    private const string DiceSource = "using System;\nusing static SWLOR.Game.Server.Core.Script;\n\nnamespace SWLOR.Game.Server.Domain.Foundation\n{\n"
        + "    public static class Dice\n    {\n        public static int Roll() => new Random(5).Next(6) + Random(3) + Log;\n    }\n}\n";

    private const string UserSource = "using SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Service;\nusing Alias = SWLOR.Game.Server.Service.Random;\n"
        + "using static SWLOR.Game.Server.Service.Menu;\n\nnamespace SWLOR.Game.Server.Feature\n{\n"
        + "    // Calls SWLOR.Game.Server.Service.Log and Service.Random.\n    public static class User\n    {\n        public static string Run()\n        {\n"
        + "            Log.Write(\"x\");\n            return Title + Stay.Value + Alias.Next(3) + Service.Random.Next(4) + SWLOR.Game.Server.Service.Menu.Title"
        + " + System.IO.Path.Combine(\"Service\", \"Log.cs\") + nameof(Script);\n        }\n    }\n}\n";

    private const string Guide = "Seed `Service.Random` in SWLOR.Game.Server/Service/Log.cs and `SWLOR.Game.Server.Service.Menu`.\n";

    private string _root = null!;

    [SetUp]
    public void CreateRepository()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-type-moves-" + Guid.NewGuid().ToString("N")[..8]);
        Write("SWLOR.Game.Server.sln", string.Empty);
        Write("SWLOR.Game.Server/SWLOR.Game.Server.csproj", Project);
        Write("SWLOR.Game.Server/Service/Random.cs", RandomSource);
        Write("SWLOR.Game.Server/Service/Log.cs", LogSource);
        Write("SWLOR.Game.Server/Service/Menu.cs", MenuSource);
        Write("SWLOR.Game.Server/Service/Stay.cs", StaySource);
        Write("SWLOR.Game.Server/Core/Script.cs", ScriptSource);
        Write("SWLOR.Game.Server/Domain/Foundation/Dice.cs", DiceSource);
        Write("SWLOR.Game.Server/Domain/Economy/Bank.cs", "namespace SWLOR.Game.Server.Domain.Economy\n{\n    public static class Bank\n    {\n    }\n}\n");
        Write("SWLOR.Game.Server/Feature/User.cs", UserSource);
        Write("SWLOR.Game.Server/Feature/Only.cs", "using SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Feature\n{\n    public static class Only\n    {\n        public static void Run() => Log.Write(\"only\");\n    }\n}\n");
        Write("SWLOR.Game.Server/Feature/Stale.cs", "using System;\nusing SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Feature\n{\n    public static class Stale\n    {\n    }\n}\n");
        Write("SWLOR.Game.Server/Feature/Lone.cs", "using SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Feature\n{\n    public static class Lone\n    {\n    }\n}\n");
        Write("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt", "mod_load\n  action      SWLOR.Game.Server.Service.Log.Flush\n  action      SWLOR.Game.Server.Service.Stay.Load\n");
        Write("SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj", TestProject);
        Write("SWLOR.Game.Server.Tests/GlobalUsings.cs", "global using System;\nglobal using SWLOR.Game.Server.Service;\n");
        Write("SWLOR.Game.Server.Tests/UsesLog.cs", "namespace SWLOR.Game.Server.Tests\n{\n    public static class UsesLog\n    {\n        public static void Run() => Log.Write(\"test\");\n    }\n}\n");
        Write("tools/Refactor/HandlerRenames.txt", "# renames\n");
        Write("tools/Refactor/TypeMoves.json", Data);
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
    public void Apply_moves_the_types_and_rewrites_their_references()
    {
        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        File.Exists(Full("SWLOR.Game.Server/Service/Random.cs")).Should().BeFalse();
        Text("SWLOR.Game.Server/Domain/Foundation/Random.cs").Should().Be(RandomSource.Replace("namespace SWLOR.Game.Server.Service", "namespace SWLOR.Game.Server.Domain.Foundation"));
        Text("SWLOR.Game.Server/Domain/Economy/Menu.cs").Should().Be(MenuSource.Replace("namespace SWLOR.Game.Server.Service;", "namespace SWLOR.Game.Server.Domain.Economy;"));
        Text("SWLOR.Game.Server/Service/Stay.cs").Should().Be(StaySource, "a type that is not listed stays");
        Text("SWLOR.Game.Server/Domain/Foundation/Log.cs").Should().Be(LogSource
            .Replace("using System;\n", "using System;\nusing SWLOR.Game.Server.Domain.Economy;\nusing SWLOR.Game.Server.Service;\n")
            .Replace("namespace SWLOR.Game.Server.Service", "namespace SWLOR.Game.Server.Domain.Foundation"),
            "a moved file imports what it saw from inside the old namespace, and not its own new namespace");

        Text("SWLOR.Game.Server/Feature/User.cs").Should().Be(
            "using SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Domain.Foundation;\nusing SWLOR.Game.Server.Service;\nusing Alias = SWLOR.Game.Server.Domain.Foundation.Random;\n"
            + "using static SWLOR.Game.Server.Domain.Economy.Menu;\n\nnamespace SWLOR.Game.Server.Feature\n{\n"
            + "    // Calls SWLOR.Game.Server.Domain.Foundation.Log and Domain.Foundation.Random.\n    public static class User\n    {\n        public static string Run()\n        {\n"
            + "            Log.Write(\"x\");\n            return Title + Stay.Value + Alias.Next(3) + Domain.Foundation.Random.Next(4) + SWLOR.Game.Server.Domain.Economy.Menu.Title"
            + " + System.IO.Path.Combine(\"Domain\", \"Foundation\", \"Log.cs\") + nameof(Script);\n        }\n    }\n}\n",
            "the sorted block takes the new using in order, and keeps the old one for Stay");
        Text("SWLOR.Game.Server/Feature/Only.cs").Should().StartWith("using SWLOR.Game.Server.Domain.Foundation;\n\nnamespace", "the new using takes the old one's place");
        Text("SWLOR.Game.Server/Feature/Stale.cs").Should().StartWith("using System;\n\nnamespace", "a using nothing binds through goes");
        Text("SWLOR.Game.Server/Feature/Lone.cs").Should().StartWith("namespace SWLOR.Game.Server.Feature\n", "the blank line goes with the last using");

        Text("SWLOR.Game.Server/Domain/Foundation/Dice.cs").Should().Contain("new System.Random(5).Next(6) + Random(3) + SWLOR.Game.Server.Core.Script.Log;",
            "a name the move would rebind is qualified with what it meant (a type, a member imported with using static); an invoked name still finds the method (a type is not invocable)");
        log.ToString().Should().Contain("Qualified SWLOR.Game.Server/Domain/Foundation/Dice.cs line 8: System.Random");

        Text("SWLOR.Game.Server.Tests/GlobalUsings.cs").Should().Be("global using System;\n", "nothing binds through the global using any more");
        Text("SWLOR.Game.Server.Tests/UsesLog.cs").Should().StartWith("using SWLOR.Game.Server.Domain.Foundation;\n\nnamespace SWLOR.Game.Server.Tests\n");

        Text("SWLOR.Game.Server/Core/ScriptHandlerOrder.txt").Should().Be("mod_load\n  action      SWLOR.Game.Server.Domain.Foundation.Log.Flush\n  action      SWLOR.Game.Server.Service.Stay.Load\n");
        Text("tools/Refactor/HandlerRenames.txt").Should().Be("# renames\nSWLOR.Game.Server.Service.Log.Flush SWLOR.Game.Server.Domain.Foundation.Log.Flush\n");
        Text("docs/Guide.md").Should().Be("Seed `Domain.Foundation.Random` in SWLOR.Game.Server/Domain/Foundation/Log.cs and `SWLOR.Game.Server.Domain.Economy.Menu`.\n");
        Text("docs/Excluded.md").Should().Be(Guide, "an excluded file keeps its text");

        var after = Snapshot();
        Run(RunMode.Apply).Should().Be(TypeMoveRunner.Ok);
        Snapshot().Should().BeEquivalentTo(after, "a second run changes nothing");
        Run(RunMode.Check).Should().Be(TypeMoveRunner.Ok, "the check is clean after an apply");
    }

    [Test]
    public void Check_lists_the_changes_and_writes_nothing()
    {
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Check, log).Should().Be(TypeMoveRunner.Differs);

        Snapshot().Should().BeEquivalentTo(before);
        log.ToString().Should().Contain("  move  SWLOR.Game.Server/Service/Log.cs -> SWLOR.Game.Server/Domain/Foundation/Log.cs")
            .And.Contain("  edit  SWLOR.Game.Server/Feature/User.cs")
            .And.Contain("Would move 3 file(s) (3 type(s))");
    }

    [Test]
    public void Code_that_names_the_old_place_after_the_move_is_moved_too()
    {
        Run(RunMode.Apply).Should().Be(TypeMoveRunner.Ok);

        // Master's code after a merge, and a master file Move-SupportNamespaces.ps1 moved out of a Service.<Name>Service folder.
        Write("SWLOR.Game.Server/Feature/FromMaster.cs", "using SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Feature\n{\n    public static class FromMaster\n    {\n"
            + "        public static int Run()\n        {\n            Log.Write(\"m\");\n            return Service.Random.Next(2);\n        }\n    }\n}\n");
        Write("SWLOR.Game.Server/Domain/Economy/Moved.cs", "using SWLOR.Game.Server.Service;\n\nnamespace SWLOR.Game.Server.Domain.Economy\n{\n"
            + "    public static class Moved\n    {\n        public static string Run() => Service.Random.Next(1) + Menu.Title;\n    }\n}\n");

        var log = new StringWriter();
        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        Text("SWLOR.Game.Server/Feature/FromMaster.cs").Should().Be("using SWLOR.Game.Server.Domain.Foundation;\n\nnamespace SWLOR.Game.Server.Feature\n{\n    public static class FromMaster\n    {\n"
            + "        public static int Run()\n        {\n            Log.Write(\"m\");\n            return Domain.Foundation.Random.Next(2);\n        }\n    }\n}\n");
        Text("SWLOR.Game.Server/Domain/Economy/Moved.cs").Should().Be("namespace SWLOR.Game.Server.Domain.Economy\n{\n"
            + "    public static class Moved\n    {\n        public static string Run() => Domain.Foundation.Random.Next(1) + Menu.Title;\n    }\n}\n",
            "Menu is in the file's own namespace now");
        Run(RunMode.Check).Should().Be(TypeMoveRunner.Ok);
    }

    [Test]
    public void A_tree_with_both_files_is_refused_and_nothing_is_written()
    {
        Write("SWLOR.Game.Server/Domain/Foundation/Random.cs", RandomSource);
        var before = Snapshot();
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Failed);

        log.ToString().Should().Contain("SWLOR.Game.Server/Service/Random.cs and SWLOR.Game.Server/Domain/Foundation/Random.cs both exist");
        Snapshot().Should().BeEquivalentTo(before);
    }

    [Test]
    public void A_moved_file_that_declares_an_unlisted_type_is_refused()
    {
        Write("SWLOR.Game.Server/Service/Menu.cs", MenuSource + "\npublic static class Extra\n{\n}\n");
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Failed);

        log.ToString().Should().Contain("SWLOR.Game.Server/Service/Menu.cs declares Extra, Menu, but the data lists Menu for it.");
    }

    [Test]
    public void A_type_the_target_namespace_already_declares_is_refused()
    {
        Write("SWLOR.Game.Server/Domain/Economy/Menu2.cs", "namespace SWLOR.Game.Server.Domain.Economy\n{\n    public static class Menu\n    {\n    }\n}\n");
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Failed);

        log.ToString().Should().Contain("SWLOR.Game.Server.Domain.Economy already declares a type Menu.");
    }

    [Test]
    public void A_tree_without_the_old_files_or_the_new_ones_skips_those_types()
    {
        File.Delete(Full("SWLOR.Game.Server/Service/Menu.cs"));
        File.WriteAllText(Full("SWLOR.Game.Server/Service/Log.cs"), LogSource.Replace(" + Menu.Title", string.Empty));
        File.WriteAllText(Full("SWLOR.Game.Server/Feature/User.cs"), UserSource
            .Replace("using static SWLOR.Game.Server.Service.Menu;\n", string.Empty)
            .Replace("Title + ", string.Empty)
            .Replace(" + SWLOR.Game.Server.Service.Menu.Title", string.Empty));
        var log = new StringWriter();

        Run(RunMode.Apply, log).Should().Be(TypeMoveRunner.Ok, log.ToString());

        log.ToString().Should().Contain("Skipped Menu: neither SWLOR.Game.Server/Service/Menu.cs nor SWLOR.Game.Server/Domain/Economy/Menu.cs exists in this tree.");
        File.Exists(Full("SWLOR.Game.Server/Domain/Foundation/Log.cs")).Should().BeTrue();
    }

    private int Run(RunMode mode, TextWriter? log = null) =>
        TypeMoveRunner.Run(new RunOptions(mode, _root, Full("tools/Refactor/TypeMoves.json")), log ?? TextWriter.Null);

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
