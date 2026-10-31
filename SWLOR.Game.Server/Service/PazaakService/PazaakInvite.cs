namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakInvite
    {
        public PazaakInvite(uint challenger, DateTime expiresUtc)
        {
            Challenger = challenger;
            ExpiresUtc = expiresUtc;
        }

        public uint Challenger { get; }
        public DateTime ExpiresUtc { get; }
    }
}
