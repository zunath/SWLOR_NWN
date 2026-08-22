using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP1.5: UndoStack dirty tracking, MarkSaved semantics, and redo-tail clearing/saved-marker
    /// invalidation when a new edit is pushed after undoing.
    /// </summary>
    public class EditingTestsUndoStack
    {
        [Test]
        public void FreshSession_IsNotDirty()
        {
            var (document, session) = OpenSample();
            using var _ = session;

            session.UndoStack.IsDirty.Should().BeFalse("a session with no edits yet must not be dirty");
        }

        [Test]
        public void AfterEdit_IsDirty_AndMarkSavedClearsIt()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.MarkSaved();
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void UndoingBackToSavedPosition_IsNotDirty()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.MarkSaved();

            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.IsDirty.Should().BeTrue();

            session.UndoStack.Undo();
            session.UndoStack.IsDirty.Should().BeFalse("undoing edit B returns exactly to the saved position");

            session.UndoStack.Redo();
            session.UndoStack.IsDirty.Should().BeTrue("redoing edit B leaves the saved position again");
        }

        [Test]
        public void RevertAfterUndoingPastSavedPosition_RedoesToTheSavedState()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);
            session.UndoStack.MarkSaved();
            var savedValue = field.GetInteger();

            session.UndoStack.Undo();
            session.UndoStack.Undo();
            session.UndoStack.Position.Should().Be(0);

            session.RevertToSaved();

            field.GetInteger().Should().Be(savedValue);
            session.UndoStack.Position.Should().Be(2);
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void PushingNewEditAfterUndo_ClearsTheRedoTail()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.Undo();
            session.UndoStack.CanRedo.Should().BeTrue();

            using (session.Begin("edit C"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.CanRedo.Should().BeFalse("committing a new transaction after undo must discard the old redo tail");
            session.UndoStack.Entries.Should().HaveCount(2, "edit B must have been replaced by edit C");
        }

        [Test]
        public void SavedMarker_BecomesUnreachable_StaysDirtyEvenAtSamePositionNumber()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.MarkSaved(); // saved at position 2
            session.UndoStack.Position.Should().Be(2);

            session.UndoStack.Undo();
            session.UndoStack.Undo();
            session.UndoStack.Position.Should().Be(0);

            // The saved state (position 2) is now unreachable: pushing a new edit here discards
            // both original entries, and any future position 2 is a different document state.
            using (session.Begin("edit D"))
                field.SetInteger(field.GetInteger() + 1);
            using (session.Begin("edit E"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.Position.Should().Be(2, "position coincidentally matches the old saved position number");
            session.UndoStack.IsDirty.Should().BeTrue(
                "the saved marker must have been invalidated, not merely compared by position number");
        }

        [Test]
        public void MarkSaved_AfterInvalidation_EstablishesANewCleanBaseline()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            session.UndoStack.MarkSaved();
            session.UndoStack.Undo();

            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);

            session.UndoStack.IsDirty.Should().BeTrue();
            session.UndoStack.MarkSaved();
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        /// <summary>
        /// An area is two documents with two stacks. An edit to one has to invalidate the other's redo
        /// side, which <see cref="UndoStack.Push"/> cannot do because it only sees its own stack - so
        /// undoing an .are edit and then making a .git edit left the .are's redo entry live, and Ctrl+Y
        /// replayed the abandoned edit on top of the newer one.
        /// </summary>
        [Test]
        public void DiscardRedo_DropsTheRedoTailButKeepsUndoHistory()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);
            session.UndoStack.Undo();

            session.UndoStack.CanRedo.Should().BeTrue("precondition: there is something to redo");

            session.UndoStack.DiscardRedo();

            session.UndoStack.CanRedo.Should().BeFalse();
            session.UndoStack.CanUndo.Should().BeTrue("the undo history must survive");
            session.UndoStack.Position.Should().Be(1);
        }

        [Test]
        public void DiscardRedo_WithNothingAhead_ChangesNothing()
        {
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            session.UndoStack.MarkSaved();

            session.UndoStack.DiscardRedo();

            session.UndoStack.Position.Should().Be(1);
            session.UndoStack.IsDirty.Should().BeFalse("a no-op must not disturb the saved baseline");
        }

        [Test]
        public void DiscardRedo_InvalidatesASavedBaselineInsideTheDiscardedTail()
        {
            // Matches what Push does: a baseline that lived in the tail can no longer be returned to.
            var (document, session) = OpenSample();
            using var _ = session;
            var field = CorpusFiles.FindFirstMutableInteger(document.Root)!;

            using (session.Begin("edit A"))
                field.SetInteger(field.GetInteger() + 1);
            using (session.Begin("edit B"))
                field.SetInteger(field.GetInteger() + 1);
            session.UndoStack.MarkSaved();
            session.UndoStack.Undo();

            session.UndoStack.DiscardRedo();

            session.UndoStack.IsDirty.Should().BeTrue(
                "the saved position was in the tail that has just been thrown away");
        }

        private static (JsonGffDocument document, DocumentSession session) OpenSample()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            return (document, new DocumentSession(path, document));
        }
    }
}
