using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class HeadLodColorRefreshTests
{
    private Type _world;
    private Type _refresh;

    [OneTimeSetUp]
    public void CompileProductionLoopAndWriterWithControlledNativeCalls()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var folder = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AppearanceDefinition", "TintMap");
        var serviceMethods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(folder, "TintMapService.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "RefreshLodHeadColors" or "WriteMaterialColor");
        var source = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Creature;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using static World;
            public static class World
            {
                public static uint OBJECT_SELF => ExecutingActor;
                public static uint ExecutingActor = 42;
                public static uint Actual = 42;
                public static bool Valid = true, Playable = true, HasHelmet, HelmetVisible;
                public static int Head = 316, BasePhenotype;
                public static bool UnequippedItemValid = true;
                public static BaseItem UnequippedBaseItem = BaseItem.Helmet;
                public static int HeadProjectionRefreshes;
                public static AppearanceType Appearance = AppearanceType.Human;
                public static Gender Sex = Gender.Female;
                public static string Uuid = "actor-a";
                public static readonly HashSet<string> Lods = new(StringComparer.OrdinalIgnoreCase);
                public static readonly List<TintMapMaterialSelection> Selections = new();
                public static readonly List<Action> Callbacks = new();
                public static readonly List<bool> Cancelled = new();
                public static readonly List<object[]> Writes = new();
                public static readonly Dictionary<TintMapLayerType, TintMapColorSelection> Colors = new();
                public static int ResourceQueries;
                public static bool GetIsObjectValid(uint actor) => actor == 11 ? UnequippedItemValid : actor == 9 ? HasHelmet : actor == 100 || Valid && actor == Actual;
                public static uint StringToObject(string value) => uint.Parse(value);
                public static BaseItem GetBaseItemType(uint item) => UnequippedBaseItem;
                public static ObjectType GetObjectType(uint actor) => actor == 100 ? ObjectType.Invalid : ObjectType.Creature;
                public static AppearanceType GetAppearanceType(uint actor) => Appearance;
                public static Gender GetGender(uint actor) => Sex;
                public static string GetObjectUUID(uint actor) => Uuid;
                public static uint GetObjectByUUID(string uuid) => Valid && uuid == Uuid ? Actual : 0;
                public static uint GetExitingObject() => Actual;
                public static int GetCreatureBodyPart(CreaturePart part, uint actor) => Head;
                public static uint GetItemInSlot(InventorySlot slot, uint actor) => HasHelmet ? 9u : 0u;
                public static string ResManGetAliasFor(string name, ResType type)
                { ResourceQueries++; return Lods.Contains(name) && type == ResType.LOD ? "HAK:test" : ""; }
                public static void SetMaterialShaderUniformVec4(uint actor, string material, string parameter,
                    float x, float y = 0, float z = 0, float w = 0) => Writes.Add([actor, material, parameter, x, y, z, w]);
                public static void Start() { ExecutingActor = Actual; HeadLodColorRefresh.Update(OBJECT_SELF, Selections); }
                public static void Step()
                {
                    ExecutingActor = 100;
                    for (var i = 0; i < Callbacks.Count; i++)
                        if (!Cancelled[i]) Callbacks[i]();
                }
            }
            namespace SWLOR.Game.Server.Core
            {
                public static class Scheduler
                {
                    private sealed class Subscription(int index) : IDisposable
                    { public void Dispose() => World.Cancelled[index] = true; }
                    public static IDisposable ScheduleRepeating(Action action, TimeSpan interval)
                    {
                        if (interval != TimeSpan.FromSeconds(0.5)) throw new Exception("Unexpected refresh period");
                        var index = World.Callbacks.Count;
                        World.Callbacks.Add(action); World.Cancelled.Add(false);
                        return new Subscription(index);
                    }
                }
            }
            namespace SWLOR.NWN.API.NWNX
            {
                public static class EventsPlugin
                { public static string GetEventData(string key) => "11"; }
            }
            namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
            {
                public static class RacialAppearanceRegistry
                { public static bool TryGet(AppearanceType value, out object result) { result = null; return World.Playable; } }
            }
            namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
            {
                public static class TintMapModelResolver
                { public static IReadOnlyList<TintMapMaterialSelection> GetCurrentSelections(uint creature) => World.Selections; }
                public static class RobeModelRenderer
                { public static int GetBasePhenotype(uint actor) => World.BasePhenotype; }
                public static class HelmetModelRenderer
                {
                    public static bool IsShownByOwner(uint helmet) => World.HelmetVisible;
                    public static void Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections) => World.HeadProjectionRefreshes++;
                }
                public static class TintMapService
                {
                    {{string.Join(Environment.NewLine, serviceMethods)}}
                    private static TintMapColorSelection GetEffectiveColor(uint actor, TintMapMaterialSelection selection, TintMapLayerType layer)
                    {
                        if (selection.PaletteSource != World.Actual || selection.CreaturePaletteSource != World.Actual)
                            throw new Exception("Deferred selections lost their creature identity");
                        return World.Colors.TryGetValue(layer, out var value) ? value : new(17, null);
                    }
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator).Append(typeof(TintMapService).Assembly.Location)
            .Append(typeof(ObjectType).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var trees = new[] { CSharpSyntaxTree.ParseText(source) }.Concat(
            new[] { "HeadLodColorRefresh.cs", "HeadLodColorRefreshSession.cs" }
                .Select(name => CSharpSyntaxTree.ParseText("using static World;\n" + File.ReadAllText(Path.Combine(folder, name)))));
        var compilation = CSharpCompilation.Create("HeadLodRefreshHarness", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        _world = assembly.GetType("World")!;
        _refresh = assembly.GetType(typeof(HeadLodColorRefresh).FullName!)!;
    }

    [SetUp]
    public void Reset()
    {
        _refresh.GetMethod("Clear")!.Invoke(null, null);
        Get<List<Action>>("Callbacks").Clear(); Get<List<bool>>("Cancelled").Clear();
        Writes.Clear(); Selections.Clear(); Get<HashSet<string>>("Lods").Clear(); Colors.Clear();
        Set("Actual", 42u); Set("ExecutingActor", 42u); Set("Valid", true); Set("Playable", true);
        Set("HasHelmet", false); Set("HelmetVisible", false); Set("Head", 316); Set("BasePhenotype", 0);
        Set("UnequippedItemValid", true); Set("UnequippedBaseItem", BaseItem.Helmet);
        Set("HeadProjectionRefreshes", 0);
        Set("Appearance", AppearanceType.Human); Set("Sex", Gender.Female); Set("Uuid", "actor-a");
        Set("ResourceQueries", 0);
        Get<HashSet<string>>("Lods").Add("pfh0_head316");
        Selections.Add(Selection("pfh0_head316", "head_mat", Enum.GetValues<TintMapLayerType>()));
        Selections.Add(Selection("pfh0_chest001", "outfit_mat", Enum.GetValues<TintMapLayerType>(), true));
    }

    [Test]
    public void OnlyHeadRowsRefreshAndEveryPaletteScalarKeepsItsValue()
    {
        Call("Start");
        Writes.Should().HaveCount(10);
        Writes.Should().OnlyContain(row => (uint)row[0] == 42 && (string)row[1] == "head_mat" && (float)row[4] == 1);
        foreach (var layer in Enum.GetValues<TintMapLayerType>())
            Writes.Single(row => (string)row[2] == TintMapMaterialRegistry.GetLayer(layer).UniformName)[3]
                .Should().Be(TintMapMaterialRegistry.GetPaletteCoordinate(layer, 17));
        Writes.Clear(); Call("Step");
        Writes.Should().HaveCount(10).And.OnlyContain(row => (string)row[1] == "head_mat" && (float)row[4] == 2);
    }

    [Test]
    public void DeferredModuleCallbackKeepsTheOriginalCreatureAndPaletteSources()
    {
        Call("Start"); Call("Step");
        Get<uint>("ExecutingActor").Should().Be(100);
        Writes.Should().HaveCount(20).And.OnlyContain(row => (uint)row[0] == 42);
    }

    [Test]
    public void EveryTickReadsTheLatestRgbOrPresetInsteadOfRetainingAnOldColor()
    {
        Call("Start");
        var color = new TintMapColor(144, 76, 68);
        foreach (var layer in Enum.GetValues<TintMapLayerType>()) Colors[layer] = new(17, color);
        Writes.Clear(); Call("Step");
        Writes.Should().HaveCount(10).And.OnlyContain(row => (float)row[3] == TintMapShaderColor.Encode(color));
        Colors[TintMapLayerType.Hair] = new(7, null);
        Writes.Clear(); Call("Step");
        Writes.Single(row => (string)row[2] == "rowHair")[3]
            .Should().Be(TintMapMaterialRegistry.GetPaletteCoordinate(TintMapLayerType.Hair, 7));
        Writes.Where(row => (string)row[2] != "rowHair").Should()
            .OnlyContain(row => (float)row[3] == TintMapShaderColor.Encode(color));
    }

    [Test]
    public void RepeatedPublicationsKeepOneTimerAndResourceLookup()
    {
        Call("Start"); Call("Start"); Call("Start");
        Get<List<Action>>("Callbacks").Should().ContainSingle();
        Get<int>("ResourceQueries").Should().Be(1);
    }

    [TestCase("missing-lod")]
    [TestCase("unsupported-species")]
    [TestCase("large-phenotype")]
    [TestCase("visible-helmet")]
    [TestCase("shared-outfit-material")]
    public void IneligibleTargetsNeverStartARefresh(string reason)
    {
        if (reason == "missing-lod") Get<HashSet<string>>("Lods").Clear();
        if (reason == "unsupported-species") Set("Playable", false);
        if (reason == "large-phenotype") Set("BasePhenotype", 2);
        if (reason == "visible-helmet") { Set("HasHelmet", true); Set("HelmetVisible", true); }
        if (reason == "shared-outfit-material") Selections.Add(Selection("pfh0_chest001", "head_mat", [TintMapLayerType.Metal1], true));
        Call("Start");
        Writes.Should().BeEmpty(); Get<List<Action>>("Callbacks").Should().BeEmpty();
    }

    [Test]
    public void HiddenHelmetAndNormalBaseRobeRemainEligible()
    {
        Set("HasHelmet", true); Set("HelmetVisible", false); Set("BasePhenotype", 0);
        Call("Start"); Call("Step");
        Writes.Should().HaveCount(20);
    }

    [TestCase("head")]
    [TestCase("gender")]
    [TestCase("appearance")]
    [TestCase("destroyed")]
    [TestCase("reused-id")]
    [TestCase("helmet")]
    public void StaleTargetsCancelWithoutPublishingAnotherRow(string reason)
    {
        Call("Start"); Writes.Clear();
        switch (reason)
        {
            case "head": Set("Head", 15); break;
            case "gender": Set("Sex", Gender.Male); break;
            case "appearance": Set("Appearance", AppearanceType.Cathar); break;
            case "destroyed": Set("Valid", false); break;
            case "reused-id": Set("Uuid", "actor-b"); break;
            case "helmet": Set("HasHelmet", true); Set("HelmetVisible", true); break;
        }
        Call("Step");
        Writes.Should().BeEmpty(); Get<List<bool>>("Cancelled").Should().OnlyContain(value => value);
    }

    [Test]
    public void ReplacedSessionCannotBeTouchedByItsOlderQueuedCallback()
    {
        Call("Start");
        var older = Get<List<Action>>("Callbacks")[0];
        Set("Uuid", "actor-b"); Call("Start"); Writes.Clear();
        older(); Writes.Should().BeEmpty();
        Call("Step"); Writes.Should().HaveCount(10);
    }

    [Test]
    public void LeavingOrReloadingCancelsTheOwnedTimer()
    {
        Call("Start");
        _refresh.GetMethod("OnExit")!.Invoke(null, null);
        Writes.Clear(); Call("Step"); Writes.Should().BeEmpty();
        Call("Start"); _refresh.GetMethod("Clear")!.Invoke(null, null);
        Writes.Clear(); Call("Step"); Writes.Should().BeEmpty();
    }

    [Test]
    public void SequenceWrapsWithinExactPositiveFloatsAndNeverFallsBackToZero()
    {
        Call("Start");
        for (var tick = 0; tick < 4095; tick++) Call("Step");
        Writes.Should().OnlyContain(row => (float)row[4] >= 1 && (float)row[4] <= 4095);
        Writes[^1][4].Should().Be(1f);
    }

    [Test]
    public void HelmetRemovalPublishesHeadColorsBeforeAnyDeferredCallback()
    {
        Set("HasHelmet", true); Set("HelmetVisible", true); Call("Start");
        Writes.Should().BeEmpty();
        Set("HasHelmet", false);
        _refresh.GetMethod("OnHelmetUnequipped")!.Invoke(null, null);
        Writes.Should().HaveCount(10).And.OnlyContain(row => (string)row[1] == "head_mat");
        Get<int>("HeadProjectionRefreshes").Should().Be(1);
        Get<List<Action>>("Callbacks").Should().ContainSingle();
        Call("Start");
        Get<List<Action>>("Callbacks").Should().ContainSingle();
    }

    [TestCase(false, BaseItem.Helmet)]
    [TestCase(true, BaseItem.Armor)]
    public void NonHelmetOrInvalidUnequipDoesNotPublish(bool valid, BaseItem itemType)
    {
        Set("UnequippedItemValid", valid); Set("UnequippedBaseItem", itemType);
        _refresh.GetMethod("OnHelmetUnequipped")!.Invoke(null, null);
        Writes.Should().BeEmpty(); Get<List<Action>>("Callbacks").Should().BeEmpty();
        Get<int>("HeadProjectionRefreshes").Should().Be(0);
    }

    [Test]
    public void OrdinaryHeadUnequipDoesNotChangeItsProjectionOrStartATimer()
    {
        Set("Head", 15); Get<HashSet<string>>("Lods").Clear();
        _refresh.GetMethod("OnHelmetUnequipped")!.Invoke(null, null);
        Writes.Should().BeEmpty(); Get<List<Action>>("Callbacks").Should().BeEmpty();
        Get<int>("HeadProjectionRefreshes").Should().Be(0);
    }

    private T Get<T>(string field) => (T)_world.GetField(field)!.GetValue(null)!;
    private void Set(string field, object value) => _world.GetField(field)!.SetValue(null, value);
    private void Call(string method) => _world.GetMethod(method)!.Invoke(null, null);
    private List<object[]> Writes => Get<List<object[]>>("Writes");
    private List<TintMapMaterialSelection> Selections => Get<List<TintMapMaterialSelection>>("Selections");
    private Dictionary<TintMapLayerType, TintMapColorSelection> Colors => Get<Dictionary<TintMapLayerType, TintMapColorSelection>>("Colors");
    private static TintMapMaterialSelection Selection(string model, string material, TintMapLayerType[] layers, bool item = false) =>
        new(model, new("test", material, layers), item ? 10u : 42u, 42, item, AppearanceArmor.Invalid);
}
