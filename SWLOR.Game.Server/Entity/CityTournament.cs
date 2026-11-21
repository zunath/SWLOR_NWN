using System.Collections.Generic;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A tournament a city holds at its Dueling Ring (Player City Revamp, "Conflict and PvP"). The prize and the entry fees
    /// sit in the city's <see cref="WorldProperty.Escrow"/> under this record's id from the moment they are paid in. The
    /// record is deleted once the tournament has been paid out or refunded; the Gazette keeps the story. A new type, so no
    /// migration.
    /// </summary>
    public class CityTournament : EntityBase
    {
        public CityTournament()
        {
            CityId = string.Empty;
            RingPropertyId = string.Empty;
            PostedByPlayerId = string.Empty;
            OfficialPlayerId = string.Empty;
            ChampionPlayerId = string.Empty;
            Entrants = new List<TournamentEntrant>();
            Matches = new List<TournamentMatch>();
            BarredPlayerIds = new List<string>();
        }

        /// <summary>The property id of the city (or station) that holds the tournament.</summary>
        [Indexed]
        public string CityId { get; set; }

        /// <summary>The property id of the Dueling Ring structure the tournament is held at.</summary>
        [Indexed]
        public string RingPropertyId { get; set; }

        [Indexed]
        public TournamentState State { get; set; }

        /// <summary>Set when the tournament starts to settle, so a restart finishes the same settlement.</summary>
        public TournamentOutcome Outcome { get; set; }

        public string PostedByPlayerId { get; set; }

        /// <summary>The official who began the tournament, or empty before it begins.</summary>
        public string OfficialPlayerId { get; set; }

        public DateTime PostedUtc { get; set; }

        /// <summary>When sign-ups close and the tournament can begin.</summary>
        public DateTime StartsUtc { get; set; }

        /// <summary>The credits the winner is paid, held out of the city's ordinary treasury.</summary>
        public long Prize { get; set; }

        /// <summary>The credits each entrant pays to sign up.</summary>
        public long EntryFee { get; set; }

        public List<TournamentEntrant> Entrants { get; set; }

        /// <summary>The bracket, empty until the tournament begins.</summary>
        public List<TournamentMatch> Matches { get; set; }

        /// <summary>Players who interrupted a match. They are barred from the ring until the tournament ends.</summary>
        public List<string> BarredPlayerIds { get; set; }

        /// <summary>The player id of the winner of the final, or empty.</summary>
        public string ChampionPlayerId { get; set; }
    }
}
