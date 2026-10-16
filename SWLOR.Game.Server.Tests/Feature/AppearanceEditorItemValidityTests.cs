using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Compiles the appearance editor's equipped-item gate against a recording engine. NPC gear is often undroppable
/// (cursed), so a DM restyling their avatar or a possessed creature must not be locked out by that flag.
/// </summary>
public class AppearanceEditorItemValidityTests
{
    private MethodInfo _isValid = null!;

    [OneTimeSetUp]
    public void CompileItemGate()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.cs"));
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.Text == "IsValidItem").ToFullString();
        var harness = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using static World;
            public static class World
            {
                public const uint Item = 10, OBJECT_INVALID = 0x7F000000;
                public static bool TargetDM, TargetPossessed, Cursed, Plot;
                public static bool GetIsObjectValid(uint obj) => obj != OBJECT_INVALID;
                public static bool GetIsDM(uint creature) => TargetDM;
                public static bool GetIsDMPossessed(uint creature) => TargetPossessed;
                public static bool GetItemCursedFlag(uint item) => Cursed;
                public static bool GetPlotFlag(uint item) => Plot;
                public static AppearanceType GetAppearanceType(uint creature) => AppearanceType.Human;
                public static int GetItemAppearance(uint item, ItemAppearanceType type, int index) => 5;
                public static BaseItem GetBaseItemType(uint item) => BaseItem.Helmet;
            }
            public sealed class ArmorParts
            {
                public int[] Helmet { get; } = { 5 };
                public int[] Cloak { get; } = { 5 };
                public int[] Neck, Torso, Belt, Pelvis, Shoulder, Bicep, Forearm, Hand, Thigh, Shin, Foot, Robe;
            }
            public sealed class WeaponParts
            {
                public bool IsSimple;
                public int[] SimpleParts, TopParts, MiddleParts, BottomParts;
            }
            public class Harness
            {
                private static readonly Dictionary<AppearanceType, ArmorParts> _armorAppearances = new() { [AppearanceType.Human] = new ArmorParts() };
                private static readonly Dictionary<BaseItem, WeaponParts> _weaponAppearances = new();
                private readonly uint _target = 2;
                private bool IsAppearanceSelected => false;
                private int SelectedItemTypeIndex => 1;
                private uint GetItem() => Item;
                {{method}}
                public static bool Run(bool targetDM, bool targetPossessed, bool cursed, bool plot)
                {
                    TargetDM = targetDM; TargetPossessed = targetPossessed; Cursed = cursed; Plot = plot;
                    return new Harness().IsValidItem();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(AppearanceType).Assembly.Location)
            .Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AppearanceEditorItemValidityHarness",
            [CSharpSyntaxTree.ParseText(harness)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _isValid = Assembly.Load(stream.ToArray()).GetType("Harness")!.GetMethod("Run")!;
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void DMTargetsCanRestyleUndroppableAndPlotGear(bool cursed, bool plot)
    {
        IsValid(targetDM: true, targetPossessed: false, cursed, plot).Should().BeTrue();
        IsValid(targetDM: false, targetPossessed: true, cursed, plot).Should().BeTrue();
    }

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    public void PlayersStillCannotRestyleUndroppableOrPlotGear(bool cursed, bool plot, bool expected)
    {
        IsValid(targetDM: false, targetPossessed: false, cursed, plot).Should().Be(expected);
    }

    private bool IsValid(bool targetDM, bool targetPossessed, bool cursed, bool plot) =>
        (bool)_isValid.Invoke(null, [targetDM, targetPossessed, cursed, plot])!;
}
