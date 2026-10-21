using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class HeadTintOwnershipTests
{
    [TestCase("pfh0_", true)]
    [TestCase("pfh0_", false)]
    [TestCase("pmh0_", true)]
    [TestCase("pmh0_", false)]
    public void WearingArmorKeepsHeadChannelsCreatureOwned(string prefix, bool hasArmor)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server/Feature/AppearanceDefinition/TintMap/TintMapModelResolver.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "AddPartsAppearanceSelections" or "ResolvePartId");
        var source = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Creature;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using ItemAppearance = SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
            public static class HeadOwnershipHarness
            {
                private static string Prefix;
                private static bool HasArmor;
                private static readonly Dictionary<CreaturePart, string> BodyPartNames = new()
                    { [CreaturePart.Head] = "head", [CreaturePart.Torso] = "chest" };
                {{string.Join(Environment.NewLine, methods)}}
                public static List<TintMapMaterialSelection> Run(string prefix, bool hasArmor)
                {
                    Prefix = prefix; HasArmor = hasArmor;
                    var selections = new List<TintMapMaterialSelection>();
                    AddPartsAppearanceSelections(100, selections, new HashSet<string>());
                    return selections;
                }
                private static string GetCreatureModelPrefix(uint creature) => Prefix;
                private static uint GetItemInSlot(InventorySlot slot, uint creature) => HasArmor ? 200u : 0u;
                private static bool GetIsObjectValid(uint item) => item != 0;
                private static int GetCreatureBodyPart(CreaturePart part, uint creature) => part == CreaturePart.Head ? 286 : 1;
                private static int GetItemAppearance(uint item, ItemAppearanceType type, int part)
                    => part == (int)AppearanceArmor.Robe ? 0 : 2;
                private static string GetRobeModelResref(string prefix, int phenotype, int robe) => "unused";
                private static void AddSimpleItemSelections(uint creature, InventorySlot slot, string prefix,
                    ICollection<TintMapMaterialSelection> selections, ISet<string> seen) { }
                private static void AddCloakSelections(uint creature, string prefix,
                    ICollection<TintMapMaterialSelection> selections, ISet<string> seen) { }
                private static void AddModelSelections(ICollection<TintMapMaterialSelection> selections, ISet<string> seen,
                    string model, uint palette, uint creature, bool item, AppearanceArmor part)
                    => selections.Add(new(model, new("test", model, TintMapLayerType.Skin, TintMapLayerType.Leather2),
                        palette, creature, item, part));
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!.Split(Path.PathSeparator)
            .Append(typeof(TintMapModelResolver).Assembly.Location).Append(typeof(InventorySlot).Assembly.Location)
            .Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("HeadOwnershipHarness", [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var harness = Assembly.Load(stream.ToArray()).GetType("HeadOwnershipHarness")!;
        var selections = (List<TintMapMaterialSelection>)harness.GetMethod("Run")!.Invoke(null, [prefix, hasArmor])!;
        var head = selections.Single(selection => selection.ModelResref.Contains("_head"));
        head.PaletteSource.Should().Be(100);
        head.UsesItemColors.Should().BeFalse();
        HeadTintChannels.GetLayers(selections).Should().Contain(TintMapLayerType.Leather2);
        var chest = selections.Single(selection => selection.ModelResref.Contains("_chest"));
        chest.PaletteSource.Should().Be(hasArmor ? 200u : 100u);
        chest.UsesItemColors.Should().Be(hasArmor);
    }
}
