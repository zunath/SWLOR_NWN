using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="TwoDaService"/> and <see cref="TwoDaTable"/>: known rows/columns
    /// from appearance.2da and portraits.2da come back with the exact values seen in the raw
    /// corpus files, **** cells map to null, table discovery includes the well-known tables, and
    /// every .2da file under SWLOR_Haks/sw_2da either parses or is reported (never throws
    /// unhandled) via TryGetTable.
    /// </summary>
    public class TwoDaTests
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

                throw new DirectoryNotFoundException(
                    "Could not locate the repository SWLOR_Haks directory from the test context.");
            }
        }

        private static string Sw2DaDirectory => Path.Combine(HaksDirectory, "sw_2da");

        private static TwoDaService CreateService() => new(Sw2DaDirectory);

        [Test]
        public void GetTable_Appearance_KnownRow_HasExpectedLabelAndNumericColumn()
        {
            var service = CreateService();

            var appearance = service.GetTable("appearance");

            // Row 6 in SWLOR_Haks/sw_2da/appearance.2da:
            // 6  "(Dynamic) Human"  ****  Character_model  H  ...  SIZECATEGORY=3  ...
            appearance.GetString(6, "LABEL").Should().Be("(Dynamic) Human");
            appearance.GetInt(6, "SIZECATEGORY").Should().Be(3);
        }

        [Test]
        public void GetTable_Appearance_EmptyCell_MapsToNull()
        {
            var service = CreateService();

            var appearance = service.GetTable("appearance");

            // STRING_REF is **** for row 6 in the corpus file.
            appearance.GetString(6, "STRING_REF").Should().BeNull();
            appearance.GetInt(6, "STRING_REF").Should().BeNull();
        }

        [Test]
        public void GetTable_Portraits_KnownRow_HasExpectedBaseResRefAndSexColumn()
        {
            var service = CreateService();

            var portraits = service.GetTable("portraits");

            // Row 1 in SWLOR_Haks/sw_2da/portraits.2da: dw_f_01_  1  0  ****  1  ****
            portraits.GetString(1, "BaseResRef").Should().Be("dw_f_01_");
            portraits.GetInt(1, "Sex").Should().Be(1);
            portraits.GetString(1, "InanimateType").Should().BeNull();
        }

        [Test]
        public void TryGetTable_UnknownName_ReturnsFalse()
        {
            var service = CreateService();

            var found = service.TryGetTable("not_a_real_table_xyz", out var table);

            found.Should().BeFalse();
            table.Should().BeNull();
        }

        [Test]
        public void GetTableNames_IncludesKnownTables()
        {
            var service = CreateService();

            var names = service.GetTableNames();

            names.Should().Contain(n => n.Equals("appearance", StringComparison.OrdinalIgnoreCase));
            names.Should().Contain(n => n.Equals("portraits", StringComparison.OrdinalIgnoreCase));
            names.Should().Contain(n => n.Equals("feat", StringComparison.OrdinalIgnoreCase));
            names.Should().Contain(n => n.Equals("placeables", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void EveryTwoDaFile_LoadsOrIsTolerantlyReported()
        {
            var service = CreateService();
            var names = service.GetTableNames();
            names.Count.Should().BeGreaterThan(500, "the sw_2da corpus should be present");

            var succeeded = new ConcurrentBag<string>();
            var failed = new ConcurrentBag<string>();

            Parallel.ForEach(names, name =>
            {
                // TryGetTable must never throw, regardless of whether the underlying file is a
                // well-formed 2DA.
                var loaded = service.TryGetTable(name, out _);
                (loaded ? succeeded : failed).Add(name);
            });

            TestContext.Out.WriteLine($"2DA corpus: {succeeded.Count} loaded, {failed.Count} failed out of {names.Count} total.");
            if (!failed.IsEmpty)
                TestContext.Out.WriteLine("Failed: " + string.Join(", ", failed));

            // Every name that TryGetTable rejects must be reported (not thrown), and taken together
            // every table name must land in exactly one bucket.
            (succeeded.Count + failed.Count).Should().Be(names.Count);
            succeeded.Count.Should().BeGreaterThan(500, "almost all corpus files should be well-formed 2DAs");
        }
    }
}
