using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP3.3: InstanceFieldMap.CreateInstance against real corpus data. Uses
    /// Module\git\ar_scor_kacademy.git.json (a real placed "vnpcsofficer" creature instance) and
    /// Module\utc\vnpcsofficer.utc.json (its blueprint) - verified while designing the field map
    /// to differ only by the blueprint-only "Comment"/"PaletteID" fields and the instance-only
    /// position/orientation/__struct_id fields.
    /// </summary>
    public class InstanceEditingTests
    {
        private static string GitPath => Path.Combine(CorpusLocator.ModuleDirectory, "git", "ar_scor_kacademy.git.json");
        private static string BlueprintPath => Path.Combine(CorpusLocator.ModuleDirectory, "utc", "vnpcsofficer.utc.json");

        /// <summary>Fields a real corpus creature instance may carry that a freshly placed
        /// instance will not (e.g. "ItemList", carrying loose inventory dropped in the toolset
        /// after placement) - documented allowlist for the field-set comparison below.</summary>
        private static readonly HashSet<string> OptionalInstanceOnlyFields = new(StringComparer.Ordinal)
        {
            "ItemList"
        };

        [Test]
        public void CreateInstance_Creature_InsertThenUndo_RestoresOriginalBytesExactly()
        {
            var original = File.ReadAllBytes(GitPath);
            var gitDocument = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(GitPath, gitDocument);

            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var listField = gitDocument.Root.Get("Creature List");
            var startCount = listField.Elements!.Count;

            using (session.Begin("add creature instance"))
            {
                var instance = InstanceFieldMap.CreateInstance(
                    ResourceType.Utc, blueprint, 12.5, 3.0, 15.0);
                listField.InsertElement(listField.Elements!.Count, instance);
            }

            listField.Elements!.Count.Should().Be(startCount + 1);
            gitDocument.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();

            listField.Elements!.Count.Should().Be(startCount);
            gitDocument.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the whole add-instance transaction must restore the exact original bytes");
        }

        [Test]
        public void CreateInstance_Creature_SerializedBytes_RoundTripThroughReparse()
        {
            var gitDocument = JsonGffDocument.Parse(File.ReadAllBytes(GitPath));
            using var session = new DocumentSession(GitPath, gitDocument);

            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var listField = gitDocument.Root.Get("Creature List");

            using (session.Begin("add creature instance"))
            {
                var instance = InstanceFieldMap.CreateInstance(
                    ResourceType.Utc, blueprint, 12.5, 3.0, 15.0);
                listField.InsertElement(listField.Elements!.Count, instance);
            }

            var firstWrite = gitDocument.ToBytes();

            // Re-parsing the written bytes and writing them again must produce byte-identical
            // output: proof the new element's fields serialized in nwn_gff's sorted order (an
            // out-of-order field would still parse, but a naive re-serialization would then not
            // match nwn_gff's own sort convention on the second write).
            var reparsed = JsonGffDocument.Parse(firstWrite);
            var secondWrite = reparsed.ToBytes();

            secondWrite.AsSpan().SequenceEqual(firstWrite).Should().BeTrue(
                "the newly inserted instance must serialize in nwn_gff's sorted field order so re-parsing and re-writing is idempotent");
        }

        [Test]
        public void CreateInstance_Creature_FieldSet_MatchesRealCorpusInstance()
        {
            var gitDocument = JsonGffDocument.Parse(File.ReadAllBytes(GitPath));
            var git = new GitDocument(gitDocument);

            // The real placed "vnpcsofficer" instance already in this corpus file - our
            // ground truth for what fields a creature instance actually carries.
            var realInstance = git.Creatures.Single(c =>
                c.GetOrNull("TemplateResRef")?.GetString() == "vnpcsofficer");
            var realFieldNames = realInstance.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var newInstance = InstanceFieldMap.CreateInstance(ResourceType.Utc, blueprint, 0, 0, 0);
            var newFieldNames = newInstance.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

            // The new instance's fields must be a subset of the real instance's fields modulo the
            // documented optional allowlist (fields the real instance happens to carry that a
            // freshly-placed instance would not yet have, e.g. loose inventory).
            var missingFromNew = realFieldNames.Except(newFieldNames).Except(OptionalInstanceOnlyFields).ToList();
            missingFromNew.Should().BeEmpty("every non-optional field the real corpus instance carries should also appear on a freshly created instance");

            var extraOnNew = newFieldNames.Except(realFieldNames).ToList();
            extraOnNew.Should().BeEmpty("a freshly created instance should not invent fields the real corpus instance does not have");
        }

        [Test]
        public void CreateInstance_Creature_PositionFloats_FormatNimStyle()
        {
            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(BlueprintPath));
            var instance = InstanceFieldMap.CreateInstance(ResourceType.Utc, blueprint, 12.5, 3.0, 15.0);

            var xPosition = instance.Get("XPosition");
            xPosition.Type.Should().Be(GffFieldType.Float);
            xPosition.GetSingle().Should().Be(12.5f);
            Encoding.ASCII.GetString(xPosition.RawValue!).Should().Be("12.5");
        }

        [Test]
        public void GetInstanceTemplateField_UsesResRefForStores_TemplateResRefOtherwise()
        {
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utm).Should().Be("ResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utc).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utp).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utd).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utw).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Uts).Should().Be("TemplateResRef");
            InstanceFieldMap.GetInstanceTemplateField(ResourceType.Utt).Should().Be("TemplateResRef");
        }
    }
}
