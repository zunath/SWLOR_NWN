using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        definition.Should().Contain(".SetText(\"Custom Color\")");
        definition.Should().Contain(".BindValue(model => model.CustomTintRed)");
        definition.Should().Contain(".BindValue(model => model.CustomTintGreen)");
        definition.Should().Contain(".BindValue(model => model.CustomTintBlue)");
        definition.Should().NotContain("IsCustomTintPickerVisible");
        definitionMethods["BuildEditorHeader"].ToString().Should().NotContain("BuildCustomTintEditor");
        definitionMethods["BuildMainEditor"].ToString().Should().Contain("BuildCustomTintEditor(col2)");
        definitionMethods["BuildColorPalette"].ToString().Should().Contain("BuildCustomTintEditor(col)");
        definition.Should().NotContain(".SetText(\"Tints\")");
        definition.Should().NotContain("TintColorSheetResref");
        viewModel.Should().Contain("new TintMapColor(value.R, value.G, value.B)");
        viewModel.Should().Contain("WatchOnClient(model => model.SelectedTintColor)");
        viewModel.Should().Contain("WatchOnClient(model => model.CustomTintRed)");
        viewModel.Should().Contain("WatchOnClient(model => model.CustomTintGreen)");
        viewModel.Should().Contain("WatchOnClient(model => model.CustomTintBlue)");
        viewModel.Should().Contain("TintMapService.GetEffectiveDisplayColor(");
        var service = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var effectiveDisplayColor = FindMethod(service, nameof(TintMapService.GetEffectiveDisplayColor));
        effectiveDisplayColor.ToString().Should().Contain("GetEffectiveColor(");
        effectiveDisplayColor.ToString().Should().Contain("TintMapPaletteColors.GetColor(",
            "legacy 1-176 overrides must initialize the picker from their rendered palette color");
        viewModel.Should().NotContain("IsCustomTintPickerVisible");
        viewModel.Should().NotContain("OnToggleCustomTintPicker");
        viewModel.Should().NotContain("TintMaterialOptions");
        viewModel.Should().NotContain("TintLayerOptions");
        viewModel.Should().NotContain("OnSelectTintColor");
        viewModel.Should().NotContain("OnSelectTintMap");
        viewModel.Should().NotContain("IsTintMapSelected");
    }

    [TestCase(1, 0, true, 1)]
    [TestCase(27, 0, true, 27)]
    [TestCase(27, 168, true, 168)]
    [TestCase(0, 168, true, 168)]
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
            .Should().Contain(nameof(TintMapService.CarryStoredCreatureCustomColors));
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
            "ResetCurrentCustomTintOverrides",
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
            "selection.GetPaletteSource(selectedLayerType) == paletteSource");
        methods["TryGetEditableTintSelections"].ToString().Should().Contain(
            "selection.ArmorPart == armorPart");
        methods["TryGetEditableTintSelections"].ToString().Should().Contain(
            "selection.Material.Layers.Contains(selectedLayerType)");
        methods["OnSelectColor"].ToString().Should().Contain("ResetCurrentCustomTintOverrides");
        methods["OnClickColorPalette"].ToString().Should().Contain("ResetCurrentCustomTintOverrides");
        methods["OnClickColorTarget"].ToString().Should().Contain("LoadTintMapEditor");
        methods["ResetCurrentCustomTintOverrides"].ToString().Should()
            .Contain("TryGetSelectedTintLayer(out var selectedLayerType)");
        methods["ResetCurrentCustomTintOverrides"].ToString().Should()
            .Contain("ResetCreatureCustomColor(_target, selectedLayerType)",
                "selecting a preset must clear an inactive creature channel's persisted RGB tint");
    }

    [Test]
    public void CreatureSemanticColorsRemainCreatureOwnedOnEquippedMeshes()
    {
        var material = new TintMapMaterialDefinition(
            "exposed_skin",
            "exposed_skin",
            TintMapLayerType.Skin,
            TintMapLayerType.Cloth1);
        var selection = new TintMapMaterialSelection(
            "pmh0_chest189",
            material,
            paletteSource: 200,
            creaturePaletteSource: 100,
            usesItemColors: true,
            AppearanceArmor.Torso);

        selection.GetPaletteSource(TintMapLayerType.Skin).Should().Be(100);
        selection.UsesItemColor(TintMapLayerType.Skin).Should().BeFalse();
        selection.GetPaletteSource(TintMapLayerType.Cloth1).Should().Be(200);
        selection.UsesItemColor(TintMapLayerType.Cloth1).Should().BeTrue();
    }

    [Test]
    public void WingAndTailEquipmentLayersUseEquippedArmorColors()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        var method = FindMethod(source, nameof(TintMapModelResolver.GetCurrentSelections));
        var appendageCalls = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => GetInvokedMethodName(invocation) == "AddTableModelSelections")
            .ToDictionary(
                invocation => invocation.ArgumentList.Arguments[0].Expression.ToString(),
                invocation => invocation.ArgumentList.Arguments
                    .Select(argument => argument.Expression.ToString())
                    .ToArray());

        appendageCalls["\"wingmodel\""]
            .Should().Equal(
                "\"wingmodel\"",
                "\"MODEL\"",
                "(int)GetCreatureWingType(creature)",
                "appendagePaletteSource",
                "creature",
                "appendagesUseItemColors",
                "selections",
                "seenSelections");
        appendageCalls["\"tailmodel\""]
            .Should().Equal(
                "\"tailmodel\"",
                "\"MODEL\"",
                "(int)GetCreatureTailType(creature)",
                "appendagePaletteSource",
                "creature",
                "appendagesUseItemColors",
                "selections",
                "seenSelections");
        method.ToString().Should().Contain(
            "var equipmentPaletteSource = GetItemInSlot(InventorySlot.Chest, creature);");
        method.ToString().Should().Contain(
            "var appendagesUseItemColors = GetIsObjectValid(equipmentPaletteSource);");
    }

    [Test]
    public void RightClickResetUsesTheClickedArmorChannel()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var method = FindMethod(source, "OnClickClearColor");

        method.ToString().Should().Contain(
            "ResetCustomTintOverrides(colorTarget, colorChannel)");
        method.ToString().Should().NotContain("ResetCurrentCustomTintOverrides()");
    }

    [Test]
    public void WorldItemsReceiveTheirOwnTintUniformsAfterTheyBecomeVisible()
    {
        var resolver = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        FindMethod(resolver, nameof(TintMapModelResolver.GetWorldItemSelections))
            .ToString().Should().Contain("ItemClass");

        var service = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        FindMethod(service, nameof(TintMapService.OnModuleUnacquire))
            .ToString().Should().Contain("QueueItemRefresh(GetModuleItemLost())");
        var areaEnter = FindMethod(service, nameof(TintMapService.OnAreaEnter)).ToString();
        areaEnter.Should().Contain("QueueOtherCreaturesInArea(area, creature)",
            "placed NPCs may have spawned before the managed tint hooks were registered");
        areaEnter.Should().Contain("QueueWorldItemsInArea(area)");
        areaEnter.Should().Contain(
            "GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature)",
            "NPC and summon entries must not rescan every ground item in the area");
        var creatureRefresh = FindMethod(service, "QueueOtherCreaturesInArea").ToString();
        creatureRefresh.Should().Contain("GetFirstObjectInArea(area, ObjectType.Creature)");
        creatureRefresh.Should().Contain("QueueRefresh(creature)");
        creatureRefresh.Should().Contain("creature != enteringCreature",
            "the entering player is already queued before the area scan");
        FindMethod(service, nameof(TintMapService.ApplyCurrentItemColors))
            .ToString().Should().Contain("GetWorldItemSelections(item)");
    }

    [Test]
    public void DroppedCloaksReadOverridesFromTheirMappedWornMaterial()
    {
        var resolver = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        var worldSelections = FindMethod(resolver, nameof(TintMapModelResolver.GetWorldItemSelections));
        var worldSource = worldSelections.ToString();
        worldSource.Should().Contain("itemClass == \"cloak\"");
        worldSource.Should().Contain("Get2DAString(\"cloakmodel\", \"TEXTURE\", modelId)");
        worldSource.Should().Contain("overrideModel");

        var service = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var savedColor = FindMethod(service, "GetSavedColor").ToString();
        savedColor.Should().Contain("selection.OverrideModelResref");
        savedColor.Should().Contain("material.Layers.Contains(layer)",
            "the worn and ground materials are matched by semantic tint layer");
        savedColor.Should().Contain("TintMapVariable.GetName(material.Resref, layer)");
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
    public void QueuedRefreshCarriesOnlyExplicitGlobalSemanticColorsOntoNewMaterials()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var queueRefresh = FindMethod(source, nameof(TintMapService.QueueRefresh));
        var delayedBody = queueRefresh.DescendantNodes()
            .OfType<ParenthesizedLambdaExpressionSyntax>()
            .Single()
            .Body.ToString();

        delayedBody.Should().Contain(nameof(TintMapService.CarryStoredCreatureCustomColors));
        delayedBody.Should().Contain(nameof(TintMapService.ApplyCurrentColors));
        delayedBody.IndexOf(nameof(TintMapService.CarryStoredCreatureCustomColors), StringComparison.Ordinal)
            .Should().BeLessThan(
                delayedBody.IndexOf(nameof(TintMapService.ApplyCurrentColors), StringComparison.Ordinal));

        var carry = FindMethod(source, nameof(TintMapService.CarryStoredCreatureCustomColors));
        carry.ToString().Should().Contain("GetCreatureCustomColorStateVariable(layer)",
            "per-material variables cannot prove that a semantic color was chosen globally");
        carry.ToString().Should().NotContain("entry.Value.Count == 1",
            "one distinct custom value may still be only one member of a partial authored tint");
        carry.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .Should().Contain("ApplyCreatureCustomColors");

        var setGlobal = FindMethod(source, nameof(TintMapService.SetCreatureCustomColor));
        setGlobal.ToString().Should().Contain("GetCreatureCustomColorStateVariable(layer)",
            "global edits need an explicit persisted marker for later equipment refreshes");

        var viewModelSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        FindMethod(viewModelSource, "LoadBodyPart").ToString().Should()
            .Contain(nameof(TintMapService.CarryStoredCreatureCustomColors));
    }

    [Test]
    public void SemanticPresetResetClearsInactiveAndPersistedOverrides()
    {
        var serviceSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var reset = FindMethod(serviceSource, nameof(TintMapService.ResetCreatureCustomColor));
        var resetInvocations = reset.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();

        reset.ToString().Should().Contain("GetCreatureCustomColorVariables(creature)");
        reset.ToString().Should().Contain("GetCreatureCustomColorStateVariable(layer)",
            "resetting to a preset must also remove the persisted global semantic tint marker");
        resetInvocations.Should().Contain("DeleteLocalInt");
        resetInvocations.Should().Contain("RemoveDroidOverrides");
        resetInvocations.Should().Contain("ApplyColor");

        var viewModelSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var resetEditor = CSharpSyntaxTree.ParseText(viewModelSource)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(method =>
                method.Identifier.ValueText == "ResetCustomTintOverrides" &&
                method.ParameterList.Parameters.Count == 3);
        resetEditor.ToString().Should().Contain("TintMapVariable.IsCreatureColorLayer(layerType)");
        resetEditor.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should().Contain(invocation =>
                IsMemberInvocation(invocation, "TintMapService", nameof(TintMapService.ResetCreatureCustomColor)));
    }

    [Test]
    public void SemanticRgbEditsSynchronizeInactiveAndPersistedOverrides()
    {
        var serviceSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var synchronize = FindMethod(serviceSource, nameof(TintMapService.SetCreatureCustomColor));
        var synchronizeCalls = synchronize.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();

        synchronize.ToString().Should().Contain("GetCreatureCustomColorVariables(creature)",
            "inactive semantic material keys must receive the newly selected RGB value");
        synchronizeCalls.Should().Contain("SetLocalInt");
        synchronizeCalls.Should().Contain("SaveDroidOverrides",
            "inactive semantic keys must remain synchronized after a droid respawns");
        synchronizeCalls.Should().Contain("SetColor",
            "the active materials must update immediately in the game preview");
        synchronizeCalls.Should().Contain(nameof(TintMapModelResolver.GetCurrentSelections),
            "an RGB edit must re-resolve body parts that changed while the editor remained open");

        var viewModelSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var selectedTintColor = CSharpSyntaxTree.ParseText(viewModelSource)
            .GetRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Single(property => property.Identifier.ValueText == "SelectedTintColor");
        selectedTintColor.ToString().Should().Contain(nameof(TintMapService.SetCreatureCustomColor));
    }

    [Test]
    public void PresetPickerColorsMatchThePaletteAtlasMiddletone()
    {
        var repositoryRoot = FindRepositoryRoot();
        var atlas = File.ReadAllBytes(Path.Combine(
            repositoryRoot.FullName,
            "SWLOR_Haks",
            "sw_item",
            "plt_palette.tga"));
        var width = BitConverter.ToUInt16(atlas, 12);
        var height = BitConverter.ToUInt16(atlas, 14);
        var bitsPerPixel = atlas[16];
        var isTopOrigin = (atlas[17] & 0x20) != 0;

        width.Should().Be(256);
        height.Should().Be(TintMapMaterialRegistry.PaletteTextureHeight);
        bitsPerPixel.Should().Be(32);

        foreach (var layer in Enum.GetValues<TintMapLayerType>())
        {
            var baseRow = TintMapMaterialRegistry.GetLayer(layer).PaletteBaseRow;
            for (var colorId = 0; colorId < TintMapMaterialRegistry.PaletteColorCount; colorId++)
            {
                var atlasY = baseRow + colorId;
                var fileY = isTopOrigin ? height - 1 - atlasY : atlasY;
                var pixelOffset = 18 + (fileY * width + 128) * 4;
                var expected = new TintMapColor(
                    atlas[pixelOffset + 2],
                    atlas[pixelOffset + 1],
                    atlas[pixelOffset]);

                TintMapPaletteColors.GetColor(layer, colorId).Should().Be(expected);
            }
        }
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
    public void LegacyArmorPartColorZeroInheritsTheAuthoredGlobalDye()
    {
        ArmorColorIndexCalculator.ShouldUsePerPartColor(0, false).Should().BeFalse();
        ArmorColorIndexCalculator.ShouldUsePerPartColor(0, true).Should().BeTrue();
        ArmorColorIndexCalculator.ShouldUsePerPartColor(23, false).Should().BeTrue();
        ArmorColorIndexCalculator.ShouldUsePerPartColor(255, true).Should().BeFalse();

        var serviceSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var standardColorMethod = FindMethod(serviceSource, "GetStandardColor").ToString();
        standardColorMethod.Should().Contain("ShouldUsePerPartColor");
        standardColorMethod.Should().Contain("GetPerPartOverrideVariableName");
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
    public void NonPartsAppearancesIncludeVisibleItemOwnedTintSelections()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapModelResolver.cs");
        var method = FindMethod(source, "GetCurrentSelections");
        var nonPartsBranch = method.DescendantNodes()
            .OfType<ElseClauseSyntax>()
            .Single();

        var calls = nonPartsBranch.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();
        calls.Should().Contain("AddSimpleItemSelections",
            "a non-modular creature can still visibly equip a helmet");
        calls.Should().Contain("AddCloakSelections",
            "a non-modular creature can still visibly equip an item-owned cloak");
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
        method.ToString().Should().Contain("destinationPartId",
            "the old destination model must be captured before its part is replaced");
        method.ToString().Should().Contain("previousDestinationMaterials");
        method.ToString().Should().Contain("activeVariables.Contains(previousVariable)",
            "an obsolete destination key must be removed without deleting a key another active part shares");
        method.ToString().Should().Contain("ResolvePartId",
            "zero-valued armor model fields render the creature's body-part fallback");
        method.ToString().Should().Contain("GetCreatureBodyPart",
            "cleanup must inspect the model that was actually rendered before mirroring");
        method.ToString().Should().NotContain("destinationPartId > 0",
            "a zero-valued destination still has a rendered fallback model whose stale tints must be removed");
        method.ToString().Should().Contain("sourceLayerMaterials.Count == 1",
            "one source material can intentionally map to several destination materials");
        method.ToString().Should().Contain("sourceLayerMaterials[index]",
            "multi-material parts must retain the corresponding material's distinct color");
        method.ToString().Should().Contain("index < sourceLayerMaterials.Count",
            "an unmatched destination material must be cleared rather than borrowing another color");
        method.ToString().Should().Contain("GetEquivalentMaterialVariables",
            "mirroring and cleanup must include the corresponding material slots for inactive wearer variants");
        FindMethod(resolverSource, "GetEquivalentMaterialVariables").ToString().Should()
            .Contain(nameof(TintMapMaterialRegistry.GetEquivalentEquipmentMaterialResrefs));

        var copyColorMethod = FindMethod(viewModelSource, "CopyColor");
        var copyColorBody = copyColorMethod.ToString();
        copyColorBody.Should().Contain("ShouldUsePerPartColor",
            "mirroring must distinguish an explicit part color from an inherited global color");
        copyColorBody.Should().Contain("sourceUsesPerPartColor ? sourceColor : 255",
            "an inherited source must leave the destination in the inherited sentinel state");
        copyColorBody.Should().Contain("ClearPerPartColorOverride",
            "an inherited source must clear any explicit destination marker");
    }

    [Test]
    public void BitmapOnlyMeshesResolveOnlyGeneratedTintMaterials()
    {
        var previewSource = ReadSource(
            "SWLOR.Toolset",
            "Workspace",
            "BlueprintPreviewRenderer.cs");
        var previewResolver = FindMethod(previewSource, "ResolveMeshTexture").ToString();
        previewResolver.Should().Contain("hasMaterial || IsGeneratedTintMaterial(surfaceName)",
            "an explicit material or a recognized generated tint material may resolve through MTR");
        previewResolver.Should().NotContain("resolveMaterial: true",
            "a bitmap-only mesh must not be replaced by an unrelated same-resref MTR");
        FindMethod(previewSource, "IsGeneratedTintMaterial").ToString()
            .Should().Contain("TintMapTextureRenderer.IsTintMapMaterial",
                "same-resref fallback is limited to the generated tint shader family");

        var viewportSource = ReadSource(
            "SWLOR.Toolset",
            "Viewport",
            "GlAreaControl.cs");
        var viewportResolver = FindMethod(viewportSource, "ResolveTexture").ToString();
        viewportResolver.Should().Contain("hasMaterial || parsedMaterial != null",
            "explicit MTR bindings remain authoritative in the live viewport");
        viewportResolver.Should().Contain("IsTintMapMaterial(candidate)",
            "bitmap-name fallback in the live viewport must use the same tint-material gate");
        viewportResolver.Should().NotContain("resolveMaterial: true",
            "ordinary bitmap-only viewport meshes must bypass same-resref MTR lookup");
    }

    [Test]
    public void EquipmentModelChangesCarryCustomTintsToReplacementMaterials()
    {
        var viewModelSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        var modifyMethod = FindMethod(viewModelSource, "ModifyItemPart");
        var modifyCalls = modifyMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();

        modifyCalls.Should().Contain("CaptureItemCustomColors",
            "the old material keys must be read before CopyItemAndModify destroys the source item");
        modifyCalls.Should().Contain("QueueItemCustomColorCarry",
            "the replacement material resrefs are only available after the new item is equipped");
        modifyMethod.ToString().Should().Contain("_target, item, copy, Player, slot, armorPart, tintCarry",
            "the delayed carry needs both sides of the replacement so it can validate rapid-click lineage");
        modifyMethod.ToString().Should().Contain("selection.ArmorPart == armorPart",
            "one modular armor part must not overwrite another part's custom dyes");

        var paletteMethod = FindMethod(viewModelSource, "OnClickColorPalette");
        paletteMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .Count(name => name == "LinkPendingItemColorCarryReplacement")
            .Should().Be(2,
                "global and per-part preset copies must remain descendants of a pending model carry");
        paletteMethod.ToString().IndexOf("ResetCurrentCustomTintOverrides", StringComparison.Ordinal)
            .Should().BeLessThan(
                paletteMethod.ToString().IndexOf("LinkPendingItemColorCarryReplacement", StringComparison.Ordinal),
                "the selected layer revision must advance before its preset replacement joins the lineage");

        var clearMethod = FindMethod(viewModelSource, "OnClickClearColor");
        clearMethod.ToString().Should().Contain("LinkPendingItemColorCarryReplacement(item, copy)",
            "right-click preset resets also replace the equipped item during a pending carry");
        clearMethod.ToString().IndexOf("ResetCustomTintOverrides", StringComparison.Ordinal)
            .Should().BeLessThan(
                clearMethod.ToString().IndexOf("LinkPendingItemColorCarryReplacement", StringComparison.Ordinal),
                "reset intent must invalidate only its layer before the replacement is linked");

        FindMethod(viewModelSource, "ModifyHelmetCloakColor").ToString().Should()
            .Contain("LinkPendingItemColorCarryReplacement(item, copy)",
                "helmet and cloak preset copies must preserve pending custom colors on untouched layers");

        var serviceSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var captureMethod = FindMethod(serviceSource, "CaptureItemCustomColors");
        captureMethod.ToString().Should().Contain("TintMapVariable.IsCreatureColorLayer(layer)",
            "equipment swaps must leave creature-owned skin, hair and tattoo colors alone");
        captureMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .Should().Contain("Any",
                "layers without custom colors need no carry state");
        captureMethod.ToString().Should().Contain("TintMapItemColorSource(variableName, color)",
            "preset slots must be retained so partial custom colors keep their material position");
        captureMethod.ToString().Should().NotContain("distinct.Count != 1",
            "ambiguous colors still need their source keys captured for stale cleanup");

        FindMethod(serviceSource, "LinkPendingItemColorCarryReplacement").Modifiers
            .Select(modifier => modifier.Text)
            .Should().Contain("public",
                "all appearance-editor replacement paths need to register descendants");

        var carryMethod = FindMethod(serviceSource, "QueueItemCustomColorCarry");
        var carryCalls = carryMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();
        carryCalls.Should().Contain("SetColor");
        carryCalls.Should().Contain("GetItemInSlot",
            "rapid model selections can destroy an intermediate item before its carry runs");
        carryMethod.ToString().Should().Contain("LinkPendingItemColorCarryReplacement(sourceItem, item)",
            "even a colorless intermediate edit must link its replacement to the earlier pending carry");
        carryMethod.ToString().IndexOf("LinkPendingItemColorCarryReplacement(sourceItem, item)", StringComparison.Ordinal)
            .Should().BeLessThan(carryMethod.ToString().IndexOf("if (carry == null)", StringComparison.Ordinal),
                "the descendant link is required precisely when the rapid intermediate capture is empty");
        carryMethod.ToString().Should().Contain("BelongsToItemColorCarryLineage",
            "a destroyed intermediate item may follow the slot only to a registered replacement descendant");
        carryMethod.ToString().Should().Contain("GetIsObjectValid(item) && slottedItem == item",
            "a surviving replacement must still occupy the original slot before its carry is applied");
        carryMethod.ToString().Should().Contain("!GetIsObjectValid(item)",
            "the slot fallback must not redirect a surviving, moved item's colors to unrelated equipment");
        carryMethod.ToString().Should().Contain("PendingItemColorCarryLayerIsCurrent",
            "a newer preset or custom edit must cancel the older delayed carry for that tint layer");
        carryMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(invocation => GetInvokedMethodName(invocation) == "PendingItemColorCarryLayerIsCurrent")
            .ArgumentList.Arguments
            .Select(argument => argument.Expression.ToString())
            .Should().Equal(
                new[]
                {
                    "registration.Lineage",
                    "layer",
                    "armorPart",
                    "registration.Revisions"
                },
                "a color edit on one armor part must not cancel another part's pending carry for the same layer");
        carryMethod.ToString().Should().Contain("invalidatePendingCarry: false",
            "the carry's own writes must not invalidate its remaining derived work");
        carryMethod.ToString().Should().Contain("selection.PaletteSource == targetItem");
        carryMethod.ToString().Should().Contain("DeleteLocalInt(targetItem, variableName)");
        carryCalls.Should().Contain("DeleteLocalInt",
            "obsolete material keys must not resurrect an older color when the model returns");
        carryMethod.ToString().Should().Contain("activeVariables.Contains(variableName)",
            "cleanup must retain a material key that another equipped armor part still uses");
        carryMethod.ToString().Should().NotContain("if (destinations.Count == 0)",
            "a model with no destination for the layer must still discard inactive source keys");
        carryMethod.ToString().Should().Contain("distinctColors.Count == 1",
            "different per-material colors must not be guessed during a model replacement");
        carryMethod.ToString().Should().Contain("index < replacementDestinations.Count",
            "every replacement material needs an opportunity to inherit a persisted global tint");
        carryMethod.ToString().Should().Contain("index < replacedSources.Count",
            "an existing per-material color may migrate only to its corresponding replacement material");
        carryMethod.ToString().Should().Contain("replacedSources[index].Color ?? globalColor",
            "preset source slots must not be flattened to the layer's one custom color");
        carryMethod.ToString().Should().Contain("GetItemGlobalColorStateName(layer)",
            "new replacement materials must inherit an explicitly global equipment tint");
        carryMethod.ToString().Should().Contain("distinctColors.Count == 1 || globalColor.HasValue",
            "different per-material colors remain positional while unmatched materials use the global tint");
        carryMethod.ToString().Should().Contain("!destinationVariables.Contains(source.VariableName)",
            "shared source slots must be removed before replacement slots are aligned");
        carryMethod.ToString().Should().Contain("!sourceVariables.Contains",
            "shared destination slots must be removed before replacement slots are aligned");
        carryMethod.ToString().Should().Contain("source.Color.HasValue",
            "ambiguous custom source variables must still reach stale-key cleanup");
        carryCalls.Should().Contain("ApplyCurrentColors",
            "the equipped replacement must render its carried colors immediately");
        carryCalls.Should().Contain("PublishRefreshEvent",
            "the open appearance editor must show the replacement material's current value");

        var setColorMethod = CSharpSyntaxTree.ParseText(serviceSource).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.Text == nameof(TintMapService.SetColor) &&
                              method.ParameterList.Parameters.Count == 4);
        setColorMethod.ToString().Should().Contain("invalidatePendingCarry: true",
            "an explicit color edit must advance the pending-carry revision for its layer");
        var privateSetColorMethod = CSharpSyntaxTree.ParseText(serviceSource).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.Text == nameof(TintMapService.SetColor) &&
                              method.ParameterList.Parameters.Count == 5);
        privateSetColorMethod.ToString().Should().Contain("GetEquivalentItemTintVariables",
            "a new custom color must replace stale equivalent wearer-variant values too");
        var resetColorMethod = FindMethod(serviceSource, nameof(TintMapService.ResetColor));
        resetColorMethod.ToString().Should().Contain(
            "MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart)",
            "reset sequencing must be scoped to the edited armor part as well as its layer");
        resetColorMethod.ToString().Should().Contain("GetEquivalentItemTintVariables",
            "resetting a carried color must remove inactive wearer-variant keys that could resurrect it");
        FindMethod(serviceSource, "MarkPendingItemColorEdit").ParameterList.Parameters
            .Select(parameter => parameter.Identifier.Text)
            .Should().Contain("armorPart");
        FindMethod(serviceSource, "RegisterPendingItemColorCarry").ToString().Should()
            .Contain("new ItemColorCarryRevisionScope(layer, armorPart)",
                "two armor parts sharing a layer need independent revision scopes");
    }

    [Test]
    public void NormalEquipmentRefreshCarriesOnlyMatchingCustomColorsToCurrentMaterials()
    {
        var serviceSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var refreshMethod = FindMethod(serviceSource, nameof(TintMapService.QueueRefresh));
        var refreshBody = refreshMethod.ToString();
        refreshBody.Should().Contain("CarryStoredEquipmentCustomColors(creature)");
        refreshBody.IndexOf("CarryStoredEquipmentCustomColors(creature)", StringComparison.Ordinal)
            .Should().BeLessThan(refreshBody.IndexOf("ApplyCurrentColors(creature)", StringComparison.Ordinal),
                "wearer-specific material locals must exist before shader uniforms are applied");

        var carryMethod = FindMethod(serviceSource, "CarryStoredEquipmentCustomColors");
        var carryCalls = carryMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetInvokedMethodName)
            .ToList();
        carryCalls.Should().Contain("GetItemTintOverrides");
        carryCalls.Should().Contain("TryParse");
        carryCalls.Should().Contain("TryFromStoredValue");
        carryCalls.Should().Contain("SetColor");
        carryCalls.Should().Contain("AreEquipmentMaterialsEquivalent",
            "hashed generated material names must still migrate across wearer variants");
        carryMethod.ToString().Should().Contain("destinationVariable",
            "an existing destination override must remain untouched");
        carryMethod.ToString().Should().Contain("destinationColor <= TintMapMaterialRegistry.PaletteColorCount",
            "compatibility-format palette overrides are authoritative destination values too");
        carryMethod.ToString().Should().Contain("matchingColors.Count == 1",
            "only an unambiguous color for the corresponding material may migrate");
        carryMethod.ToString().Should().Contain("GetItemGlobalColorStateName(layer)",
            "an explicit global tint must fill materials newly exposed by another wearer variant");
        carryMethod.ToString().Should().Contain("out var globalColor",
            "global intent must be distinguished from coincidentally uniform per-material colors");
        carryMethod.ToString().Should().Contain("invalidatePendingCarry: false",
            "automatic refresh migration must not look like a newer explicit color edit");
        carryMethod.ToString().Should().NotContain("Dictionary<TintMapLayerType",
            "a partial dye on one armor part must not be spread to every material exposing the layer");
        carryMethod.ToString().Should().Contain("TintMapVariable.IsCreatureColorLayer(layer)",
            "equipment colors must not overwrite skin, hair, or tattoos");

        var equivalentVariablesMethod = FindMethod(serviceSource, "GetEquivalentItemTintVariables");
        equivalentVariablesMethod.ToString().Should().Contain("TryParse",
            "reset cleanup must inspect every stored material key on the item");
        equivalentVariablesMethod.ToString().Should().Contain("variableLayer == layer",
            "reset cleanup must leave other tint layers untouched");
        equivalentVariablesMethod.ToString().Should().Contain("AreEquipmentMaterialsEquivalent",
            "only corresponding material slots across wearer variants may be cleared");
        var equivalenceMethod = FindMethod(serviceSource, "AreEquipmentMaterialsEquivalent");
        equivalenceMethod.ToString().Should().Contain("AreEquipmentMaterialSlotsEquivalent",
            "hashed material variants require registry slot matching");
        equivalenceMethod.ToString().Should().NotContain("GetVariantIdentity",
            "normalizing a material resref alone can alias two distinct slots across wearer variants");

        TintMapEquipmentMaterialMatcher.GetVariantIdentity("pmh0_shor012").Should().Be("shor012");
        TintMapEquipmentMaterialMatcher.GetVariantIdentity("pfh0_shor012").Should().Be("shor012");
        TintMapEquipmentMaterialMatcher.GetVariantIdentity("pmd22_shor012").Should().Be("shor012");
        TintMapEquipmentMaterialMatcher.GetVariantIdentity("shared_material").Should().Be("shared_material");

        var setGlobalItemColor = FindMethod(serviceSource, nameof(TintMapService.SetGlobalItemCustomColor));
        setGlobalItemColor.ToString().Should().Contain("GetItemGlobalColorStateName(layer)",
            "global equipment tints need persisted intent distinct from per-part material keys");
        setGlobalItemColor.ToString().Should().Contain("selectionColor == previousGlobalColor",
            "changing a global RGB tint must preserve independently customized armor parts");
        setGlobalItemColor.ToString().Should().Contain("!hasSelectionColor",
            "a newly exposed material without a stored override still inherits the changed global tint");
        setGlobalItemColor.ToString().Should().Contain("hasPreviousGlobalColor &&",
            "the first global RGB tint must preserve existing per-part custom colors");
        setGlobalItemColor.ToString().Should().NotContain("!hasPreviousGlobalColor ||",
            "missing legacy global intent must not make every per-part custom color look inherited");
        setGlobalItemColor.ToString().Should().Contain("Droid.UpdateEquippedItemSnapshot(creature, item)",
            "changing only the global marker must still persist equipped droid armor");
        var resetGlobalItemColor = FindMethod(serviceSource, nameof(TintMapService.ResetGlobalItemCustomColor));
        resetGlobalItemColor.ToString().Should().Contain("customColors.All(color => color.HasValue)",
            "legacy global state is safe to infer only when every active material is custom");
        resetGlobalItemColor.ToString().Should().Contain("color == globalColor",
            "a global preset must preserve independently customized armor parts");
        resetGlobalItemColor.ToString().Should().Contain("Droid.UpdateEquippedItemSnapshot(creature, item)",
            "removing only the global marker must still persist equipped droid armor");

        var viewModelSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "AppearanceEditorViewModel.cs");
        viewModelSource.Should().Contain(nameof(TintMapService.SetGlobalItemCustomColor));
        viewModelSource.Should().Contain(nameof(TintMapService.ResetGlobalItemCustomColor));
    }

    [Test]
    public void HashedEquipmentMaterialsMatchByCorrespondingWearerVariantSlot()
    {
        var catalog = new Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["pmh0_robe030"] = new[]
            {
                new TintMapMaterialDefinition(
                    "male skin",
                    "pmh0_p_ro_6c9e96",
                    TintMapLayerType.Skin,
                    TintMapLayerType.Cloth2),
                new TintMapMaterialDefinition(
                    "male equipment",
                    "pmh0_r_ro_7a1a22",
                    TintMapLayerType.Metal1,
                    TintMapLayerType.Cloth1)
            },
            ["pfh0_robe030"] = new[]
            {
                // tintmap.2da lists the equipment material before the skin material for the
                // female model, unlike the male model. Matching absolute list indexes is wrong.
                new TintMapMaterialDefinition(
                    "female equipment",
                    "pfh0_r_ro_82b326",
                    TintMapLayerType.Metal1,
                    TintMapLayerType.Cloth1),
                new TintMapMaterialDefinition(
                    "female skin",
                    "pmh0_p_ro_6c9e96",
                    TintMapLayerType.Skin,
                    TintMapLayerType.Cloth2)
            }
        };

        var index = new TintMapEquipmentMaterialIndex(catalog);
        index.AreEquivalent(
                "pmh0_r_ro_7a1a22",
                "pfh0_robe030",
                "pfh0_r_ro_82b326",
                TintMapLayerType.Metal1)
            .Should().BeTrue();
        index.AreEquivalent(
                "pmh0_p_ro_6c9e96",
                "pfh0_robe030",
                "pfh0_r_ro_82b326",
                TintMapLayerType.Metal1)
            .Should().BeFalse("a material in another slot must not receive the equipment dye");
        index.GetEquivalentMaterialResrefs(
                "pmh0_robe030",
                "pmh0_r_ro_7a1a22",
                TintMapLayerType.Metal1)
            .Should().BeEquivalentTo(
                "pmh0_r_ro_7a1a22",
                "pfh0_r_ro_82b326");
    }

    [Test]
    public void AmbiguousCrossVariantMaterialSlotsAreNotTreatedAsEquivalent()
    {
        var sharedMaterial = "pmh0_h_lh_1cb350";
        var catalog = new Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["pmh0_handl153"] = new[]
            {
                new TintMapMaterialDefinition("shared male slot zero", sharedMaterial, TintMapLayerType.Cloth1),
                new TintMapMaterialDefinition("male slot one", "pmh0_h_lh_other", TintMapLayerType.Cloth1)
            },
            ["pfh0_handl153"] = new[]
            {
                new TintMapMaterialDefinition("female slot zero", "pfh0_h_lh_other", TintMapLayerType.Cloth1),
                new TintMapMaterialDefinition("shared female slot one", sharedMaterial, TintMapLayerType.Cloth1)
            }
        };

        new TintMapEquipmentMaterialIndex(catalog).AreEquivalent(
                sharedMaterial,
                "pfh0_handl153",
                "pfh0_h_lh_other",
                TintMapLayerType.Cloth1)
            .Should().BeFalse(
                "the stored material resref does not identify which wearer variant supplied its slot");
    }

    [Test]
    public void EquipmentMaterialSlotsAreIndexedIndependentlyForEachLayer()
    {
        var catalog = new Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["pfa0_bicepr122"] = new[]
            {
                new TintMapMaterialDefinition(
                    "female material",
                    "pfh0_b_rb_805035",
                    TintMapLayerType.Metal1,
                    TintMapLayerType.Metal2)
            },
            ["pfh0_bicepr122"] = new[]
            {
                new TintMapMaterialDefinition(
                    "half-elf material",
                    "pfh0_bicepr122",
                    TintMapLayerType.Metal1,
                    TintMapLayerType.Cloth1,
                    TintMapLayerType.Cloth2,
                    TintMapLayerType.Leather1,
                    TintMapLayerType.Leather2)
            }
        };

        var index = new TintMapEquipmentMaterialIndex(catalog);
        index.AreEquivalent(
                "pfh0_b_rb_805035",
                "pfh0_bicepr122",
                "pfh0_bicepr122",
                TintMapLayerType.Metal1)
            .Should().BeTrue(
                "the first Metal 1 material remains the same slot even when its extra layers differ");
        index.GetEquivalentMaterialResrefs(
                "pfa0_bicepr122",
                "pfh0_b_rb_805035",
                TintMapLayerType.Metal1)
            .Should().BeEquivalentTo("pfh0_b_rb_805035", "pfh0_bicepr122");
    }

    [Test]
    public void NormalizedVariantIdentityDoesNotOverrideMaterialSlotIdentity()
    {
        var catalog = new Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["pmh0_robe017"] = new[]
            {
                new TintMapMaterialDefinition("male slot", "pmh0_robe017", TintMapLayerType.Cloth1)
            },
            ["pfh0_robe017"] = new[]
            {
                new TintMapMaterialDefinition("male material in female slot zero", "pmh0_robe017", TintMapLayerType.Cloth1),
                new TintMapMaterialDefinition("female material in slot one", "pfh0_robe017", TintMapLayerType.Cloth1)
            }
        };

        TintMapEquipmentMaterialMatcher.GetVariantIdentity("pmh0_robe017")
            .Should().Be(TintMapEquipmentMaterialMatcher.GetVariantIdentity("pfh0_robe017"));
        new TintMapEquipmentMaterialIndex(catalog).AreEquivalent(
                "pmh0_robe017",
                "pfh0_robe017",
                "pfh0_robe017",
                TintMapLayerType.Cloth1)
            .Should().BeFalse(
                "materials with the same normalized variant identity can occupy different slots");
    }

    [Test]
    public void TintMapVariableParsingRetainsTheMaterialIdentity()
    {
        TintMapVariable.TryParse(
                "TM_pmh0_chest070_4",
                out var materialResref,
                out var layer)
            .Should().BeTrue();
        materialResref.Should().Be("pmh0_chest070");
        layer.Should().Be(TintMapLayerType.Cloth1);

        TintMapVariable.TryParse("TM__5", out _, out _).Should().BeFalse();
        TintMapVariable.TryParse("APC_1_5", out _, out _).Should().BeFalse();
    }

    [Test]
    public void FlatColorViewportPassesClearTintMapState()
    {
        var viewportSource = ReadSource(
            "SWLOR.Toolset",
            "Viewport",
            "GlAreaControl.cs");
        var setUniformMethod = FindMethod(viewportSource, "SetUniformBool");
        var methodBody = setUniformMethod.ToString();

        methodBody.Should().Contain("name == \"hasTexture\" && !value",
            "every existing flat-color pass disables hasTexture through this shared path");
        methodBody.Should().Contain("SetUniformBoolCore(\"hasTintMap\", false)");
        methodBody.Should().Contain("SetUniformBoolCore(\"hasTintAlpha\", false)");
    }

    [Test]
    public void RodianMusicianPartsBindTheConvertedHumanFallbackMaterials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rows = ReadSource("SWLOR_Haks", "sw_2da", "tintmap.2da")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 4)
            .ToList();
        var expectedRows = new Dictionary<string, (string Material, string Layers)>
        {
            ["pfe0_chest070"] = ("pfh0_chest070", "0,2,5,6,7"),
            ["pfe0_legl123"] = ("pfh0_legl123", "0,6"),
            ["pfe0_legr123"] = ("pfh0_legr123", "0,6"),
            ["pfe0_shinl080"] = ("pfh0_shinl080", "7"),
            ["pfe0_shinr080"] = ("pfh0_shinr080", "7"),
            ["pfh0_legl123"] = ("pfh0_legl123", "0,6"),
            ["pfh0_legr123"] = ("pfh0_legr123", "0,6"),
            ["pme0_chest070"] = ("pmh0_chest070", "0,2,3,5,6,7"),
            ["pme0_footl102"] = ("pmh0_footl102", "2,3"),
            ["pme0_legl123"] = ("pmh0_legl123", "0,6"),
            ["pme0_legr123"] = ("pmh0_legr123", "0,6"),
            ["pme0_shinl080"] = ("pmh0_shinl080", "7"),
            ["pme0_shinr080"] = ("pmh0_shinr080", "7")
        };

        foreach (var (model, expected) in expectedRows)
        {
            var row = rows.Single(columns => columns[1] == model);
            row[2].Should().Be(expected.Material);
            row[3].Should().Be(expected.Layers);
        }

        var modelBindings = new Dictionary<string, string>
        {
            [Path.Combine("sw_pt_chest", "pfe0_chest070.mdl")] = "pfh0_chest070",
            [Path.Combine("sw_pt_lthigh", "pfe0_legl123.mdl")] = "pfh0_legl123",
            [Path.Combine("sw_pt_rthigh", "pfe0_legr123.mdl")] = "pfh0_legr123",
            [Path.Combine("sw_pt_lshin", "pfe0_shinl080.mdl")] = "pfh0_shinl080",
            [Path.Combine("sw_pt_rshin", "pfe0_shinr080.mdl")] = "pfh0_shinr080",
            [Path.Combine("sw_pt_chest", "pme0_chest070.mdl")] = "pmh0_chest070",
            [Path.Combine("sw_pt_lfoot", "pme0_footl102.mdl")] = "pmh0_footl102",
            [Path.Combine("sw_pt_lthigh", "pme0_legl123.mdl")] = "pmh0_legl123",
            [Path.Combine("sw_pt_rthigh", "pme0_legr123.mdl")] = "pmh0_legr123",
            [Path.Combine("sw_pt_lshin", "pme0_shinl080.mdl")] = "pmh0_shinl080",
            [Path.Combine("sw_pt_rshin", "pme0_shinr080.mdl")] = "pmh0_shinr080"
        };
        foreach (var (relativePath, material) in modelBindings)
        {
            var modelPath = Path.Combine(repositoryRoot.FullName, "SWLOR_Haks", relativePath);
            System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(modelPath))
                .Should().Contain(material);
        }

        foreach (var material in new[] { "pfh0_legl123", "pfh0_legr123" })
        {
            var materialPath = Path.Combine(
                repositoryRoot.FullName,
                "SWLOR_Haks",
                "sw_tint_mtr",
                $"{material}.mtr");
            File.ReadAllText(materialPath).Should().Contain("texture7 tm_53954255618f4");
        }
        File.Exists(Path.Combine(
                repositoryRoot.FullName,
                "SWLOR_Haks",
                "sw_tint1",
                "tm_53954255618f4.dds"))
            .Should().BeTrue();
    }

    [Test]
    public void HumanHand246ModelsUseTheirAuthoredSkinTintMasks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rows = ReadSource("SWLOR_Haks", "sw_2da", "tintmap.2da")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 4)
            .ToList();
        var expectedModels = new Dictionary<string, string>
        {
            [Path.Combine("sw_pt_lhand", "pfh0_handl246.mdl")] = "pfh0_handl246",
            [Path.Combine("sw_pt_rhand", "pfh0_handr246.mdl")] = "pfh0_handr246",
            [Path.Combine("sw_pt_lhand", "pmh0_handl246.mdl")] = "pmh0_handl246",
            [Path.Combine("sw_pt_rhand", "pmh0_handr246.mdl")] = "pmh0_handr246"
        };

        foreach (var (relativePath, model) in expectedModels)
        {
            var row = rows.Single(columns => columns[1] == model);
            row[2].Should().Be(model);
            row[3].Should().Be("0,1,2,4,5,6,7");

            var modelPath = Path.Combine(repositoryRoot.FullName, "SWLOR_Haks", relativePath);
            System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(modelPath))
                .Should().Contain(model);
        }

        var fallbackConfiguration = JObject.Parse(
            ReadSource("SWLOR_Haks", "tools", "TintMapFallbacks.json"));
        var authoredTextureOverrides = fallbackConfiguration["authoredTextureOverrides"]!
            .ToObject<Dictionary<string, string>>();
        authoredTextureOverrides.Should().BeEquivalentTo(
            expectedModels.Values.ToDictionary(model => model, model => model));
    }

    [Test]
    public void MaleMusicianOutfitKeepsItsAuthoredDyesAndConvertedThighMaterials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outfit = JObject.Parse(ReadSource("Module", "uti", "musicianoutfit1.uti.json"));
        outfit["Cloth1Color"]?["value"]?.Value<int>().Should().Be(132);
        outfit["Cloth2Color"]?["value"]?.Value<int>().Should().Be(132);
        outfit["Leather1Color"]?["value"]?.Value<int>().Should().Be(23);
        outfit["Leather2Color"]?["value"]?.Value<int>().Should().Be(23);
        outfit["Metal1Color"]?["value"]?.Value<int>().Should().Be(7);
        outfit["Metal2Color"]?["value"]?.Value<int>().Should().Be(7);

        var rows = ReadSource("SWLOR_Haks", "sw_2da", "tintmap.2da")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 4)
            .ToList();
        var expectedRows = new Dictionary<string, (string Material, string Layers)>
        {
            ["pmh0_chest070"] = ("pmh0_chest070", "0,2,3,5,6,7"),
            ["pmh0_legl123"] = ("pmh0_legl123", "0,6"),
            ["pmh0_legr123"] = ("pmh0_legr123", "0,6"),
            ["pmh0_shinl080"] = ("pmh0_shinl080", "7"),
            ["pmh0_shinr080"] = ("pmh0_shinr080", "7"),
            ["pmh0_footl102"] = ("pmh0_footl102", "2,3"),
            ["pmh0_footr102"] = ("pmh0_footr102", "2,3")
        };
        foreach (var (model, expected) in expectedRows)
        {
            var row = rows.Single(columns => columns[1] == model);
            row[2].Should().Be(expected.Material);
            row[3].Should().Be(expected.Layers);
        }

        foreach (var material in new[] { "pmh0_legl123", "pmh0_legr123" })
        {
            var modelPath = Path.Combine(
                repositoryRoot.FullName,
                "SWLOR_Haks",
                material.Contains("legl", StringComparison.Ordinal) ? "sw_pt_lthigh" : "sw_pt_rthigh",
                $"{material}.mdl");
            System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(modelPath))
                .Should().Contain(material);
            File.ReadAllText(Path.Combine(
                    repositoryRoot.FullName,
                    "SWLOR_Haks",
                    "sw_tint_mtr",
                    $"{material}.mtr"))
                .Should().Contain("texture7 tm_53954255618f4");
        }
    }

    [Test]
    public void TintLayersUseDedicatedScalarCustomModeUniforms()
    {
        var expectedUniforms = new Dictionary<TintMapLayerType, string>
        {
            [TintMapLayerType.Skin] = "useCustomSkin",
            [TintMapLayerType.Hair] = "useCustomHair",
            [TintMapLayerType.Metal1] = "useCustomMetal1",
            [TintMapLayerType.Metal2] = "useCustomMetal2",
            [TintMapLayerType.Cloth1] = "useCustomCloth1",
            [TintMapLayerType.Cloth2] = "useCustomCloth2",
            [TintMapLayerType.Leather1] = "useCustomLeath1",
            [TintMapLayerType.Leather2] = "useCustomLeath2",
            [TintMapLayerType.Tattoo1] = "useCustomTat1",
            [TintMapLayerType.Tattoo2] = "useCustomTat2"
        };

        foreach (var (layer, uniformName) in expectedUniforms)
        {
            TintMapMaterialRegistry.GetLayer(layer).CustomModeUniformName.Should().Be(uniformName);
            TintMapMaterialRegistry.GetPaletteCoordinate(layer, 42).Should().BePositive();
        }
    }

    [Test]
    public void ApplyingAColorWritesRgbAndAnIndependentCustomModeScalar()
    {
        var serviceSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapService.cs");
        var applyColor = FindMethod(serviceSource, "ApplyColor");
        var materialWrites = applyColor.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
                invocation.Expression.ToString() == "SetMaterialShaderUniformVec4")
            .ToList();

        materialWrites.Should().HaveCount(3);
        applyColor.ToString().Should().Contain("layerDefinition.ColorUniformName");
        applyColor.ToString().Should().Contain("layerDefinition.CustomModeUniformName");
        applyColor.ToString().Should().Contain("customColor.HasValue ? 1f : 0f");
        applyColor.ToString().Should().Contain("customColor.HasValue");
        applyColor.ToString().Should().Contain("? 0f");

        foreach (var shaderName in new[] { "fs_plt_tinter.shd", "fs_plt_tinter_nm.shd" })
        {
            ReadSource("SWLOR_Haks", "sw_shader", shaderName)
                .Should().Contain("customTintMode > 0.5 || v <= 0.0",
                    "custom RGB must still activate when a client drops the scalar mode override");
        }
    }

    [Test]
    public void RodianBountyHunterPartsBindTheConvertedHumanFallbackMaterials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rows = ReadSource("SWLOR_Haks", "sw_2da", "tintmap.2da")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length >= 4)
            .ToList();
        var expectedRows = new Dictionary<string, (string Material, string Layers)>
        {
            ["pme0_legl104"] = ("pmh0_legl104", "4"),
            ["pme0_pelvis102"] = ("pmh0_pelvis102", "4,6")
        };

        foreach (var (model, expected) in expectedRows)
        {
            var row = rows.Single(columns => columns[1] == model);
            row[2].Should().Be(expected.Material);
            row[3].Should().Be(expected.Layers);
        }

        var modelBindings = new Dictionary<string, string>
        {
            [Path.Combine("sw_pt_lthigh", "pme0_legl104.mdl")] = "pmh0_legl104",
            [Path.Combine("sw_pt_pelvis", "pme0_pelvis102.mdl")] = "pmh0_pelvis102"
        };
        foreach (var (relativePath, material) in modelBindings)
        {
            var modelPath = Path.Combine(repositoryRoot.FullName, "SWLOR_Haks", relativePath);
            System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(modelPath))
                .Should().Contain(material);
        }
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
    public void TintMapRegistryPrecomputesEquipmentMaterialSlotsDuringLoad()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "AppearanceDefinition",
            "TintMap",
            "TintMapMaterialRegistry.cs");
        var loadMethod = FindMethod(source, nameof(TintMapMaterialRegistry.Load));
        loadMethod.ToString().Should().Contain(
            "_equipmentMaterialIndex = new TintMapEquipmentMaterialIndex(MaterialsByModel)",
            "the complete tint-map catalog should be indexed once when the registry loads");

        var equivalenceMethod = FindMethod(
            source,
            nameof(TintMapMaterialRegistry.AreEquipmentMaterialSlotsEquivalent));
        equivalenceMethod.ToString().Should().Contain("_equipmentMaterialIndex.AreEquivalent");
        equivalenceMethod.ToString().Should().NotContain("MaterialsByModel",
            "interactive tint changes must not rescan every registered model");

        var equivalentMaterialsMethod = FindMethod(
            source,
            nameof(TintMapMaterialRegistry.GetEquivalentEquipmentMaterialResrefs));
        equivalentMaterialsMethod.ToString().Should()
            .Contain("_equipmentMaterialIndex.GetEquivalentMaterialResrefs");
        equivalentMaterialsMethod.ToString().Should().NotContain("MaterialsByModel",
            "equivalent material expansion should use the precomputed slot index");
    }

    [Test]
    public void PlainPltBodyPartsRetainLegacyTangentsWhileMappedMaterialsRetainTheirMaps()
    {
        var repositoryRoot = FindRepositoryRoot();
        var materialRoot = Path.Combine(repositoryRoot.FullName, "SWLOR_Haks", "sw_tint_mtr");
        var plainBodyMaterials = new[]
        {
            // Default nude male body pieces from the in-game seam regression.
            "pmh0_chest001", "pmh0_bicepl001", "pmh0_forel001", "pmh0_handl001",
            "pmh0_legl001", "pmh0_shinl001", "pmh0_footl001",

            // Party Outfit Male (party_male.uti.json), which must follow the
            // same legacy PLT lighting path as the stock NWN renderer.
            "pmh0_chest168", "pmh0_bicepl008", "pmh0_neck115", "pmh0_pelvis237",
            "pmh0_legl088", "pmh0_shinl081", "pmh0_footl052"
        };

        foreach (var materialName in plainBodyMaterials)
        {
            var material = File.ReadAllText(Path.Combine(materialRoot, $"{materialName}.mtr"));
            material.Should().Contain("customshaderVS vslit_sm");
            material.Should().Contain("customshaderFS fs_plt_tinter");
            material.Should().NotContain("customshaderVS vslit_sm_nm");
            material.Should().NotContain("customshaderFS fs_plt_tinter_nm");
            material.Should().Contain("renderhint NormalTangents");
            material.Should().NotContain("renderhint NormalAndSpecMapped");
        }

        var mappedMaterial = File.ReadAllText(Path.Combine(materialRoot, "helm_034.mtr"));
        mappedMaterial.Should().Contain("renderhint NormalAndSpecMapped");
        mappedMaterial.Should().Contain("texture1 helm_034_n");
        mappedMaterial.Should().Contain("customshaderVS vslit_sm_nm");
        mappedMaterial.Should().Contain("customshaderFS fs_plt_tinter_nm");

        var legacyShader = ReadSource("SWLOR_Haks", "sw_shader", "fs_plt_tinter.shd");
        var mappedShader = ReadSource("SWLOR_Haks", "sw_shader", "fs_plt_tinter_nm.shd");
        foreach (var macro in new[]
                 {
                     "NORMAL_MAP", "SPECULAR_MAP", "ROUGHNESS_MAP", "SELF_ILLUMINATION_MAP"
                 })
        {
            legacyShader.Should().Contain($"#define {macro} 0");
            mappedShader.Should().Contain($"#define {macro} 1");
        }

        foreach (var shader in new[] { legacyShader, mappedShader })
        {
            shader.Should().Contain("float referenceV = 0.000244;");
            shader.Should().Contain("clamp(customTint.rgb * shadeScale, 0.0, 1.0)");
            shader.Should().Contain("vec2(128.5 / 256.0, referenceV)",
                "custom RGB is represented by the same midtone as the preset swatches");
            shader.Should().NotContain("vec2(255.5 / 256.0, referenceV)",
                "normalizing at the brightest palette texel darkens ordinary skin texels");
            shader.Should().NotContain("customTint.rgb * g",
                "raw PLT intensity exaggerates seams between modular skin parts");
        }
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
        getOverridesMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should()
            .Contain(invocation =>
                IsMemberInvocation(
                    invocation,
                    "TintMapVariable",
                    nameof(TintMapVariable.IsItemGlobalColorStateName)),
                "saved global tint intent must replace stale global state on the currently equipped armor");
        getOverridesMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Should()
            .Contain(invocation =>
                IsMemberInvocation(
                    invocation,
                    "ArmorColorIndexCalculator",
                    "IsPerPartOverrideVariableName"),
                "saved palette-zero markers must replace stale markers on the currently equipped armor");
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
        loadInvocations.Where(invocation => GetInvokedMethodName(invocation) == "CopyItemAndModify")
            .Should()
            .OnlyContain(invocation =>
                    invocation.ArgumentList.Arguments.Count == 5 &&
                    invocation.ArgumentList.Arguments[4].Expression.Kind() == SyntaxKind.TrueLiteralExpression,
                "every intermediate armor copy must retain unrelated item locals");
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
            if (Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")) &&
                File.Exists(Path.Combine(directory.FullName, "Build", "hakbuilder.json")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
