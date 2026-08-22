using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Verifies FacDocument against Module/fac/repute.fac.json.</summary>
    public class FacDocumentTests
    {
        private static string ReputeFacPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "fac", "repute.fac.json");

        [Test]
        public void ReputeFac_KnownValues_ReadCorrectly()
        {
            var document = FacDocument.Load(ReputeFacPath);

            document.FactionList.Should().NotBeEmpty();
            document.FactionList[0].Get("FactionName").GetString().Should().Be("PC");
            document.FactionList[0].Get("FactionGlobal").GetInteger().Should().Be(1);
            document.FactionList[1].Get("FactionName").GetString().Should().Be("Hostile");

            document.RepList.Should().NotBeEmpty();
            document.RepList[0].Get("FactionID1").GetInteger().Should().Be(0);
            document.RepList[0].Get("FactionID2").GetInteger().Should().Be(1);
            document.RepList[1].Get("FactionRep").GetInteger().Should().Be(50);
        }
    }
}
