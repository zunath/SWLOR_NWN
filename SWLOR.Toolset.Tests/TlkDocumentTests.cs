using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.GameData.Tlk;

namespace SWLOR.Toolset.Tests
{
    public class TlkDocumentTests
    {
        [Test]
        public void Parse_Edit_Clear_KeepSparseRowsAndMeaningfulWhitespace()
        {
            var document = TlkDocument.Parse(
                """
                {
                  "language": 0,
                  "entries": [
                    { "id": 8, "text": "eight" },
                    { "id": 2, "text": "two" }
                  ]
                }
                """);

            document.Entries.Select(entry => entry.Id).Should().Equal(2, 8);
            document.SetText(5, "  deliberately padded  ");
            document.GetText(5).Should().Be("  deliberately padded  ");
            document.SetText(8, string.Empty);

            document.ContainsEntry(8).Should().BeFalse("empty text clears rather than reserving a row");
            document.Clear(12345).Should().BeFalse();
            document.Entries.Select(entry => entry.Id).Should().Equal(2, 5);
        }

        [Test]
        public void Parse_RejectsNegativeAndDuplicateEntryIds()
        {
            Action negative = () => TlkDocument.Parse(
                """{ "language": 0, "entries": [ { "id": -1, "text": "bad" } ] }""");
            Action duplicate = () => TlkDocument.Parse(
                """{ "language": 0, "entries": [ { "id": 7, "text": "a" }, { "id": 7, "text": "b" } ] }""");

            negative.Should().Throw<InvalidDataException>().WithMessage("*between 0 and*");
            duplicate.Should().Throw<InvalidDataException>().WithMessage("*appears more than once*");
        }

        [TestCase("{}")]
        [TestCase("{ \"language\": 0 }")]
        [TestCase("{ \"entries\": [] }")]
        [TestCase("{ \"language\": -1, \"entries\": [] }")]
        [TestCase("{ \"language\": 0, \"entries\": null }")]
        [TestCase("{ \"language\": 0, \"entries\": [ { \"text\": \"missing id\" } ] }")]
        [TestCase("{ \"language\": 0, \"entries\": [ { \"id\": 1 } ] }")]
        [TestCase("{ \"language\": 0, \"entries\": [ { \"id\": 1, \"text\": null } ] }")]
        public void Parse_RejectsTruncatedOrSemanticallyInvalidDocuments(string json)
        {
            Action parse = () => TlkDocument.Parse(json);

            parse.Should().Throw<InvalidDataException>();
        }

        [Test]
        public void Parse_NormalizesExplicitEmptyTextToAnAvailableBlank()
        {
            var document = TlkDocument.Parse(
                """{ "language": 0, "entries": [ { "id": 4, "text": "" } ] }""");

            document.ContainsEntry(4).Should().BeFalse();
            document.FindFirstAvailableBlank(TlkReferenceIndex.Empty).Should().Be(0);
            document.ToJson().Should().NotContain("\"id\": 4");
        }

        [Test]
        public void EntryIds_EnforceTheSharedBinaryFormatBoundary()
        {
            var lastValidId = TlkFormatLimits.MaximumEntryId;
            var firstInvalidId = lastValidId + 1;
            var document = TlkDocument.Parse(
                $$"""{ "language": 0, "entries": [ { "id": {{lastValidId}}, "text": "last" } ] }""");

            document.GetText(lastValidId).Should().Be("last");
            document.SetText(lastValidId, "changed");
            document.FindNextAvailableBlank(lastValidId, TlkReferenceIndex.Empty).Should().Be(0,
                "navigation wraps inside the supported range instead of returning an unsaveable id");

            Action parsePastEnd = () => TlkDocument.Parse(
                $$"""{ "language": 0, "entries": [ { "id": {{firstInvalidId}}, "text": "bad" } ] }""");
            Action setPastEnd = () => document.SetText(firstInvalidId, "bad");
            Action clearPastEnd = () => document.Clear(firstInvalidId);
            Action navigatePastEnd = () => document.FindNextAvailableBlank(firstInvalidId, TlkReferenceIndex.Empty);

            parsePastEnd.Should().Throw<InvalidDataException>().WithMessage("*between 0 and*");
            setPastEnd.Should().Throw<ArgumentOutOfRangeException>();
            clearPastEnd.Should().Throw<ArgumentOutOfRangeException>();
            navigatePastEnd.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ToJson_IsDeterministicSortedReadableUtf8AndLfOnly()
        {
            var document = TlkDocument.Parse("""{ "language": 0, "entries": [] }""");
            document.SetText(9, "Line one\nLine two");
            document.SetText(3, "Café");

            var json = document.ToJson();

            json.Should().Be(
                "{\n" +
                "  \"language\": 0,\n" +
                "  \"entries\": [\n" +
                "    {\n" +
                "      \"id\": 3,\n" +
                "      \"text\": \"Café\"\n" +
                "    },\n" +
                "    {\n" +
                "      \"id\": 9,\n" +
                "      \"text\": \"Line one\\nLine two\"\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");
            json.Should().NotContain("\r");

            var reparsed = TlkDocument.Parse(json);
            reparsed.Entries.Should().Equal(document.Entries);
        }

        [Test]
        public void BlankNavigation_WrapsBeforeAppendingAndNeverReturnsReferencedRows()
        {
            using var directory = TemporaryTwoDaDirectory.Create(
                """
                2DA V2.0

                    Name        Description
                zero Filled      16777217
                one  "Quoted"    16777219
                """);
            var references = TlkReferenceIndex.Build(directory.Path);
            var document = TlkDocument.Parse(
                """{ "language": 0, "entries": [ { "id": 0, "text": "zero" }, { "id": 2, "text": "two" } ] }""");

            document.FindFirstAvailableBlank(references).Should().Be(4,
                "rows 0 and 2 are populated while rows 1 and 3 are referenced");
            document.FindNextAvailableBlank(2, references).Should().Be(4);

            document.Clear(2);
            document.FindNextAvailableBlank(3, references).Should().Be(2,
                "next-blank navigation wraps through safe gaps before appending");
        }

        private sealed class TemporaryTwoDaDirectory : IDisposable
        {
            private TemporaryTwoDaDirectory(string path) => Path = path;

            public string Path { get; }

            public static TemporaryTwoDaDirectory Create(string twoDa)
            {
                var path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);
                File.WriteAllText(System.IO.Path.Combine(path, "sample.2da"), twoDa);
                return new TemporaryTwoDaDirectory(path);
            }

            public void Dispose() => Directory.Delete(Path, recursive: true);
        }
    }
}
