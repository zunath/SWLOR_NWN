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

        /// <param name="cdKey">The CD key the record is for.</param>
        public VoterRewardLedger(string cdKey)
        {
            Id = KeyFor(cdKey);
        }

        /// <summary>
        /// The DB cache holds one entity per id for the frame, whatever its type, so the id is prefixed: the bare CD key
        /// is also the id of the account.
        /// </summary>
        public static string KeyFor(string cdKey) => "voter-reward:" + cdKey;

        public DateTime LastRewardUtc { get; set; }
    }
}
