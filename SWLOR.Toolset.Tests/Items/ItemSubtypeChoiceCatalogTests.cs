using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// <see cref="ItemSubtypeChoiceCatalog"/>'s reserved-row filtering: a row whose display would
    /// fall back to a placeholder Label is skipped. The source Label is authoritative for whether
    /// a row is real content, even when a reserved slot happens to carry a resolvable TLK Name.
    /// </summary>
    [TestFixture]
    public class ItemSubtypeChoiceCatalogTests
    {
        private string _scratchDirectory = string.Empty;

        [SetUp]
        public void CreateScratchTable()
        {
            _scratchDirectory = Path.Combine(Path.GetTempPath(), "swlor-subtype-choice-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_scratchDirectory);
            File.WriteAllText(
                Path.Combine(_scratchDirectory, "racialtypes.2da"),
                "2DA V2.0\r\n\r\n     Label            Name    Constant\r\n" +
                "0    Human            100     RACIAL_TYPE_HUMAN\r\n" +
                "1    Bio_reserved     ****    ****\r\n" +
                "2    cep_reserved     ****    ****\r\n" +
                "3    DELETED          ****    ****\r\n" +
                "4    Padding          ****    ****\r\n" +
                "5    Wookiee          ****    RACIAL_TYPE_WOOKIEE\r\n" +
                "6    Zabrak           200     RACIAL_TYPE_ZABRAK\r\n" +
                "7    Bio_reserved     300     RACIAL_TYPE_RESERVED\r\n" +
                "8    USER             ****    ****\r\n" +
                "9    Unused           ****    ****\r\n" +
                "10   INVALID_RACE     ****    ****\r\n" +
                "11   NULL6            ****    ****\r\n" +
                "12   Looks_Real       400     ****\r\n");
        }

        [TearDown]
        public void DeleteScratchTable()
        {
            if (Directory.Exists(_scratchDirectory))
                Directory.Delete(_scratchDirectory, recursive: true);
        }

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ReadFixture()
        {
            var twoDa = new TwoDaService(_scratchDirectory);
            string? Tlk(int strref) => strref switch
            {
                100 => "Human",
                200 => string.Empty, // resolves to nothing - falls back to Label
                300 => "Real Bio-Reserved Name", // a real Name strref despite the placeholder label
                _ => null
            };

            return ItemSubtypeChoiceCatalog.Read(twoDa, "racialtypes", Tlk);
        }

        [Test]
        public void ReservedPlaceholderLabelsAreExcludedWhenTheyWouldBeTheDisplay()
        {
            var choices = ReadFixture();

            choices.Select(choice => choice.Display).Should().NotContain(display =>
                display.Contains("reserved", StringComparison.OrdinalIgnoreCase) &&
                display != "Real Bio-Reserved Name");
            choices.Should().NotContain(choice => choice.Display == "DELETED");
            choices.Should().NotContain(choice => choice.Display == "Padding");
            choices.Should().NotContain(choice => choice.Display == "USER");
            choices.Should().NotContain(choice => choice.Display == "Unused");
            choices.Should().NotContain(choice => choice.Display == "INVALID_RACE");
            choices.Should().NotContain(choice => choice.Display == "NULL6");
        }

        [Test]
        public void ARealLabelThatDoesNotResolveAgainstTlkStillOffersItsLabel()
        {
            var choices = ReadFixture();

            choices.Should().Contain(choice => choice.Display == "Human");
            choices.Should().Contain(choice => choice.Display == "Wookiee");
            choices.Should().Contain(choice => choice.Display == "Zabrak", "Zabrak falls back to its own real label, not a reserved one");
        }

        [Test]
        public void ARowWithARealTlkNameIsStillExcludedWhenItsSourceLabelIsReserved()
        {
            var choices = ReadFixture();

            choices.Should().NotContain(choice => choice.Display == "Real Bio-Reserved Name");
        }

        [Test]
        public void ARealLookingRaceWithoutItsRequiredConstantIsExcluded()
        {
            ReadFixture().Should().NotContain(choice => choice.Display == "Looks_Real");
        }

        [Test]
        public void AnUndeclaredDynamicSubtypeSourceFailsClosed()
        {
            File.WriteAllText(
                Path.Combine(_scratchDirectory, "unexpected_subtypes.2da"),
                "2DA V2.0\r\n\r\nLabel Name\r\n0 Looks_Real 100\r\n");

            ItemSubtypeChoiceCatalog.Read(
                    new TwoDaService(_scratchDirectory),
                    "unexpected_subtypes",
                    tlk: null)
                .Should().BeEmpty();
        }

        [Test]
        public void RealRacialTypesTableNeverOffersABioreservedLikeOption()
        {
            var sw2Da = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");
            var twoDa = new TwoDaService(sw2Da);

            // No base TLK is loaded here, matching the normal degraded case in this test
            // environment - every row falls back to its 2da Label, which is exactly what exercises
            // the reserved-shape filter against the real corpus.
            var choices = ItemSubtypeChoiceCatalog.Read(twoDa, "racialtypes", tlk: null);

            choices.Should().NotBeEmpty();
            choices.Should().NotContain(choice =>
                choice.Display.Replace("_", string.Empty).Contains("reserved", StringComparison.OrdinalIgnoreCase));
            choices.Should().NotContain(choice => choice.Display.Equals("DELETED", StringComparison.OrdinalIgnoreCase));
            choices.Should().Contain(choice => choice.Display == "Wookiee");
        }
    }
}
