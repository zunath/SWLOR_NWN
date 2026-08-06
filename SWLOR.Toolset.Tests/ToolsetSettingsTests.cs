using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// What the settings file has to keep between sessions, tested against a real file.
    /// </summary>
    /// <remarks>
    /// Every save in this store is best-effort and swallows its exception, so a write that throws is
    /// indistinguishable from a feature that was never built - which is exactly how the window placement
    /// and the panel layout came to look unimplemented. These tests read the file back rather than
    /// trusting the setters.
    /// </remarks>
    [TestFixture]
    public class ToolsetSettingsTests
    {
        private string _directory = string.Empty;
        private string _file = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "swlor-toolset-settings-" + Guid.NewGuid().ToString("N"));
            _file = Path.Combine(_directory, "settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp folder is not worth failing a test over.
            }
        }

        private ToolsetSettings Reload() => ToolsetSettings.Load(_file);

        [Test]
        public void TheSettingsFileIsCreatedOnFirstWrite()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.ShowFog = true;

            File.Exists(_file).Should().BeTrue("the containing folder has to be created along with it");
            settings.LastSaveError.Should().BeNull();
        }

        [Test]
        public void RecentErfArchivesSurviveARestartAndMoveBackToTheFront()
        {
            var settings = ToolsetSettings.Load(_file);
            var first = Path.Combine(_directory, "first.erf");
            var second = Path.Combine(_directory, "second.erf");

            settings.AddRecentErfArchive(first);
            settings.AddRecentErfArchive(second);
            settings.AddRecentErfArchive(first);

            Reload().RecentErfArchives.Should().Equal(first, second);
        }

        [Test]
        public void FailedSettingsReplacementPreservesTheLastValidFile()
        {
            var settings = ToolsetSettings.Load(_file);
            settings.ShowFog = true;
            var before = File.ReadAllBytes(_file);

            using (var hold = new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.None))
                settings.ShowCeilings = true;

            settings.LastSaveError.Should().NotBeNull();
            File.ReadAllBytes(_file).Should().Equal(before);
            Directory.EnumerateFiles(_directory, "*.tmp").Should().BeEmpty();
        }

        [Test]
        public void WindowSizeAndPositionSurviveARestart()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.Window = new WindowPlacement(1600, 900, 240, 120, IsMaximized: false);

            var reloaded = Reload();
            reloaded.Window.Should().Be(new WindowPlacement(1600, 900, 240, 120, false));
        }

        [Test]
        public void AMaximizedWindowRemembersTheSizeItHadBeforeBeingMaximized()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.Window = new WindowPlacement(1600, 900, 240, 120, IsMaximized: true);

            var reloaded = Reload();
            reloaded.Window.IsMaximized.Should().BeTrue();
            reloaded.Window.Width.Should().Be(1600, "un-maximising has to give back the window that was in use");
            reloaded.Window.Height.Should().Be(900);
        }

        [Test]
        public void AWindowWithNoRecordedPositionStillSavesEverythingElse()
        {
            var settings = ToolsetSettings.Load(_file);

            // NaN is the in-memory "no position" marker, and the JSON writer refuses to write one. If it
            // reaches the serializer it takes the whole file with it - silently, because Save() catches.
            settings.Window = new WindowPlacement(1600, 900, double.NaN, double.NaN, false);
            settings.ShowFog = true;

            settings.LastSaveError.Should().BeNull();

            var reloaded = Reload();
            reloaded.Window.HasPosition.Should().BeFalse();
            reloaded.Window.Width.Should().Be(1600);
            reloaded.ShowFog.Should().BeTrue();
        }

        [Test]
        public void ATinyWindowSizeIsNotWorthRestoring()
        {
            // A window reports a handful of pixels while it is being torn down.
            new WindowPlacement(40, 30, 0, 0, false).HasSize.Should().BeFalse();
            new WindowPlacement(1600, 900, 0, 0, false).HasSize.Should().BeTrue();
        }

        [Test]
        public void DockDividerPositionsSurviveARestart()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.SetDockProportions(new Dictionary<string, double>
            {
                ["ExplorerDock"] = 0.33,
                ["PaletteDock"] = 0.21,
                ["OutputDock"] = 0.14
            });

            var reloaded = Reload();
            reloaded.DockProportions.Should().HaveCount(3);
            reloaded.DockProportions["ExplorerDock"].Should().BeApproximately(0.33, 0.0001);
            reloaded.DockProportions["PaletteDock"].Should().BeApproximately(0.21, 0.0001);
            reloaded.DockProportions["OutputDock"].Should().BeApproximately(0.14, 0.0001);
        }

        [Test]
        public void OneUnusableDividerDoesNotCostTheOthers()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.SetDockProportions(new Dictionary<string, double>
            {
                ["ExplorerDock"] = 0.33,
                // Dock leaves this on anything it never sized, and it would take the file down with it.
                ["Documents"] = double.NaN,
                ["Nonsense"] = 4.5,
                [""] = 0.5
            });

            settings.LastSaveError.Should().BeNull();

            var reloaded = Reload();
            reloaded.DockProportions.Should().ContainKey("ExplorerDock");
            reloaded.DockProportions.Should().NotContainKey("Documents");
            reloaded.DockProportions.Should().NotContainKey("Nonsense");
            reloaded.DockProportions.Should().NotContainKey("");
        }

        [Test]
        public void ReplacingTheLayoutDropsDividersThatAreNoLongerThere()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.SetDockProportions(new Dictionary<string, double> { ["OldDock"] = 0.4 });
            settings.SetDockProportions(new Dictionary<string, double> { ["ExplorerDock"] = 0.3 });

            var reloaded = Reload();
            reloaded.DockProportions.Should().ContainKey("ExplorerDock");
            reloaded.DockProportions.Should().NotContainKey("OldDock");
        }

        [Test]
        public void ThePaletteDividerSurvivesARestart()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.PaletteCategoryProportion = 0.62;

            Reload().PaletteCategoryProportion.Should().BeApproximately(0.62, 0.0001);
        }

        [Test]
        public void APaletteDividerOutsideThePanelIsTreatedAsUnset()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.PaletteCategoryProportion = 1.4;

            settings.PaletteCategoryProportion.Should().Be(0, "0 is what the panel reads as 'use the design'");
            Reload().PaletteCategoryProportion.Should().Be(0);
        }

        [Test]
        public void ThePanelPreferencesSurviveARestart()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.PalettePreviewSize = 168;
            settings.PaletteSelection = "utc";
            settings.PaletteShowsStandard = true;
            settings.ModuleContentsTab = "utp";
            settings.ShowAreaLighting = true;
            settings.ShowFog = true;
            settings.NwnInstallOverride = @"C:\Games\NWN";
            settings.AddRecentModule(@"C:\Projects\SWLOR_NWN\Module");

            var reloaded = Reload();
            reloaded.PalettePreviewSize.Should().Be(168);
            reloaded.PaletteSelection.Should().Be("utc");
            reloaded.PaletteShowsStandard.Should().BeTrue();
            reloaded.ModuleContentsTab.Should().Be("utp");
            reloaded.ShowAreaLighting.Should().BeTrue();
            reloaded.ShowFog.Should().BeTrue();
            reloaded.NwnInstallOverride.Should().Be(@"C:\Games\NWN");
            reloaded.RecentModules.Should().ContainSingle().Which.Should().Be(@"C:\Projects\SWLOR_NWN\Module");
        }

        [Test]
        public void ASettingsFileFromBeforeTheLayoutWasSavedStillLoads()
        {
            // Written by a build that had no dockProportions or paletteCategoryProportion key.
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_file, """
                {
                  "moduleRoot": "C:\\Projects\\SWLOR_NWN\\Module",
                  "windowWidth": 1600,
                  "windowHeight": 900,
                  "showFog": true
                }
                """);

            var settings = Reload();

            settings.ModuleRoot.Should().Be(@"C:\Projects\SWLOR_NWN\Module");
            settings.Window.Width.Should().Be(1600);
            settings.ShowFog.Should().BeTrue();
            settings.DockProportions.Should().BeEmpty("an absent layout means the designed one, not a broken load");
            settings.PaletteCategoryProportion.Should().Be(0);
        }

        [Test]
        public void ACorruptSettingsFileDoesNotStopTheToolsetStarting()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_file, "{ this is not json");

            var settings = Reload();

            settings.PaletteSelection.Should().BeEmpty();
            settings.DockProportions.Should().BeEmpty();
        }

        [Test]
        public void EverySavedNumberIsSomethingTheJsonWriterAccepts()
        {
            var settings = ToolsetSettings.Load(_file);

            settings.Window = new WindowPlacement(double.NaN, double.PositiveInfinity, double.NaN, 10, false);
            settings.PalettePreviewSize = double.NaN;

            settings.LastSaveError.Should().BeNull("a single bad number must not cost the whole file");

            var act = () => JsonSerializer.Deserialize<JsonDocument>(File.ReadAllText(_file));
            act.Should().NotThrow();
        }
    }
}
