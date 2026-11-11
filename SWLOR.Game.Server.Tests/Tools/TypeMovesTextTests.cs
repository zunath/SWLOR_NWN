using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.TypeMoves;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// The text pieces of <c>tools/Refactor/SWLOR.TypeMoves</c> (P2-Z3): the rewrite of names and paths outside code, and the
/// placement of using lines the way a file already sorts them.
/// </summary>
[TestFixture]
public class TypeMovesTextTests
{
    private static readonly TypeMoveData Data = TypeMoveData.Parse("""
        {
          "from": "SWLOR.Game.Server.Service",
          "types": [
            { "type": "Gauge", "to": "SWLOR.Game.Server.Domain.Gadgets" },
            { "type": "IGauge", "to": "SWLOR.Game.Server.Domain.Gadgets", "file": "Gauge.cs" }
          ]
        }
        """, "data.json");

    [Test]
    public void Other_text_follows_the_move()
    {
        var count = 0;
        var text = new TextRewriter(Data).RewriteText(
            "global::SWLOR.Game.Server.Service.Gauge, Service.IGauge, X.Service.Gauge, ServiceGauge, Service.Gauges, " +
            "SWLOR.Game.Server\\\\Service\\\\Gauge.cs, Service/IGauge.cs, \"Service\", \"Gauge.cs\"", ref count);

        text.Should().Be(
            "global::SWLOR.Game.Server.Domain.Gadgets.Gauge, Domain.Gadgets.IGauge, X.Service.Gauge, ServiceGauge, Service.Gauges, " +
            "SWLOR.Game.Server\\\\Domain\\\\Gadgets\\\\Gauge.cs, Service/IGauge.cs, \"Domain\", \"Gadgets\", \"Gauge.cs\"");
        count.Should().Be(4, "a path names a file, and IGauge has no file of its own");
    }

    [Test]
    public void In_csharp_only_comments_and_literals_change_and_a_literal_keeps_relative_names()
    {
        const string source = "// Service.Gauge and SWLOR.Game.Server.Service.Gauge\nclass C\n{\n    string A = \"Service.Gauge\";\n"
                              + "    string B = \"SWLOR.Game.Server.Service.Gauge\";\n    string P = System.IO.Path.Combine(\"Service\", \"Gauge.cs\");\n    object Q = Service.Gauge.Read;\n}\n";
        var count = 0;

        var text = new TextRewriter(Data).RewriteCSharp(source, ref count);

        text.Should().Be("// Domain.Gadgets.Gauge and SWLOR.Game.Server.Domain.Gadgets.Gauge\nclass C\n{\n    string A = \"Service.Gauge\";\n"
                         + "    string B = \"SWLOR.Game.Server.Domain.Gadgets.Gauge\";\n    string P = System.IO.Path.Combine(\"Domain\", \"Gadgets\", \"Gauge.cs\");\n    object Q = Service.Gauge.Read;\n}\n");
        count.Should().Be(4);
        new TextRewriter(Data).RewriteCSharp("class C { }\n", ref count).Should().Be("class C { }\n");
    }

