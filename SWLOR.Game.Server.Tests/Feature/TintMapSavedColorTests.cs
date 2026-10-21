using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Newtonsoft.Json;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class TintMapSavedColorTests
{
    private Type _harness;

    [OneTimeSetUp]
    public void CompileProductionColorStorage()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName,
                "SWLOR.Game.Server", "Feature", "AppearanceDefinition", "TintMap", "TintMapService.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(node => new[] { "GetSavedColor", "GetEffectiveColor", "SetStoredColor", "SetPaletteColor",
                    "CaptureCurrentHeadColors", "GetCreatureTintSnapshotVariables", "SavePlayerOverrides",
                    "RestorePlayerOverrides", "UpdatePlayerTintOverrides" }.Contains(node.Identifier.ValueText))
            .Select(method => method.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword))).NormalizeWhitespace());

        // Run production storage, carry and persistence with controlled native calls.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using Entity = SWLOR.Game.Server.Entity;
            public static class SavedColorHarness
            {
                public static readonly Dictionary<string, int> Locals = new();
                public static readonly List<TintMapMaterialSelection> Selections = new();
                public static Entity.Player Player = new("test-player");
                public static bool ExplicitPreset;
                {{string.Join(Environment.NewLine, methods)}}
                private static int GetLocalInt(uint item, string name) => Locals.GetValueOrDefault(name);
                private static void SetLocalInt(uint item, string name, int value) => Locals[name] = value;
                private static void DeleteLocalInt(uint item, string name) => Locals.Remove(name);
                private static bool GetIsObjectValid(uint item) => item != 0;
                private static bool GetIsPC(uint item) => true;
                private static bool GetIsDM(uint item) => false;
                private static bool GetIsDMPossessed(uint item) => false;
                private static string GetObjectUUID(uint item) => "test-player";
                private static ObjectType GetObjectType(uint item) => ObjectType.Item;
                private static bool HasExplicitItemPresetColor(TintMapMaterialSelection selection,
                    TintMapLayerType layer, int savedColor) => ExplicitPreset;
                private static Dictionary<string, int> GetItemTintOverrides(uint item) => Locals;
                private static int GetStandardColor(uint creature, TintMapMaterialSelection selection,
                    TintMapLayerType layer) => 0;
                private static TintMapColorSelection GetEffectiveCreatureColor(uint creature, TintMapLayerType layer)
                    => new(0, null);
                private static IEnumerable<string> GetCreatureCustomColorVariables(uint creature) => Locals.Keys
                    .Where(name => TintMapVariable.TryGetLayer(name, out var layer) && TintMapVariable.IsCreatureColorLayer(layer));
                private static string GetCreatureCustomColorStateVariable(TintMapLayerType layer)
                    => TintMapVariable.GetCreatureColorStateName(layer);
                private static List<string> GetEquivalentItemTintVariables(uint item,
                    TintMapMaterialSelection selection, TintMapLayerType layer) => new();
                private static void MarkPendingItemColorEdit(uint item, TintMapLayerType layer, AppearanceArmor part) { }
                private static void SaveDroidOverride(uint creature, TintMapMaterialSelection selection,
                    TintMapLayerType layer, string name, int value) { }
                private static void ApplyColor(uint creature, TintMapMaterialSelection selection,
                    TintMapLayerType layer, TintMapColorSelection color) { }
                private static void ApplyCurrentColorsAndPublish(uint creature) { }
                private static class TintMapModelResolver
                {
                    public static IReadOnlyList<TintMapMaterialSelection> GetCurrentSelections(uint creature) => Selections;
                }
                private static class DB
                {
                    public static T Get<T>(string id) => (T)(object)Player;
                    public static void Set<T>(T value) { }
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator).Append(typeof(TintMapService).Assembly.Location)
            .Append(typeof(ObjectType).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("SavedColorHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("SavedColorHarness")!;
    }

    [SetUp]
    public void ClearColorState()
    {
        Locals.Clear();
        Selections.Clear();
        _harness.GetField("Player")!.SetValue(null, new SWLOR.Game.Server.Entity.Player("test-player"));
        _harness.GetField("ExplicitPreset")!.SetValue(null, false);
    }

    [TestCaseSource(nameof(Layers))]
    public void GroundItemLookupKeepsCreatureLayersOutOfEquipmentGlobalState(TintMapLayerType layer)
    {
        var locals = (Dictionary<string, int>)_harness.GetField("Locals")!.GetValue(null)!;
        locals.Clear();
        _harness.GetField("ExplicitPreset")!.SetValue(null, false);
        var selection = new TintMapMaterialSelection("ground_model",
            new TintMapMaterialDefinition("Ground material", "ground_material", layer),
            paletteSource: 200, creaturePaletteSource: 200, usesItemColors: true, AppearanceArmor.Invalid);
        var lookup = _harness.GetMethod("GetSavedColor")!;
        int Read(bool includeGlobal = true) =>
            (int)lookup.Invoke(null, [selection, layer, includeGlobal])!;

        Read().Should().Be(0, "an untinted ground item has no saved override, including skin and hair");
        var rgb = new TintMapColor(17, 83, 209).ToStoredValue();
        // Also seed invalid creature TMG locals: they must never become a fallback.
        locals[$"TMG_{(int)layer}"] = rgb;
        var expected = TintMapVariable.IsCreatureColorLayer(layer) ? 0 : rgb;
        Read().Should().Be(expected);
        Read(includeGlobal: false).Should().Be(0);

        _harness.GetField("ExplicitPreset")!.SetValue(null, true);
        Read().Should().Be(0, "an explicit preset opts out of item-wide RGB");
        locals[TintMapVariable.GetName(selection.Material.Resref, layer)] = rgb;
        Read().Should().Be(rgb, "material-specific overrides remain available for every semantic layer");
        Read(includeGlobal: false).Should().Be(rgb);
    }

    [TestCaseSource(nameof(AdditionalHeadLayers))]
    public void HeadPaletteAndRgbEditsFollowTheChannelInsteadOfTheMaterialName(TintMapLayerType layer)
    {
        var first = Head("pfh0_head286", "first_lips", layer);
        var next = Head("pfh0_head327", "next_lips", layer);
        Locals[TintMapVariable.GetName(next.Material.Resref, layer)] = 33;
        var native = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
        Locals[native] = new TintMapColor(120, 80, 40).ToStoredValue();

        foreach (var color in new[] { new TintMapColor(12, 34, 56), new TintMapColor(0, 0, 0) })
        {
            Invoke("SetStoredColor", 1u, first, layer, color.ToStoredValue(), true);
            Read(next, layer).Should().Be(color.ToStoredValue());
            Read(next, layer, false).Should().Be(33, "raw material exceptions remain intact");
        }

        foreach (var palette in new[] { 0, 175 })
        {
            Invoke("SetPaletteColor", 1u, next, layer, palette);
            Read(first, layer).Should().Be(palette + 1, "a preset must replace the inherited RGB too");
        }
        Locals[native].Should().Be(new TintMapColor(120, 80, 40).ToStoredValue());
    }

    [TestCaseSource(nameof(AdditionalHeadLayers))]
    public void ExistingHeadColorsSurviveAHeadWithoutThatChannelAndRecordReload(TintMapLayerType layer)
    {
        var oldHead = Head("pmh0_head312", "old_material", layer);
        var newHead = Head("pmh0_head313", "new_material", layer);
        var color = new TintMapColor(17, 83, 209).ToStoredValue();
        Locals[TintMapVariable.GetName(oldHead.Material.Resref, layer)] = color;
        Selections.Add(oldHead);
        Invoke("CaptureCurrentHeadColors", 1u);
        Read(newHead, layer).Should().Be(color);

        Selections.Clear();
        Selections.Add(Head("pmh0_head001", "plain_head", TintMapLayerType.Skin));
        Invoke("CaptureCurrentHeadColors", 1u);
        Read(newHead, layer).Should().Be(color);

        var player = (SWLOR.Game.Server.Entity.Player)_harness.GetField("Player")!.GetValue(null)!;
        var restored = JsonConvert.DeserializeObject<SWLOR.Game.Server.Entity.Player>(JsonConvert.SerializeObject(player))!;
        restored.CreatureTintOverrides.Should().ContainKey(TintMapVariable.GetHeadColorStateName(layer));
        _harness.GetField("Player")!.SetValue(null, restored);
        Locals.Clear();
        Locals[TintMapVariable.GetHeadColorStateName(layer)] = 31;
        Invoke("RestorePlayerOverrides", 1u);
        Read(newHead, layer).Should().Be(color, "the durable state replaces stale exported locals");
    }

    [TestCaseSource(nameof(AdditionalHeadLayers))]
    public void HeadChannelsCannotRecolorEquipmentOrBodyMaterials(TintMapLayerType layer)
    {
        Locals[TintMapVariable.GetHeadColorStateName(layer)] = new TintMapColor(17, 83, 209).ToStoredValue();
        foreach (var selection in new[] {
            Head("pmh0_chest303", "body", layer),
            new TintMapMaterialSelection("helm_064", new TintMapMaterialDefinition("Helmet", "helmet", layer),
                1, 1, true, AppearanceArmor.Invalid)
        })
        {
            Read(selection, layer).Should().Be(0);
            Locals[TintMapVariable.GetName(selection.Material.Resref, layer)] = 15;
            Read(selection, layer).Should().Be(15);
        }
    }

    [Test]
    public void CaptureKeepsConflictingAuthoredMaterialColorsInsteadOfPickingOneArbitrarily()
    {
        var layer = TintMapLayerType.Leather2;
        var first = Head("pmh0_head312", "upper_lip", layer);
        var second = Head("pmh0_head312", "lower_lip", layer);
        Selections.AddRange([first, second]);
        Locals[TintMapVariable.GetName(first.Material.Resref, layer)] = 15;
        Locals[TintMapVariable.GetName(second.Material.Resref, layer)] = 33;
        Invoke("CaptureCurrentHeadColors", 1u);
        Locals.Should().NotContainKey(TintMapVariable.GetHeadColorStateName(layer));
    }

    [Test]
    public void CaptureCarriesTheDisplayedDefaultPaletteEvenBeforeTheFirstEdit()
    {
        var layer = TintMapLayerType.Cloth1;
        Selections.Add(Head("pfh0_head327", "bandanna", layer));
        Invoke("CaptureCurrentHeadColors", 1u);
        Locals[TintMapVariable.GetHeadColorStateName(layer)].Should().Be(1);
    }

    private Dictionary<string, int> Locals => (Dictionary<string, int>)_harness.GetField("Locals")!.GetValue(null)!;
    private List<TintMapMaterialSelection> Selections =>
        (List<TintMapMaterialSelection>)_harness.GetField("Selections")!.GetValue(null)!;
    private object Invoke(string method, params object[] arguments) => _harness.GetMethod(method)!.Invoke(null, arguments)!;
    private int Read(TintMapMaterialSelection selection, TintMapLayerType layer, bool includeGlobal = true) =>
        (int)Invoke("GetSavedColor", selection, layer, includeGlobal);
    private static TintMapMaterialSelection Head(string model, string material, TintMapLayerType layer) =>
        new(model, new TintMapMaterialDefinition(material, material, layer), 1, 1, false, AppearanceArmor.Invalid);

    private static IEnumerable<TintMapLayerType> Layers => Enum.GetValues<TintMapLayerType>();
    private static IEnumerable<TintMapLayerType> AdditionalHeadLayers => Layers.Where(layer => !TintMapVariable.IsCreatureColorLayer(layer));
}
