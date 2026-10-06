using FluentAssertions;
using Nwn.Toolset.Avalonia.Areas;
using NUnit.Framework;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The viewport display switches are owned by the shared options model; SWLOR only has to keep
    /// them in the same settings keys it always has.
    /// </summary>
    [TestFixture]
    public class ToolsetSettingsViewportDisplayPersistenceTests
    {
        private string _directory = string.Empty;
        private string _file = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "swlor-display-" + Guid.NewGuid().ToString("N"));
            _file = Path.Combine(_directory, "toolset-settings.json");
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

        private static AreaViewportDisplayOptions OptionsFor(ToolsetSettings settings) =>
            new(new ToolsetSettingsViewportDisplayPersistence(settings));

        [Test]
        public void ADefaultSettingsFileGivesTheAreaEditingDefaults()
        {
            var options = OptionsFor(ToolsetSettings.Load(_file));

            options.ShowAreaLighting.Should().BeFalse();
            options.ShowFog.Should().BeFalse();
            options.ShowCeilings.Should().BeFalse();
            options.ShowMaterialMaps.Should().BeTrue();
        }

        [Test]
        public void EachSwitchIsWrittenToItsExistingSettingsKeyAndSurvivesARestart()
        {
            var settings = ToolsetSettings.Load(_file);
            var options = OptionsFor(settings);

            options.ShowAreaLighting = true;
            options.ShowFog = true;
            options.ShowCeilings = true;
            options.ShowMaterialMaps = false;

            settings.ShowAreaLighting.Should().BeTrue();
            settings.ShowFog.Should().BeTrue();
            settings.ShowCeilings.Should().BeTrue();
            settings.ShowMaterialMaps.Should().BeFalse();

            var json = File.ReadAllText(_file);
            json.Should().Contain("\"showAreaLighting\": true");
            json.Should().Contain("\"showFog\": true");
            json.Should().Contain("\"showCeilings\": true");
            json.Should().Contain("\"showMaterialMaps\": false");

            var reloaded = OptionsFor(ToolsetSettings.Load(_file));
            reloaded.ShowAreaLighting.Should().BeTrue();
            reloaded.ShowFog.Should().BeTrue();
            reloaded.ShowCeilings.Should().BeTrue();
            reloaded.ShowMaterialMaps.Should().BeFalse();
        }

        [Test]
        public void ConstructingTheOptionsDoesNotWriteTheSettingsFile()
        {
            OptionsFor(ToolsetSettings.Load(_file));

            File.Exists(_file).Should().BeFalse("loading the stored values is not a change to save");
        }
    }
}
