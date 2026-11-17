using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Proves <see cref="StatSource"/> keeps the source-reading tests of <c>StatService</c> honest while it is split: it reads
/// every part file, the one rewrite removes only the receiver a move added, and a changed condition, a removed call or a
/// different argument still fails a needle.
/// </summary>
[TestFixture]
public class StatSourceTests
{
    private static readonly IReadOnlySet<string> Parts = new HashSet<string>(StringComparer.Ordinal) { "IStatPoolService" };

    private static readonly string PartUser = Lines(
        "namespace SWLOR.Game.Server.Domain.Combat",
        "{",
        "    public class CombatRatingService : ICombatRatingService",
        "    {",
        "        private readonly IStatPoolService _pool;",
        "        private readonly IOtherService _other;",
        "",
        "        private int Compute(uint creature, int attack)",
        "        {",
        "            if (attack > 10)",
        "                return _pool.GetMaxFP(creature, attack);",
        "            _other.GetMaxFP(creature);",
        "            var count = _pool.Count;",
        "            if (_pool.Count < 10)",
        "                Log(\"_pool.GetMaxFP(creature) is logged as text\");",
        "            return _pool.Fallback<int>(creature) + count;",
        "        }",
        "    }",
        "}");

    private static string Lines(params string[] lines) => string.Join("\r\n", lines) + "\r\n";

    [Test]
    public void ACallOnAnInjectedPartLosesItsReceiver()
    {
        var text = StatSource.Normalise(PartUser, Parts);

        text.Should().Contain("                return GetMaxFP(creature, attack);");
        text.Should().Contain("            return Fallback<int>(creature) + count;");
    }

    [Test]
    public void OtherReceiversAndDeclarationsStayAsTheyAre()
    {
        var text = StatSource.Normalise(PartUser, Parts);

        text.Should().Contain("            _other.GetMaxFP(creature);");
        text.Should().Contain("            var count = _pool.Count;", "a property read is not a call the move rewrote");
        text.Should().Contain("            if (_pool.Count < 10)", "a property compared with < is not a generic call");
        text.Should().Contain("\"_pool.GetMaxFP(creature) is logged as text\"", "text inside a string is not code");
        text.Should().Contain("        private readonly IStatPoolService _pool;");
    }

    [Test]
    public void ATextWithoutPartFieldsIsUnchanged()
    {
        StatSource.Normalise(PartUser, new HashSet<string>(StringComparer.Ordinal)).Should().Be(PartUser);
    }

    private static readonly string ShadowingUser = Lines(
        "namespace SWLOR.Game.Server.Domain.Combat",
        "{",
        "    public class CombatRatingService : ICombatRatingService",
        "    {",
        "        private readonly IStatPoolService _pool;",
        "",
        "        private int FromParameter(IOtherService _pool) => _pool.GetMaxFP();",
        "",
        "        private int FromLocal(uint creature)",
        "        {",
        "            var _pool = Find(creature);",
        "            return _pool.GetMaxFP();",
        "        }",
        "",
        "        private int FromLambda(uint creature) => Apply(_pool => _pool.GetMaxStamina(creature));",
        "",
        "        private int FromPattern(object source) => source is IOtherService _pool ? _pool.GetCurrentFP() : 0;",
        "",
        "        private int FromField(uint creature)",
        "        {",
        "            Apply(other => other.GetMaxStamina(creature));",
        "            return this._pool.GetCurrentStamina(creature) + _pool.ReduceFP(creature);",
        "        }",
        "    }",
        "}");

    [Test]
    public void ACallThroughANameThatShadowsTheFieldKeepsItsReceiver()
    {
        var text = StatSource.Normalise(ShadowingUser, Parts);

        text.Should().Contain("private int FromParameter(IOtherService _pool) => _pool.GetMaxFP();", "a parameter shadows the field");
        text.Should().Contain("            return _pool.GetMaxFP();", "a local shadows the field");
        text.Should().Contain("Apply(_pool => _pool.GetMaxStamina(creature));", "a lambda parameter shadows the field");
        text.Should().Contain("source is IOtherService _pool ? _pool.GetCurrentFP() : 0;", "a pattern variable shadows the field");
    }

