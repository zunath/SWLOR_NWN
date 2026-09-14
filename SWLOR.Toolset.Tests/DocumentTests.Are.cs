using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Verifies AreDocument against a real corpus file (Module/are/bank.are.json).</summary>
    public class AreDocumentTests
    {
        private static string BankArePath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "are", "bank.are.json");

        [Test]
        public void BankAre_KnownValues_ReadCorrectly()
        {
            var document = AreDocument.Load(BankArePath);

            document.Tileset.Should().Be("tfb01");
            document.Width.Should().Be(4);
            document.Height.Should().Be(4);
            document.Tag.Should().Be("bank");
            document.Name.Text.Should().Be("Building Template - Bank Style 1");
            document.Flags.Should().Be(1);
            document.FogClipDist.Should().Be(45.0f);
            document.SunAmbientColor.Should().Be(6566450u);
            document.SunDiffuseColor.Should().Be(16777215u);
            document.SunFogAmount.Should().Be(0);
            document.MoonDiffuseColor.Should().Be(13132900u);
            document.IsNight.Should().Be(false);
            document.DayNightCycle.Should().Be(1);
            document.ChanceRain.Should().Be(0);
            document.WindPower.Should().Be(0);
        }

        [Test]
        public void BankAre_Tiles_MatchesRawList()
        {
            var document = AreDocument.Load(BankArePath);

            document.Tiles.Should().NotBeEmpty();
            document.Tiles.Count.Should().Be(document.Fields.Get("Tile_List").Elements!.Count);
            document.Tiles[0].Get("Tile_ID").GetInteger().Should().Be(320);
        }

        [Test]
        public void SettingTag_ThenSerializing_RoundTripsTheNewValue()
        {
            var original = File.ReadAllBytes(BankArePath);
            var document = AreDocument.Parse(original);

            document.Tag = "bank_renamed";
            var written = document.ToBytes();

            var reparsed = AreDocument.Parse(written);
            reparsed.Tag.Should().Be("bank_renamed");
            // Everything else must be unaffected by the edit.
            reparsed.Tileset.Should().Be("tfb01");
            reparsed.Width.Should().Be(4);
        }
    }
}
