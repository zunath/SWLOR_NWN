using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Incubation field notes are key items that document how to produce a mutated beast.
    /// There is one note per mutation target beast; the note aggregates every incubation
    /// method that yields that target. Notes are always discoverable by performing the
    /// mutation, and may additionally be sold or dropped depending on their acquisition type.
    /// The logic and state live in <see cref="IIncubationFieldNoteService"/>; every member here forwards to the
    /// container's instance, and the event handler stays here so its name in the handler order manifest does not change.
    /// </summary>
    public static class IncubationFieldNote
    {
        private static IIncubationFieldNoteService Instance => FacadeAccessor.Resolve<IIncubationFieldNoteService>();

        /// <summary>
        /// Populates the hand-declared field note registry. Idempotent so it is safe to call
        /// from both the module boot event and from tests without a running engine.
        /// </summary>
        public static void EnsureRegistered()
        {
            Instance.EnsureRegistered();
        }

        /// <summary>
        /// When the module caches, inject each note's canonical name and full requirement
        /// description from the live mutation configuration. Runs after beasts and key items
        /// have both been cached (OnModuleCacheBefore).
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void LoadDisplayText()
        {
            Instance.LoadDisplayText();
        }

        /// <summary>
        /// Grants the field note for a beast a player just produced via mutation, revealing all
        /// of that beast's incubation methods. No-op if the produced beast has no note or the
        /// player already owns it.
        /// </summary>
        public static void GrantDiscoveredNote(uint player, BeastType producedBeast)
        {
            Instance.GrantDiscoveredNote(player, producedBeast);
        }

        /// <summary>
        /// All registered field notes. Populates the registry on first access.
        /// </summary>
        public static IReadOnlyCollection<FieldNoteDetail> GetAllNotes()
        {
            return Instance.GetAllNotes();
        }

        public static bool TryGetNoteForTarget(BeastType target, out FieldNoteDetail detail)
        {
            return Instance.TryGetNoteForTarget(target, out detail);
        }

        public static bool TryGetNoteForKeyItem(KeyItemType keyItem, out FieldNoteDetail detail)
        {
            return Instance.TryGetNoteForKeyItem(keyItem, out detail);
        }
    }
}
