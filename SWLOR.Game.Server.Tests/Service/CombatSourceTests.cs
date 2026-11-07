using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Proves <see cref="CombatSource"/> and <see cref="CombatMembers"/> keep the source-reading and reflection tests of
/// <c>Combat</c> honest while it is split: the two rewrites restore only what a move changes, and a changed condition, a
/// removed call or a different argument still fails a needle.
/// </summary>
[TestFixture]
public class CombatSourceTests
{
    private static readonly string PartService = Lines(
        "namespace SWLOR.Game.Server.Domain.Combat",
        "{",
        "    public class CombatFooService : ICombatFooService",
        "    {",
        "        private const int Limit = 5;",
        "        private readonly ICombatQueriesService _queries;",
        "        private readonly IOtherService _other;",
        "",
        "        public CombatFooService(ICombatQueriesService queries, IOtherService other)",
        "        {",
        "            _queries = queries;",
        "            _other = other;",
        "        }",
        "",
        "        private int Compute(uint creature, int damage)",
        "        {",
        "            if (damage > 10)",
        "                return _queries.GetWeapon(creature, damage);",
        "            _other.GetWeapon(creature);",
        "            return _queries.Fallback<int>(creature) + Limit;",
        "        }",
        "",
        "        public virtual void Hook() { }",
        "        public override string ToString() => string.Empty;",
        "        private static int AlreadyStatic() => 1;",
        "        public int Bonus { get; }",
        "        protected internal int Wide(int value) => value;",
        "",
        "        private sealed class Nested",
        "        {",
        "            public int Inner() => 1;",
        "        }",
        "    }",
        "}");

    private static string Lines(params string[] lines) => string.Join("\r\n", lines) + "\r\n";

    private static string Normalised(string? text = null) => CombatSource.Normalise(text ?? PartService, "CombatFooService");

    [Test]
    public void StaticIsRestoredOnMembersAtMemberIndentation()
    {
        var text = Normalised();

        text.Should().Contain("        private static int Compute(uint creature, int damage)");
        text.Should().Contain("        public static int Bonus { get; }");
        text.Should().Contain("        private static int AlreadyStatic() => 1;");
        text.Should().NotContain("static static");
    }

    [Test]
    public void StaticIsNotAddedWhereTheMoveDidNotRemoveIt()
    {
        var text = Normalised();

        text.Should().Contain("        private const int Limit = 5;");
        text.Should().Contain("        public CombatFooService(ICombatQueriesService queries, IOtherService other)");
        text.Should().Contain("        private readonly ICombatQueriesService _queries;");
        text.Should().Contain("        private readonly IOtherService _other;");
        text.Should().Contain("        public virtual void Hook() { }");
        text.Should().Contain("        public override string ToString() => string.Empty;");
        text.Should().Contain("            public int Inner() => 1;", "members of a nested type are left alone");
    }

    [Test]
    public void ACallThroughAnInjectedPartLosesItsReceiver()
    {
        var text = Normalised();

        text.Should().Contain("                return GetWeapon(creature, damage);");
        text.Should().Contain("            return Fallback<int>(creature) + Limit;");
        text.Should().Contain("            _queries = queries;", "an assignment is not a call");
    }

    [Test]
    public void ACallOnAnythingElseKeepsItsReceiver()
    {
        Normalised().Should().Contain("            _other.GetWeapon(creature);",
            "only fields declared as an ICombat...Service in the same file are rewritten");
    }

    [Test]
    public void AReceiverIsOnlyRemovedFromAFieldThisFileDeclares()
    {
        var text = Lines(
            "        private int Compute()",
            "        {",
            "            return _queries.GetWeapon(1);",
            "        }");

        Normalised(text).Should().Contain("_queries.GetWeapon(1)");
    }

    [Test]
    public void ALongerNameDoesNotLoseItsPrefixByAccident()
    {
        var text = Lines(
            "        private readonly ICombatQueriesService _queries;",
            "        private int Compute()",
            "        {",
            "            return my_queries.GetWeapon(1) + _queries2.Other(2) + x._queries.Third(3);",
            "        }");

        Normalised(text).Should().Contain("return my_queries.GetWeapon(1) + _queries2.Other(2) + x._queries.Third(3);");
    }

    [Test]
    public void ANeedleFromTheOriginalStillMatchesTheMovedBody()
    {
        var original = "if (damage > 10)\r\n                return GetWeapon(creature, damage);";

        Normalised().Should().Contain(original);
    }

    [Test]
    public void AChangedConditionStillFailsTheNeedle()
    {
        var changed = PartService.Replace("damage > 10", "damage > 11");

        Normalised(changed).Should().NotContain("if (damage > 10)");
    }

    [Test]
    public void ARemovedCallStillFailsTheNeedle()
    {
        var removed = PartService.Replace("                return _queries.GetWeapon(creature, damage);", "                return 0;");

        Normalised(removed).Should().NotContain("GetWeapon(creature, damage)");
    }

    [Test]
    public void ACallMovedToAnotherReceiverStillFailsAnExactNeedle()
    {
        var moved = PartService.Replace("return _queries.GetWeapon(creature, damage);", "return _other.GetWeapon(creature, damage);");

        Normalised(moved).Should().NotContain("return GetWeapon(creature, damage);");
    }

