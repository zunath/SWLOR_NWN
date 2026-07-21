using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class TintMapReviewTests
{
    [TestCase(Gender.Male, "m")]
    [TestCase(Gender.Female, "f")]
    [TestCase(Gender.Both, "m")]
    [TestCase(Gender.Other, "m")]
    [TestCase(Gender.None, "m")]
    public void PartsBasedModelGenderUsesEngineCompatiblePrefix(Gender gender, string expected)
    {
        var method = typeof(TintMapModelResolver).GetMethod(
            "GetGenderModelCode",
            BindingFlags.Static | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.Invoke(null, new object[] { gender }).Should().Be(expected);
    }

    [Test]
    public void ConstructedDroidTintOverridesSurviveSerialization()
    {
        var droid = new ConstructedDroid();
        droid.TintOverrides["TM_droidmat_2"] = 43;

        var serialized = JsonConvert.SerializeObject(droid);
        var restored = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);

        restored.Should().NotBeNull();
        restored!.TintOverrides.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, int>("TM_droidmat_2", 43));
    }

    [Test]
    public void ManagedCreatureEquipmentChangesRefreshTintUniformsAndEditor()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var method = CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == nameof(TintMapService.OnAppearanceEquipmentChanged));

        var handlerScripts = method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Where(attribute => attribute.Name.ToString().EndsWith("NWNEventHandler"))
            .SelectMany(attribute => attribute.ArgumentList!.Arguments)
            .Select(argument => argument.Expression.ToString());

        handlerScripts.Should().BeEquivalentTo(
            "ScriptName.OnSWLORItemEquipValidBefore",
            "ScriptName.OnItemUnequipBefore");
        var invocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .ToList();
        var queuesEditorRefresh = invocations
            .Any(invocation =>
                invocation.Expression is IdentifierNameSyntax methodName &&
                methodName.Identifier.ValueText == "QueueRefreshAndEditor" &&
                invocation.ArgumentList.Arguments.Count == 2 &&
                invocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax argument &&
                argument.Identifier.ValueText == "creature");
        var resolvesOwner = invocations.Any(invocation =>
            invocation.Expression is IdentifierNameSyntax methodName &&
            methodName.Identifier.ValueText == "GetMaster" &&
            invocation.ArgumentList.Arguments.Count == 1 &&
            invocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax argument &&
            argument.Identifier.ValueText == "creature");
        var acceptsDroids = invocations.Any(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "Droid" },
                Name.Identifier.ValueText: "IsDroid"
            } &&
            invocation.ArgumentList.Arguments.Count == 1 &&
            invocation.ArgumentList.Arguments[0].Expression.ToString() == "creature");
        var acceptsDmPossessedCreatures = invocations.Any(invocation =>
            invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "GetIsDMPossessed" } &&
            invocation.ArgumentList.Arguments.Count == 1 &&
            invocation.ArgumentList.Arguments[0].Expression.ToString() == "creature");
        queuesEditorRefresh.Should().BeTrue();
        resolvesOwner.Should().BeTrue();
        acceptsDroids.Should().BeTrue();
        acceptsDmPossessedCreatures.Should().BeTrue();
        typeof(AppearanceEditorViewModel)
            .GetInterfaces()
            .Should()
            .Contain(typeof(IGuiRefreshable<AppearanceChangedRefreshEvent>));
    }

    [Test]
    public void SpeederAppearanceChangesRefreshTintMaps()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "ItemDefinition",
            "SpeederItemDefinition.cs");
        var methods = CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(node => new[]
            {
                "Speeder",
                "AttackedDismount",
                "AttackDismount",
                "AreaTransitionDismount"
            }.Contains(node.Identifier.ValueText))
            .ToDictionary(node => node.Identifier.ValueText);

        methods.Keys.Should().BeEquivalentTo(
            "Speeder",
            "AttackedDismount",
            "AttackDismount",
            "AreaTransitionDismount");
        foreach (var method in methods.Values)
        {
            method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Should()
                .ContainSingle(invocation =>
                    invocation.Expression.ToString() == "TintMapService.QueueRefreshAndEditor" &&
                    invocation.ArgumentList.Arguments.Count == 2 &&
                    invocation.ArgumentList.Arguments[0].Expression.ToString() ==
                    invocation.ArgumentList.Arguments[1].Expression.ToString());
        }
    }

    [Test]
    public void CombinedAppearanceRefreshUpdatesTintUniformsAndEditor()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var method = CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == nameof(TintMapService.QueueRefreshAndEditor));
        var invocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .ToList();

        var queuesTintRefresh = invocations.Any(invocation =>
            invocation.Expression.ToString() == nameof(TintMapService.QueueRefresh));
        var publishesEditorRefresh = invocations.Any(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "Gui" },
                Name.Identifier.ValueText: "PublishRefreshEvent"
            } &&
            invocation.ArgumentList.Arguments.Any(argument =>
                argument.Expression is ObjectCreationExpressionSyntax creation &&
                creation.Type.ToString() == nameof(AppearanceChangedRefreshEvent)));

        queuesTintRefresh.Should().BeTrue();
        publishesEditorRefresh.Should().BeTrue();
    }

    [Test]
    public void PerPartArmorColorsUseSharedIndexEncoding()
    {
        ArmorColorIndexCalculator.CalculatePerPart(
                AppearanceArmor.LeftForearm,
                AppearanceArmorColor.Cloth2)
            .Should()
            .Be(
                (int)AppearanceArmorColor.NumColors +
                (int)AppearanceArmor.LeftForearm * (int)AppearanceArmorColor.NumColors +
                (int)AppearanceArmorColor.Cloth2);
    }

    [Test]
    public void EquippedDroidTintChangesUpdateSerializedItemSnapshot()
    {
        var tintSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var droidSource = ReadSource("SWLOR.Game.Server", "Service", "Droid.cs");

        tintSource.Should().Contain(
            "Droid.UpdateEquippedItemSnapshot(creature, selection.PaletteSource);");
        droidSource.Should().Contain(
            "constructedDroid.EquippedItems[slot] = ObjectPlugin.Serialize(item);");
        droidSource.Should().Contain(
            "constructedDroid.Inventory[itemId] = ObjectPlugin.Serialize(item);");
    }

    [Test]
    public void PartsBasedCloaksFallBackToGenericModel()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        var method = CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "AddPartsAppearanceSelections");
        var cloakInvocation = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(invocation =>
                invocation.Expression is IdentifierNameSyntax
                {
                    Identifier.ValueText: "AddSimpleItemSelections"
                } &&
                invocation.ArgumentList.Arguments.Any(argument =>
                    argument.Expression.ToString() == "InventorySlot.Cloak"));

        cloakInvocation.ArgumentList.Arguments
            .Select(argument => argument.Expression.ToString())
            .Should()
            .ContainInOrder(
                "creature",
                "InventorySlot.Cloak",
                "$\"{prefix}cloak\"",
                "selections",
                "seenSelections",
                "\"cloak\"");
    }

    [Test]
    public void PaddedModularModelUsesItsUnpaddedTintMaterial()
    {
        var rows = ReadSource("SWLOR_Haks", "sw_2da", "tintmap.2da")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 4)
            .ToList();
        var paddedFoot = rows.Single(columns => columns[1] == "pmo0_footl010");

        paddedFoot[2].Should().Be("pmo0_footl10");
        rows.Should().NotContain(columns => columns[1] == "pmo0_footl10");
    }

    [Test]
    public void TintMapRegistryUsesStructuredServerLogging()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapMaterialRegistry.cs");

        source.Should().Contain("Log.WriteStructured(");
        source.Should().Contain("LogGroup.Server,");
        source.Should().Contain("\"Loaded {TintMapModelCount} tint-map models.\",");
        source.Should().NotContain("Console.WriteLine");
        source.Should().NotContain("Serilog.Log.Information");
    }

    [Test]
    public void OutfitLoadsReplaceTintOverridesFromSavedOutfit()
    {
        var tintSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var replaceMethod = CSharpSyntaxTree.ParseText(tintSource)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == nameof(TintMapService.ReplaceItemTintOverrides));
        var replaceInvocations = replaceMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => invocation.Expression.ToString())
            .ToList();

        replaceInvocations.Should().Contain("GetItemTintOverrides");
        replaceInvocations.Should().Contain("DeleteLocalInt");
        replaceInvocations.Should().Contain("SetLocalInt");
        tintSource.Should().Contain("variable.Type != LocalVariableType.Int");
        tintSource.Should().Contain(
            "!variable.Key.StartsWith(LocalVariablePrefix, StringComparison.Ordinal)");

        var outfitSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "OutfitViewModel.cs");
        var loadMethod = CSharpSyntaxTree.ParseText(outfitSource)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "LoadOutfit");
        var loadInvocations = loadMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .ToList();
        var replaceInvocation = loadInvocations.Single(invocation =>
            invocation.Expression.ToString() == "TintMapService.ReplaceItemTintOverrides");
        var finalCopyInvocation = loadInvocations.Single(invocation =>
            invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "CopyItem" } &&
            invocation.ArgumentList.Arguments.Count == 3 &&
            invocation.ArgumentList.Arguments[0].Expression.ToString() == "copy" &&
            invocation.ArgumentList.Arguments[1].Expression.ToString() == "Player");

        replaceInvocation.ArgumentList.Arguments
            .Select(argument => argument.Expression.ToString())
            .Should()
            .ContainInOrder("deserialized", "copy");
        replaceInvocation.SpanStart.Should().BeLessThan(finalCopyInvocation.SpanStart);
    }

    private static string ReadSource(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        var fullPath = Path.Combine(new[] { repositoryRoot.FullName }.Concat(pathParts).ToArray());
        return File.ReadAllText(fullPath);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
