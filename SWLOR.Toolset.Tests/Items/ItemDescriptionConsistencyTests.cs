using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// Item description cleanup must only make changes when the intended value is unambiguous.
    /// </summary>
    /// <remarks>
    /// A uti carries Description (unidentified) and DescIdentified. Most server surfaces request
    /// the identified string, but Description remains meaningful for an item whose Identified flag
    /// is zero: authors can deliberately withhold details until identification. Historical
    /// blueprints can also have two different authored values, and a bulk cleanup cannot safely
    /// decide which one belongs to the item.
    /// <para>
    /// Run <c>python tools/NormalizeItemDescriptions.py</c> to fix a failure here. It blanks a name
    /// only when both fields contain nothing else, and copies prose only when the other field is
    /// empty. An unidentified blueprint may intentionally keep Description blank until the item
    /// is identified; those withheld descriptions and conflicting non-empty values are deliberately
    /// preserved for human review.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class ItemDescriptionConsistencyTests
    {
        [Test]
        public void NoBlueprintHasAnUnambiguousDescriptionRepairPending()
        {
            var nameAsDescription = new List<string>();
            var oneSided = new List<string>();

            foreach (var path in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "uti"), "*.uti.json"))
            {
                UtiDocument document;
                try
                {
                    document = UtiDocument.Load(path);
                }
                catch (Exception)
                {
                    continue;
                }

                var store = new ItemValueStore(document.Fields);
                var name = store.GetLocalizedText("LocalizedName").Trim();
                var unidentified = store.GetLocalizedText("Description").Trim();
                var identified = store.GetLocalizedText("DescIdentified").Trim();

                if (name.Length > 0 && name == unidentified && name == identified)
                    nameAsDescription.Add(Path.GetFileName(path));
                else if ((unidentified.Length == 0 ^ identified.Length == 0) &&
                         !(document.Fields.GetIntOrNull("Identified") == 0 &&
                           unidentified.Length == 0 &&
                           identified.Length > 0))
                    oneSided.Add(Path.GetFileName(path));
            }

            var because = "run tools/NormalizeItemDescriptions.py to reconcile them";
            nameAsDescription.Should().BeEmpty(
                "an item's description must not just repeat its name - {0}", because);
            oneSided.Should().BeEmpty(
                "a single unambiguous description should be copied to its empty companion - {0}", because);
        }

        [TestCase("TaserPistol")]
        [TestCase("drakebruiser_wp")]
        [TestCase("key_smuggler01")]
        [TestCase("novauniform")]
        [TestCase("novauniformsup")]
        [TestCase("tuskenmelee_wp")]
        public void UnidentifiedBlueprintsKeepTheirAuthoredDescriptionWithheld(string resRef)
        {
            var document = UtiDocument.Load(Path.Combine(
                CorpusLocator.ModuleDirectory, "uti", resRef + ".uti.json"));
            var store = new ItemValueStore(document.Fields);

            document.Fields.GetIntOrNull("Identified").Should().Be(0);
            store.GetLocalizedText("Description").Should().BeEmpty();
            store.GetLocalizedText("DescIdentified").Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public void AmbiguousHistoricalDescriptionsRemainAuthored()
        {
            var signal = UtiDocument.Load(Path.Combine(
                CorpusLocator.ModuleDirectory, "uti", "signal_disr.uti.json"));
            var signalStore = new ItemValueStore(signal.Fields);
            signalStore.GetLocalizedText("DescIdentified").Should().Be("Signal Disruptor");
            signalStore.GetLocalizedText("Description")
                .Should().Be("A layered aquatic scale used in Tideguard gear and coastal cooking recipes.");

            var eclipse = UtiDocument.Load(Path.Combine(
                CorpusLocator.ModuleDirectory, "uti", "bp_eclipseshade.uti.json"));
            var eclipseStore = new ItemValueStore(eclipse.Fields);
            eclipseStore.GetLocalizedText("DescIdentified").Should().Be("Blueprint: Eclipse Cuirass");
            eclipseStore.GetLocalizedText("Description")
                .Should().Be("A rare schematic for crafting Resonant Broth.");
        }
    }
}
