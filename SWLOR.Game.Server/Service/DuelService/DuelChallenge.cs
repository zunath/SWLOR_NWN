namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// A challenge waiting for its target to accept or decline. Held in memory.
    /// </summary>
    public sealed class DuelChallenge
    {
        public DuelChallenge(uint challenger, uint target, uint ring, DateTime expiresUtc)
        {
            Challenger = challenger;
            Target = target;
            Ring = ring;
            ExpiresUtc = expiresUtc;
        }

        public uint Challenger { get; }

        public uint Target { get; }

        public uint Ring { get; }

        public DateTime ExpiresUtc { get; }
    }
}
