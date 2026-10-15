using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

public class AppearanceEditorSettingsTests
{
    private MethodInfo _execute = null!;

    [OneTimeSetUp]
    public void CompileSettingsWithRecordingEngineAndDatabase()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.cs"));
        var names = new[] { "InitializeSettings", "LoadSettings", "OnClickSaveSettings" };
        var methods = string.Concat(CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(node => names.Contains(node.Identifier.Text))
            .Select(node => node.ToFullString()));
        var harness = $$"""
            using System;
            using System.Linq.Expressions;
            using SWLOR.NWN.API.NWScript.Enum;
            using Player = SWLOR.Game.Server.Entity.Player;
            using static World;
            public static class World
            {
                public static bool ViewerDM, ViewerPossessed, TargetPC, TargetDM, TargetPossessed;
                public static bool GetIsPC(uint creature) => creature == 2 && TargetPC;
                public static bool GetIsDM(uint creature) => creature == 1 ? ViewerDM : TargetDM;
                public static bool GetIsDMPossessed(uint creature) => creature == 1 ? ViewerPossessed : TargetPossessed;
                public static string GetObjectUUID(uint creature) => "test-player";
                public static void SendMessageToPC(uint creature, string message) { }
                public static float GetObjectVisualTransform(uint creature, ObjectVisualTransform transform) => 0.92f;
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
            public static class HelmetModelRenderer
            {
                public static float GetHeadScale(uint creature) => 1.07f;
            }
            public static class ColorToken { public static string Green(string text) => text; }
            public class Harness
            {
                private uint _target = 2;
                public uint Player = 1;
                public bool IsSettingsVisible, IsPlayerSettingsVisible, ShowHelmet, ShowCloak;
                private int _watches;
                private void WatchOnClient(Expression<Func<Harness, bool>> binding) => _watches++;
                private void UpdateArmorDisplay() { }
                {{methods}}
                public static (bool Visible, bool PlayerControls, int Reads, int Writes, int Watches,
                    float Height, float Head, bool Helmet, bool Cloak)
                    Execute(bool viewerDM, bool viewerPossessed, bool targetPC, bool targetDM, bool targetPossessed)
                {
                    ViewerDM = viewerDM; ViewerPossessed = viewerPossessed;
                    TargetPC = targetPC; TargetDM = targetDM; TargetPossessed = targetPossessed;
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
                        DB.Record.HeadAppearanceScale, harness.ShowHelmet, harness.ShowCloak);
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
        _execute = Assembly.Load(stream.ToArray()).GetType("Harness")!.GetMethod("Execute")!;
    }

    [TestCase(true, false, false, false, false)]
    [TestCase(false, true, false, false, true)]
    [TestCase(false, true, true, false, true)]
    [TestCase(true, false, true, true, false)]
    public void DMCanOpenSettingsWithoutLoadingOrSavingPlayerData(
        bool viewerDM, bool viewerPossessed, bool targetPC, bool targetDM, bool targetPossessed)
    {
        var result = Execute(viewerDM, viewerPossessed, targetPC, targetDM, targetPossessed);
        result.Visible.Should().BeTrue();
        result.PlayerControls.Should().BeFalse();
        result.Reads.Should().Be(0);
        result.Writes.Should().Be(0);
        result.Watches.Should().Be(0);
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

    private (bool Visible, bool PlayerControls, int Reads, int Writes, int Watches,
        float Height, float Head, bool Helmet, bool Cloak) Execute(
        bool viewerDM, bool viewerPossessed, bool targetPC, bool targetDM, bool targetPossessed)
    {
        return ((bool, bool, int, int, int, float, float, bool, bool))_execute.Invoke(null,
            [viewerDM, viewerPossessed, targetPC, targetDM, targetPossessed])!;
    }
}
