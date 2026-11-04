namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// When a CD key last earned the election voter reward, across all cities. The entry is written with the first
    /// vote that pays, so changing or withdrawing a vote never pays again. A new type, so no migration.
    /// </summary>
    public class VoterRewardLedger : EntityBase
    {
        public VoterRewardLedger()
        {
        }

        /// <param name="cdKey">The CD key, which is also the record id.</param>
        public VoterRewardLedger(string cdKey)
        {
            Id = cdKey;
        }

        public DateTime LastRewardUtc { get; set; }
    }
}
