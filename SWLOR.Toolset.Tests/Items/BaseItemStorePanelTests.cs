using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests.Items
{
    [TestFixture]
    public class BaseItemStorePanelTests
    {
        [Test]
        public void ServiceReadsEveryNativeCategoryAndDefaultsInvalidValuesToMiscellaneous()
        {
            var scratch = Path.Combine(
                Path.GetTempPath(),
                "swlor-baseitem-store-panel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "baseitems.2da"),
                    "2DA V2.0\r\n\r\nlabel ModelType StorePanel EquipableSlots ItemClass\r\n" +
                    "0 armor 3 0 0x2 armor\r\n" +
                    "1 longsword 2 1 0x1C030 sword\r\n" +
                    "2 potions 0 2 **** potion\r\n" +
                    "3 amulet 0 3 512 amulet\r\n" +
                    "4 miscmedium 0 4 invalid misc\r\n" +
                    "5 missing_panel 0 **** 0 missing\r\n" +
                    "6 bad_panel 0 99 0 invalid\r\n" +
                    "7 malformed_panel 0 nonsense 0 malformed\r\n");

                var rows = new BaseItemRowService(new TwoDaService(scratch));

                rows.All.OrderBy(row => row.Id).Select(row => row.StorePanel).Should().Equal(
                    0, 1, 2, 3, 4, 4, 4, 4);
                rows.All.OrderBy(row => row.Id).Select(row => row.EquipableSlots).Should().Equal(
                    2, 0x1C030, 0, 512, 0, 0, 0, 0);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void RealBaseItemsTableUsesOnlyTheFiveNativeMerchantCategories()
        {
            var directory = Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR_Haks",
                "sw_2da");
            var path = Path.Combine(directory, "baseitems.2da");
            if (!File.Exists(path))
                Assert.Ignore("SWLOR_Haks/sw_2da is not initialized in this worktree.");

            var rows = new BaseItemRowService(new TwoDaService(directory));

            rows.All.Should().HaveCountGreaterThan(150);
            rows.All.Should().OnlyContain(row => row.StorePanel >= 0 && row.StorePanel <= 4);
            rows.GetOrNull(19)!.Label.Should().Be("amulet");
            rows.GetOrNull(19)!.StorePanel.Should().Be(3);
            rows.GetOrNull(52)!.Label.Should().Be("ring");
            rows.GetOrNull(52)!.StorePanel.Should().Be(3);
        }
    }
}
