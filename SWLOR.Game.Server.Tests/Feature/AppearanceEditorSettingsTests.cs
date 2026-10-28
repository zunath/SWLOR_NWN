using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Tests.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AppearanceEditorSettingsTests
{
    private MethodInfo _execute = null!;
    private MethodInfo _adjustScale = null!;
    private MethodInfo _loadDMItems = null!;
    private MethodInfo _adjustPosition = null!;
    private MethodInfo _editPosition = null!;
    private MethodInfo _resetHead = null!;
    private MethodInfo _headSettingsText = null!;

    [OneTimeSetUp]
    public void CompileSettingsWithRecordingEngineAndDatabase()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.cs"));
        source += File.ReadAllText(Path.Combine(directory.FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.HeadSettings.cs"));
        var names = new[] { "InitializeSettings", "LoadSettings", "LoadDMArmorDisplay", "OnClickSaveSettings", "SendScaleMessage",
            "OnIncreaseAppearanceScale", "OnDecreaseAppearanceScale", "OnIncreaseHeadScale", "OnDecreaseHeadScale",
            "RefreshHeadTransformSettings", "OnAdjustHeadPosition", "OnResetHead", "OnCloseWindow" };
        var methods = string.Concat(CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(node => names.Contains(node.Identifier.Text))
            .Select(node => node.ToFullString()));
        var armorDisplaySource = File.ReadAllText(Path.Combine(directory.FullName,
            "SWLOR.Game.Server", "Feature", "ArmorDisplay.cs"));
        var getDMArmorDisplay = CSharpSyntaxTree.ParseText(armorDisplaySource).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.Text == "GetDMArmorDisplay").ToFullString();
        var dmPresence = DMPossessionHarness.Method("Service/DMPresence.cs", "GetControlledCreature") +
                         DMPossessionHarness.Method("Service/DMPresence.cs", "SendMessage");
        var harness = $$"""
            using System;
            using System.Globalization;
            using System.Linq.Expressions;
            using SWLOR.Game.Server.Feature.AppearanceDefinition;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
            using SWLOR.NWN.API.NWScript.Enum;
            using DMPreferences = SWLOR.Game.Server.Entity.DMPreferences;
            using Player = SWLOR.Game.Server.Entity.Player;
            using static World;
            public static class World
            {
                public static bool ViewerDM, ViewerPossessed, TargetPC, TargetDM, TargetPossessed;
                public static bool WearsHelmet, HelmetShown, WearsCloak, CloakHidden;
                public static float Height, Head, X, Y, Z;
                public static ObjectVisualTransformDataScopeType LastScope;
                public static uint Recipient;
                public static string Message;
                // The window owner (1) is what the viewer controls; a possessing DM's avatar (3) is not a PC.
                public static bool GetIsPC(uint creature) => creature == 1 || creature == 2 && TargetPC;
                public static bool GetIsDM(uint creature) => creature == 1 ? ViewerDM : TargetDM;
                public static bool GetIsDMPossessed(uint creature) => creature == 1 ? ViewerPossessed : TargetPossessed;
                public static uint GetMaster(uint creature) => 3;
                public static AppearanceType GetAppearanceType(uint creature) => AppearanceType.Human;
                public static string GetObjectUUID(uint creature) => "test-player";
                public static void SendMessageToPC(uint creature, string message) { Recipient = creature; Message = message; }
                public static float GetObjectVisualTransform(uint creature, ObjectVisualTransform transform,
                    bool bCurrentLerp = false, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Base)
                {
                    LastScope = nScope;
                    if (nScope != ObjectVisualTransformDataScopeType.CreatureHead) return Height;
                    return transform == ObjectVisualTransform.TranslateX ? X : transform == ObjectVisualTransform.TranslateY ? Y : Z;
                }
                public static void SetObjectVisualTransform(uint creature, ObjectVisualTransform transform, float value,
                    ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Base)
                {
                    LastScope = nScope;
                    if (nScope != ObjectVisualTransformDataScopeType.CreatureHead) Height = value;
                    else if (transform == ObjectVisualTransform.TranslateX) X = value;
                    else if (transform == ObjectVisualTransform.TranslateY) Y = value;
                    else Z = value;
                }
                public const uint Helmet = 10, Cloak = 11, OBJECT_INVALID = 0x7F000000;
                public static uint GetItemInSlot(InventorySlot slot, uint creature) =>
                    slot == InventorySlot.Head ? (WearsHelmet ? Helmet : OBJECT_INVALID) : (WearsCloak ? Cloak : OBJECT_INVALID);
                public static bool GetIsObjectValid(uint obj) => obj != OBJECT_INVALID;
                public static int GetHiddenWhenEquipped(uint item) => item == Cloak && CloakHidden ? 1 : 0;
            }
            public static class DMPreference
            {
                public static DMPreferences Saved = new();
                public static int Reads;
                public static DMPreferences Get(uint actor)
                {
                    if (!TargetDM) throw new Exception("Only a DM avatar falls back to saved DM preferences");
                    Reads++;
                    return Saved;
                }
            }
            public static class DB
            {
                public static Player Record;
                public static int Reads, Writes;
                public static T Get<T>(string id) where T : class
                {
                    if (!TargetPC || TargetDM || TargetPossessed) throw new Exception("NPC or DM accessed player data");
                    Reads++;
                    return Record as T;
                }
                public static void Set(Player player) => Writes++;
            }
            public static class ArmorDisplay
            {
                {{getDMArmorDisplay}}
            }
            public static class HelmetModelRenderer
            {
                public static float GetHeadScale(uint creature) => Head;
                public static void SetHeadScale(uint creature, float scale) => Head = scale;
                public static bool IsShownByOwner(uint helmet) => HelmetShown;
            }
            public static class ColorToken { public static string Green(string text) => text; }
            public static class PlayerPlugin { public static uint GetGameObject(uint player) => player; }
            public static class DMPresence
            {
                {{dmPresence}}
            }
            public class Harness
            {
                private uint _target = 2;
                public uint Player = 1;
                public bool IsSettingsVisible, IsPlayerSettingsVisible, IsArmorDisplayVisible, ShowHelmet, ShowCloak;
                public string HeadSizeText, HeadPositionXText, HeadPositionYText, HeadPositionZText;
                private int _tintEditGeneration;
                private void FlushPendingPickerColor() { }
                private void CancelTintPickerGesture() { }
                private void CommitCustomTintComponents() { }
                private bool _isDMArmorDisplay;
                private int _watches;
                private void WatchOnClient(Expression<Func<Harness, bool>> binding) => _watches++;
                private void UpdateArmorDisplay() { }
                private void CloseForUnsupportedAppearance() => throw new Exception("Unexpected unsupported appearance");
                {{methods}}
                public static (bool Visible, bool PlayerControls, int Reads, int Writes, int Watches,
                    float Height, float Head, bool Helmet, bool Cloak, bool ArmorDisplay)
                    Execute(bool viewerDM, bool viewerPossessed, bool targetPC, bool targetDM, bool targetPossessed)
                {
                    ViewerDM = viewerDM; ViewerPossessed = viewerPossessed;
                    TargetPC = targetPC; TargetDM = targetDM; TargetPossessed = targetPossessed;
                    WearsHelmet = WearsCloak = false;
                    Height = 0.92f; Head = 1.07f;
                    X = Y = Z = 0f;
                    DB.Record = new Player { AppearanceScale = 1f, HeadAppearanceScale = 1f };
                    DB.Record.Settings.ShowHelmet = false;
                    DB.Record.Settings.ShowCloak = false;
                    DB.Reads = 0; DB.Writes = 0;
                    var harness = new Harness();
                    harness.InitializeSettings();
                    // Selecting the tab reloads settings. Even a forged Save click must
                    // leave NPC/DM targets out of the player database.
                    harness.LoadSettings();
                    harness.OnClickSaveSettings()();
                    return (harness.IsSettingsVisible, harness.IsPlayerSettingsVisible,
                        DB.Reads, DB.Writes, harness._watches, DB.Record.AppearanceScale,
                        DB.Record.HeadAppearanceScale, harness.ShowHelmet, harness.ShowCloak, harness.IsArmorDisplayVisible);
                }
                public static (bool Helmet, bool Cloak, int PreferenceReads) LoadDMItems(
                    bool targetDM, bool wearsHelmet, bool helmetShown, bool wearsCloak, bool cloakHidden, bool savedHelmet, bool savedCloak)
                {
                    ViewerDM = targetDM; ViewerPossessed = !targetDM;
                    TargetPC = true; TargetDM = targetDM; TargetPossessed = !targetDM;
                    WearsHelmet = wearsHelmet; HelmetShown = helmetShown; WearsCloak = wearsCloak; CloakHidden = cloakHidden;
                    DMPreference.Saved = new DMPreferences { ShowHelmet = savedHelmet, ShowCloak = savedCloak };
                    DMPreference.Reads = 0;
                    var harness = new Harness();
                    harness.InitializeSettings();
                    return (harness.ShowHelmet, harness.ShowCloak, DMPreference.Reads);
                }
                public static (uint Recipient, string Message, float Height, float Head)
                    AdjustScale(string operation, bool possessed, float scale)
                {
                    ViewerPossessed = possessed; Height = scale; Head = scale;
                    Recipient = 0; Message = "";
                    var harness = new Harness();
                    var action = (Action)typeof(Harness).GetMethod(operation).Invoke(harness, null);
                    action();
                    return (Recipient, Message, Height, Head);
                }
                public static (float X, float Y, float Z, float Height, string Display, ObjectVisualTransformDataScopeType Scope)
                    AdjustPosition(ObjectVisualTransform axis, bool increase, float start)
                {
                    TargetPC = true; TargetDM = TargetPossessed = ViewerDM = ViewerPossessed = false;
                    Height = 0.92f; Head = 1.07f; X = Y = Z = start;
                    var harness = new Harness();
                    harness.OnAdjustHeadPosition(axis, increase)();
                    var text = axis == ObjectVisualTransform.TranslateX ? harness.HeadPositionXText :
                        axis == ObjectVisualTransform.TranslateY ? harness.HeadPositionYText : harness.HeadPositionZText;
                    return (X, Y, Z, Height, text, LastScope);
                }
                public static (float X, float Y, float Z, int Writes) EditPosition(bool save)
                {
                    TargetPC = true; TargetDM = TargetPossessed = ViewerDM = ViewerPossessed = false;
                    Height = 0.92f; Head = 1.07f; X = -0.05f; Y = 0.02f; Z = 0.03f;
                    DB.Writes = 0;
                    DB.Record = new Player { AppearanceScale = Height, HeadAppearanceScale = Head,
                        HeadAppearancePositionX = X, HeadAppearancePositionY = Y, HeadAppearancePositionZ = Z };
                    var harness = new Harness();
                    harness.InitializeSettings();
                    harness.OnAdjustHeadPosition(ObjectVisualTransform.TranslateX, true)();
                    harness.OnAdjustHeadPosition(ObjectVisualTransform.TranslateY, false)();
                    harness.OnAdjustHeadPosition(ObjectVisualTransform.TranslateZ, true)();
                    if (save) harness.OnClickSaveSettings()();
                    harness.OnCloseWindow()();
                    if (X != DB.Record.HeadAppearancePositionX || Y != DB.Record.HeadAppearancePositionY ||
                        Z != DB.Record.HeadAppearancePositionZ)
                        throw new Exception("Closing the window did not restore the saved head position");
                    X = Y = Z = 0f;
                    HeadPosition.ApplySaved(2, DB.Record);
                    return (X, Y, Z, DB.Writes);
                }
                public static string HeadSettingsText(float headScale)
                {
                    Head = headScale;
                    var harness = new Harness();
                    harness.RefreshHeadTransformSettings();
                    return harness.HeadSizeText;
                }
                public static (float Head, float X, float Y, float Z, float Height, int Writes) ResetHead(bool save)
                {
                    TargetPC = true; TargetDM = TargetPossessed = ViewerDM = ViewerPossessed = false;
                    Height = 0.92f; Head = 1.11f; X = 0.1f; Y = -0.1f; Z = 0.05f; DB.Writes = 0;
                    DB.Record = new Player { AppearanceScale = Height, HeadAppearanceScale = Head,
                        HeadAppearancePositionX = X, HeadAppearancePositionY = Y, HeadAppearancePositionZ = Z };
                    var harness = new Harness();
                    harness.InitializeSettings();
                    harness.OnResetHead()();
                    if (Head != 1f || X != 0f || Y != 0f || Z != 0f || harness.HeadSizeText != "1.00" ||
                        harness.HeadPositionXText != "0.00" || harness.HeadPositionYText != "0.00" || harness.HeadPositionZText != "0.00")
                        throw new Exception("Reset did not preview default head transforms and readouts");
                    if (save) harness.OnClickSaveSettings()();
                    harness.OnCloseWindow()();
                    return (Head, X, Y, Z, Height, DB.Writes);
                }
            }
            """;
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Concat(new[] { typeof(AppearanceEditorViewModel).Assembly.Location,
                typeof(SWLOR.NWN.API.NWScript.Enum.AppearanceType).Assembly.Location });
        var references = referencePaths.Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var positionSource = "using static World;\n" + File.ReadAllText(Path.Combine(directory.FullName,
            "SWLOR.Game.Server", "Feature", "AppearanceDefinition", "HeadPosition.cs"));
        var compilation = CSharpCompilation.Create("AppearanceEditorSettingsHarness",
            [CSharpSyntaxTree.ParseText(harness), CSharpSyntaxTree.ParseText(positionSource)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var harnessType = Assembly.Load(stream.ToArray()).GetType("Harness")!;
        _execute = harnessType.GetMethod("Execute")!;
        _loadDMItems = harnessType.GetMethod("LoadDMItems")!;
        _adjustScale = harnessType.GetMethod("AdjustScale")!;
        _adjustPosition = harnessType.GetMethod("AdjustPosition")!;
        _editPosition = harnessType.GetMethod("EditPosition")!;
        _resetHead = harnessType.GetMethod("ResetHead")!;
        _headSettingsText = harnessType.GetMethod("HeadSettingsText")!;
    }

    [TestCase(0f, "1.00")]
    [TestCase(-0.25f, "1.00")]
    [TestCase(1.13f, "1.13")]
    public void HeadSettingsDisplayNormalizesNonPositiveCachedScales(float headScale, string expected)
    {
        _headSettingsText.Invoke(null, new object[] { headScale }).Should().Be(expected);
    }

    [TestCase(ObjectVisualTransform.TranslateX)]
    [TestCase(ObjectVisualTransform.TranslateY)]
    [TestCase(ObjectVisualTransform.TranslateZ)]
    public void HeadPositionAdjustsOnlyItsAxisAndClampsAtBothLimits(ObjectVisualTransform axis)
    {
        foreach (var (start, increase, expected) in new[] { (0f, true, 0.01f), (0f, false, -0.01f),
                     (0.09f, true, 0.10f), (0.10f, true, 0.10f), (-0.09f, false, -0.10f), (-0.10f, false, -0.10f) })
        {
            var result = ((float X, float Y, float Z, float Height, string Display, ObjectVisualTransformDataScopeType Scope))
                _adjustPosition.Invoke(null, [axis, increase, start])!;
            result.X.Should().Be(axis == ObjectVisualTransform.TranslateX ? expected : start);
            result.Y.Should().Be(axis == ObjectVisualTransform.TranslateY ? expected : start);
            result.Z.Should().Be(axis == ObjectVisualTransform.TranslateZ ? expected : start);
            result.Height.Should().Be(0.92f);
            result.Scope.Should().Be(ObjectVisualTransformDataScopeType.CreatureHead);
            result.Display.Should().Be(expected.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [TestCase(false, -0.05f, 0.02f, 0.03f, 0)]
    [TestCase(true, -0.04f, 0.01f, 0.04f, 1)]
    public void HeadPositionUsesSavedValuesAfterCloseAndReload(bool save, float x, float y, float z, int writes)
    {
        var result = ((float X, float Y, float Z, int Writes))_editPosition.Invoke(null, [save])!;
        result.Should().Be((x, y, z, writes));
    }

    [TestCase(false, 1.11f, 0.1f, -0.1f, 0.05f, 0)]
    [TestCase(true, 1f, 0f, 0f, 0f, 1)]
    public void ResetHeadPreviewsDefaultsAndUsesTheExistingSaveCancelBehavior(bool save,
        float head, float x, float y, float z, int writes)
    {
        var result = ((float Head, float X, float Y, float Z, float Height, int Writes))_resetHead.Invoke(null, [save])!;
        result.Should().Be((head, x, y, z, 0.92f, writes));
    }

    [TestCase(true, false, false, false, false, false)]
    [TestCase(false, true, false, false, true, true)]
    [TestCase(false, true, true, false, true, true)]
    [TestCase(true, false, true, true, false, true)]
    public void DMCanOpenSettingsWithoutLoadingOrSavingPlayerData(
        bool viewerDM, bool viewerPossessed, bool targetPC, bool targetDM, bool targetPossessed, bool armorDisplay)
    {
        var result = Execute(viewerDM, viewerPossessed, targetPC, targetDM, targetPossessed);
        result.Visible.Should().BeTrue();
        result.PlayerControls.Should().BeFalse();
        result.Reads.Should().Be(0);
        result.Writes.Should().Be(0);
        result.ArmorDisplay.Should().Be(armorDisplay,
            "a DM's avatar or possessed creature gets helmet and cloak toggles; another NPC does not");
        result.Watches.Should().Be(armorDisplay ? 2 : 0);
    }

    [TestCase(true, false, true, false, false)]
    [TestCase(true, true, false, true, true)]
    [TestCase(false, false, true, false, false)]
    public void DMHelmetAndCloakTogglesShowWhatTheTargetWears(
        bool targetDM, bool helmetShown, bool cloakHidden, bool expectedHelmet, bool expectedCloak)
    {
        var result = LoadDMItems(targetDM, wearsHelmet: true, helmetShown, wearsCloak: true, cloakHidden,
            savedHelmet: !helmetShown, savedCloak: cloakHidden);
        result.Helmet.Should().Be(expectedHelmet);
        result.Cloak.Should().Be(expectedCloak);
    }

    [Test]
    public void DMAvatarWithEmptySlotsShowsItsSavedPreference()
    {
        var result = LoadDMItems(targetDM: true, wearsHelmet: false, helmetShown: true, wearsCloak: false,
            cloakHidden: false, savedHelmet: false, savedCloak: true);
        result.Helmet.Should().BeFalse();
        result.Cloak.Should().BeTrue();
        result.PreferenceReads.Should().Be(1);
    }

    [Test]
    public void PossessedCreatureWithEmptySlotsDefaultsToShown()
    {
        var result = LoadDMItems(targetDM: false, wearsHelmet: false, helmetShown: false, wearsCloak: false,
            cloakHidden: true, savedHelmet: false, savedCloak: false);
        result.Helmet.Should().BeTrue();
        result.Cloak.Should().BeTrue();
        result.PreferenceReads.Should().Be(0, "a possessed creature never borrows the DM avatar's preference");
    }

    private (bool Helmet, bool Cloak, int PreferenceReads) LoadDMItems(bool targetDM, bool wearsHelmet, bool helmetShown,
        bool wearsCloak, bool cloakHidden, bool savedHelmet, bool savedCloak)
    {
        return ((bool, bool, int))_loadDMItems.Invoke(null,
            [targetDM, wearsHelmet, helmetShown, wearsCloak, cloakHidden, savedHelmet, savedCloak])!;
    }

    [Test]
    public void PlayerEditingAnNPCDoesNotGainSettingsAccess()
    {
        var result = Execute(false, false, false, false, false);
        result.Visible.Should().BeFalse();
        result.PlayerControls.Should().BeFalse();
        result.Reads.Should().Be(0);
        result.Writes.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PlayerTargetRetainsPreferenceControlsAndScaleSaving(bool viewerDM)
    {
        var result = Execute(viewerDM, false, true, false, false);
        result.Visible.Should().BeTrue();
        result.PlayerControls.Should().BeTrue();
        result.Watches.Should().Be(2);
        result.Writes.Should().Be(1);
        result.Height.Should().Be(0.92f);
        result.Head.Should().Be(1.07f);
        result.Helmet.Should().BeFalse();
        result.Cloak.Should().BeFalse();
    }

    [TestCase("OnIncreaseHeadScale", 1.01f, "Head Size:", false)]
    [TestCase("OnIncreaseHeadScale", 1.01f, "Head Size:", true)]
    [TestCase("OnDecreaseHeadScale", 0.99f, "Head Size:", false)]
    [TestCase("OnDecreaseHeadScale", 0.99f, "Head Size:", true)]
    [TestCase("OnIncreaseAppearanceScale", 1.01f, "Height:", false)]
    [TestCase("OnIncreaseAppearanceScale", 1.01f, "Height:", true)]
    [TestCase("OnDecreaseAppearanceScale", 0.99f, "Height:", false)]
    [TestCase("OnDecreaseAppearanceScale", 0.99f, "Height:", true)]
    public void ScaleChangeReportsItsValueToTheWindowOwner(
        string operation, float expected, string message, bool possessed)
    {
        var result = AdjustScale(operation, possessed, 1f);
        result.Recipient.Should().Be(1u, "a possessing DM's avatar receives no messages");
        result.Message.Should().StartWith(message);
        (operation.Contains("Head") ? result.Head : result.Height).Should().BeApproximately(expected, 0.0001f);
    }

    [TestCase("OnIncreaseHeadScale", 1.15f, false)]
    [TestCase("OnIncreaseHeadScale", 1.15f, true)]
    [TestCase("OnDecreaseHeadScale", 0.85f, false)]
    [TestCase("OnDecreaseHeadScale", 0.85f, true)]
    [TestCase("OnIncreaseAppearanceScale", 1.15f, false)]
    [TestCase("OnIncreaseAppearanceScale", 1.15f, true)]
    [TestCase("OnDecreaseAppearanceScale", 0.85f, false)]
    [TestCase("OnDecreaseAppearanceScale", 0.85f, true)]
    public void ScaleLimitWarningReachesTheWindowOwner(
        string operation, float limit, bool possessed)
    {
        var result = AdjustScale(operation, possessed, limit);
        result.Recipient.Should().Be(1u, "a possessing DM's avatar receives no messages");
        result.Message.Should().Contain("any further");
        result.Head.Should().Be(limit);
        result.Height.Should().Be(limit);
    }

    private (uint Recipient, string Message, float Height, float Head) AdjustScale(string operation, bool possessed, float scale)
    {
        return ((uint, string, float, float))_adjustScale.Invoke(null, [operation, possessed, scale])!;
    }

    private (bool Visible, bool PlayerControls, int Reads, int Writes, int Watches,
        float Height, float Head, bool Helmet, bool Cloak, bool ArmorDisplay) Execute(
        bool viewerDM, bool viewerPossessed, bool targetPC, bool targetDM, bool targetPossessed)
    {
        return ((bool, bool, int, int, int, float, float, bool, bool, bool))_execute.Invoke(null,
            [viewerDM, viewerPossessed, targetPC, targetDM, targetPossessed])!;
    }
}
