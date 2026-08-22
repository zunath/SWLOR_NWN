using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP1.5: list-typed field insert/remove/move, struct field add/remove, and cexolocstring
    /// entry add/remove, all round-tripped through undo back to byte-identical originals.
    /// </summary>
    public class EditingTestsLists
    {
        [Test]
        public void InsertElement_ThenUndo_RestoresOriginalBytesExactly()
        {
            var path = CorpusFiles.FindFileWithListOfSize("utc", 1, out var listFieldName);
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var listField = document.Root.Get(listFieldName);
            var startCount = listField.Elements!.Count;
            var newElement = JsonGffField.CreateStruct(0).Struct!;

            using (session.Begin("insert element"))
                listField.InsertElement(0, newElement);

            listField.Elements!.Count.Should().Be(startCount + 1);
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();

            listField.Elements!.Count.Should().Be(startCount);
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the insert must restore the original element count and bytes");
        }

        [Test]
        public void RemoveElement_ThenUndo_RestoresOriginalBytesExactly()
        {
            var path = CorpusFiles.FindFileWithListOfSize("utc", 1, out var listFieldName);
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var listField = document.Root.Get(listFieldName);
            var startCount = listField.Elements!.Count;

            using (session.Begin("remove element"))
                listField.RemoveElementAt(0);

            listField.Elements!.Count.Should().Be(startCount - 1);

            session.UndoStack.Undo();

            listField.Elements!.Count.Should().Be(startCount);
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the removal must restore the exact original element and its position");
        }

        [Test]
        public void MoveElement_ThenUndo_RestoresOriginalOrderAndBytesExactly()
        {
            var path = CorpusFiles.FindFileWithListOfSize("utc", 2, out var listFieldName);
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var listField = document.Root.Get(listFieldName);
            var firstElement = listField.Elements![0];
            var lastIndex = listField.Elements.Count - 1;

            using (session.Begin("move element"))
                listField.MoveElement(0, lastIndex);

            listField.Elements[lastIndex].Should().BeSameAs(firstElement);
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();

            listField.Elements[0].Should().BeSameAs(firstElement);
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the move must restore the exact original order and bytes");
        }

        [Test]
        public void RedoAfterListEdits_ReproducesPostEditBytesExactly()
        {
            var path = CorpusFiles.FindFileWithListOfSize("utc", 2, out var listFieldName);
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var listField = document.Root.Get(listFieldName);

            using (session.Begin("insert then move"))
            {
                var newElement = JsonGffField.CreateStruct(0).Struct!;
                listField.InsertElement(0, newElement);
                listField.MoveElement(0, listField.Elements!.Count - 1);
            }

            var mutated = document.ToBytes();

            session.UndoStack.Undo();
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue();

            session.UndoStack.Redo();
            document.ToBytes().AsSpan().SequenceEqual(mutated).Should().BeTrue(
                "redo must reproduce the exact grouped post-edit bytes");
        }

        [Test]
        public void AddThenRemoveField_ThenUndo_RestoresOriginalBytesExactly()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            document.Root.Contains("ToolsetTestField").Should().BeFalse();

            using (session.Begin("add field"))
            {
                document.Root.Add("ToolsetTestField",
                    JsonGffField.CreateScalar(GffFieldType.Int, "42"u8.ToArray()));
            }

            document.Root.Contains("ToolsetTestField").Should().BeTrue();
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();

            document.Root.Contains("ToolsetTestField").Should().BeFalse();
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the add must remove the field and restore the exact original bytes");

            session.UndoStack.Redo();
            document.Root.Contains("ToolsetTestField").Should().BeTrue();

            using (session.Begin("remove field"))
                document.Root.Remove("ToolsetTestField");

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "removing the re-added field must restore the exact original bytes again");
        }

        [Test]
        public void LocStringEntryAddAndRemove_ThenUndo_RestoresOriginalBytesExactly()
        {
            // "Description" is a cexolocstring field present on every UTC blueprint; using a
            // file where it happens to hold zero entries keeps the before/after counts simple.
            var path = CorpusFiles.FindFileWithEmptyLocStringField("utc", "Description");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var locField = document.Root.Get("Description");
            var startCount = locField.LocStringEntries!.Count;
            var entry = new LocStringEntry("5", JsonStringCodec.Encode("hello"));

            using (session.Begin("add locstring entry"))
                locField.AddLocStringEntry(entry);

            locField.LocStringEntries!.Count.Should().Be(startCount + 1);

            using (session.Begin("remove locstring entry"))
                locField.RemoveLocStringEntry("5");

            locField.LocStringEntries!.Count.Should().Be(startCount);

            session.UndoStack.Undo(); // undo remove
            locField.LocStringEntries!.Count.Should().Be(startCount + 1);

            session.UndoStack.Undo(); // undo add
            locField.LocStringEntries!.Count.Should().Be(startCount);

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing both locstring entry edits must restore the exact original bytes");
        }

        [Test]
        public void AuthoringInlineLocStringTextClearsStrRefAndUndoRestoresBothExactly()
        {
            var original = System.Text.Encoding.UTF8.GetBytes("""
            {
              "__data_type": "UTI ",
              "LocalizedName": {
                "id": 12843,
                "type": "cexolocstring",
                "value": {
                  "0": "TLK-backed name"
                }
              }
            }
            """);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession("locstring-test.uti.json", document);
            var name = new UtiDocument(document).LocalizedName;

            using (session.Begin("author localized name"))
                name.Text = "Authored inline";

            name.StrRef.Should().BeNull(
                "inline text must not retain a TLK reference that can override it in game");
            name.Text.Should().Be("Authored inline");
            var authored = document.ToBytes();

            session.UndoStack.Undo();

            name.StrRef.Should().Be(12843);
            name.Text.Should().Be("TLK-backed name");
            document.ToBytes().Should().Equal(original);

            session.UndoStack.Redo();

            name.StrRef.Should().BeNull();
            name.Text.Should().Be("Authored inline");
            document.ToBytes().Should().Equal(authored);
        }
    }
}
