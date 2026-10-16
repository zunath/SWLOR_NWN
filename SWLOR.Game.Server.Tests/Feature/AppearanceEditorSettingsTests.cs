using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Tests.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class AppearanceEditorSettingsTests
{
    private MethodInfo _execute = null!;
    private MethodInfo _adjustScale = null!;
    private MethodInfo _loadDMItems = null!;

    [OneTimeSetUp]
    public void CompileSettingsWithRecordingEngineAndDatabase()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.cs"));
        var names = new[] { "InitializeSettings", "LoadSettings", "LoadDMArmorDisplay", "OnClickSaveSettings", "SendScaleMessage",
            "OnIncreaseAppearanceScale", "OnDecreaseAppearanceScale", "OnIncreaseHeadScale", "OnDecreaseHeadScale" };
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
            using System.Linq.Expressions;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
            using SWLOR.NWN.API.NWScript.Enum;
            using DMPreferences = SWLOR.Game.Server.Entity.DMPreferences;
            using Player = SWLOR.Game.Server.Entity.Player;
            using static World;
            public static class World
            {
                public static bool ViewerDM, ViewerPossessed, TargetPC, TargetDM, TargetPossessed;
                public static bool WearsHelmet, HelmetShown, WearsCloak, CloakHidden;
                public static float Height, Head;
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
                public static float GetObjectVisualTransform(uint creature, ObjectVisualTransform transform) => Height;
                public static void SetObjectVisualTransform(uint creature, ObjectVisualTransform transform, float scale) => Height = scale;
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
            }
            """;
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Concat(new[] { typeof(AppearanceEditorViewModel).Assembly.Location,
                typeof(SWLOR.NWN.API.NWScript.Enum.AppearanceType).Assembly.Location });
        var references = referencePaths.Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("AppearanceEditorSettingsHarness",
            [CSharpSyntaxTree.ParseText(harness)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var harnessType = Assembly.Load(stream.ToArray()).GetType("Harness")!;
        _execute = harnessType.GetMethod("Execute")!;
        _loadDMItems = harnessType.GetMethod("LoadDMItems")!;
        _adjustScale = harnessType.GetMethod("AdjustScale")!;
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
