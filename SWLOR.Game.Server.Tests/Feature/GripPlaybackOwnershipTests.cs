using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class GripPlaybackOwnershipTests
{
    private MethodInfo _run = null!;

    [OneTimeSetUp]
    public void CompileProductionPlaybackAgainstRecordingRuntime()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        string Read(string path) => File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", path));
        // Compile the real native adapter as well: its ownership handoff must not be duplicated in the fake.
        var native = CSharpSyntaxTree.ParseText(Read("Service/NamedAnimation.cs")).GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "NativeRuntime");
        var adapter = $$"""
            using System;
            using static World;
            using SWLOR.Game.Server.Feature;
            using SWLOR.NWN.API.NWScript.Enum;
            namespace SWLOR.Game.Server.Service;
            public static class NamedHarness
            {
                private const string PlaybackTokenVariable = "_NAMED_ANIMATION_PLAYBACK";
                {{native.ToFullString()}}
                public static NamedAnimationPlayback Create() => new(new NativeRuntime());
            }
            """;
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("using System; using static World;\n" + Read("Feature/Grip.cs")),
            CSharpSyntaxTree.ParseText(Read("Service/NamedAnimationPlayback.cs")),
            CSharpSyntaxTree.ParseText(Read("Service/AnimationService/AnimationClip.cs")),
            CSharpSyntaxTree.ParseText(adapter), CSharpSyntaxTree.ParseText(Harness)
        };
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(path => !Path.GetFileName(path).StartsWith("SWLOR.Game.Server", StringComparison.Ordinal))
            .Append(typeof(BaseItem).Assembly.Location).Distinct();
        var compilation = CSharpCompilation.Create("GripOwnershipHarness_" + Guid.NewGuid().ToString("N"), trees,
            paths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        emitted.Success.Should().BeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        _run = Assembly.Load(stream.ToArray()).GetType("World")!.GetMethod("Run")!;
    }

    [TestCase("normal")]
    [TestCase("rifle")]
    [TestCase("same-mode-weapon")]
    [TestCase("new-gesture")]
    [TestCase("named-owner")]
    [TestCase("deferred-start")]
    [TestCase("queued-attack")]
    [TestCase("unowned-alias")]
    [TestCase("native-dodge")]
    [TestCase("combat-toggle")]
    [TestCase("combat-deactivate")]
    [TestCase("deferred-combat")]
    public void StaleGripWorkCannotReleaseAnotherOwner(string scenario) =>
        _run.Invoke(null, [scenario]).Should().Be(true);

    private const string Harness = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using SWLOR.Game.Server.Feature;
        using SWLOR.Game.Server.Service;
        using SWLOR.Game.Server.Service.AnimationService;
        using SWLOR.NWN.API.NWScript.Enum;
        using SWLOR.NWN.API.NWScript.Enum.Item;
        namespace SWLOR.Game.Server.Core
        {
            public sealed class NWNEventHandlerAttribute : Attribute { public NWNEventHandlerAttribute(string name) {} }
            public static class ScriptName { public const string OnModuleRespawn = "respawn"; }
        }
        namespace SWLOR.Game.Server.Entity
        {
            public class PlayerSettings { public bool AlternateGripEnabled; }
            public class Player { public PlayerSettings Settings = new(); }
        }
        namespace SWLOR.Game.Server.Service
        {
            public static class DB { public static T Get<T>(string id) where T:new() => new(); public static void Set(object value) {} }
            public static class QueuedAttackAnimation { public static bool IsActive(uint c) => World.AttackActive; }
            public static class QueuedAttackAnimationPlayback
            {
                public static System.Collections.Generic.IReadOnlyList<string> SwingKeys { get; } = new[] { "1h", "2h", "2w", "pl" }
                    .SelectMany(p => new[] { "slashl", "slashr", "slasho", "stab", "closeh", "closel", "reach" }.Select(s => p + s)).ToArray();
            }
        }
        public static class World
        {
            static Dictionary<string, object> locals = new();
            public static Dictionary<string, string> Maps = new();
            static List<Action> delays = new();
            static Queue<Action> assigned = new();
            static uint right = 10, left;
            public static bool AttackActive, Defer, InCombat;
            public static int GestureCount;
            public static bool GetIsInCombat(uint c) => InCombat;
            public static bool GetIsPC(uint c) => true;
            public static bool GetIsDM(uint c) => false;
            public static bool GetIsDead(uint c) => false;
            public static bool GetIsObjectValid(uint c) => c != 0;
            public static ObjectType GetObjectType(uint c) => ObjectType.Creature;
            public static string GetObjectUUID(uint c) => "player";
            public static uint GetLastRespawnButtonPresser() => 1;
            public static uint GetModule() => 100;
            public static uint GetItemInSlot(InventorySlot slot, uint c) => slot == InventorySlot.RightHand ? right : left;
            public static BaseItem GetBaseItemType(uint c) => c == 20 ? BaseItem.Rifle : BaseItem.Saberstaff;
            static T Get<T>(string key) => locals.TryGetValue(key, out var value) ? (T)value : default;
            public static bool GetLocalBool(uint c, string key) => Get<bool>(key);
            public static void SetLocalBool(uint c, string key, bool value) => locals[key] = value;
            public static int GetLocalInt(uint c, string key) => Get<int>(key);
            public static void SetLocalInt(uint c, string key, int value) => locals[key] = value;
            public static uint GetLocalObject(uint c, string key) => Get<uint>(key);
            public static void SetLocalObject(uint c, string key, uint value) => locals[key] = value;
            public static string GetLocalString(uint c, string key) => Get<string>(key) ?? "";
            public static void SetLocalString(uint c, string key, string value) => locals[key] = value;
            public static void DeleteLocalString(uint c, string key) => locals.Remove(key);
            public static void ReplaceObjectAnimation(uint c, string from, string to) => Maps[from] = to;
            public static void AssignCommand(uint c, Action action) { if (Defer) assigned.Enqueue(action); else action(); }
            public static void DelayCommand(float seconds, Action action) => delays.Add(action);
            public static void PlayAnimation(Animation animation, float speed = 1, float duration = 0) { GestureCount++; }
            public static void SendMessageToPC(uint c, string text) {}
            public static ActionType GetCurrentAction(uint c) => ActionType.Invalid;
            public static void ClearAllActions(uint oObject) {}
            static void Check(bool condition) { if (!condition) throw new Exception("Playback ownership violated"); }
            public static bool Run(string scenario)
            {
                locals.Clear(); Maps.Clear(); delays.Clear(); assigned.Clear();
                right = 10; left = 0; AttackActive = false; InCombat = scenario == "combat-toggle"; GestureCount = 0;
                Defer = scenario is "deferred-start" or "deferred-combat";
                SetLocalBool(1, "DOUBLEHAND_ENABLED", true);
                if (scenario == "unowned-alias")
                {
                    Maps["dodges"] = "other_owner";
                    right = 20; Grip.Refresh(1, true);
                    Check(Maps["dodges"] == "other_owner"); return true;
                }
                Grip.Refresh(1, true);
                if (scenario == "combat-toggle")
                {
                    Check(GetLocalInt(1, "DOUBLEHAND_MODE") == 2 && Maps["2hstab"] == "1hstab");
                    Check(!Maps.ContainsKey("dodges") && GestureCount == 0); return true;
                }
                if (scenario == "combat-deactivate")
                {
                    var before = GestureCount;
                    InCombat = true; Grip.Toggle(1);
                    Check(GetLocalInt(1, "DOUBLEHAND_MODE") == 0 && GestureCount == before && Maps["2hstab"] == "");
                    return true;
                }
                if (scenario == "deferred-combat")
                {
                    InCombat = true; Defer = false;
                    while (assigned.TryDequeue(out var callback)) callback();
                    Check(!Maps.ContainsKey("dodges") && GestureCount == 0); return true;
                }
                var oldTimers = delays.ToArray();
                if (scenario == "native-dodge")
                {
                    Grip.ReleaseActivationParryForNativeDodge(1);
                    Check(Maps["dodges"] == "" && GetLocalInt(1, "DOUBLEHAND_MODE") == 2);
                    foreach (var timer in oldTimers.Reverse()) timer();
                    Check(Maps["plpause1"] == "sw_nohold" && Maps["dodges"] == ""); return true;
                }
                if (scenario == "deferred-start")
                {
                    Defer = false;
                    var playback = NamedHarness.Create();
                    var token = playback.Begin(1, new AnimationClip("rifle_emote", 2), 2, suppressRifleHold: true);
                    while (assigned.TryDequeue(out var callback)) callback();
                    Check(Maps["dodges"] == "rifle_emote" && Maps["plpause1"] == "xbowr");
                    Check(playback.IsCurrent(1, token)); return true;
                }
                Check(Maps["dodges"] == "plparryl");
                if (scenario == "normal")
                {
                    foreach (var timer in oldTimers.Reverse()) timer();
                    Check(Maps["dodges"] == "" && Maps["plpause1"] == "sw_nohold"); return true;
                }
                if (scenario == "new-gesture")
                {
                    Grip.Refresh(1, true, true);
                    foreach (var timer in oldTimers) timer();
                    Check(Maps["dodges"] == "plparryl"); return true;
                }
                if (scenario == "same-mode-weapon")
                {
                    right = 11; Grip.Refresh(1, true);
                    Check(Maps["dodges"] == "");
                }
                if (scenario is "rifle" or "queued-attack")
                {
                    right = 20; AttackActive = scenario == "queued-attack";
                    Grip.Refresh(1, true); Check(Maps["dodges"] == "");
                }
                var named = NamedHarness.Create();
                var owner = named.Begin(1, new AnimationClip("rifle_emote", 2), 2, suppressRifleHold: true);
                foreach (var timer in oldTimers) timer();
                Check(Maps["dodges"] == "rifle_emote" && Maps["plpause1"] == "xbowr");
                Check(named.IsCurrent(1, owner)); return true;
            }
        }
        """;
}
