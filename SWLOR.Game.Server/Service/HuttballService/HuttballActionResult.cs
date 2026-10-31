using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// What happened when a Huttball command ran: whether it was allowed, why not, the play-by-play to announce,
    /// the squares a moving player walked through, and the moments the world should dramatize.
    /// </summary>
    public class HuttballActionResult
    {
        public bool Succeeded { get; private set; } = true;
        public string Error { get; private set; } = string.Empty;
        public List<string> Messages { get; } = new();
        public List<HuttballSquare> Path { get; } = new();

        /// <summary>
        /// True when a block was tied, so both players bounce off each other.
        /// </summary>
        public bool Rebounded { get; set; }

        /// <summary>
        /// The player who scored, when the command ended in a touchdown.
        /// </summary>
        public HuttballPlayer Scorer { get; set; }

        public static HuttballActionResult Fail(string error)
        {
            return new HuttballActionResult
            {
                Succeeded = false,
                Error = error,
            };
        }
    }
}