    [Test]
    public void ACallOnTheFieldLosesItsReceiverWithOrWithoutThis()
    {
        var text = StatSource.Normalise(ShadowingUser, Parts);

        text.Should().Contain("            return GetCurrentStamina(creature) + ReduceFP(creature);");
        text.Should().Contain("            Apply(other => other.GetMaxStamina(creature));", "another name in a sibling lambda is not the field");
    }

    [Test]
    public void AChangedConditionOrArgumentStillFailsANeedle()
    {
        var changed = StatSource.Normalise(PartUser.Replace("attack > 10", "attack >= 10").Replace("GetMaxFP(creature, attack)", "GetMaxFP(creature, 0)"), Parts);

        changed.Should().NotContain("if (attack > 10)");
        changed.Should().NotContain("return GetMaxFP(creature, attack);");
    }

    [Test]
    public void TheCombinedSourceHoldsEveryPartFileInOrder()
    {
        var paths = StatSource.Paths();

        paths.Select(path => path.Replace('\\', '/')).Should().HaveCount(StatSource.PartFiles.Count)
            .And.SatisfyRespectively(StatSource.PartFiles.Select(relative => (Action<string>)(path => path.Should().EndWith(relative))));
        paths.Should().OnlyContain(path => File.Exists(path));
        paths[0].Replace('\\', '/').Should().EndWith("SWLOR.Game.Server/Domain/Character/StatService.cs");
        StatSource.IsStatFile(paths[0]).Should().BeTrue();
        StatSource.IsStatFile(Path.Combine(Path.GetDirectoryName(paths[0])!, "StatusEffectService.cs")).Should().BeFalse();
    }

    [Test]
    public void TheCombinedSourceKeepsEachFileOnItsOwnLines()
    {
        var files = new[]
        {
            new StatSource.SourceFile("a.cs", "class A {}"),
            new StatSource.SourceFile("b.cs", "class B {}\r\n")
        };

        StatSource.Combine(files).Should().Be("class A {}\r\nclass B {}\r\n");
    }

    [Test]
    public void TheCombinedSourceFindsTheServiceMembers()
    {
        StatSource.Read().Should().Contain("public int GetStatAdjustment(uint creature, StatType stat)");
        StatSource.Method("GetMaxFP").ToString().Should().Contain("dbPlayer.MaxFP", "the longest overload holds the code");
        StatSource.Member("StatService", "GetStatSources").Should().Contain("CopyStatSources(perks, sources, 0);");
        StatSource.Members("GetEvasion").Should().Contain("hasNpcStatBudget").And.Contain("incomingSkillType");
        StatSource.Nodes<ClassDeclarationSyntax>().Should().Contain(type => type.Identifier.Text == "StatService");
        StatSource.ReadInterfaces().Should().Contain("int GetStatAdjustment(uint creature, StatType stat);");
    }

    [Test]
    public void EveryPartAndInterfaceFileIsDeclaredInTheServer()
    {
        var root = Path.GetDirectoryName(StatSource.Paths()[0])!;
        while (!File.Exists(Path.Combine(root, "SWLOR.Game.Server.sln")))
            root = Path.GetDirectoryName(root)!;
        var domain = Path.Combine(root, "SWLOR.Game.Server", "Domain");

        foreach (var name in StatSource.PartInterfaces)
            Directory.EnumerateFiles(domain, name + ".cs", SearchOption.AllDirectories).Should().ContainSingle(
                $"{name} is a part interface, so a file declares it");
        foreach (var relative in StatSource.InterfaceFiles)
            File.Exists(Path.Combine(root, relative)).Should().BeTrue($"{relative} is listed as an interface file");
    }
}