    [Test]
    public void ADifferentArgumentStillFailsTheNeedle()
    {
        var changed = PartService.Replace("GetWeapon(creature, damage)", "GetWeapon(creature, damage + 1)");

        Normalised(changed).Should().NotContain("GetWeapon(creature, damage);");
    }

    [Test]
    public void ALineEndingIsKept()
    {
        var text = "        private int A()\n        {\n            return _q.B();\n        }\n";

        Normalised(text).Should().Be("        private static int A()\n        {\n            return _q.B();\n        }\n");
    }

    [Test]
    public void CombinedSourceReadsTheFacadeThenThePartsThenThePartServices()
    {
        var root = Path.Combine(Path.GetTempPath(), "mr-p2-cb0-" + Guid.NewGuid().ToString("N"));
        try
        {
            var server = Path.Combine(root, "SWLOR.Game.Server");
            Write(server, "Service/Combat.cs", "FACADE\r\n");
            Write(server, "Service/CombatParts/Combat.Activity.cs", "PARTIAL\r\n");
            Write(server, "Domain/Combat/CombatRules.cs", "RULES");
            Write(server, "Domain/Combat/CombatFooService.cs", PartService);
            Write(server, "Domain/Combat/ICombatFooService.cs", "INTERFACE\r\n");
            Write(server, "Domain/Combat/CombatPointService.cs", "OTHER SERVICE\r\n");
            Write(server, "Domain/Combat/CombatRecordWriter.cs", "NOT A SERVICE\r\n");
            Write(server, "Domain/Combat/StealthService.cs", "ANOTHER\r\n");

            var files = CombatSource.Load(root);

            files.Select(file => Path.GetFileName(file.FullPath)).Should().Equal(
                "Combat.cs", "Combat.Activity.cs", "CombatFooService.cs", "CombatRules.cs");
            var text = CombatSource.Combine(files);
            text.Should().StartWith("FACADE\r\nPARTIAL\r\n");
            text.Should().Contain("private static int Compute(uint creature, int damage)");
            text.Should().Contain("RULES\r\n", "a file without a final line break still ends one");
            text.Should().NotContain("INTERFACE").And.NotContain("OTHER SERVICE").And.NotContain("NOT A SERVICE").And.NotContain("ANOTHER");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void TheRealSourceStartsWithCombatCsAndKeepsItsText()
    {
        var combat = CombatSource.Paths()[0];

        Path.GetFileName(combat).Should().Be("Combat.cs");
        Path.GetFileName(Path.GetDirectoryName(combat)).Should().Be("Service");
        CombatSource.Read().Should().Contain(File.ReadAllText(combat));
        CombatSource.IsCombatFile(combat).Should().BeTrue();
    }

    [Test]
    public void TheServicesThatAreNotPartsOfCombatExistAndAreNotRead()
    {
        var server = Path.GetDirectoryName(Path.GetDirectoryName(CombatSource.Paths()[0]))!;
        var domain = Path.Combine(server, "Domain", "Combat");

        foreach (var name in CombatParts.OtherServices)
        {
            File.Exists(Path.Combine(domain, name + ".cs")).Should().BeTrue($"{name} is a service of its own, so the exclusion is still needed");
            CombatSource.IsCombatFile(Path.Combine(domain, name + ".cs")).Should().BeFalse();
        }
    }

    [Test]
    public void TheTreesFindAMethodAndItsCallsAcrossTheFiles()
    {
        CombatSource.Trees().Should().NotBeEmpty();
        CombatSource.Method("ApplyAutoAttackSplashEffects").Identifier.Text.Should().Be("ApplyAutoAttackSplashEffects");
        CombatSource.Nodes<InvocationExpressionSyntax>().Should().NotBeEmpty();
    }

    [TestCase("CombatQueriesService", true)]
    [TestCase("CombatService", true)]
    [TestCase("CombatRules", true)]
    [TestCase("CombatPointService", false)]
    [TestCase("CombatRecorderService", false)]
    [TestCase("ICombatQueriesService", false)]
    [TestCase("PvpCombatService", false)]
    [TestCase("CombatRecordWriter", false)]
    public void PartTypeNamesFollowTheSplitPlan(string name, bool isPart)
    {
        CombatParts.IsPartTypeName(name).Should().Be(isPart);
    }

    [Test]
    public void ReflectionFindsThePrivateMembersTheTestsUse()
    {
        CombatMembers.FindMethod("CalculateResourceRestoreFromCost").Should().NotBeNull();
        CombatMembers.FindMethod("TryUseStatTrigger", typeof(uint), typeof(StatType), typeof(int)).Should().NotBeNull();
        CombatMembers.FindField("_attackSwingDebts").Should().NotBeNull();
        CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts").Should().NotBeNull();
    }

    [Test]
    public void ReflectionReturnsNullForAMemberThatDoesNotExist()
    {
        CombatMembers.FindMethod("NoSuchMethod").Should().BeNull();
        CombatMembers.FindField("_noSuchField").Should().BeNull();
        var invoke = () => CombatMembers.FieldValue<int>("_noSuchField");
        invoke.Should().Throw<MissingFieldException>();
    }

    private static void Write(string server, string relative, string text)
    {
        var path = Path.Combine(server, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
