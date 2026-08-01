using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Stamps the WP3.2 blueprint/area schemas (everything beyond the UTC reference schema)
    /// against real corpus files: every declared field must exist with the declared GFF type,
    /// and a representative accessor read/edit round-trips cleanly through the undo stack.
    /// </summary>
    public class EditorSchemaStampTests
    {
        private static string CorpusPath(string folder, string fileName) =>
            Path.Combine(CorpusLocator.ModuleDirectory, folder, fileName);

        /// <summary>One case per WP3.2 schema: the schema builder, a representative corpus file,
        /// and an allowlist of fields that may legitimately be absent from that one sample file
        /// (kept empty here because every declared field is present in 100% of the corpus for
        /// its type; the allowlist exists for future schemas where that isn't true).</summary>
        private static IEnumerable<TestCaseData> SchemaCases()
        {
            yield return new TestCaseData((Func<EditorSchema>)UtiSchema.Build, CorpusPath("uti", "001.uti.json"), Array.Empty<string>())
                .SetName("Uti");
            yield return new TestCaseData((Func<EditorSchema>)UtpSchema.Build, CorpusPath("utp", "_mdrn_chair.utp.json"), Array.Empty<string>())
                .SetName("Utp");
            yield return new TestCaseData((Func<EditorSchema>)UtdSchema.Build, CorpusPath("utd", "_mdrn_dt_alien1.utd.json"), Array.Empty<string>())
                .SetName("Utd");
            yield return new TestCaseData((Func<EditorSchema>)UtwSchema.Build, CorpusPath("utw", "beetle_spwn001.utw.json"), Array.Empty<string>())
                .SetName("Utw");
            yield return new TestCaseData((Func<EditorSchema>)UtsSchema.Build, CorpusPath("uts", "night_bazzarnois.uts.json"), Array.Empty<string>())
                .SetName("Uts");
            yield return new TestCaseData((Func<EditorSchema>)UttSchema.Build, CorpusPath("utt", "anti_spawn_trigg.utt.json"), Array.Empty<string>())
                .SetName("Utt");
            yield return new TestCaseData((Func<EditorSchema>)UtmSchema.Build, CorpusPath("utm", "bartender.utm.json"), Array.Empty<string>())
                .SetName("Utm");
            yield return new TestCaseData((Func<EditorSchema>)AreSchema.Build, CorpusPath("are", "bank.are.json"), Array.Empty<string>())
                .SetName("Area");
        }

        [TestCaseSource(nameof(SchemaCases))]
        public void Schema_AtLeast90PercentOfFieldsExistInCorpusFile(
            Func<EditorSchema> build, string corpusPath, string[] allowedMissing)
        {
            var document = JsonGffDocument.Load(corpusPath);
            var schema = build();
            var fields = schema.AllFields.ToList();

            var missing = fields
                .Where(field => !document.Root.Contains(field.FieldName))
                .Select(field => field.FieldName)
                .ToList();

            var unexpectedlyMissing = missing.Except(allowedMissing).ToList();
            unexpectedlyMissing.Should().BeEmpty(
                "fields outside the allowlist must be real corpus fields");

            var presentCount = fields.Count - missing.Count;
            var presentRatio = (double)presentCount / fields.Count;
            presentRatio.Should().BeGreaterThan(0.9,
                "the schema should describe fields that actually exist in the corpus");
        }

        [TestCaseSource(nameof(SchemaCases))]
        public void Schema_FieldTypesMatchCorpus(
            Func<EditorSchema> build, string corpusPath, string[] allowedMissing)
        {
            var document = JsonGffDocument.Load(corpusPath);
            var schema = build();

            foreach (var descriptor in schema.AllFields)
            {
                var field = document.Root.GetOrNull(descriptor.FieldName);
                if (field != null)
                    field.Type.Should().Be(descriptor.FieldType,
                        $"descriptor '{descriptor.FieldName}' must declare the corpus GFF type");
            }
        }

        [Test]
        public void UtiSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("uti", "001.uti.json");
            var document = JsonGffDocument.Load(path);
            var byName = UtiSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("tat_militia_helmet");
            SchemaFieldAccessor.GetInteger(document, byName["BaseItem"]).Should().Be(17);
            SchemaFieldAccessor.GetText(document, byName["LocalizedName"]).Should().Be("Tatooine Militia Field Helmet");
        }

        [Test]
        public void UtiSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("uti", "001.uti.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UtiSchema.Build().AllFields.First(field => field.FieldName == "Charges");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change Charges"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 5);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(5);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UtpSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("utp", "_mdrn_chair.utp.json");
            var document = JsonGffDocument.Load(path);
            var byName = UtpSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("Chair");
            SchemaFieldAccessor.GetText(document, byName["LocName"]).Should().Be("Chair, Wieldable");
            SchemaFieldAccessor.GetInteger(document, byName["HP"]).Should().Be(15);
        }

        [Test]
        public void UtpSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("utp", "_mdrn_chair.utp.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            // HP rather than Hardness: hardness measures out as unauthored (8,199 of 8,355
            // blueprints carry the default 5), so the placeable schema no longer offers it.
            var descriptor = UtpSchema.Build().AllFields.First(field => field.FieldName == "HP");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change Hit Points"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 10);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(10);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UtdSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("utd", "_mdrn_dt_alien1.utd.json");
            var document = JsonGffDocument.Load(path);
            var byName = UtdSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("_mdrn_dt_alien1");
            SchemaFieldAccessor.GetText(document, byName["LocName"]).Should().Be("Alien Door 1");
            SchemaFieldAccessor.GetInteger(document, byName["Faction"]).Should().Be(1);
        }

        [Test]
        public void UtdSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("utd", "_mdrn_dt_alien1.utd.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UtdSchema.Build().AllFields.First(field => field.FieldName == "HP");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change HP"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 20);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(20);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UtwSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("utw", "beetle_spwn001.utw.json");
            var document = JsonGffDocument.Load(path);
            var byName = UtwSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("TATOOINE_WRAID");
            SchemaFieldAccessor.GetText(document, byName["LocalizedName"]).Should().Be("Tatooine - Sand Beetle");
            SchemaFieldAccessor.GetInteger(document, byName["HasMapNote"]).Should().Be(0);
        }

        [Test]
        public void UtwSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("utw", "beetle_spwn001.utw.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UtwSchema.Build().AllFields.First(field => field.FieldName == "MapNoteEnabled");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change MapNoteEnabled"))
            {
                SchemaFieldAccessor.SetBool(document, descriptor, true);
            }

            SchemaFieldAccessor.GetBool(document, descriptor).Should().BeTrue();
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UtsSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("uts", "night_bazzarnois.uts.json");
            var document = JsonGffDocument.Load(path);
            var byName = UtsSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("BazzarNoises");
            SchemaFieldAccessor.GetText(document, byName["LocName"]).Should().Be("BazzarNoises");
            SchemaFieldAccessor.GetInteger(document, byName["Volume"]).Should().Be(49);
        }

        [Test]
        public void UtsSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("uts", "night_bazzarnois.uts.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UtsSchema.Build().AllFields.First(field => field.FieldName == "Priority");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change Priority"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 1);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(1);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UttSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("utt", "anti_spawn_trigg.utt.json");
            var document = JsonGffDocument.Load(path);
            var byName = UttSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("anti_spawn_trigg");
            SchemaFieldAccessor.GetText(document, byName["LocalizedName"]).Should().Be("No Spawn Zone");
            SchemaFieldAccessor.GetInteger(document, byName["Faction"]).Should().Be(1);
        }

        [Test]
        public void UttSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("utt", "anti_spawn_trigg.utt.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UttSchema.Build().AllFields.First(field => field.FieldName == "Cursor");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change Cursor"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 1);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(1);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UtmSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("utm", "bartender.utm.json");
            var document = JsonGffDocument.Load(path);
            var byName = UtmSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("bartender");
            SchemaFieldAccessor.GetText(document, byName["LocName"]).Should().Be("Bartender");
            SchemaFieldAccessor.GetInteger(document, byName["MarkUp"]).Should().Be(100);
        }

        [Test]
        public void UtmSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("utm", "bartender.utm.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UtmSchema.Build().AllFields.First(field => field.FieldName == "MarkDown");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change MarkDown"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 50);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(50);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void AreSchema_Accessor_ReadsKnownValues()
        {
            var path = CorpusPath("are", "bank.are.json");
            var document = JsonGffDocument.Load(path);
            var byName = AreSchema.Build().AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("bank");
            SchemaFieldAccessor.GetText(document, byName["Name"]).Should().Be("Building Template - Bank Style 1");
            SchemaFieldAccessor.GetInteger(document, byName["Width"]).Should().Be(4);
        }

        [Test]
        public void AreSchema_Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var path = CorpusPath("are", "bank.are.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var descriptor = AreSchema.Build().AllFields.First(field => field.FieldName == "WindPower");

            using var session = new DocumentSession(path, document);
            using (session.Begin("Change WindPower"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 5);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(5);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }
    }
}