    [TestCase("using System;\nusing System.Linq;\nusing SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Service;\n\nclass C { }\n",
        "using System;\nusing System.Linq;\nusing SWLOR.Game.Server.Core;\nusing SWLOR.Game.Server.Domain.Gadgets;\n\nclass C { }\n",
        TestName = "System first and sorted: the new using goes to its place")]
    [TestCase("using SWLOR.Game.Server.Service;\nusing SWLOR.Game.Server.Core;\nusing System;\n\nclass C { }\n",
        "using SWLOR.Game.Server.Domain.Gadgets;\nusing SWLOR.Game.Server.Core;\nusing System;\n\nclass C { }\n",
        TestName = "Unsorted: the new using takes the removed one's place")]
    [TestCase("using SWLOR.Game.Server.Core;\nusing System;\nusing SWLOR.Game.Server.Entity;\n\nclass C { }\n",
        "using SWLOR.Game.Server.Core;\nusing System;\nusing SWLOR.Game.Server.Entity;\nusing SWLOR.Game.Server.Domain.Gadgets;\n\nclass C { }\n",
        TestName = "Unsorted without a removal: after the last server using")]
    [TestCase("namespace N\n{\n    using SWLOR.Game.Server.Core;\n    using SWLOR.Game.Server.Service;\n\n    class C { }\n}\n",
        "namespace N\n{\n    using SWLOR.Game.Server.Core;\n    using SWLOR.Game.Server.Domain.Gadgets;\n\n    class C { }\n}\n",
        TestName = "Usings inside a namespace keep their indentation")]
    [TestCase("// header\n\nnamespace N;\n\nclass C { }\n",
        "// header\n\nusing SWLOR.Game.Server.Domain.Gadgets;\n\nnamespace N;\n\nclass C { }\n",
        TestName = "A file without usings gets one above its first member")]
    [TestCase("using SWLOR.Game.Server.Core;\r\nusing SWLOR.Game.Server.Service;\r\nusing System;\r\n\r\nclass C { }\r\n",
        "using SWLOR.Game.Server.Core;\r\nusing SWLOR.Game.Server.Domain.Gadgets;\r\nusing System;\r\n\r\nclass C { }\r\n",
        TestName = "Ordinal sort and CRLF")]
    public void Using_lines_go_where_the_file_sorts_them(string before, string after)
    {
        var root = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(before).GetRoot();
        var removals = SyntaxQueries.AllUsings(root).Where(directive => SyntaxQueries.Dotted(directive.Name!) == "SWLOR.Game.Server.Service").ToList();

        var edits = UsingEditor.Plan(before, root, ["SWLOR.Game.Server.Domain.Gadgets"], removals);

        TextEdit.Apply(before, edits).Should().Be(after);
    }

    [Test]
    public void A_renamed_handler_updates_its_rename_line_or_gets_one_and_a_split_keeps_its_parts_in_place()
    {
        const string renames = "# renames\r\n"
                               + "M.Gauge.Load B.Service.Gauge.Load\r\n"
                               + "M.Gauge.Tick B.Gauge.Start B.Service.Gauge.Tick\r\n";

        var text = HandlerRenames.Update(renames,
        [
            ("B.Service.Gauge.Load", "B.Domain.Gadgets.Gauge.Load"),
            ("B.Service.Gauge.Tick", "B.Domain.Gadgets.Gauge.Tick"),
            ("SWLOR.Game.Server.Service.Gauge.Flush", "SWLOR.Game.Server.Domain.Gadgets.Gauge.Flush")
        ]);

        text.Should().Be("# renames\r\n"
                         + "M.Gauge.Load B.Domain.Gadgets.Gauge.Load\r\n"
                         + "M.Gauge.Tick B.Gauge.Start B.Domain.Gadgets.Gauge.Tick\r\n"
                         + "SWLOR.Game.Server.Service.Gauge.Flush SWLOR.Game.Server.Domain.Gadgets.Gauge.Flush\r\n");
        HandlerRenames.Pairs("x\n  action      A.B\n  action      C.D  # note\n", "x\n  action      A.E\n  action      C.D  # note\n")
            .Should().Equal(("A.B", "A.E"));
    }

    [Test]
    public void A_position_maps_through_the_edits()
    {
        var edits = new List<TextEdit> { new(0, 0, "ab"), new(5, 3, "x"), new(10, 0, "yy") };

        TextEdit.MapPosition(0, edits).Should().Be(2);
        TextEdit.MapPosition(4, edits).Should().Be(6);
        TextEdit.MapPosition(6, edits).Should().Be(-1, "the edit replaced it");
        TextEdit.MapPosition(8, edits).Should().Be(8);
        TextEdit.MapPosition(10, edits).Should().Be(12);
        var overlap = () => TextEdit.Apply("0123456789", [new(2, 4, "a"), new(3, 1, "b")]);
        overlap.Should().Throw<TypeMoveException>();
    }
}
