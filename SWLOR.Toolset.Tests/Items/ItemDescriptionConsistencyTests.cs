using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// An item's two description fields must agree, and neither may be the item's own name.
    /// </summary>
    /// <remarks>
    /// A uti carries Description (unidentified) and DescIdentified, but only the second is live:
    /// NWScript's GetDescription defaults to the identified string and every examine surface on the
    /// server takes that default. The editor therefore shows one Description box bound to
    /// DescIdentified, which is only honest as long as the two fields say the same thing.
    /// <para>
    /// Run <c>python tools/NormalizeItemDescriptions.py</c> to fix a failure here. Blanking rather
    /// than echoing is deliberate: an item whose description is its own name has no description,
    /// and "Bane Cuirass: Bane Cuirass" reads worse in game than no description at all.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class ItemDescriptionConsistencyTests
    {
        [Test]
        public void NoBlueprintUsesItsOwnNameAsItsDescriptionOrDisagreesWithItself()
        {
            var nameAsDescription = new List<string>();
            var disagreeing = new List<string>();

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

                if (name.Length > 0 && (name == unidentified || name == identified))
                    nameAsDescription.Add(Path.GetFileName(path));
                else if (unidentified != identified)
                    disagreeing.Add(Path.GetFileName(path));
            }

            var because = "run tools/NormalizeItemDescriptions.py to reconcile them";
            nameAsDescription.Should().BeEmpty(
                "an item's description must not just repeat its name - {0}", because);
            disagreeing.Should().BeEmpty(
                "the identified and unidentified descriptions must match - {0}", because);
        }
    }
}
