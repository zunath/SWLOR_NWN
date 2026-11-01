using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class WalkModeTests
{
    private Type _movement = null!;
    private Type _world = null!;

    [OneTimeSetUp]
    public void CompileProductionMovementMethods()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var stat = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
            "SWLOR.Game.Server", "Service", "Stat.cs"))).GetRoot();
        var methodNames = new HashSet<string>
        {
            "IsWalkModeEnabled", "ClearWalkModeBeforeDMPossessionChange", "ToggleWalkMode", "ApplyCreatureMovementRate", "GetMovementSpeedMultiplier",
            "GetMovementSpeedAdjustment", "GetBaseMovementSpeedIncrease"
        };
        var methods = stat.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => methodNames.Contains(method.Identifier.Text)).ToArray();
        methods.Should().HaveCount(methodNames.Count);
        var fields = stat.DescendantNodes().OfType<FieldDeclarationSyntax>().Where(field =>
            field.Declaration.Variables.Any(variable => variable.Identifier.Text.Contains("Walk", StringComparison.Ordinal) ||
                variable.Identifier.Text.Contains("MovementSpeed", StringComparison.Ordinal)));
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Service.StatService;
            using SWLOR.Game.Server.Core;
            using SWLOR.NWN.API.NWScript.Enum;
            using static World;
            public static class Movement
            {
                {{string.Join("\n", fields.Select(field => field.ToFullString()))}}
                {{string.Join("\n", methods.Select(method => method.ToFullString()))}}
            }
            public static class CreaturePlugin
            {
                public static float GetMovementRateFactor(uint creature) => NativeRate;
                public static void SetMovementRateFactor(uint creature, float value) => NativeRate = Math.Max(0.125f, value);
                public static void SetMovementRate(uint creature, MovementRate rate) => BaseRateSets++;
            }
            public static class PlayerPlugin
            {
                public static void SetAlwaysWalk(uint creature, bool value) => AlwaysWalk = value;
            }
            public static class Droid { public static bool IsDroid(uint creature) => false; }
            public static class BeastMastery { public static bool IsPlayerBeast(uint creature) => false; }
            public static class World
            {
                public static bool IsDM, IsPossessed, AlwaysWalk;
                public static bool IsPlayer;
                public const uint OBJECT_SELF = 1;
                public static float NativeRate;
                public static int Adjustment, Disabled, NativeSlow, BaseRateSets, OutOfCombatAdjustment;
                public static bool InCombat;
                public static readonly Dictionary<string, int> Ints = new();
                public static readonly Dictionary<string, float> Floats = new();
                public static bool GetIsObjectValid(uint creature) => creature == 1;
                public static ObjectType GetObjectType(uint creature) => ObjectType.Creature;
                public static bool GetIsPC(uint creature) => IsPlayer && !IsPossessed;
                public static bool GetIsDM(uint creature) => IsDM;
                public static bool GetIsDMPossessed(uint creature) => IsPossessed;
                public static int GetLocalInt(uint creature, string key) => Ints.GetValueOrDefault(key);
                public static void SetLocalInt(uint creature, string key, int value) => Ints[key] = value;
                public static void DeleteLocalInt(uint creature, string key) => Ints.Remove(key);
                public static float GetLocalFloat(uint creature, string key) => Floats.GetValueOrDefault(key);
                public static void SetLocalFloat(uint creature, string key, float value) => Floats[key] = value;
                public static void DeleteLocalFloat(uint creature, string key) => Floats.Remove(key);
                public static int GetStatAdjustment(uint creature, StatType type) =>
                    type == StatType.MovementSpeedDisabled ? Disabled
                    : type == StatType.OutOfCombatMovementSpeedPercentAdjustment ? OutOfCombatAdjustment
                    : Adjustment;
                public static bool GetIsInCombat(uint creature) => InCombat;
                public static int GetFirstEffect(uint creature) => NativeSlow > 0 ? 1 : 0;
                public static int GetNextEffect(uint creature) => 0;
                public static bool GetIsEffectValid(int effect) => effect != 0;
                public static EffectTypeScript GetEffectType(int effect) => EffectTypeScript.MovementSpeedDecrease;
                public static int GetEffectInteger(int effect, int index) => NativeSlow;
                public static void Reset()
                {
                    IsDM = IsPossessed = AlwaysWalk = false;
                    IsPlayer = true;
                    NativeRate = 1.25f;
                    Adjustment = Disabled = NativeSlow = BaseRateSets = OutOfCombatAdjustment = 0;
                    InCombat = false;
                    Ints.Clear(); Floats.Clear();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(new[] { typeof(StatType).Assembly.Location, typeof(ObjectType).Assembly.Location })
            .Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("WalkModeHarness", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        _movement = assembly.GetType("Movement")!;
        _world = assembly.GetType("World")!;
    }

    [SetUp]
    public void Reset() => _world.GetMethod("Reset")!.Invoke(null, null);

    [TestCase(false, 0.65f)]
    [TestCase(false, 1f)]
    [TestCase(false, 1.4f)]
    [TestCase(false, 2f)]
    [TestCase(true, 0.65f)]
    [TestCase(true, 1f)]
    [TestCase(true, 1.4f)]
    [TestCase(true, 2f)]
    public void StaffTogglesRestoreTheirNativeFactor(bool possessed, float previousRate)
    {
        Set("IsDM", !possessed);
        Set("IsPossessed", possessed);
        Set("NativeRate", previousRate);
        for (var cycle = 0; cycle < 3; cycle++)
        {
            Toggle().Should().BeTrue();
            Get<bool>("AlwaysWalk").Should().BeTrue();
            Get<float>("NativeRate").Should().BeApproximately(Math.Min(1f, previousRate), 0.001f);
            Invoke("ApplyCreatureMovementRate");
            Toggle().Should().BeFalse();
            Get<bool>("AlwaysWalk").Should().BeFalse();
            Get<float>("NativeRate").Should().BeApproximately(previousRate, 0.001f);
            Get<Dictionary<string, float>>("Floats").Should().BeEmpty();
        }
        Get<int>("BaseRateSets").Should().Be(0);
    }

    [Test]
    public void StaffRestorationAccountsForModifierChangesWhileWalking()
    {
        Set("IsDM", true);
        Set("NativeRate", 1.4f);
        Set("Adjustment", 20);
        Toggle();
        Set("Adjustment", -20);
        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().BeApproximately(0.8f, 0.001f);
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(1f, 0.001f);
    }

    [TestCase(false, 50)]
    [TestCase(false, 80)]
    [TestCase(false, -125)]
    [TestCase(true, 50)]
    [TestCase(true, 80)]
    [TestCase(true, -125)]
    public void StaffSnapshotsPreserveTheBaselineBehindMovementCaps(bool possessed, int adjustment)
    {
        Set("IsDM", !possessed);
        Set("IsPossessed", possessed);
        Set("Adjustment", adjustment);
        Invoke("ApplyCreatureMovementRate");
        var previousRate = Get<float>("NativeRate");
        Toggle();
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(previousRate, 0.001f,
            "an unchanged modifier retains the same capped or native-minimum rate");
        Toggle();
        Set("Adjustment", 0);
        Invoke("ApplyCreatureMovementRate");
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(1.3f, 0.001f,
            "expiration restores the unclamped staff baseline");
    }

    [TestCase(2f, 20)]
    [TestCase(1.4f, 50)]
    public void NativeStaffOverridesRetainTheirModifierChanges(float previousRate, int adjustment)
    {
        Set("IsDM", true);
        Set("NativeRate", previousRate);
        Toggle();
        Set("Adjustment", adjustment);
        Invoke("ApplyCreatureMovementRate");
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(previousRate + adjustment * 0.01f, 0.001f);
    }

    [Test]
    public void PossessionChangeClearsOutgoingCreatureWalkStateBeforeRelease()
    {
        Set("IsPlayer", false);
        Set("IsPossessed", true);
        Set("NativeRate", 1.3f);
        for (var cycle = 0; cycle < 2; cycle++)
        {
            Toggle();
            Invoke("ClearWalkModeBeforeDMPossessionChange");
            Get<bool>("AlwaysWalk").Should().BeFalse();
            ((bool)Invoke("IsWalkModeEnabled")).Should().BeFalse();
            Set("IsPossessed", false);
            Invoke("ApplyCreatureMovementRate");
            Get<float>("NativeRate").Should().BeApproximately(1.3f, 0.001f);
            Get<Dictionary<string, float>>("Floats").Should().BeEmpty();
            Set("IsPossessed", true);
            ((bool)Invoke("IsWalkModeEnabled")).Should().BeFalse();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReleasedNpcCannotRetainTheWalkCapIfThePossessionEventWasMissed(bool disabled)
    {
        Set("IsPlayer", false);
        Set("IsPossessed", true);
        Set("NativeRate", 1.3f);
        Toggle();
        Set("Disabled", disabled ? 1 : 0);
        Set("IsPossessed", false);
        ((bool)Invoke("IsWalkModeEnabled")).Should().BeFalse();
        ((float)Invoke("GetMovementSpeedMultiplier")).Should().BeApproximately(disabled ? 0f : 1.3f, 0.001f);
        Invoke("ApplyCreatureMovementRate");
        Get<Dictionary<string, int>>("Ints").Should().NotContainKey("WALK_TOGGLE");
        Set("Disabled", 0);
        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().BeApproximately(1.3f, 0.001f);
        Get<Dictionary<string, float>>("Floats").Should().BeEmpty();
        Set("IsPossessed", true);
        ((bool)Invoke("IsWalkModeEnabled")).Should().BeFalse();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void StaffRestorationSurvivesDisabledMovementAndFurtherToggles(bool toggleWhileDisabled, bool useSlow)
    {
        Set("IsDM", true);
        Set("NativeRate", 1.4f);
        Toggle();
        Set(useSlow ? "Adjustment" : "Disabled", useSlow ? -200 : 1);
        Toggle();
        Get<float>("NativeRate").Should().Be(0.125f);
        Get<Dictionary<string, float>>("Floats").Should().NotBeEmpty();
        if (toggleWhileDisabled)
        {
            Toggle();
            Toggle();
        }
        Set("Disabled", 0);
        Set("Adjustment", 0);
        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().BeApproximately(1.4f, 0.001f);
        Get<Dictionary<string, float>>("Floats").Should().BeEmpty();
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void StaffEnteringWalkWhileAlreadyDisabledNeverSnapshotsTheDisabledFactor(bool recoverWhileWalking, bool useSlow)
    {
        Set("IsDM", true);
        Set("NativeRate", 1.4f);
        Set(useSlow ? "Adjustment" : "Disabled", useSlow ? -200 : 1);
        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().Be(0.125f);
        Toggle();
        Toggle();
        Toggle();
        if (!recoverWhileWalking)
            Toggle();
        Set("Disabled", 0);
        Set("Adjustment", 0);
        Invoke("ApplyCreatureMovementRate");
        if (recoverWhileWalking)
        {
            Get<float>("NativeRate").Should().Be(1f);
            Toggle();
        }
        Get<float>("NativeRate").Should().BeApproximately(1.3f, 0.001f,
            "the pre-existing staff refresh baseline applies when no valid prior factor could be captured");
        Get<Dictionary<string, float>>("Floats").Should().BeEmpty();
    }

    [Test]
    public void LegacyStaffWalkStateWithoutSnapshotRetainsItsNativeFactor()
    {
        Set("IsDM", true);
        Set("NativeRate", 1.4f);
        Get<Dictionary<string, int>>("Ints")["WALK_TOGGLE"] = 1;
        Toggle().Should().BeFalse();
        Get<float>("NativeRate").Should().Be(1.4f);
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(20)]
    [TestCase(25)]
    [TestCase(50)]
    public void WalkAppliesStatSlowsFromTheNormalBaseline(int slow)
    {
        Set("Adjustment", -slow);
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(1f - slow * 0.01f, 0.001f);
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(1.25f - slow * 0.01f, 0.001f);
    }

    [Test]
    public void WalkAlsoAppliesNativeEffectSlowsFromTheNormalBaseline()
    {
        Set("NativeSlow", 20);
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(0.8f, 0.001f);
        Toggle();
        Get<float>("NativeRate").Should().BeApproximately(1.05f, 0.001f);
    }

    [TestCase(10)]
    [TestCase(15)]
    public void OutOfCombatSpeedAppliesOnlyWhileTheCreatureIsNotInCombat(int percent)
    {
        Set("OutOfCombatAdjustment", percent);

        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().BeApproximately(1.25f + percent * 0.01f, 0.001f);

        Set("InCombat", true);
        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().BeApproximately(1.25f, 0.001f);

        Set("InCombat", false);
        Invoke("ApplyCreatureMovementRate");
        Get<float>("NativeRate").Should().BeApproximately(1.25f + percent * 0.01f, 0.001f);
    }

    private bool Toggle() => (bool)Invoke("ToggleWalkMode")!;
    private object Invoke(string method)
    {
        var target = _movement.GetMethod(method)!;
        return target.Invoke(null, target.GetParameters().Length == 0 ? [] : [1u]);
    }
    private void Set(string field, object value) => _world.GetField(field)!.SetValue(null, value);
    private T Get<T>(string field) => (T)_world.GetField(field)!.GetValue(null)!;
}
