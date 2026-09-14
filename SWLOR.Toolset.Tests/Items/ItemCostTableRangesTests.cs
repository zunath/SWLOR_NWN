using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// <see cref="ItemCostTableRanges"/>: resolving an itempropdef.2da CostTableResRef id to the real
    /// engine cap that cost table offers, against both a fabricated registry and the real
    /// SWLOR_Haks/sw_2da corpus.
    /// </summary>
    [TestFixture]
    public class ItemCostTableRangesTests
    {
        private static string Sw2DaDirectory =>
            Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");

        [Test]
        public void DefensesRealCostTableResolvesToItsHighestRow()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));

            // Defense (property 94) uses CostTableId 35 -> iprp_costtable.2da row 35's Name column
            // is "IPRP_DEFENSE" -> iprp_defense.2da, whose highest labeled row is 1000 (verified
            // against the corpus - rows 0-1000, a plain contiguous ladder).
            ranges.MaxFor(35).Should().Be(1000);
        }

        [Test]
        public void AnUnresolvableCostTableIdReturnsNull()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));

            ranges.MaxFor(-1).Should().BeNull();
            ranges.MaxFor(99999).Should().BeNull();
        }

        [Test]
        public void ResistanceOptionsShowActualAmountsWhileKeepingEncodedRows()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));

            var options = ranges.OptionsFor(54);

            options.Should().HaveCount(201);
            options.Single(option => option.Value == 0).Label.Should().Be("0");
            options.Single(option => option.Value == 15).Label.Should().Be("15");
            options.Single(option => option.Value == 100).Label.Should().Be("100");
            options.Single(option => option.Value == 101).Label.Should().Be("-1");
            options.Single(option => option.Value == 200).Label.Should().Be("-100");
            options.Should().NotContain(option => option.Label.StartsWith("Resistance_"));
        }

        [Test]
        public void FabricatedRegistryResolvesThroughToItsTargetTablesRowCount()
        {
            var scratch = Path.Combine(Path.GetTempPath(), "swlor-cost-table-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "iprp_costtable.2da"),
                    "2DA V2.0\r\n\r\nName\r\n0 FIXTURE_TABLE\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "fixture_table.2da"),
                    "2DA V2.0\r\n\r\nLabel\r\n0 a\r\n1 b\r\n2 c\r\n");

                var ranges = new ItemCostTableRanges(new TwoDaService(scratch));

                ranges.MaxFor(0).Should().Be(2, "fixture_table.2da has rows 0-2");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void DefaultMaxMatchesTheWordSizedCostValueField()
        {
            ItemCostTableRanges.DefaultMax.Should().Be(ushort.MaxValue);
        }

        [Test]
        public void TrailingBlankRowsDoNotIncreaseTheReportedMaximum()
        {
            var scratch = Path.Combine(Path.GetTempPath(), "swlor-cost-table-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "iprp_costtable.2da"),
                    "2DA V2.0\r\n\r\nName\r\n0 FIXTURE_TABLE\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "fixture_table.2da"),
                    "2DA V2.0\r\n\r\nLabel\r\n0 a\r\n1 b\r\n2 c\r\n3 ****\r\n4 *****\r\n");

                var ranges = new ItemCostTableRanges(new TwoDaService(scratch));

                ranges.MaxFor(0).Should().Be(2, "empty padding rows are not selectable CostValues");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void OptionsExcludeSharedPlaceholderLabels()
        {
            var scratch = Path.Combine(Path.GetTempPath(), "swlor-cost-table-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "iprp_costtable.2da"),
                    "2DA V2.0\r\n\r\nName\r\n0 FIXTURE_TABLE\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "fixture_table.2da"),
                    "2DA V2.0\r\n\r\nLabel Amount\r\n" +
                    "0 Real_Value 10\r\n" +
                    "1 DELETED 20\r\n" +
                    "2 USER 30\r\n" +
                    "3 Bio_reserved 40\r\n" +
                    "4 ***** 50\r\n");

                var ranges = new ItemCostTableRanges(new TwoDaService(scratch));

                ranges.OptionsFor(0).Should().ContainSingle()
                    .Which.Should().Be(new ItemCostTableOption(0, "10"));
                ranges.MaxFor(0).Should().Be(0);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void CostTableWithoutDeclaredLabelColumnFailsClosed()
        {
            var scratch = Path.Combine(Path.GetTempPath(), "swlor-cost-table-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "iprp_costtable.2da"),
                    "2DA V2.0\r\n\r\nName\r\n0 FIXTURE_TABLE\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "fixture_table.2da"),
                    "2DA V2.0\r\n\r\nName\r\n0 Looks_Real\r\n");

                var ranges = new ItemCostTableRanges(new TwoDaService(scratch));

                ranges.OptionsFor(0).Should().BeEmpty();
                ranges.MaxFor(0).Should().BeNull();
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }
    }
}
