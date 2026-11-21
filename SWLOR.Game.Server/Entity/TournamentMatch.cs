using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One match of a tournament bracket. The winner of the match in slot S of a round goes to slot S / 2 of the next
    /// round, as the first duelist when S is even and the second when S is odd.
    /// </summary>
    public class TournamentMatch
    {
        public TournamentMatch()
        {
            PlayerAId = string.Empty;
            PlayerBId = string.Empty;
            WinnerId = string.Empty;
            DecidedByPlayerId = string.Empty;
        }

        /// <summary>1 for the first round. The highest round holds the final.</summary>
        public int Round { get; set; }

        /// <summary>The match's place in its round, counted from 0.</summary>
        public int Slot { get; set; }

        /// <summary>The first duelist's player id, or empty while that place waits on an earlier match or stands empty.</summary>
        public string PlayerAId { get; set; }

        public string PlayerBId { get; set; }

        /// <summary>The winner's player id, or empty while the match is open or when nobody advanced.</summary>
        public string WinnerId { get; set; }

        public TournamentMatchState State { get; set; }

        public TournamentMatchResult Result { get; set; }

        /// <summary>When the match was called, or when its duelists last got a fresh five minutes.</summary>
        public DateTime? CalledUtc { get; set; }

        /// <summary>How many times someone else joined the fight and voided a duel of this match.</summary>
        public int Interruptions { get; set; }

        /// <summary>The official who picked the winner after the third interruption, or empty.</summary>
        public string DecidedByPlayerId { get; set; }
    }
}
