using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Data
{
    /// <summary>
    /// The gear reader takes each stat's amount from the item property's stored cost value. That equals the property's
    /// cost-table value only while the three cost tables map every row to its own number.
    /// </summary>
    [TestFixture]
    public class CraftingGearCostTableTests
    {
        [TestCase("iprp_control.2da")]
        [TestCase("iprp_craftsman.2da")]
        [TestCase("iprp_enhancenum.2da")]
        public void TheCostTableMapsEveryRowToItsOwnNumber(string table)
        {
            var path = Path.Combine(RepositoryPaths.HakTwoDa(SharedData.Root), table);
            if (!File.Exists(path))
                Assert.Ignore($"{table} is not checked out: the SWLOR_Haks submodule needs sw_2da.");

            var rows = File.ReadAllLines(path).Skip(3).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            rows.Should().NotBeEmpty();
            foreach (var row in rows)
            {
                var columns = row.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                columns[0].Should().Be(columns[^1], $"row {columns[0]} of {table} gives the number {columns[^1]}");
            }
        }
    }
}
