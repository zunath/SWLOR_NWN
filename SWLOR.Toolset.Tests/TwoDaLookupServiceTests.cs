using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="TwoDaLookupService"/> - the shared label+strref lookup behind the
    /// editor's gender/phenotype/sound-set/base-item dropdowns. Runs against the real
    /// SWLOR_Haks/sw_2da corpus, since the point of these dropdowns is that they show readable text
    /// for real game data. Only the SWLOR custom TLK is loaded here (as in the app), so base-game
    /// strrefs fall back to each table's label column - which is exactly why the tables wired to
    /// dropdowns were chosen for having readable labels.
    /// </summary>
    public class TwoDaLookupServiceTests
    {
        private static string HaksDirectory
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (Directory.Exists(candidate))
                        return candidate;
                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate SWLOR_Haks from the test context.");
            }
        }

        private static TwoDaLookupService CreateService() => new(
            new TwoDaService(Path.Combine(HaksDirectory, "sw_2da")),
            TlkService.Load(Path.Combine(HaksDirectory, "sw_tlk", "sw_tlk.tlk.json")));

        [Test]
        public void EveryWiredTable_ProducesReadableOptions()
        {
            var service = CreateService();

            foreach (var table in new[]
                     {
                         TwoDaLookupTables.Gender, TwoDaLookupTables.Phenotype,
                         TwoDaLookupTables.SoundSet, TwoDaLookupTables.BaseItem,
                         TwoDaLookupTables.PlaceableModel, TwoDaLookupTables.ItemSpell,
                         TwoDaLookupTables.Race,
                         TwoDaLookupTables.CreatureSpeed
                     })
            {
                var rows = service.GetRows(table);

                rows.Should().NotBeEmpty($"{table.TableName}.2da must yield dropdown options");
                rows.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.DisplayName),
                    $"every {table.TableName} option needs display text - a blank one would render as an unusable empty row");
                rows.Select(r => r.Id).Should().OnlyHaveUniqueItems($"{table.TableName} ids are row indices");
            }
        }

        [Test]
        public void RaceOptionsExcludeEveryReservedAndStructurallyInvalidRow()
        {
            var rows = CreateService().GetRows(TwoDaLookupTables.Race);

            rows.Should().Contain(row => row.Label == "Human");
            rows.Should().Contain(row => row.Label == "Chiss");
            rows.Should().OnlyContain(row => TwoDaChoicePolicy.IsSelectableLabel(row.Label));
            rows.Select(row => row.Id).Should().NotContain(new[] { 21, 22, 26, 27, 28, 30, 54 });
        }

        [Test]
        public void RowIdsAreRowIndices_MatchingWhatIsStoredInGff()
        {
            var service = CreateService();

            // baseitems.2da row 0 is the shortsword; the GFF BaseItem field stores that row index.
            var baseItems = service.GetRows("baseitems", "label", "Name");
            baseItems.First(r => r.Id == 0).Label.Should().BeEquivalentTo("shortsword");

            // phenotype.2da row 0 is "Normal".
            service.GetRows("phenotype", "Label", "Name").First(r => r.Id == 0)
                .Label.Should().BeEquivalentTo("Normal");
        }

        [Test]
        public void GenderOptions_CoverTheEngineGenders()
        {
            var rows = CreateService().GetRows(
                TwoDaLookupTables.Gender.TableName,
                TwoDaLookupTables.Gender.LabelColumn,
                TwoDaLookupTables.Gender.StrRefColumn);

            // gender.2da is tiny and fixed; ids 0/1 must be the male/female rows the GFF stores.
            rows.Should().HaveCountGreaterThanOrEqualTo(2);
            rows.First(r => r.Id == 0).DisplayName.Should().ContainEquivalentOf("male");
            rows.First(r => r.Id == 1).DisplayName.Should().ContainEquivalentOf("female");
        }

        [Test]
        public void UnknownTableOrColumn_DegradesToEmptyRatherThanThrowing()
        {
            var service = CreateService();

            service.GetRows("no_such_table_xyz", "Label").Should().BeEmpty();
            service.GetRows("baseitems", "NoSuchColumnXyz").Should().BeEmpty(
                "a missing label column must degrade the field to a numeric box, not crash the editor");
        }

        [Test]
        public void RowsAreCachedPerTableAndColumns()
        {
            var service = CreateService();

            var first = service.GetRows("phenotype", "Label", "Name");
            var second = service.GetRows("phenotype", "Label", "Name");

            second.Should().BeSameAs(first, "repeated dropdown builds must not re-parse the table");
        }
    }
}
