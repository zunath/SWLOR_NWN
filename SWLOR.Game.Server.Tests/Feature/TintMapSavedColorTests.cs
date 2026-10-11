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

public class TintMapSavedColorTests
{
    private Type _harness;

    [OneTimeSetUp]
    public void CompileProductionLookupWithControlledItemStorage()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var method = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName,
                "SWLOR.Game.Server", "Feature", "AppearanceDefinition", "TintMap", "TintMapService.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "GetSavedColor")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword))).NormalizeWhitespace();

        // Run the actual lookup without the native engine, retaining real layer and variable contracts.
        var source = $$"""
            using System.Collections.Generic;
            using System.Linq;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
            using SWLOR.NWN.API.NWScript.Enum;
            public static class SavedColorHarness
            {
                public static readonly Dictionary<string, int> Locals = new();
                public static bool ExplicitPreset;
                {{method}}
                private static int GetLocalInt(uint item, string name) => Locals.GetValueOrDefault(name);
                private static ObjectType GetObjectType(uint item) => ObjectType.Item;
                private static bool HasExplicitItemPresetColor(TintMapMaterialSelection selection,
                    TintMapLayerType layer, int savedColor) => ExplicitPreset;
                private static Dictionary<string, int> GetItemTintOverrides(uint item) => Locals;
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

    private static IEnumerable<TintMapLayerType> Layers => Enum.GetValues<TintMapLayerType>();
}
