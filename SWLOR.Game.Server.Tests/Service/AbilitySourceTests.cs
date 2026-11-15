using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Proves <see cref="AbilitySource"/> keeps the source-reading tests of <c>AbilityService</c> honest while it is split: it
/// reads every part file, the one rewrite removes only the receiver a move added, and a changed condition, a removed call or
/// a different argument still fails a needle.
/// </summary>
[TestFixture]
public class AbilitySourceTests
{
    private static readonly IReadOnlySet<string> Parts = new HashSet<string>(StringComparer.Ordinal) { "IAbilityFooState" };

    private static readonly string PartUser = Lines(
        "namespace SWLOR.Game.Server.Domain.Abilities",
        "{",
        "    public class AbilityService : IAbilityService",
        "    {",
        "        private readonly IAbilityFooState _fooState;",
        "        private readonly IOtherService _other;",
        "",
        "        private int Compute(uint creature, int damage)",
        "        {",
        "            if (damage > 10)",
        "                return _fooState.GetTracked(creature, damage);",
        "            _other.GetTracked(creature);",
        "            var count = _fooState.Count;",
        "            if (_fooState.Count < 10)",
        "                Log(\"_fooState.GetTracked(creature) is logged as text\");",
        "            return _fooState.Fallback<int>(creature) + count;",
        "        }",
        "    }",
        "}");

    private static string Lines(params string[] lines) => string.Join("\r\n", lines) + "\r\n";

    [Test]
    public void ACallOnAnInjectedPartLosesItsReceiver()
    {
        var text = AbilitySource.Normalise(PartUser, Parts);

        text.Should().Contain("                return GetTracked(creature, damage);");
        text.Should().Contain("            return Fallback<int>(creature) + count;");
    }

    [Test]
    public void OtherReceiversAndDeclarationsStayAsTheyAre()
    {
        var text = AbilitySource.Normalise(PartUser, Parts);

        text.Should().Contain("            _other.GetTracked(creature);");
        text.Should().Contain("            var count = _fooState.Count;", "a property read is not a call the move rewrote");
        text.Should().Contain("            if (_fooState.Count < 10)", "a property compared with < is not a generic call");
        text.Should().Contain("\"_fooState.GetTracked(creature) is logged as text\"", "text inside a string is not code");
        text.Should().Contain("        private readonly IAbilityFooState _fooState;");
    }

    [Test]
    public void ATextWithoutPartFieldsIsUnchanged()
    {
        AbilitySource.Normalise(PartUser, new HashSet<string>(StringComparer.Ordinal)).Should().Be(PartUser);
    }

    [Test]
    public void AChangedConditionOrArgumentStillFailsANeedle()
    {
        var changed = AbilitySource.Normalise(PartUser.Replace("damage > 10", "damage >= 10").Replace("GetTracked(creature, damage)", "GetTracked(creature, 0)"), Parts);

        changed.Should().NotContain("if (damage > 10)");
        changed.Should().NotContain("return GetTracked(creature, damage);");
    }

    [Test]
    public void TheCombinedSourceHoldsEveryPartFileInOrder()
    {
        var paths = AbilitySource.Paths();

        paths.Select(path => path.Replace('\\', '/')).Should().HaveCount(AbilitySource.PartFiles.Count)
            .And.SatisfyRespectively(AbilitySource.PartFiles.Select(relative => (Action<string>)(path => path.Should().EndWith(relative))));
        paths.Should().OnlyContain(path => File.Exists(path));
        paths[0].Replace('\\', '/').Should().EndWith("SWLOR.Game.Server/Domain/Abilities/AbilityService.cs");
        AbilitySource.IsAbilityFile(paths[0]).Should().BeTrue();
        AbilitySource.IsAbilityFile(Path.Combine(Path.GetDirectoryName(paths[0])!, "MimicryService.cs")).Should().BeFalse();
    }

    [Test]
    public void TheCombinedSourceKeepsEachFileOnItsOwnLines()
    {
        var files = new[]
        {
            new AbilitySource.SourceFile("a.cs", "class A {}"),
            new AbilitySource.SourceFile("b.cs", "class B {}\r\n")
        };

        AbilitySource.Combine(files).Should().Be("class A {}\r\nclass B {}\r\n");
    }

    [Test]
    public void TheCombinedSourceFindsTheServiceMembers()
    {
        AbilitySource.Read().Should().Contain("public bool CanUseAbility(");
        AbilitySource.Method("CanUseAbility").Identifier.Text.Should().Be("CanUseAbility");
        AbilitySource.Members("GetAbilityDetail").Should().Contain("is not registered to an ability");
        AbilitySource.Nodes<ClassDeclarationSyntax>().Should().Contain(type => type.Identifier.Text == "AbilityService");
    }

    [Test]
    public void EveryPartInterfaceIsDeclaredInTheServer()
    {
        var root = Path.GetDirectoryName(AbilitySource.Paths()[0])!;
        while (!File.Exists(Path.Combine(root, "SWLOR.Game.Server.sln")))
            root = Path.GetDirectoryName(root)!;
        var domain = Path.Combine(root, "SWLOR.Game.Server", "Domain");

        foreach (var name in AbilitySource.PartInterfaces)
            Directory.EnumerateFiles(domain, name + ".cs", SearchOption.AllDirectories).Should().ContainSingle(
                $"{name} is a part interface, so a file declares it");
    }
}
