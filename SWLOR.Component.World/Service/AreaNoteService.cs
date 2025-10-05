using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.UI.Service;
using SWLOR.Component.World.Entity;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.World.Service
{
    public class AreaNoteService : IAreaNoteService
    {
        private readonly IAreaNoteRepository _areaNoteRepository;

        public AreaNoteService(IAreaNoteRepository areaNoteRepository)
        {
            _areaNoteRepository = areaNoteRepository;
        }

        /// <summary>
        /// Displays area notes for the specified area to the specified player.
        /// </summary>
        /// <param name="player">The player to display notes to</param>
        /// <param name="area">The area to get notes for</param>
        public void DisplayAreaNotes(uint player, uint area)
        {
            var notes = _areaNoteRepository.GetByAreaResref(GetResRef(area))
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
