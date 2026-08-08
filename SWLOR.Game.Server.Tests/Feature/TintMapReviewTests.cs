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
        var savedColor = new TintMapColor(12, 34, 56).ToStoredValue();
        droid.TintOverrides["TM_droidmat_2"] = savedColor;

        var serialized = JsonConvert.SerializeObject(droid);
        var restored = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);

        restored.Should().NotBeNull();
        restored!.TintOverrides.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, int>("TM_droidmat_2", savedColor));
    }

    [TestCase(0, 0, 0)]
    [TestCase(12, 34, 56)]
    [TestCase(255, 255, 255)]
    public void TintMapRgbStorageRoundTrips(byte red, byte green, byte blue)
    {
        var expected = new TintMapColor(red, green, blue);
        var stored = expected.ToStoredValue();

        stored.Should().BePositive();
        TintMapColor.TryFromStoredValue(stored, out var restored).Should().BeTrue();
        restored.Should().Be(expected);
        TintMapColor.TryFromStoredValue(43, out _).Should().BeFalse(
            "legacy palette values must remain distinguishable from RGB colors");
    }

    [Test]
    public void TintPaletteCoordinatesPreserveEveryLegacyMaterialFamily()
    {
        TintMapMaterialRegistry.PaletteTextureHeight.Should().Be(2048);
        var expectedBaseRows = new Dictionary<TintMapLayerType, int>
        {
            [TintMapLayerType.Skin] = 0,
            [TintMapLayerType.Hair] = 176,
            [TintMapLayerType.Metal1] = 352,
            [TintMapLayerType.Metal2] = 528,
            [TintMapLayerType.Cloth1] = 704,
            [TintMapLayerType.Cloth2] = 704,
            [TintMapLayerType.Leather1] = 880,
            [TintMapLayerType.Leather2] = 880,
            [TintMapLayerType.Tattoo1] = 1056,
            [TintMapLayerType.Tattoo2] = 1056,
        };

        foreach (var (layer, baseRow) in expectedBaseRows)
        {
            TintMapMaterialRegistry.GetLayer(layer).PaletteBaseRow.Should().Be(baseRow);
            TintMapMaterialRegistry.GetPaletteCoordinate(layer, 0).Should().BeApproximately(
                (baseRow + 0.5f) / TintMapMaterialRegistry.PaletteTextureHeight,
                0.000001f);
        }
    }

    [Test]
    public void AppearanceTintEditorAddsCustomColorAlongsideExistingPresetPalettes()
    {
        var definition = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "AppearanceEditorDefinition.cs");
        var viewModel = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");

        var definitionRoot = CSharpSyntaxTree.ParseText(definition).GetRoot();
        var definitionMethods = definitionRoot.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .ToDictionary(method => method.Identifier.ValueText);

        definition.Should().Contain("row.AddColorPicker()");
        definition.Should().Contain(".BindSelectedColor(model => model.SelectedTintColor)");
        definition.Should().Contain(".BindResref(model => model.ColorSheetResref)");
        definition.Should().Contain("model => model.OnClickColorPalette(paletteIndex)");
        definition.Should().Contain(".SetText(\"Custom Color...\")");
        definitionMethods["BuildEditorHeader"].ToString().Should().NotContain("BuildCustomTintEditor");
        definitionMethods["BuildMainEditor"].ToString().Should().Contain("BuildCustomTintEditor(col2)");
        definitionMethods["BuildColorPalette"].ToString().Should().Contain("BuildCustomTintEditor(col)");
        definition.Should().NotContain(".SetText(\"Tints\")");
        definition.Should().NotContain("TintColorSheetResref");
        viewModel.Should().Contain("new TintMapColor(value.R, value.G, value.B)");
        viewModel.Should().Contain("WatchOnClient(model => model.SelectedTintColor)");
        viewModel.Should().NotContain("TintMaterialOptions");
        viewModel.Should().NotContain("TintLayerOptions");
        viewModel.Should().NotContain("OnSelectTintColor");
        viewModel.Should().NotContain("OnSelectTintMap");
        viewModel.Should().NotContain("IsTintMapSelected");
    }

    [TestCase(1, 0, true, 1)]
    [TestCase(27, 0, true, 27)]
    [TestCase(27, 168, true, 168)]
    [TestCase(0, 168, true, 0)]
    [TestCase(27, 168, false, 27)]
    public void PartsBasedModelUsesCreaturePartWhenArmorLeavesSlotAtZero(
        int creaturePart,
        int armorPart,
        bool usesItemColors,
        int expected)
    {
        var method = typeof(TintMapModelResolver).GetMethod(
            "ResolvePartId",
            BindingFlags.Static | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.Invoke(null, new object[] { creaturePart, armorPart, usesItemColors })
            .Should().Be(expected);
    }

    [Test]
    public void BodyPartChangesRefreshCustomTintTargets()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var loadBodyPart = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "LoadBodyPart");

        loadBodyPart.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .Should().Contain("LoadTintMapEditor");
        loadBodyPart.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .Should().NotContain("RefreshTintMapAvailability");
    }

    [Test]
    public void AppearanceTintEditorTargetsTheExistingColorSelectionAndRestoresPresets()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var methodNames = new HashSet<string>
        {
            "TryGetSelectedTintLayer",
            "TryGetEditableTintSelections",
            "OnSelectColor",
            "OnClickColorPalette",
            "OnClickColorTarget"
        };
        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(method => methodNames.Contains(method.Identifier.ValueText))
            .ToDictionary(method => method.Identifier.ValueText);

        methods["TryGetSelectedTintLayer"].ToString().Should().Contain("SelectedColorCategoryIndex switch");
        methods["TryGetSelectedTintLayer"].ToString().Should().Contain("_selectedColorChannel switch");
        methods["TryGetEditableTintSelections"].ToString().Should().Contain(
            "selection.PaletteSource == paletteSource");
        methods["TryGetEditableTintSelections"].ToString().Should().Contain(
            "selection.ArmorPart == armorPart");
        methods["TryGetEditableTintSelections"].ToString().Should().Contain(
            "selection.Material.Layers.Contains(selectedLayerType)");
        methods["OnSelectColor"].ToString().Should().Contain("ResetCurrentCustomTintOverrides");
        methods["OnClickColorPalette"].ToString().Should().Contain("ResetCurrentCustomTintOverrides");
        methods["OnClickColorTarget"].ToString().Should().Contain("LoadTintMapEditor");
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
        var saveOverrideMethod = FindMethod(tintSource, "SaveDroidOverride");
        var updateSnapshotMethod = FindMethod(droidSource, "UpdateEquippedItemSnapshot");
        var unequipMethod = FindMethod(droidSource, "OnUnequipItem");

        saveOverrideMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should()
            .ContainSingle(invocation =>
                IsMemberInvocation(invocation, "Droid", "UpdateEquippedItemSnapshot"));
        updateSnapshotMethod.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Should()
            .ContainSingle(assignment => IsSerializedDictionaryAssignment(assignment, "EquippedItems"));
        unequipMethod.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Should()
            .ContainSingle(assignment => IsSerializedDictionaryAssignment(assignment, "Inventory"));
    }

    [Test]
    public void PartsBasedCloaksResolveTextureThroughCloakModelTable()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        var partsMethod = FindMethod(source, "AddPartsAppearanceSelections");
        partsMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(invocation =>
                GetInvokedMethodName(invocation) == "AddCloakSelections");

        var cloakMethod = FindMethod(source, "AddCloakSelections");
        cloakMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should()
            .Contain(invocation =>
                GetInvokedMethodName(invocation) == "Get2DAString" &&
                invocation.ArgumentList.Arguments.Count == 3 &&
                invocation.ArgumentList.Arguments[0].Expression.ToString() == "\"cloakmodel\"" &&
                invocation.ArgumentList.Arguments[1].Expression.ToString() == "\"TEXTURE\"");
        cloakMethod.ToString().Should().Contain("cloak_{textureId:D3}");
    }

    [Test]
    public void EquipmentTintEditsHonorItemRestrictions()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var method = FindMethod(source, "TryGetEditableTintSelections");

        method.ToString().Should().Contain("IsEquipmentSelected && !IsValidItem()");
    }

    [Test]
    public void MirroringArmorCopiesCustomTintOverrides()
    {
        var viewModelSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        FindMethod(viewModelSource, "CopyColors")
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should()
            .Contain(invocation =>
                IsMemberInvocation(invocation, "TintMapModelResolver", "CopyArmorPartTintOverrides"));

        var resolverSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        var method = FindMethod(resolverSource, "CopyArmorPartTintOverrides");
        var invocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();

        invocations.Should().Contain("GetMaterials");
        invocations.Should().Contain("SetLocalInt");
        invocations.Should().Contain("DeleteLocalInt");
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
        var loadMethod = FindMethod(source, nameof(TintMapMaterialRegistry.Load));
        var invocations = loadMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .ToList();
        var logInvocation = invocations.Single(invocation =>
            IsMemberInvocation(invocation, "Log", "WriteStructured"));

        logInvocation.ArgumentList.Arguments.Any(argument =>
            argument.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Server"
            })
            .Should()
            .BeTrue();
        logInvocation.ArgumentList.Arguments.Any(argument =>
            argument.Expression is LiteralExpressionSyntax literal &&
            literal.Token.ValueText == "Loaded {TintMapModelCount} tint-map models.")
            .Should()
            .BeTrue();
        logInvocation.ArgumentList.Arguments.Any(argument =>
            argument.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count"
            })
            .Should()
            .BeTrue();
        invocations.Should().NotContain(invocation =>
            IsMemberInvocation(invocation, "Console", "WriteLine"));
        invocations.Should().NotContain(invocation =>
            GetInvokedMethodName(invocation) == "Information");
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
        var replaceMethod = FindMethod(tintSource, nameof(TintMapService.ReplaceItemTintOverrides));
        var replaceInvocations = replaceMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();

        replaceInvocations.Should().Contain("GetItemTintOverrides");
        replaceInvocations.Should().Contain("DeleteLocalInt");
        replaceInvocations.Should().Contain("SetLocalInt");
        var getOverridesMethod = FindMethod(tintSource, "GetItemTintOverrides");
        getOverridesMethod.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Should()
            .Contain(expression =>
                expression.Kind() == SyntaxKind.NotEqualsExpression &&
                expression.Left.DescendantNodesAndSelf()
                    .OfType<SimpleNameSyntax>()
                    .Any(name => name.Identifier.ValueText == "Type") &&
                expression.Right.DescendantNodesAndSelf()
                    .OfType<SimpleNameSyntax>()
                    .Any(name => name.Identifier.ValueText == "Int"));
        getOverridesMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should()
            .Contain(invocation =>
                GetInvokedMethodName(invocation) == "StartsWith" &&
                invocation.ArgumentList.Arguments.Any(argument =>
                    argument.Expression.DescendantNodesAndSelf()
                        .OfType<SimpleNameSyntax>()
                        .Any(name => name.Identifier.ValueText == "Ordinal")));
        var outfitSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "OutfitViewModel.cs");
        var loadMethod = FindMethod(outfitSource, "LoadOutfit");
        var loadInvocations = loadMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .ToList();
        var replaceInvocation = loadInvocations.Single(invocation =>
            IsMemberInvocation(invocation, "TintMapService", "ReplaceItemTintOverrides"));
        loadInvocations.Where(invocation =>
                GetInvokedMethodName(invocation) == "CopyItem" &&
                invocation.ArgumentList.Arguments.Count == 3 &&
                invocation.ArgumentList.Arguments.Any(argument =>
                    argument.Expression is IdentifierNameSyntax { Identifier.ValueText: "Player" }))
            .Should()
            .ContainSingle();
        replaceInvocation.ArgumentList.Arguments.Should().HaveCount(2);
    }

    private static MethodDeclarationSyntax FindMethod(string source, string methodName)
    {
        return CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == methodName);
    }

    private static string GetInvokedMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => string.Empty
        };
    }

    private static bool IsMemberInvocation(
        InvocationExpressionSyntax invocation,
        string receiver,
        string methodName)
    {
        return invocation.Expression is MemberAccessExpressionSyntax
        {
            Expression: IdentifierNameSyntax receiverIdentifier,
            Name.Identifier.ValueText: var invokedMethod
        } &&
               receiverIdentifier.Identifier.ValueText == receiver &&
               invokedMethod == methodName;
    }

    private static bool IsSerializedDictionaryAssignment(
        AssignmentExpressionSyntax assignment,
        string dictionaryName)
    {
        return assignment.Left is ElementAccessExpressionSyntax
               {
                   Expression: MemberAccessExpressionSyntax
                   {
                       Name.Identifier.ValueText: var assignedDictionary
                   }
               } &&
               assignedDictionary == dictionaryName &&
               assignment.Right is InvocationExpressionSyntax serialization &&
               IsMemberInvocation(serialization, "ObjectPlugin", "Serialize");
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
