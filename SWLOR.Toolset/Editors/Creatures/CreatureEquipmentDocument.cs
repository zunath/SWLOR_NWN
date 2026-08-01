using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>One linked item blueprint participating in a creature document.</summary>
    public sealed class CreatureEquipmentDocument : IDisposable
    {
        public string ResRef { get; }
        public bool IsNew { get; private set; }
        public DocumentSession Session { get; }
        public ItemValueStore Store { get; }

        public CreatureEquipmentDocument(string resRef, bool isNew, DocumentSession session)
        {
            ResRef = resRef;
            IsNew = isNew;
            Session = session;
            Store = new ItemValueStore(session.Document.Root);
        }

        public void MarkSaved() => IsNew = false;

        public void Dispose() => Session.Dispose();
    }
}
