using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP1.5: transaction grouping (many edits collapse into one undo step) and the ambient
    /// guard that forbids mutating a session-attached document outside a transaction while
    /// leaving unattached documents (plain parse/mutate, as every other test in this suite does)
    /// completely unrestricted.
    /// </summary>
    public class EditingTestsTransactions
    {
        [Test]
        public void MultipleEditsInOneTransaction_CollapseToOneUndoStep()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);

            var fields = CollectMutableIntegers(document.Root, 2);
            fields.Count.Should().BeGreaterOrEqualTo(2, "the sample file should have at least two mutable integer fields");

            using (session.Begin("bump two fields"))
            {
                fields[0].SetInteger(fields[0].GetInteger() + 1);
                fields[1].SetInteger(fields[1].GetInteger() + 1);
            }

            session.UndoStack.Entries.Should().HaveCount(1, "both edits must collapse into a single undo step");
            session.UndoStack.CanUndo.Should().BeTrue();

            var beforeUndo1 = fields[0].GetInteger();
            var beforeUndo2 = fields[1].GetInteger();

            session.UndoStack.Undo();

            fields[0].GetInteger().Should().Be(beforeUndo1 - 1, "undoing the single grouped step must revert both edits");
            fields[1].GetInteger().Should().Be(beforeUndo2 - 1);
        }

        [Test]
        public void EmptyTransaction_DoesNotPushAnUndoStep()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);

            using (session.Begin("no-op"))
            {
                // Intentionally no mutations.
            }

            session.UndoStack.Entries.Should().BeEmpty();
            session.UndoStack.CanUndo.Should().BeFalse();
        }

        [Test]
        public void ExecuteCoalesced_GroupsDeferredWorkWithItsOriginatingEdit()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);
            var fields = CollectMutableIntegers(document.Root, 2);
            var firstOriginal = fields[0].GetInteger();
            var secondOriginal = fields[1].GetInteger();

            session.Execute("change model", () => fields[0].SetInteger(firstOriginal + 1));
            var origin = session.UndoStack.CurrentAppliedEntry;
            origin.Should().NotBeNull();

            session.ExecuteCoalesced(
                    origin!,
                    "carry generated tint variables",
                    () => fields[1].SetInteger(secondOriginal + 1))
                .Should().BeTrue();

            session.UndoStack.Entries.Should().ContainSingle();
            session.UndoStack.Entries[0].Describe().Should().Be("change model");

            session.Undo();
            fields[0].GetInteger().Should().Be(firstOriginal);
            fields[1].GetInteger().Should().Be(secondOriginal);

            session.Redo();
            fields[0].GetInteger().Should().Be(firstOriginal + 1);
            fields[1].GetInteger().Should().Be(secondOriginal + 1);
        }

        [Test]
        public void ExecuteCoalesced_PreservesRedoHistoryAfterLaterEditWasUndone()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);
            var fields = CollectMutableIntegers(document.Root, 2);
            var firstOriginal = fields[0].GetInteger();
            var secondOriginal = fields[1].GetInteger();

            session.Execute("change model", () => fields[0].SetInteger(firstOriginal + 1));
            var origin = session.UndoStack.CurrentAppliedEntry;
            origin.Should().NotBeNull();
            session.Execute("unrelated edit", () => fields[1].SetInteger(secondOriginal + 1));
            session.Undo();

            session.ExecuteCoalesced(
                    origin!,
                    "carry generated tint variables",
                    () => fields[0].SetInteger(firstOriginal + 2))
                .Should().BeTrue();

            session.UndoStack.Entries.Should().HaveCount(2);
            session.UndoStack.Position.Should().Be(1);
            session.UndoStack.CanRedo.Should().BeTrue(
                "a deferred continuation must not branch away an unrelated redo entry");
            fields[0].GetInteger().Should().Be(firstOriginal + 2);
            fields[1].GetInteger().Should().Be(secondOriginal);

            session.Redo();
            fields[1].GetInteger().Should().Be(secondOriginal + 1);
            session.Undo();
            session.Undo();
            fields[0].GetInteger().Should().Be(firstOriginal);
            fields[1].GetInteger().Should().Be(secondOriginal);
        }

        [Test]
        public void Execute_WhenMutationThrows_RollsBackCapturedEditsAndLeavesHistoryClean()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);

            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;
            var original = field.GetInteger();

            var act = () => session.Execute("failing multi-field edit", () =>
            {
                field.SetInteger(original + 1);
                throw new FormatException("second field is malformed");
            });

            act.Should().Throw<FormatException>();
            field.GetInteger().Should().Be(original, "the mutation before the failure must be reverted");
            session.UndoStack.Entries.Should().BeEmpty();
            session.UndoStack.IsDirty.Should().BeFalse();
            EditScope.IsTransactionOpen.Should().BeFalse();
        }

        [Test]
        public void MutatingGuardedDocumentOutsideTransaction_Throws()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);

            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            var act = () => field.SetInteger(field.GetInteger() + 1);

            act.Should().Throw<InvalidOperationException>(
                "a document attached to an undo stack must not be mutated outside a transaction");
        }

        [Test]
        public void MutatingUnattachedDocument_NeverThrows()
        {
            // No DocumentSession/UndoStack ever created for this document: plain parse-and-mutate
            // (as every non-Editing test in this suite already does) must remain unrestricted.
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));

            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;
            var act = () => field.SetInteger(field.GetInteger() + 1);

            act.Should().NotThrow();
        }

        [Test]
        public void NestedTransaction_Throws()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            using var session = new DocumentSession(path, document);

            using var outer = session.Begin("outer");
            var act = () => session.Begin("inner");

            act.Should().Throw<InvalidOperationException>("nested transactions are not supported");
        }

        [Test]
        public void DisposingSession_LiftsTheGuard()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            var session = new DocumentSession(path, document);
            session.Dispose();

            var act = () => field.SetInteger(field.GetInteger() + 1);
            act.Should().NotThrow("disposing the session must release its guard on the ambient EditScope");
        }

        private static List<JsonGffField> CollectMutableIntegers(JsonGffStruct target, int max)
        {
            var found = new List<JsonGffField>();
            CollectMutableIntegers(target, found, max);
            return found;
        }

        private static void CollectMutableIntegers(JsonGffStruct target, List<JsonGffField> found, int max)
        {
            foreach (var (_, field) in target.Entries)
            {
                if (found.Count >= max)
                    return;

                switch (field.Type)
                {
                    case GffFieldType.Byte:
                    case GffFieldType.Word:
                    case GffFieldType.Short:
                    case GffFieldType.Dword:
                    case GffFieldType.Int:
                        found.Add(field);
                        break;
                    case GffFieldType.Struct:
                        CollectMutableIntegers(field.Struct!, found, max);
                        break;
                    case GffFieldType.List:
                        foreach (var element in field.Elements!)
                        {
                            CollectMutableIntegers(element, found, max);
                            if (found.Count >= max)
                                return;
                        }

                        break;
                }
            }
        }
    }
}
