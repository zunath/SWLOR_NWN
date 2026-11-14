using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Incubation field notes are key items that document how to produce a mutated beast. There is one note per
    /// mutation target beast; the note aggregates every incubation method that yields that target. Notes are always
    /// discoverable by performing the mutation, and may additionally be sold or dropped depending on their acquisition
    /// type. Static callers reach the container's instance through
    /// <c>GameServices.IncubationFieldNote</c>; the event handler is on <see cref="IncubationFieldNoteService"/>.
    /// </summary>
    public interface IIncubationFieldNoteService
    {
        /// <summary>
        /// Populates the hand-declared field note registry. Idempotent so it is safe to call
        /// from both the module boot event and from tests without a running engine.
        /// </summary>
        void EnsureRegistered();

        /// <summary>
        /// When the module caches, inject each note's canonical name and full requirement
        /// description from the live mutation configuration. Runs after beasts and key items
        /// have both been cached (OnModuleCacheBefore).
        /// </summary>
        void LoadDisplayText();

        /// <summary>
        /// Grants the field note for a beast a player just produced via mutation, revealing all
        /// of that beast's incubation methods. No-op if the produced beast has no note or the
        /// player already owns it.
        /// </summary>
        void GrantDiscoveredNote(uint player, BeastType producedBeast);

        /// <summary>
        /// All registered field notes. Populates the registry on first access.
        /// </summary>
        IReadOnlyCollection<FieldNoteDetail> GetAllNotes();

        bool TryGetNoteForTarget(BeastType target, out FieldNoteDetail detail);

        bool TryGetNoteForKeyItem(KeyItemType keyItem, out FieldNoteDetail detail);
    }
}
