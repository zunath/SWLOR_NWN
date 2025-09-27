using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.UI.Service;
using SWLOR.Component.World.Entity;

namespace SWLOR.Component.World.Service
{
    public class AreaNoteService : IAreaNoteService
    {
        private readonly IDatabaseService _db;

        public AreaNoteService(IDatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// Displays area notes for the specified area to the specified player.
        /// </summary>
        /// <param name="player">The player to display notes to</param>
        /// <param name="area">The area to get notes for</param>
        public void DisplayAreaNotes(uint player, uint area)
        {
            var query = new DBQuery<AreaNote>()
                .AddFieldSearch(nameof(AreaNote.AreaResref), GetResRef(area), false)
                .OrderBy(nameof(AreaNote.AreaResref));
            var notes = _db.Search(query)
                .ToList();

            if (notes.Count > 0)
            {
                var prefix = GetName(area) + ": ";
                var message = string.Empty;
                foreach (var note in notes)
                {
                    message += note.PublicText;
                }

                if (!string.IsNullOrWhiteSpace(message.Trim()))
                {
                    SendMessageToPC(player, ColorToken.Purple(prefix + message));
                }
            }
        }
    }
}
