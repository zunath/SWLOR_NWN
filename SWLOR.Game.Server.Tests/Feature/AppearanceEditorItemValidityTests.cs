using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

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
        var methods = string.Join(Environment.NewLine, CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(node => node.Identifier.Text is "IsValidItem" or "AllowsItemFlags" or "AllowsWeaponParts")
            .Select(node => node.ToFullString().Replace("IWeaponAppearanceDefinition", "WeaponAppearanceBaseDefinition")));
        var harness = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
            using SWLOR.NWN.API.NWScript.Enum;
            using SWLOR.NWN.API.NWScript.Enum.Item;
            using static World;
            public static class World
            {
                public const uint Item = 10, OBJECT_INVALID = 0x7F000000;
                public static bool TargetDM, TargetPossessed, Cursed, Plot;
                public static BaseItem ItemType;
                public static bool GetIsObjectValid(uint obj) => obj != OBJECT_INVALID;
                public static bool GetIsDM(uint creature) => TargetDM;
                public static bool GetIsDMPossessed(uint creature) => TargetPossessed;
                public static bool GetItemCursedFlag(uint item) => Cursed;
                public static bool GetPlotFlag(uint item) => Plot;
                public static AppearanceType GetAppearanceType(uint creature) => AppearanceType.Human;
                public static int GetItemAppearance(uint item, ItemAppearanceType type, int index) => type switch
                {
                    ItemAppearanceType.WeaponColor => 1,
                    ItemAppearanceType.WeaponModel => index == (int)AppearanceWeapon.Top ? 2
                        : index == (int)AppearanceWeapon.Middle ? 1 : ItemType == BaseItem.Lightsaber ? 15 : 7,
                    _ => 5
                };
                public static BaseItem GetBaseItemType(uint item) => ItemType;
            }
            public sealed class ArmorParts
            {
                public int[] Helmet { get; } = { 5 };
                public int[] Cloak { get; } = { 5 };
                public int[] Neck, Torso, Belt, Pelvis, Shoulder, Bicep, Forearm, Hand, Thigh, Shin, Foot, Robe;
            }
            public class Harness
            {
                private static readonly Dictionary<AppearanceType, ArmorParts> _armorAppearances = new() { [AppearanceType.Human] = new ArmorParts() };
                private static readonly Dictionary<BaseItem, WeaponAppearanceBaseDefinition> _weaponAppearances = new()
                {
                    [BaseItem.Lightsaber] = new LightsaberAppearanceDefinition(),
                    [BaseItem.Saberstaff] = new SaberstaffAppearanceDefinition()
                };
                private readonly uint _target = 2;
                private bool IsAppearanceSelected => false;
                private int SelectedItemTypeIndex => ItemType == BaseItem.Helmet ? 1 : 2;
                private uint GetItem() => Item;
                {{methods}}
                public static bool Run(bool targetDM, bool targetPossessed, bool cursed, bool plot, BaseItem itemType)
                {
                    TargetDM = targetDM; TargetPossessed = targetPossessed; Cursed = cursed; Plot = plot;
                    ItemType = itemType;
                    return new Harness().IsValidItem();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(AppearanceType).Assembly.Location)
            .Append(typeof(WeaponAppearanceBaseDefinition).Assembly.Location)
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

    [TestCase(BaseItem.Lightsaber)]
    [TestCase(BaseItem.Saberstaff)]
    public void TrainingSaberFlagsKeepPlayerRestrictionsAndDMExceptions(BaseItem itemType)
    {
        IsValid(false, false, false, true, itemType).Should().BeTrue();
        IsValid(false, false, true, true, itemType).Should().BeFalse();
        IsValid(true, false, true, true, itemType).Should().BeTrue();
        IsValid(false, true, true, true, itemType).Should().BeTrue();
    }

    private bool IsValid(bool targetDM, bool targetPossessed, bool cursed, bool plot, BaseItem itemType = BaseItem.Helmet) =>
        (bool)_isValid.Invoke(null, [targetDM, targetPossessed, cursed, plot, itemType])!;
}
