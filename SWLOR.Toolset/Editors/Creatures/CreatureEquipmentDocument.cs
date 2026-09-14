using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One linked item blueprint participating in a creature document.</summary>
    public sealed class CreatureEquipmentDocument : IDisposable
    {
        private byte[] _savedBytes;

        public string ResRef { get; }
        public bool IsNew { get; private set; }
        public DocumentSession Session { get; }
        public ItemValueStore Store { get; }

        /// <summary>
        /// Whether this linked document differs from the bytes it had when opened or last saved.
        /// Linked-item mutations participate in the creature's undo stack rather than this session's,
        /// so <see cref="DocumentSession.UndoStack"/> cannot answer this question for them.
        /// </summary>
        public bool HasUnsavedChanges => !Session.ToBytes().AsSpan().SequenceEqual(_savedBytes);

        public CreatureEquipmentDocument(string resRef, bool isNew, DocumentSession session)
        {
            ResRef = resRef;
            IsNew = isNew;
            Session = session;
            Store = new ItemValueStore(session.Document.Root);
            _savedBytes = session.ToBytes();
        }

        public void MarkSaved(byte[] savedBytes)
        {
            ArgumentNullException.ThrowIfNull(savedBytes);
            IsNew = false;
            _savedBytes = savedBytes.ToArray();
        }

        public void Dispose() => Session.Dispose();
    }
}
