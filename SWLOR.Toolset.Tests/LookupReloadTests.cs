using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests
{
    public class LookupReloadTests
    {
        [Test]
        public async Task TwoDaLookupsRebuildWhenTheModuleHakStackChanges()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var firstDirectory = Path.Combine(tempRoot, "first");
            var secondDirectory = Path.Combine(tempRoot, "second");
            Directory.CreateDirectory(firstDirectory);
            Directory.CreateDirectory(secondDirectory);
            WritePortraitsTable(firstDirectory, "portrait_before");
            WritePortraitsTable(secondDirectory, "portrait_after");

            try
            {
                var index = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[] { new ResourceIndex.HakLayer("first", firstDirectory) });
                var twoDa = new TwoDaService(index);
                var portraits = new PortraitService(twoDa);

                portraits.Get(0).BaseResRef.Should().Be("portrait_before");

                await index.ReloadHakLayersAsync(
                    new[] { new ResourceIndex.HakLayer("second", secondDirectory) });

                portraits.Get(0).BaseResRef.Should().Be("portrait_after");
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        private static void WritePortraitsTable(string directory, string baseResRef)
        {
            File.WriteAllText(
                Path.Combine(directory, "portraits.2da"),
                $"2DA V2.0\r\n\r\nBaseResRef Sex Race InanimateType\r\n0 {baseResRef} 0 0 ****\r\n");
        }
    }
}
