using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

public class HakSetupValidationTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-hak-setup-" + Guid.NewGuid().ToString("N"));
        Write("Build/hakbuilder.json", """
            {"HakList":[{"Name":"sw_2da","Path":"../SWLOR_Haks/sw_2da/"},
                        {"Name":"tiles","Path":"../SWLOR_Haks/tiles/"}]}
            """);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void UnclonedSubmoduleExplainsHowToRestoreContent()
    {
        var action = () => HakSetupValidation.Validate(_root);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*sw_tlk/sw_tlk.tlk.json*sw_2da*tiles*" + _root +
                         "*git submodule update --init --recursive -- SWLOR_Haks*restart*");
    }

    [Test]
    public void StartupGateFindsRepositoryEvenWhenTheSubmoduleDirectoryIsAbsent()
    {
        var settings = SWLOR.Toolset.Settings.ToolsetSettings.Load(Path.Combine(_root, "settings.json"));
        settings.ModuleRoot = Path.Combine(_root, "Module");
        var gate = typeof(App).GetMethod("ValidateStartupHakSetup",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var action = () => gate.Invoke(null, new object[] { settings });
        action.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<InvalidOperationException>().WithMessage("*" + _root + "*git submodule update*");
    }

    [Test]
    public void CompleteSourceContentPassesWithoutInstalledHaks()
    {
        WriteRequiredSources();
        Write("SWLOR_Haks/tiles/example.set", "[GENERAL]");
        var action = () => HakSetupValidation.Validate(_root);
        action.Should().NotThrow();
    }

    [Test]
    public void MusicSourceLayersAcceptBmuFiles()
    {
        WriteRequiredSources();
        Write("SWLOR_Haks/tiles/example.bmu", "music");
        var action = () => HakSetupValidation.Validate(_root);
        action.Should().NotThrow();
    }

    [Test]
    public void PlaceholderFilesAndEmptyResourcesDoNotCountAsSetup()
    {
        WriteRequiredSources();
        Write("SWLOR_Haks/tiles/README.md", "Not game data");
        Write("SWLOR_Haks/tiles/example.set", "");
        Write("SWLOR_Haks/tiles/source/example.set", "not visible to the resource index");
        var action = () => HakSetupValidation.Validate(_root);
        action.Should().Throw<InvalidOperationException>().WithMessage("*tiles (missing or empty)*");
    }

    [Test]
    public void InstalledStackDoesNotRequireUnusedLooseTileFolders()
    {
        WriteRequiredSources();
        Write("installed/tiles.hak", "archive");
        ResourceIndexTests.WriteSingleResourceHak(Path.Combine(_root, "installed/tiles.hak"),
            "example", "set", System.Text.Encoding.ASCII.GetBytes("[GENERAL]"));
        var action = () => HakSetupValidation.Validate(_root,
            new[] { new ResourceIndex.HakLayer("tiles", Path.Combine(_root, "installed/tiles.hak")) });
        action.Should().NotThrow();
    }

    [Test]
    public void CorruptInstalledArchiveIsRejectedWithSetupInstructions()
    {
        WriteRequiredSources();
        Write("installed/tiles.hak", "not a HAK archive");
        var action = () => HakSetupValidation.Validate(_root,
            new[] { new ResourceIndex.HakLayer("tiles", Path.Combine(_root, "installed/tiles.hak")) });
        action.Should().Throw<InvalidOperationException>().WithMessage("*tiles.hak*git submodule update*");
    }

    [Test]
    public void EmptyInstalledArchiveIsRejected()
    {
        WriteRequiredSources();
        Write("installed/tiles.hak", "");
        var action = () => HakSetupValidation.Validate(_root,
            new[] { new ResourceIndex.HakLayer("tiles", Path.Combine(_root, "installed/tiles.hak")) });
        action.Should().Throw<InvalidOperationException>().WithMessage("*tiles.hak*");
    }

    private void WriteRequiredSources()
    {
        Write("SWLOR_Haks/sw_tlk/sw_tlk.tlk.json", "{}");
        Write("SWLOR_Haks/sw_2da/appearance.2da", "2DA V2.0");
    }

    private void Write(string relativePath, string text)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
