using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class AppearanceEditorHeadColorTests
{
    private Type _harness;
    private Type _colors;
    private object _picker;
    private static readonly TintMapColor FirstColor = new(17, 83, 209);
    private static readonly TintMapColor SecondColor = new(151, 76, 68);

    [OneTimeSetUp]
    public void CompileProductionPickerLookup()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var method = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "TryGetSelectedCustomColor");
        var source = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
            public class PickerHarness
            {
                public bool IsAppearanceSelected = true;
                public bool IsEquipmentSelected;
                public int SelectedItemTypeIndex;
                public int NativeState;
                private readonly uint _target = 1;
                public enum ColorTarget { Global, Part }
                public ColorTarget _colorTarget;
                private uint GetItem() => 2;
                private int GetLocalInt(uint target, string name) => NativeState;
                {{method}}
            }
            namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
            {
                public static class TintMapService
                {
                    public static readonly Dictionary<string, TintMapColor> Colors = new();
                    public static readonly HashSet<string> Custom = new();
                    public static readonly List<string> ReadMaterials = new();
                    public static bool TryGetCustomColor(TintMapMaterialSelection selection, TintMapLayerType layer,
                        out TintMapColor color)
                    { color = Colors[selection.Material.Resref]; return Custom.Contains(selection.Material.Resref); }
                    public static TintMapColor GetEffectiveDisplayColor(uint actor,
                        TintMapMaterialSelection selection, TintMapLayerType layer)
                    { ReadMaterials.Add(selection.Material.Resref); return Colors[selection.Material.Resref]; }
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(TintMapService).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("HeadPickerLookupHarness",
            new[] { CSharpSyntaxTree.ParseText(source) }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        _harness = assembly.GetType("PickerHarness")!;
        _colors = assembly.GetType(typeof(TintMapService).FullName!)!;
    }

    private Dictionary<string, TintMapColor> Colors =>
        (Dictionary<string, TintMapColor>)_colors.GetField("Colors")!.GetValue(null)!;
    private HashSet<string> Custom => (HashSet<string>)_colors.GetField("Custom")!.GetValue(null)!;
    private List<string> ReadMaterials => (List<string>)_colors.GetField("ReadMaterials")!.GetValue(null)!;

    [SetUp]
    public void Reset()
    {
        _picker = Activator.CreateInstance(_harness)!;
        Colors.Clear(); Custom.Clear(); ReadMaterials.Clear();
        Colors["first"] = FirstColor;
        Colors["second"] = SecondColor;
    }

    private static TintMapMaterialSelection Head(string name) => new("pmh0_head312",
        new TintMapMaterialDefinition("Head", name, TintMapLayerType.Leather2), 1, 1, false, AppearanceArmor.Invalid);

    private (bool Shared, TintMapColor Color) Lookup(TintMapLayerType layer = TintMapLayerType.Leather2,
        bool empty = false)
    {
        var values = new object[] { empty ? Array.Empty<TintMapMaterialSelection>() :
            new[] { Head("first"), Head("second") }, layer, default(TintMapColor) };
        var shared = (bool)_harness.GetMethod("TryGetSelectedCustomColor", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_picker, values)!;
        return (shared, (TintMapColor)values[2]);
    }

    [Test]
    public void LegacyRgbAndDifferentPresetRemainMixed()
    {
        Custom.Add("first");
        Lookup().Shared.Should().BeFalse();
        ReadMaterials.Should().BeEquivalentTo("first", "second");
    }

    [Test]
    public void LegacyRgbAndMatchingPresetShareTheirDisplayColor()
    {
        Custom.Add("first");
        Colors["second"] = FirstColor;
        Lookup().Should().Be((true, FirstColor));
        ReadMaterials.Should().BeEquivalentTo("first", "second");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EveryPresetParticipatesInTheHeadAccessoryLookup(bool matching)
    {
        if (matching) Colors["second"] = FirstColor;
        Lookup().Shared.Should().Be(matching);
        ReadMaterials.Should().BeEquivalentTo("first", "second");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeAndEquipmentGlobalRgbKeepTheirExistingPriority(bool equipment)
    {
        _harness.GetField("IsAppearanceSelected")!.SetValue(_picker, !equipment);
        _harness.GetField("IsEquipmentSelected")!.SetValue(_picker, equipment);
        _harness.GetField("NativeState")!.SetValue(_picker, FirstColor.ToStoredValue());
        Lookup(equipment ? TintMapLayerType.Leather2 : TintMapLayerType.Skin).Should().Be((true, FirstColor));
        ReadMaterials.Should().BeEmpty();
    }

    [Test]
    public void EquipmentPartLookupKeepsItsExistingLegacyCustomSelection()
    {
        _harness.GetField("IsAppearanceSelected")!.SetValue(_picker, false);
        _harness.GetField("IsEquipmentSelected")!.SetValue(_picker, true);
        _harness.GetField("_colorTarget")!.SetValue(_picker,
            Enum.ToObject(_harness.GetNestedType("ColorTarget")!, 1));
        Custom.Add("first");
        Lookup().Should().Be((true, FirstColor));
        ReadMaterials.Should().Equal("first");
    }

    [Test]
    public void EmptyHeadSelectionHasNoSharedColor()
    {
        Lookup(empty: true).Shared.Should().BeFalse();
    }
}
