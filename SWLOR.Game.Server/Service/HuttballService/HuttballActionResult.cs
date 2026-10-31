using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// What happened when a Huttball command ran: whether it was allowed, why not, the play-by-play to announce,
    /// and the squares a moving player walked through.
    /// </summary>
    public class HuttballActionResult
    {
        public bool Succeeded { get; private set; } = true;
        public string Error { get; private set; } = string.Empty;
        public List<string> Messages { get; } = new();
        public List<HuttballSquare> Path { get; } = new();

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
