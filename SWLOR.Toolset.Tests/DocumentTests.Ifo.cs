using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Verifies IfoDocument against Module/ifo/module.ifo.json.</summary>
    public class IfoDocumentTests
    {
        private static string ModuleIfoPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");

        [Test]
        public void ModuleIfo_KnownValues_ReadCorrectly()
        {
            var document = IfoDocument.Load(ModuleIfoPath);

            document.Tag.Should().Be("SWLOR");
            document.EntryArea.Should().Be("ooc_area");
            document.Name.Text.Should().Be("Star Wars Legends of the Old Republic MODULE");
            // A floor rather than an exact count: since WP7.3 the toolset can create areas (which
            // register themselves here), so the module is a living corpus that legitimately grows.
            document.AreaList.Should().HaveCountGreaterThanOrEqualTo(438);
            document.AreaResRefs[0].Should().Be("anchor_entreenor");
            document.AreaResRefs[1].Should().Be("anchor_entreesud");
            document.HakList.Should().HaveCount(113);
            document.HakNames[0].Should().Be("sw_2da");
            document.HakNames[1].Should().Be("sw_ability");
        }

        [Test]
        public void ModuleProperties_ModificationsRoundTripWithoutChangingTheEntryPoint()
        {
            var original = File.ReadAllBytes(ModuleIfoPath);
            var document = IfoDocument.Parse(original);
            var entryArea = document.EntryArea;
            var entryX = document.EntryX;
            var entryY = document.EntryY;
            var entryZ = document.EntryZ;

            document.Tag = "TEST_MODULE";
            document.Description.Text = "Edited module description";
            document.MinutesPerHour = 8;
            document.DawnHour = 5;
            document.DuskHour = 19;
            document.StartingMonth = 7;
            document.StartingDay = 2;
            document.StartingHour = 14;
            document.StartingYear = 1400;
            document.XpScale = 125;
            document.StartingMovie = "intro_movie";
            document.SetScript("Mod_OnModStart", "mod_test_start");
            document.CustomTlk = "test_tlk";
            document.SetHakNames(new[] { "first_hak", "second_hak" });

            var reparsed = IfoDocument.Parse(document.ToBytes());

            reparsed.Tag.Should().Be("TEST_MODULE");
            reparsed.Description.Text.Should().Be("Edited module description");
            reparsed.MinutesPerHour.Should().Be(8);
            reparsed.DawnHour.Should().Be(5);
            reparsed.DuskHour.Should().Be(19);
            reparsed.StartingMonth.Should().Be(7);
            reparsed.StartingDay.Should().Be(2);
            reparsed.StartingHour.Should().Be(14);
            reparsed.StartingYear.Should().Be(1400);
            reparsed.XpScale.Should().Be(125);
            reparsed.StartingMovie.Should().Be("intro_movie");
            reparsed.GetScript("Mod_OnModStart").Should().Be("mod_test_start");
            reparsed.CustomTlk.Should().Be("test_tlk");
            reparsed.HakNames.Should().Equal("first_hak", "second_hak");
            reparsed.EntryArea.Should().Be(entryArea);
            reparsed.EntryX.Should().Be(entryX);
            reparsed.EntryY.Should().Be(entryY);
            reparsed.EntryZ.Should().Be(entryZ);
        }
    }
}
