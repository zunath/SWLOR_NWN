using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
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
    public void APartsOneLineForwardersAreLeftOutAndEverythingElseStays()
    {
        var partial = Lines(
            "namespace SWLOR.Game.Server.Service",
            "{",
            "    public static partial class Combat",
            "    {",
            "        private static ICombatQueriesService Queries => FacadeAccessor.Resolve<ICombatQueriesService>();",
            "        private static ICombatRecorderService Recorder => FacadeAccessor.Resolve<ICombatRecorderService>();",
            "",
            "        /// <inheritdoc cref=\"ICombatQueriesService.GetWeapon\"/>",
            "        public static uint GetWeapon(uint creature) => Queries.GetWeapon(creature);",
            "",
            "        private static bool IsBehind(uint a, uint b) =>",
            "            Queries.IsBehind(a, b);",
            "",
            "        public static uint Renamed(uint creature) => Queries.GetWeapon(creature);",
            "        public static int Rule(int value) => CombatRules.Rule(value);",
            "        public static void Record(uint creature) => Recorder.Record(creature);",
            "        public static uint Block(uint creature)",
            "        {",
            "            return Queries.Block(creature);",
            "        }",
            "    }",
            "}");

        var text = CombatSource.ElidePartForwarders(partial);

        text.Should().NotContain("GetWeapon(uint creature) => Queries.GetWeapon").And.NotContain("inheritdoc").And.NotContain("IsBehind");
        text.Should().Contain("public static uint Renamed(uint creature) => Queries.GetWeapon(creature);", "it forwards to another name");
        text.Should().Contain("public static int Rule(int value) => CombatRules.Rule(value);", "rule forwarders stay");
        text.Should().Contain("public static void Record(uint creature) => Recorder.Record(creature);", "the recorder is not a part");
        text.Should().Contain("            return Queries.Block(creature);", "a block body is code, not a forwarder");
        text.Should().Contain("private static ICombatQueriesService Queries => FacadeAccessor.Resolve<ICombatQueriesService>();");
        CombatSource.ElidePartForwarders("namespace N { class C { int A() => B.A(); } }").Should().Be("namespace N { class C { int A() => B.A(); } }");
    }

    [Test]
    public void TheRealSourceHoldsAMovedMemberOnceWithItsBody()
    {
        var source = CombatSource.Read();

        Occurrences(source, "uint GetRelevantSkillWeapon(").Should().Be(1);
        Occurrences(source, "bool TryUseStatTrigger(").Should().Be(2, "it has two overloads");
        CombatSource.Method("GetRelevantSkillWeapon").Body.Should().NotBeNull("the one declaration left is the body");
    }

    private static int Occurrences(string text, string needle)
    {
        var count = 0;
        for (var index = text.IndexOf(needle, StringComparison.Ordinal); index >= 0; index = text.IndexOf(needle, index + 1, StringComparison.Ordinal))
            count++;
        return count;
    }

    [Test]
    public void ACallOnACombatServiceThatIsNotAPartKeepsItsReceiver()
    {
        var text = Lines(
            "        private readonly ICombatRecorderService _combatRecorder;",
            "        private readonly ICombatPointService _combatPoint;",
            "        private void Record(uint creature)",
            "        {",
            "            using var scope = _combatRecorder.BeginRiderAttribution(creature, 1);",
            "            _combatPoint.AddCombatPoint(creature);",
            "        }");

        var normalised = Normalised(text);

        normalised.Should().Contain("using var scope = _combatRecorder.BeginRiderAttribution(creature, 1);",
            "CombatRecorder converted on its own, so the move did not take its facade's name away");
        normalised.Should().Contain("_combatPoint.AddCombatPoint(creature);");
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
        CombatMembers.FindField("_lastAttackActivity").Should().NotBeNull();
        CombatMembers.FindField("_lastCombatAbilityUse").Should().NotBeNull();
    }

    [Test]
    public void ReflectionReachesTheTimingAndActivityTablesOfTheInstalledContainer()
    {
        // CombatAttackDelayTests reads the swing debts and RifleIdleReadinessEngineTests the activity records this way.
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;

        var debts = CombatMembers.FieldValue<Dictionary<uint, float>>("_attackSwingDebts");
        ((object)debts).Should().BeSameAs(typeof(CombatAttackTimingService).GetField("_attackSwingDebts", Instance)!
            .GetValue(facades.Get<ICombatAttackTimingService>()));
        var attacks = CombatMembers.FieldValue<Dictionary<uint, DateTime>>("_lastAttackActivity");
        ((object)attacks).Should().BeSameAs(typeof(CombatActivityService).GetField("_lastAttackActivity", Instance)!
            .GetValue(facades.Get<ICombatActivityService>()));
        var abilities = CombatMembers.FieldValue<Dictionary<uint, DateTime>>("_lastCombatAbilityUse");
        ((object)abilities).Should().BeSameAs(typeof(CombatActivityService).GetField("_lastCombatAbilityUse", Instance)!
            .GetValue(facades.Get<ICombatActivityService>()));

        SWLOR.Game.Server.Service.Combat.ConsumeAttacksPerSwing(0xFFFFFFF1, 1500);
        debts.Should().ContainKey(0xFFFFFFF1, "the facade and the reflected table are the same instance's");
        SWLOR.Game.Server.Service.Combat.ClearAttackSwingDebt(0xFFFFFFF1);
        debts.Should().NotContainKey(0xFFFFFFF1);
    }

    [TestCase("_autoAttackCycleCriticalCounts")]
    [TestCase("_repeatedTargetDamageStates")]
    [TestCase("_meleeRepeatedTargetDamageStates")]
    [TestCase("_rangedRepeatedTargetDamageStates")]
    [TestCase("_sameTargetPressureStates")]
    public void ReflectionReachesTheDamageModifierTablesOfTheInstalledContainer(string name)
    {
        // The damage modifier stacks moved from static fields of Combat to the instance; a test that reads one by name
        // gets the installed container's table.
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var field = CombatMembers.FindField(name);
        field.Should().NotBeNull();
        field!.IsStatic.Should().BeFalse();
        field.DeclaringType.Should().Be(typeof(CombatDamageModifiersService));
        CombatMembers.FieldValue<object>(name).Should().BeSameAs(field.GetValue(facades.Get<ICombatDamageModifiersService>()));
        typeof(SWLOR.Game.Server.Service.Combat).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).Should().BeNull(
            "Combat keeps no copy");
    }

    [TestCase("_criticalHitSequenceStates", typeof(CombatHitEffectsService))]
    [TestCase("_autoAttackCycleCounts", typeof(CombatHitEffectsService))]
    [TestCase("_sourceStatusAutoAttackCycleCounts", typeof(CombatHitEffectsService))]
    [TestCase("_meleeAutoAttackCycleCounts", typeof(CombatHitEffectsService))]
    [TestCase("MimicryTraitProcs", typeof(CombatHitEffectsService))]
    [TestCase("_abilityHitResolutionOverride", typeof(CombatAbilityHitService))]
    [TestCase("_autoAttackHitResolutionOverride", typeof(CombatAbilityHitService))]
    public void ReflectionReachesTheHitTablesAndOverridesOfTheInstalledContainer(string name, Type owner)
    {
        // The hit effect tables, the proc table and the two test overrides of the hit rolls moved from static fields of Combat
        // to the instance; a test that reads one by name gets the installed container's.
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var field = CombatMembers.FindField(name);
        field.Should().NotBeNull();
        field!.IsStatic.Should().BeFalse();
        field.IsInitOnly.Should().Be(!name.EndsWith("Override", StringComparison.Ordinal), "only the test overrides are written after construction");
        field.DeclaringType.Should().Be(owner);
        object instance = owner == typeof(CombatHitEffectsService)
            ? facades.Get<ICombatHitEffectsService>()
            : facades.Get<ICombatAbilityHitService>();
        if (field.FieldType.IsValueType)
            CombatMembers.FieldValue<object>(name).Should().Be(field.GetValue(instance));
        else
            CombatMembers.FieldValue<object>(name).Should().BeSameAs(field.GetValue(instance));
        typeof(SWLOR.Game.Server.Service.Combat).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).Should().BeNull(
            "Combat keeps no copy");
    }

    [Test]
    public void TheTestOverridesOfTheHitRollsAreTheInstalledContainersAndCombatSetsThem()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        SWLOR.Game.Server.Service.Combat.SetAbilityHitResolutionOverride(false);
        SWLOR.Game.Server.Service.Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            CombatMembers.FieldValue<bool?>("_abilityHitResolutionOverride").Should().BeFalse();
            CombatMembers.FieldValue<bool?>("_autoAttackHitResolutionOverride").Should().BeTrue();
            SWLOR.Game.Server.Service.Combat.GetAutoAttackHitResolutionOverride().Should().BeTrue();
            facades.Get<ICombatAbilityHitService>().GetAutoAttackHitResolutionOverride().Should().BeTrue();
        }
        finally
        {
            SWLOR.Game.Server.Service.Combat.SetAbilityHitResolutionOverride(null);
            SWLOR.Game.Server.Service.Combat.SetAutoAttackHitResolutionOverride(null);
        }

        CombatMembers.FieldValue<bool?>("_abilityHitResolutionOverride").Should().BeNull();
        SWLOR.Game.Server.Service.Combat.GetAutoAttackHitResolutionOverride().Should().BeNull();
    }

    [Test]
    public void ReflectionReachesAPartsInstanceMembersInTheInstalledContainer()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        var resources = facades.Get<ICombatResourcesService>();

        var cooldowns = CombatMembers.FieldValue<Dictionary<(uint, StatType), DateTime>>("_statTriggerCooldowns");
        ((object)cooldowns).Should().BeSameAs(typeof(CombatResourcesService)
            .GetField("_statTriggerCooldowns", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(resources));
        CombatMembers.FindMethod("TryUseStatTrigger", typeof(uint), typeof(StatType), typeof(int))!.IsStatic.Should().BeTrue(
            "the static forwarder on Combat is found first");
        var trigger = CombatMembers.CreateDelegate<Func<uint, StatType, int, bool>>(
            "TryUseStatTrigger", typeof(uint), typeof(StatType), typeof(int));
        trigger(0xFFFFFFF0, StatType.SideAttackSkillType, 30).Should().BeTrue();
        cooldowns.Should().ContainKey((0xFFFFFFF0, StatType.SideAttackSkillType));

        CombatMembers.FindMethod("IsWeaponOrForceAbility")!.IsStatic.Should().BeFalse("only the queries service has it");
        CombatMembers.Invoke("IsWeaponOrForceAbility", SkillType.Force).Should().Be(true);
        CombatMembers.CreateDelegate<Func<SkillType, bool>>("IsWeaponOrForceAbility")(SkillType.Mimicry).Should().BeFalse();
        CombatMembers.Invoke("CalculateResourceRestoreFromCost", 10, 50).Should().Be(5, "a static member needs no instance");
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
