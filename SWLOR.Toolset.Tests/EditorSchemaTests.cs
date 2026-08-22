using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    public class EditorSchemaTests
    {
        private static string UtcPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "utc", "zomb_guard.utc.json");

        [Test]
        public void UtcSchema_EveryFieldNameExistsInCorpusFile()
        {
            var document = JsonGffDocument.Load(UtcPath);
            var schema = UtcSchema.Build();

            var missing = schema.AllFields
                .Where(field => !document.Root.Contains(field.FieldName))
                .Select(field => field.FieldName)
                .ToList();

            missing.Should().BeEmpty("the reference UTC schema should describe real corpus fields");
        }

        [Test]
        public void UtcSchema_FieldTypesMatchCorpus()
        {
            var document = JsonGffDocument.Load(UtcPath);
            var schema = UtcSchema.Build();

            foreach (var descriptor in schema.AllFields)
            {
                var field = document.Root.GetOrNull(descriptor.FieldName);
                if (field != null)
                    field.Type.Should().Be(descriptor.FieldType,
                        $"descriptor '{descriptor.FieldName}' must declare the corpus GFF type");
            }
        }

        [Test]
        public void Accessor_ReadsKnownValues()
        {
            var document = JsonGffDocument.Load(UtcPath);
            var schema = UtcSchema.Build();
            var byName = schema.AllFields.ToDictionary(field => field.FieldName);

            SchemaFieldAccessor.GetText(document, byName["Tag"]).Should().Be("zomb_guard");
            SchemaFieldAccessor.GetInteger(document, byName["Dex"]).Should().Be(22);
            SchemaFieldAccessor.GetText(document, byName["FirstName"]).Should().Be("Zombie, Guard");
        }

        [Test]
        public void Accessor_ExplicitEmptyLocalizedOverride_WinsOverTlkText()
        {
            var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                """
                {
                  "__data_type": "UTP ",
                  "LocName": {
                    "id": 123,
                    "type": "cexolocstring",
                    "value": {
                      "0": ""
                    }
                  }
                }
                """));
            var descriptor = new FieldDescriptor
            {
                Label = "Name",
                FieldName = "LocName",
                Kind = EditorKind.LocString,
                FieldType = GffFieldType.CExoLocString
            };

            SchemaFieldAccessor.GetText(document, descriptor, _ => "TLK fallback")
                .Should().BeEmpty("an explicit empty language override is still the authored value");
        }

        [Test]
        public void Accessor_EditThroughTransaction_UndoRestoresExactBytes()
        {
            var original = File.ReadAllBytes(UtcPath);
            var document = JsonGffDocument.Parse(original);
            var descriptor = UtcSchema.Build().AllFields.First(field => field.FieldName == "Dex");

            using var session = new DocumentSession(UtcPath, document);
            using (session.Begin("Change DEX"))
            {
                SchemaFieldAccessor.SetInteger(document, descriptor, 14);
            }

            SchemaFieldAccessor.GetInteger(document, descriptor).Should().Be(14);
            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            document.ToBytes().Should().Equal(original);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void Accessor_CreatesMissingFieldAtSortedPosition()
        {
            var document = JsonGffDocument.Load(UtcPath);
            document.Root.Remove("Plot").Should().BeTrue();
            var descriptor = UtcSchema.Build().AllFields.First(field => field.FieldName == "Plot");

            SchemaFieldAccessor.SetBool(document, descriptor, true);

            var written = Encoding.UTF8.GetString(document.ToBytes());
            var plotIndex = written.IndexOf("\"Plot\"", StringComparison.Ordinal);
            var perceptionIndex = written.IndexOf("\"PerceptionRange\"", StringComparison.Ordinal);
            var portraitIndex = written.IndexOf("\"PortraitId\"", StringComparison.Ordinal);

            plotIndex.Should().BeGreaterThan(perceptionIndex, "case-insensitive sort places Plot after PerceptionRange");
            plotIndex.Should().BeLessThan(portraitIndex, "case-insensitive sort places Plot before PortraitId");
            document.Root.Get("Plot").GetInteger().Should().Be(1);
        }
    }
}
