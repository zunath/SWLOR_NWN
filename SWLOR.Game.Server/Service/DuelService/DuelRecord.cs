namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// One duel between two players at a Dueling Ring. It lives in memory only, so a server restart voids it.
    /// <see cref="DuelRules"/> decides what happens to it; <see cref="Duels"/> keeps it and acts on it.
    /// </summary>
    public sealed class DuelRecord
    {
        public DuelRecord(uint challenger, uint target, uint ring, string cityId, DateTime startedUtc)
        {
            Challenger = challenger;
            Target = target;
            Ring = ring;
            CityId = cityId ?? string.Empty;
            StartedUtc = startedUtc;
            State = DuelState.Active;
            ChallengerLabel = string.Empty;
            TargetLabel = string.Empty;
        }

        public uint Challenger { get; }

        public uint Target { get; }

        /// <summary>The ring terminal the duel was fought at.</summary>
        public uint Ring { get; }

        /// <summary>The property id of the city or station that owns the ring; its Gazette gets the result.</summary>
        public string CityId { get; }

        public DateTime StartedUtc { get; }

        public DuelState State { get; set; }

        /// <summary>
        /// Until when both duelists stay protected after a third party voided the duel. It means nothing while the
        /// duel is Active, which protects the duelists on its own.
        /// </summary>
        public DateTime ProtectedUntilUtc { get; set; }

        /// <summary>When the first duelist fell, or null while neither has.</summary>
        public DateTime? FirstFallUtc { get; set; }

        public bool ChallengerFell { get; set; }

        public bool TargetFell { get; set; }

        public bool ChallengerForfeited { get; set; }

        public bool TargetForfeited { get; set; }

        /// <summary>How each duelist appears to strangers, for the Gazette. Kept fresh while they are online.</summary>
        public string ChallengerLabel { get; set; }

        public string TargetLabel { get; set; }

        /// <summary>Whether each side had marked the other hostile before the duel, so it can be put back after it.</summary>
        public bool ChallengerWasHostile { get; set; }

        public bool TargetWasHostile { get; set; }

        public bool HostilityRestored { get; set; }

        /// <summary>The duelists have not yet been told the duel was voided.</summary>
        public bool InterruptionNoticePending { get; set; }
    }
}
